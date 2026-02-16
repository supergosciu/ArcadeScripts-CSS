using System.Numerics;
using CounterStrikeSharp.API.Core;

namespace ArcadeScripts.Scripts;

public class Surf : ScriptBase
{
    public Surf(CLogicScript owner) : base(owner)
    {
        Functions.Add("AddToWinnersSetting", new ScriptFunction<int>(AddToWinnersSetting));
		Functions.Add("AddToDamage", new ScriptFunction<int>(AddToDamage));
		Functions.Add("StopActivator", new ScriptFunction<CEntityInstance>(StopActivator));
		Functions.Add("ResetWinners", new ScriptFunction(ResetWinners));
		Functions.Add("AddToWinners", new ScriptFunction(AddToWinners));

		EntityGroupNames[0] = "decathlon_destination1";
		EntityGroupNames[1] = "decathlon_destination2";
		EntityGroupNames[2] = "decathlon_destination3";
		EntityGroupNames[3] = "decathlon_destination4";
		EntityGroupNames[4] = "decathlon_destination5";
		EntityGroupNames[5] = "surf_winner_tp";

		EntityNames = ["surf_damage_sign_model", "surf_damage", "surf_winners_sign_model", "decathlon_destination1", "decathlon_destination2", "decathlon_destination3", "decathlon_destination4", "decathlon_destination5", "surf_winner_tp"];

		ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
    }

    private static Dictionary<int, int> DamageToTexture = new() { { 0, 0 }, { 5, 1 }, { 10, 2 }, { 15, 3 }, { 20, 4 }, { 25, 5 }, { 50, 6 }, { 75, 7 }, { 100, 8 } };

    private int Damage = 0;
    private int WinnersSettings = 4;
    private int Winners = 0;
    private const int MinDamage = 0;
    private const int MaxDamage = 20;
    private const int MinWinners = 1;
    private const int MaxWinners = 32;

    private CDynamicProp SurfDamageSignModel = null!;
    private CDynamicProp SurfWinnersSettingModel = null!;
    private CTriggerHurt SurfDamage = null!;


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

			if (targetname == "surf_damage_sign_model")
            {
                SurfDamageSignModel = entities[0].As<CDynamicProp>();
            }
			else if (targetname == "surf_damage")
            {
                SurfDamage = entities[0].As<CTriggerHurt>();
            }
			else if (targetname == "surf_winners_sign_model")
            {
                SurfWinnersSettingModel = entities[0].As<CDynamicProp>();
            }

			int index = Array.IndexOf(EntityGroupNames, targetname);
            if (index != -1)
            {
                EntityGroup[index] = entities[0].As<CBaseEntity>();
            }
		}
	}

	public void AddToDamage(int toAdd)
	{
		Damage += toAdd;
		if (Damage > MaxDamage) Damage = MaxDamage;
		if (Damage < MinDamage) Damage = MinDamage;

		SurfDamageSignModel.AddEntityIOEvent(inputName: "Skin", value: $"{DamageToTexture[Damage]}", delay: 0.0f);
		if (Damage == 0)
		{
			SurfDamage.AddEntityIOEvent(inputName: "Disable", delay: 0.0f);
		}
		else
		{
			SurfDamage.AddEntityIOEvent(inputName: "Enable", delay: 0.0f);
		}
		SurfDamage.AddEntityIOEvent(inputName: "SetDamage", value: $"{Damage * 2}", delay: 0.0f);
	}

	public void StopActivator(CEntityInstance? activator)
	{
		if (activator != null) StopPlayer(activator.As<CCSPlayerPawn>());
	}

    private void StopPlayer(CCSPlayerPawn pawn)
	{
		pawn.Teleport(null, null, Vector3.Zero);
	}

    private void UpdateTeleports()
	{
		CBaseEntity[] teleportDesinationEntities = [EntityGroup[0], EntityGroup[1], EntityGroup[2]];
		CBaseEntity winnersDestination = EntityGroup[3];
		CBaseEntity losersDestination = EntityGroup[4];
		CBaseEntity teleportDestination = losersDestination;
		CTriggerTeleport winTeleport = EntityGroup[5].As<CTriggerTeleport>();

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
		winTeleport.Target = teleportDestination.Entity.Name;
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

		SurfWinnersSettingModel.AddEntityIOEvent(inputName: "Skin", value: $"{WinnersSettings}", delay: 0.0f);

		if (Winners > 0)
		{
			UpdateTeleports();
		}
	}

	public override void Remove()
    {
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
    }
}