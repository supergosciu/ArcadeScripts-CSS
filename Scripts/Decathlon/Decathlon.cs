using CounterStrikeSharp.API.Core;

namespace ArcadeScripts.Scripts;

public class Decathlon : ScriptBase
{
    public Decathlon(CLogicScript owner) : base(owner)
    {
        Functions.Add("AddToWinnersSetting", new ScriptFunction<int>(AddToWinnersSetting));
		Functions.Add("AddToDamage", new ScriptFunction<int>(AddToDamage));
		Functions.Add("ResetWinners", new ScriptFunction(ResetWinners));
		Functions.Add("AddToWinners", new ScriptFunction(AddToWinners));

		EntityGroupNames[0] = "decathlon_destination1";
		EntityGroupNames[1] = "decathlon_destination2";
		EntityGroupNames[2] = "decathlon_destination3";
		EntityGroupNames[3] = "decathlon_destination4";
		EntityGroupNames[4] = "decathlon_destination5";
		EntityGroupNames[5] = "decathlon_winner_tp";

		EntityNames = ["decathlon_damage_sign_model", "decathlon_damage", "decathlon_winners_sign_model", "decathlon_destination1", "decathlon_destination2", "decathlon_destination3", "decathlon_destination4", "decathlon_destination5", "decathlon_winner_tp"];

		ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
    }

    private int Damage = 0;
    private const int MinDamage = 0;
    private const int MaxDamage = 20;
    private int Winners = 0;
    private int WinnersSettings = 4;
    private const int MinWinners = 1;
    private const int MaxWinners = 32;

    private static Dictionary<int, int> DamageToTexture = new() { { 0, 0 }, { 5, 1 }, { 10, 2 }, { 15, 3 }, { 20, 4 }, { 25, 5 }, { 50, 6 }, { 75, 7 }, { 100, 8 } };

    private List<CTriggerHurt> DecathlonDamage = [];
    private CDynamicProp DecathlonDamageSignModel = null!;
    private CDynamicProp DecathlonWinnersSettingModel = null!;

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

            if (targetname == "decathlon_damage_sign_model")
            {
                DecathlonDamageSignModel = entities[0].As<CDynamicProp>();
            }
			else if (targetname == "decathlon_damage")
            {
                for (int i = 0; i < entities.Count; i++) DecathlonDamage.Add(entities[i].As<CTriggerHurt>());
            }
			else if (targetname == "decathlon_winners_sign_model")
            {
                DecathlonWinnersSettingModel = entities[0].As<CDynamicProp>();
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

		DecathlonDamageSignModel.AddEntityIOEvent(inputName: "Skin", value: $"{DamageToTexture[Damage]}", delay: 0.0f);
		if (Damage == 0)
		{
			foreach (CTriggerHurt trigger in DecathlonDamage)
            {
				trigger.AddEntityIOEvent(inputName: "Disable", delay: 0.0f);
            }
		}
		else
		{
			foreach (CTriggerHurt trigger in DecathlonDamage)
            {
				trigger.AddEntityIOEvent(inputName: "Enable", delay: 0.0f);
            }
		}
		foreach (CTriggerHurt trigger in DecathlonDamage)
		{
			trigger.AddEntityIOEvent(inputName: "SetDamage", value: $"{Damage * 2}", delay: 0.0f);
		}
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

		DecathlonWinnersSettingModel.AddEntityIOEvent(inputName: "Skin", value: $"{WinnersSettings}", delay: 0.0f);

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