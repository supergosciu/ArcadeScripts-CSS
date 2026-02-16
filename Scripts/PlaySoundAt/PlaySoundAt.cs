using CounterStrikeSharp.API.Core;
using static ArcadeScripts.Random;

namespace ArcadeScripts.Scripts;

public class PlaySoundAt : ScriptBase
{
    private static List<CAmbientGeneric> Sounds = null!;

    public PlaySoundAt(CLogicScript owner) : base(owner)
    {
        Functions.Add("PlaySound", new ScriptFunction<CEntityInstance>(PlaySound));

        string targetname = owner.Entity!.Name;
        switch (targetname)
        {
            case "lightning_sound_script1":
                EntityGroupNames[0] = "lightning_sound_target1";
                EntityGroupNames[1] = "lightning_sound1";
                break;
            case "lightning_sound_script2":
                EntityGroupNames[0] = "lightning_sound_target2";
                EntityGroupNames[1] = "lightning_sound2";
                break;
            case "toxic_river_sound_script":
                EntityGroupNames[0] = "toxic_river_sound_target";
                EntityGroupNames[1] = "toxic_river_sound";
                break;
            case "climb_winning_sound_script":
                EntityGroupNames[0] = "climb_winning_sound_target";
                EntityGroupNames[1] = "climb_winning_sound";
                break;
            case "cell_bhop_sound_script":
                EntityGroupNames[0] = "cell_bhop_sound_target";
                EntityGroupNames[1] = "cell_bhop_sound1";
                EntityGroupNames[2] = "cell_bhop_sound2";
                EntityGroupNames[3] = "cell_bhop_sound3";
                EntityGroupNames[4] = "cell_bhop_sound4";
                EntityGroupNames[5] = "cell_bhop_sound5";
                break;
            case "sj_winning_sound_script":
                EntityGroupNames[0] = "sj_winning_sound_target";
                EntityGroupNames[1] = "sj_winning_sound";
                break;
            case "surf_winning_sound_script":
                EntityGroupNames[0] = "surf_winning_sound_target";
                EntityGroupNames[1] = "surf_winning_sound";
                break;
        }

        foreach (string name in EntityGroupNames)
        {
            if (string.IsNullOrEmpty(name)) continue;
            EntityNames.Add(name);
        }

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
    }

    private void OnAllEntitiesSpawned()
    {
        foreach (KeyValuePair<string, List<CEntityInstance>> kv in EntityList)
        {
            List<CEntityInstance> entities = kv.Value;
            string targetname = kv.Key;

            int index = Array.IndexOf(EntityGroupNames, targetname);
            if (index != -1)
            {
                EntityGroup[index] = entities[0].As<CBaseEntity>();
            }
        }
    }

    private void Setup()
    {
        if (Sounds == null)
        {
            Sounds = [];
            for (int i = 1; i < 16; i++)
            {
                if (EntityGroup[i] == null || !EntityGroup[i].IsValid) continue;

                Sounds.Add(EntityGroup[i].As<CAmbientGeneric>());
            }
        }
    }

    public void PlaySound(CEntityInstance? activator)
    {
        if (activator == null) return;

        Setup();

        CCSPlayerPawn pawn = activator.As<CCSPlayerPawn>();
        EntityGroup[0]?.Teleport(pawn.AbsOrigin, null, null);
        EntityGroup[RandomInt(1, Sounds.Count)]?.AddEntityIOEvent(inputName: "PlaySound", delay: 0.05f, activator: pawn);
    }

    public override void Remove()
    {
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
    }
}