using CounterStrikeSharp.API.Core;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

using static ArcadeScripts.Random;

namespace ArcadeScripts.Scripts;

public class CellRewards : ScriptBase
{
    public CellRewards(CLogicScript owner) : base(owner)
    {
        Functions.Add("Setup", new ScriptFunction(Setup));
        Functions.Add("UseTeleport", new ScriptFunction<CEntityInstance>(UseTeleport));
        Functions.Add("ReceiveGun", new ScriptFunction<CEntityInstance>(ReceiveGun));
        Functions.Add("DisableRewards", new ScriptFunction(DisableRewards));

        EntityNames = ["cell_vent_tp", "cell_vent_trigger", "cell_vent_glock_template", "cell_vent_p250_template", "cell_vent_hkp_template", "cell_vent_decoy_template"];
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent += OnEntitySpawned;
    }

    private const float OddsDoubleTeleportToArmory = 0.0625f; // 1/16
    private const float OddsSingleTeleporToArmory = 0.25f; // 1/4
    private const float OddsDoubleTeleportToMap = 0.0625f; // 1/16
    private const float OddsSingleTeleportToMap = 0.6875f; // 11/16
    private const float OddsDoubleGun = 0.0625f; // 1/16
    private const float OddsSingleGun = 0.626f; // 10/16

    private int TeleportsToArmoryRemaining = 0;
    private int TeleportsToMapRemaining = 0;
    private int GunsRemaining = 0;
    private int ArmorRemaining = 0;

    private List<CPointTemplate> GunTemplates = [];
    private List<CCSPlayerPawn> WinningPlayers = [];
    private List<CDecoyGrenade> TeleportedDecoys = [];
    private List<CTriggerTeleport> CellVentTPs = [];
    private List<CTriggerMultiple> CellVentTriggers = [];
    private CPointTemplate CellVentDecoyTemplate = null!;

    private Dictionary<CPointTemplate, string> GunTemplateToGunName = [];
    private Dictionary<string, CBasePlayerWeapon> GunNameToEntity = [];
    private Dictionary<string, CCSPlayerPawn> GunNameToWinner = [];

    private bool DEBUG = false;
    private void debugprint(string text)
    {
        if (!DEBUG) return;
        Console.WriteLine($"************* {text}");
    }

    private void OnAllEntitiesSpawned()
    {
        foreach (KeyValuePair<string, List<CEntityInstance>> kv in EntityList)
        {
            List<CEntityInstance> entities = kv.Value;
            string targetname = kv.Key;

            if (targetname == "cell_vent_tp")
            {
                for (int i = 0; i < entities.Count; i++) CellVentTPs.Add(entities[i].As<CTriggerTeleport>());
            }
            else if (targetname == "cell_vent_trigger")
            {
                for (int i = 0; i < entities.Count; i++) CellVentTriggers.Add(entities[i].As<CTriggerMultiple>());
            }
            else if (targetname == "cell_vent_glock_template")
            {
                for (int i = 0; i < entities.Count; i++)
                {
                    GunTemplates.Add(entities[i].As<CPointTemplate>());
                    GunTemplateToGunName[entities[i].As<CPointTemplate>()] = "cell_vent_glock";
                }
            }
            else if (targetname == "cell_vent_p250_template")
            {
                for (int i = 0; i < entities.Count; i++)
                {
                    GunTemplates.Add(entities[i].As<CPointTemplate>());
                    GunTemplateToGunName[entities[i].As<CPointTemplate>()] = "cell_vent_p250";
                }
            }
            else if (targetname == "cell_vent_hkp_template")
            {
                for (int i = 0; i < entities.Count; i++)
                {
                    GunTemplates.Add(entities[i].As<CPointTemplate>());
                    GunTemplateToGunName[entities[i].As<CPointTemplate>()] = "cell_vent_hkp";
                }
            }
            else if (targetname == "cell_vent_decoy_template")
            {
                CellVentDecoyTemplate = entities[0].As<CPointTemplate>();
            }
        }
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        string? targetname = entity.Entity?.Name;
        if (string.IsNullOrEmpty(targetname)) return;

        if (targetname == "cell_vent_glock")
        {
            GunNameToEntity["cell_vent_glock"] = entity.As<CBasePlayerWeapon>();
        }
        if (targetname == "cell_vent_p250")
        {
            GunNameToEntity["cell_vent_p250"] = entity.As<CBasePlayerWeapon>();
        }
        if (targetname == "cell_vent_hkp")
        {
            GunNameToEntity["cell_vent_hkp"] = entity.As<CBasePlayerWeapon>();
        }
        if (targetname.StartsWith("cell_vent_decoy"))
        {
            TeleportedDecoys.Add(entity.As<CDecoyGrenade>());
        }
    }

    public void Setup()
    {
        if (RandomFloat(0.0f, 1.0f) < OddsDoubleTeleportToArmory)
        {
            TeleportsToArmoryRemaining = 2;
        }
        else if (RandomFloat(0.0f, 1.0f) < OddsSingleTeleporToArmory)
        {
            TeleportsToArmoryRemaining = 1;
        }
        else
        {
            TeleportsToArmoryRemaining = 0;
        }

        if (RandomFloat(0.0f, 1.0f) < OddsDoubleTeleportToMap)
        {
            TeleportsToMapRemaining = 2;
        }
        else if (RandomFloat(0.0f, 1.0f) < OddsSingleTeleportToMap)
        {
            TeleportsToMapRemaining = 1;
        }
        else
        {
            TeleportsToMapRemaining = 0;
        }

        if (RandomFloat(0.0f, 1.0f) < OddsDoubleGun)
        {
            GunsRemaining = 2;
        }
        else if (RandomFloat(0.0f, 1.0f) < OddsSingleGun)
        {
            GunsRemaining = 1;
        }

        ArmorRemaining = 3;

        UpdateTeleportsAndTriggers();

        debugprint("After setup, there are " + TeleportsToArmoryRemaining + " tps to armory, " + TeleportsToMapRemaining + " tps to map, and " + GunsRemaining + " guns");
    }

    private void UpdateTeleportsAndTriggers()
    {
        if (TeleportsToArmoryRemaining > 0)
        {
            //Do nothing
        }
        else if (TeleportsToMapRemaining > 0)
        {
            foreach (CTriggerTeleport tp in CellVentTPs) tp.Target = "cell_vent_tpdest";
        }
        else
        {
            foreach (CTriggerTeleport tp in CellVentTPs) tp.AddEntityIOEvent(inputName: "Disable");
            //Small delay to prevent teleporter from getting gun
            foreach (CTriggerMultiple trigger in CellVentTriggers) trigger.AddEntityIOEvent(inputName: "Enable", delay: 0.1f);
        }
    }

    public void DisableRewards()
    {
        TeleportsToArmoryRemaining = 0;
        TeleportsToMapRemaining = 0;
        GunsRemaining = 0;
        foreach (CTriggerTeleport tp in CellVentTPs) tp.AddEntityIOEvent(inputName: "Disable");
        foreach (CTriggerMultiple trigger in CellVentTriggers) trigger.AddEntityIOEvent(inputName: "Disable");
    }

    public void UseTeleport(CEntityInstance? activator)
    {
        if (activator == null) return;

        CCSPlayerPawn pawn = activator.As<CCSPlayerPawn>();
        WinningPlayers.Add(pawn);
        if (TeleportsToArmoryRemaining > 0)
        {
            TeleportsToArmoryRemaining--;
        }
        else if (TeleportsToMapRemaining > 0)
        {
            TeleportsToMapRemaining--;
        }
        UpdateTeleportsAndTriggers();
    }

    public void ReceiveGun(CEntityInstance? activator)
    {
        if (activator == null) return;

        CCSPlayerPawn? pawn = activator.As<CCSPlayerPawn>();
        if (pawn == null || WinningPlayers.Contains(pawn)) return;
        WinningPlayers.Add(pawn);
        if (GunsRemaining == 0 || GunTemplates.Count == 0)
        {
            ReceiveDecoy(activator);
            if (ArmorRemaining > 0)
            {
                ReceiveArmor(activator);
                ArmorRemaining--;
            }
            return;
        }

        int template_index = RandomInt(0, GunTemplates.Count - 1);
        CPointTemplate template = GunTemplates[template_index];
        debugprint("Picked " + template + " (" + template_index + ")" + " which has gun name " + GunTemplateToGunName[template]);
        GunTemplates.RemoveAt(template_index);

        template.AddEntityIOEvent(inputName: "ForceSpawn");

        Timers.Add(new(0.05f, () => TeleportGun(GunTemplateToGunName[template])));
        GunNameToWinner[GunTemplateToGunName[template]] = pawn;
        GunsRemaining--;
    }

    private void TeleportGun(string gunName)
    {
        CCSPlayerPawn winner = GunNameToWinner[gunName];
        debugprint("In TeleportGun, gunName = " + gunName + " and winner = " + winner);
        CBasePlayerWeapon weapon = GunNameToEntity[gunName];
        debugprint("In TeleportGun, weapon = " + weapon);
        if (winner == null || !winner.IsValid || weapon == null) return;

        weapon.Teleport(winner.AbsOrigin);
    }

    private void ReceiveDecoy(CEntityInstance activator)
    {
        CellVentDecoyTemplate.AddEntityIOEvent(inputName: "ForceSpawn");
        Timers.Add(new(0.05f, () => TeleportDecoy(activator)));
    }

    private void ReceiveArmor(CEntityInstance activator)
    {
        CCSPlayerController? player = activator.As<CCSPlayerPawn>().OriginalController.Value;
        player?.GiveNamedItem("item_assaultsuit");
    }

    private void TeleportDecoy(CEntityInstance activator)
    {
        CDecoyGrenade? decoy = TeleportedDecoys.FirstOrDefault();
        if (decoy == null || !decoy.IsValid) return;

        TeleportedDecoys.Remove(decoy);
        decoy.Teleport(activator.As<CCSPlayerPawn>().AbsOrigin);
    }

    public override void Remove()
    {
        foreach (Timer timer in Timers) timer?.Kill();

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent -= OnEntitySpawned;
    }
}