using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static ArcadeScripts.TextDisplayHelper;

namespace ArcadeScripts.Scripts;

public class SurfTimer : ScriptBase
{
    public SurfTimer(CLogicScript owner) : base(owner)
    {
        Functions.Add("SetWinners", new ScriptFunction<int>(SetWinners));
        Functions.Add("DisplayGameInformation", new ScriptFunction(DisplayGameInformation));
        Functions.Add("SetPlayerInGame", new ScriptFunction<CEntityInstance, bool>(SetPlayerInGame));
        Functions.Add("SetDifficulty", new ScriptFunction<int>(SetDifficulty));
        Functions.Add("EndTimer", new ScriptFunction<CEntityInstance>(EndTimer));
        Functions.Add("StartTimer", new ScriptFunction<CEntityInstance>(StartTimer));
        Functions.Add("InitializeBestTimes", new ScriptFunction(InitializeBestTimes));
        Functions.Add("SetMaxZ", new ScriptFunction<float>(SetMaxZ));

        EntityGroupNames[0] = "surf_timer_score1";
        EntityGroupNames[1] = "surf_timer_score2";
        EntityGroupNames[2] = "surf_timer_score3";
        EntityGroupNames[3] = "surf_timer_record0";
        EntityGroupNames[4] = "surf_timer_record1";
        EntityGroupNames[5] = "surf_timer_record2";

        EntityNames = ["surf_timer_timer", "surf_timer_score1", "surf_timer_score2", "surf_timer_score3", "surf_timer_record0", "surf_timer_record1", "surf_timer_record2"];

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
    }

    private int Winners = 0;
    private const int BestTimeTextEntityOffset = 3;
    private const string BestTimePrefix = "BEST_TIME_";
    private int Difficulty = 0;
    private float? MaxZ = null;

    private bool[] IsPlayerInGame = new bool[Server.MaxPlayers];
    private float[] StartTime = new float[Server.MaxPlayers];
    private float[] EndTime = new float[Server.MaxPlayers];
    private float[] BestTimes = [0.0f, 0.0f, 0.0f];
    private Dictionary<string, float> SurfTimes = [];

    private Dictionary<int, CCSPlayerPawn> PlayerEntities = [];
    private CPointWorldText[] BestTimeTextEntities = [null!, null!, null!];
    private CTimerEntity SurfTimerEntity = null!;

    private void OnAllEntitiesSpawned()
    {
        foreach (KeyValuePair<string, List<CEntityInstance>> kv in EntityList)
        {
            List<CEntityInstance> entities = kv.Value;
            string targetname = kv.Key;

            if (targetname == "surf_timer_timer")
            {
                SurfTimerEntity = entities[0].As<CTimerEntity>();
            }

            int index = Array.IndexOf(EntityGroupNames, targetname);
            if (index != -1)
            {
                EntityGroup[index] = entities[0].As<CBaseEntity>();
            }
        }
    }

    public void InitializeBestTimes()
    {
        for (int diff = 0; diff < 3; diff++)
        {
            BestTimeTextEntities[diff] = EntityGroup[BestTimeTextEntityOffset + diff].As<CPointWorldText>();
            if (!SurfTimes.ContainsKey($"{BestTimePrefix}{diff}")) SurfTimes[$"{BestTimePrefix}{diff}"] = 0.0f;
            BestTimes[diff] = SurfTimes[$"{BestTimePrefix}{diff}"];
            if (BestTimes[diff] > 0.1f)
            {
                UpdateTimeTextEntity(diff);
            }
        }
    }

    public void StartTimer(CEntityInstance? activator)
    {
        if (activator != null)
        {
            CCSPlayerController? player = activator.As<CCSPlayerPawn>().OriginalController.Value;
            if (player == null) return;

            StartTime[player.Slot] = Server.CurrentTime;
            EndTime[player.Slot] = 0.0f;
        }
    }

    public void EndTimer(CEntityInstance? activator)
    {
        CCSPlayerController? player = activator?.As<CCSPlayerPawn>().OriginalController.Value;
        if (player == null) return;

        int slot = player.Slot;
        EndTime[slot] = Server.CurrentTime;
        if (Winners <= 2)
        {
            float totalTime = EndTime[slot] - StartTime[slot];
            //Set visual time for 1st, 2nd and 3rd place
            if (Winners <= 2)
            {
                EntityGroup[Winners].AddEntityIOEvent(inputName: "SetMessage", value: $"{totalTime}".Replace(",", ".") + "s");
            }

            //Set overall record for map
            if (BestTimes[Difficulty] < 0.1f || totalTime < BestTimes[Difficulty])
            {
                BestTimes[Difficulty] = totalTime;
                SurfTimes[$"{BestTimePrefix}{Difficulty}"] = totalTime;
                UpdateTimeTextEntity(Difficulty);
            }
        }
        DisplayGameInformationToPlayer(slot, true);
        Winners++;
    }

    public void SetWinners(int val)
    {
        Winners = val;
    }

    public void SetDifficulty(int diff)
    {
        Difficulty = diff;
    }


    private void UpdateTimeTextEntity(int diff)
    {
        EntityGroup[BestTimeTextEntityOffset + diff]?.AddEntityIOEvent(inputName: "SetMessage", value: $"{BestTimes[diff]}".Replace(",", ".") + "s");
    }

    public void DisplayGameInformation()
    {
        int players = 0;
        for (int slot = 0; slot <= Server.MaxPlayers; slot++)
        {
            players += DisplayGameInformationToPlayer(slot, false);
        }

        if (players == 0)
        {
            SurfTimerEntity.AddEntityIOEvent(inputName: "Disable");
        }
    }

    private int DisplayGameInformationToPlayer(int slot, bool wonGame)
    {
        if (!PlayerEntities.TryGetValue(slot, out CCSPlayerPawn? pawn) || pawn == null || !pawn.IsValid || pawn.Health <= 0 || (!wonGame && MaxZ != null && pawn.AbsOrigin!.Z > MaxZ)) return 0;

        float endTime = (EndTime[slot] > 0.0f) ? (EndTime[slot] - StartTime[slot]) : (Server.CurrentTime - StartTime[slot]);
        string score_text = $"Time: {(wonGame ? endTime : (int)endTime)}".Replace(",", ".") + "s";
        SetMessage(slot, score_text, 2.0f);

        return 1;
    }

    public void SetPlayerInGame(CEntityInstance? activator, bool inGame)
    {
        if (activator == null) return;

        CCSPlayerController? player = activator.As<CCSPlayerPawn>().OriginalController.Value;
        if (player == null) return;

        CCSPlayerPawn pawn = activator.As<CCSPlayerPawn>();
        int slot = player.Slot;
        IsPlayerInGame[slot] = inGame;

        if (inGame)
        {
            PlayerEntities[slot] = pawn;
            DisplayGameInformationToPlayer(slot, false);
            pawn.PrivateVScripts = "inGame";
        }
        else
        {
            pawn.PrivateVScripts = "";
            PlayerEntities[slot] = null!;
        }
    }

    public void SetMaxZ(float z)
    {
        MaxZ = z;
    }

    public override void Remove()
    {
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
    }
}