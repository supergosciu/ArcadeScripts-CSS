using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace ArcadeScripts.Scripts;

public partial class StrafeJump : ScriptBase
{
    public StrafeJump(CLogicScript owner) : base(owner)
    {
        Functions.Add("Recall", new ScriptFunction(Recall));
        Functions.Add("AddToWinnersSetting", new ScriptFunction<int>(AddToWinnersSetting));
        Functions.Add("StopActivatorZ", new ScriptFunction<CEntityInstance>(StopActivatorZ));
        Functions.Add("AddToDamage", new ScriptFunction<int>(AddToDamage));
        Functions.Add("ResetWinners", new ScriptFunction(ResetWinners));
        Functions.Add("AddToWinners", new ScriptFunction(AddToWinners));

        EntityGroupNames[0] = "blob_teleport1";
		EntityGroupNames[1] = "blob_teleport2";
		EntityGroupNames[2] = "blob_teleport3";
		EntityGroupNames[3] = "blob_teleport4";
		EntityGroupNames[4] = "blob_teleport5";
		EntityGroupNames[5] = "sj_easy_win_teleport";
        EntityGroupNames[6] = "sj_hard_win_teleport";
        EntityGroupNames[12] = "sj_tp_dest";

        EntityNames = ["sj_easy_damage", "sj_medium_damage", "sj_hard_damage", "sj_fail_damage", "sj_damage_sign_model", "sj_winners_sign_model", "blob_teleport1", "blob_teleport2", "blob_teleport3", "blob_teleport4", "blob_teleport5", "sj_easy_win_teleport", "sj_hard_win_teleport", "sj_tp_dest"];

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
    }

    private static Dictionary<int, int> DAMAGE_TO_TEXTURE = new() { { 0, 0 }, { 5, 1 }, { 10, 2 }, { 15, 3 }, { 20, 4 }, { 25, 5 }, { 50, 6 }, { 75, 7 }, { 100, 8 } };

    private int Damage = 0;
    private int WinnersSettings = 4;
    private int Winners = 0;
    private const int MinDamage = 0;
    private const int MaxDamage = 20;
    private const int MinWinners = 1;
    private const int MaxWinners = 32;

    private List<CTriggerHurt> HurtTriggers = [];
    private CDynamicProp DamageSignModel = null!;
    private CDynamicProp WinnersSettingModel = null!;

    private static Vector StrafeJumpMin = new(1024.0f, -1312.0f, -16072.0f);
    private static Vector StrafeJumpMax = new(1536.0f, -800.0f, -511.0f);
    private static Vector StrafeJumpHardMin = new(1088.0f, -6720.0f, -16072.0f);
    private static Vector StrafeJumpHardMax = new(1600.0f, -6208.0f, 16000.0f);

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

            if (targetname == "sj_easy_damage" || targetname == "sj_medium_damage" || targetname == "sj_hard_damage" || targetname == "sj_fail_damage")
            {
                HurtTriggers.Add(entities[0].As<CTriggerHurt>());
            }
            else if (targetname == "sj_damage_sign_model")
            {
                DamageSignModel = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "sj_winners_sign_model")
            {
                WinnersSettingModel = entities[0].As<CDynamicProp>();
            }

            int index = Array.IndexOf(EntityGroupNames, targetname);
            if (index != -1)
            {
                EntityGroup[index] = entities[0].As<CBaseEntity>();
            }
        }
    }

    private void UpdateTeleports()
    {
        CBaseEntity[] teleportDesinationEntities = [EntityGroup[0], EntityGroup[1], EntityGroup[2]];
        CBaseEntity winnersDestination = EntityGroup[3];
        CBaseEntity losersDestination = EntityGroup[4];
        CBaseEntity teleportDestination = losersDestination;
        CTriggerTeleport easyWinTeleport = EntityGroup[5].As<CTriggerTeleport>();
        CTriggerTeleport hardWinTeleport = EntityGroup[6].As<CTriggerTeleport>();

        if (Winners < 3 && Winners < WinnersSettings)
		{
			debugprint("Setting teleportDestination to podium " + (Winners + 1));
			teleportDestination = teleportDesinationEntities[Winners];
		}
        else if (Winners < WinnersSettings)
        {
            debugprint("Setting teleportDestination to winners podium");
            teleportDestination = winnersDestination;
        }
        else
        {
            debugprint("Setting teleportDestination to losers podium");
        }
        debugprint("podium name: " + teleportDestination.Entity!.Name);
        easyWinTeleport.Target = teleportDestination.Entity!.Name;
        hardWinTeleport.Target = teleportDestination.Entity!.Name;
    }

    public void AddToWinners()
    {
        Winners++;
        UpdateTeleports();
    }

    public void ResetWinners()
    {
        Winners = 0;
        UpdateTeleports();
    }

    public void AddToWinnersSetting(int toAdd)
	{
		WinnersSettings += toAdd;
		if (WinnersSettings > MaxWinners) WinnersSettings = MaxWinners;
		if (WinnersSettings < MinWinners) WinnersSettings = MinWinners;

		WinnersSettingModel.AddEntityIOEvent(inputName: "Skin", value: $"{WinnersSettings}", delay: 0.0f);

		if (Winners > 0)
		{
			UpdateTeleports();
		}
	}

    public void AddToDamage(int toAdd)
    {
        Damage += toAdd;
        if (Damage > MaxDamage) Damage = MaxDamage;
        if (Damage < MinDamage) Damage = MinDamage;

        DamageSignModel.AddEntityIOEvent(inputName: "Skin", value: $"{DAMAGE_TO_TEXTURE[Damage]}");
        foreach (CTriggerHurt trigger in HurtTriggers)
        {
            trigger.AddEntityIOEvent(inputName: "SetDamage", value: $"{Damage * 2}");
        }
    }

    public void StopActivatorZ(CEntityInstance? activator)
    {
        StopPlayerZ(activator?.As<CCSPlayerPawn>());
    }

    private void StopPlayerZ(CCSPlayerPawn? pawn)
    {
        if (pawn != null)
        {
            Vector oldVel = pawn.AbsVelocity;
            pawn.Teleport(null, null, new Vector(oldVel.X, oldVel.Y, 0));
        }
    }

    private bool IsWithinGame(CCSPlayerPawn pawn)
    {
        Vector playerOrigin = pawn.AbsOrigin!;
        if (playerOrigin.X >= StrafeJumpMin.X && playerOrigin.X <= StrafeJumpMax.X &&
            playerOrigin.Y >= StrafeJumpMin.Y && playerOrigin.Y <= StrafeJumpMax.Y &&
            playerOrigin.Z >= StrafeJumpMin.Z && playerOrigin.Z < StrafeJumpMax.Z)
        {
            return true;
        }
        else if (playerOrigin.X >= StrafeJumpHardMin.X && playerOrigin.X <= StrafeJumpHardMax.X &&
            playerOrigin.Y >= StrafeJumpHardMin.Y && playerOrigin.Y <= StrafeJumpHardMax.Y &&
            playerOrigin.Z >= StrafeJumpHardMin.Z && playerOrigin.Z < StrafeJumpHardMax.Z)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Recall()
    {
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            if (pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE && IsWithinGame(pawn))
            {
                pawn.Teleport(EntityGroup[12].AbsOrigin, null, Vector.Zero);
            }
        }
    }

    public override void Remove()
    {
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
    }
}