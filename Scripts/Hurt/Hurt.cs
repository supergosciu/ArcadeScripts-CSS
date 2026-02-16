using ArcadeScripts;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using static ArcadeScripts.Random;

public class Hurt : ScriptBase
{
    public Hurt(CLogicScript owner) : base(owner)
    {
        Functions.Add("HurtActivator", new ScriptFunction<CEntityInstance, string>(HurtActivator));
        Functions.Add("SetAttacker", new ScriptFunction<CEntityInstance, string>(SetAttacker));

        Setup();
    }
    
    private const string WeaponNuclear = "weapon_nuclear";
    private const string WeaponPoison = "weapon_poison";
    private const string WeaponLightning = "weapon_lightning";
    private const string WeaponWreckingball = "weapon_wreckingball";

    public const string HurterOne = "1";
    public const string HurterTwo = "2";
    public const string HurterThree = "3";
    public const string HurterFour = "4";
    public const string HurterFive = "5";
    public const string HurterSix = "6";

    private Dictionary<string, CPointHurt> Hurters = [];
    private Dictionary<string, CCSPlayerPawn> Attackers = [];

    private void Setup()
    {
        if (!Hurters.ContainsKey(HurterOne))
        {
            CPointHurt hurter = Utilities.CreateEntityByName<CPointHurt>("point_hurt")!;
            using (CEntityKeyValues kv = new())
            {
                kv.SetInt("damagetype", 16);
                kv.SetInt("damage", 500);
                hurter.DispatchSpawn(kv);
            }
            hurter.Entity!.DesignerName = WeaponNuclear;
            Hurters[HurterOne] = hurter;
        }
        if (!Hurters.ContainsKey(HurterTwo))
        {
            CPointHurt hurter = Utilities.CreateEntityByName<CPointHurt>("point_hurt")!;
            using (CEntityKeyValues kv = new())
            {
                kv.SetInt("damagetype", 16);
                kv.SetInt("damage", 9);
                hurter.DispatchSpawn(kv);
            }
            hurter.Entity!.DesignerName = WeaponNuclear;
            Hurters[HurterTwo] = hurter;
        }
        if (!Hurters.ContainsKey(HurterThree))
        {
            CPointHurt hurter = Utilities.CreateEntityByName<CPointHurt>("point_hurt")!;
            using (CEntityKeyValues kv = new())
            {
                kv.SetInt("damagetype", 16);
                kv.SetInt("damage", 3);
                hurter.DispatchSpawn(kv);
            }
            hurter.Entity!.DesignerName = WeaponPoison;
            Hurters[HurterThree] = hurter;
        }
        if (!Hurters.ContainsKey(HurterFour))
        {
            CPointHurt hurter = Utilities.CreateEntityByName<CPointHurt>("point_hurt")!;
            using (CEntityKeyValues kv = new())
            {
                kv.SetInt("damagetype", 16);
                kv.SetInt("damage", 20);
                hurter.DispatchSpawn(kv);
            }
            hurter.Entity!.DesignerName = WeaponLightning;
            Hurters[HurterFour] = hurter;
        }
        if (!Hurters.ContainsKey(HurterFive))
        {
            CPointHurt hurter = Utilities.CreateEntityByName<CPointHurt>("point_hurt")!;
            using (CEntityKeyValues kv = new())
            {
                kv.SetInt("damagetype", 16);
                kv.SetInt("damage", 10);
                hurter.DispatchSpawn(kv);
            }
            hurter.Entity!.DesignerName = WeaponLightning;
            Hurters[HurterFive] = hurter;
        }
        if (!Hurters.ContainsKey(HurterSix))
        {
            CPointHurt hurter = Utilities.CreateEntityByName<CPointHurt>("point_hurt")!;
            using (CEntityKeyValues kv = new())
            {
                kv.SetInt("damagetype", 16);
                kv.SetInt("damage", 500);
                hurter.DispatchSpawn(kv);
            }
            hurter.Entity!.DesignerName = WeaponWreckingball;
            Hurters[HurterSix] = hurter;
        }
    }

    private CCSPlayerPawn? GetOtherPlayer(CCSPlayerPawn target)
    {
        CsTeam playerTeam = (CsTeam)target.TeamNum;
        List<CCSPlayerController> otherPlayers = Utilities.GetPlayers().Where(p => p.PawnIsAlive && p.Team != playerTeam && p.PlayerPawn.Value != target).ToList();

        if (otherPlayers.Count == 0) return null;

        int index = RandomInt(0, otherPlayers.Count - 1);
        return otherPlayers[index].PlayerPawn.Value;
    }

    public void HurtActivator(CEntityInstance? activator, string hurterKey)
    {
        if (activator == null) return;
        HurtPlayer(hurterKey, activator.As<CCSPlayerPawn>());
    }

    public void SetAttacker(CEntityInstance? activator, string hurterKey)
    {
        if (activator == null) return;
        Attackers[hurterKey] = activator.As<CCSPlayerPawn>();
    }

    public void HurtPlayer(string hurterKey, CCSPlayerPawn target)
    {
        CCSPlayerPawn? attacker;
        if (Attackers.TryGetValue(hurterKey, out CCSPlayerPawn? value)) attacker = value;
        else attacker = GetOtherPlayer(target);

        Hurters[hurterKey].Teleport(target.AbsOrigin);
        if (attacker != null)
        {
            Schema.SetSchemaValue(Hurters[hurterKey].Handle, "CBaseEntity", "m_hOwnerEntity", attacker.EntityHandle.Raw);
            Hurters[hurterKey].AddEntityIOEvent(inputName: "Hurt", activator: attacker);
        }
        else
        {
            Schema.SetSchemaValue(Hurters[hurterKey].Handle, "CBaseEntity", "m_hOwnerEntity", target.EntityHandle.Raw);
            Hurters[hurterKey].AddEntityIOEvent(inputName: "Hurt", activator: target);
        }
    }
}