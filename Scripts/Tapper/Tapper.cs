using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

using static ArcadeScripts.Random;
using static ArcadeScripts.TextDisplayHelper;

namespace ArcadeScripts.Scripts;

public partial class Tapper : ScriptBase
{
    public Tapper(CLogicScript owner) : base(owner)
    {
        Functions.Add("DisplayHelpText", new ScriptFunction(DisplayHelpText));
        Functions.Add("RandomizePlayers", new ScriptFunction(RandomizePlayers));
        Functions.Add("ClearTapperScoreTexts", new ScriptFunction(ClearTapperScoreTexts));
        Functions.Add("BuyTapperUpgrade", new ScriptFunction<int, int>(BuyTapperUpgrade));
        Functions.Add("AutoTap", new ScriptFunction(AutoTap));
        Functions.Add("ResetTime", new ScriptFunction(ResetTime));
        Functions.Add("AddToTapperScore", new ScriptFunction<int, int, bool>(AddToTapperScore));
        Functions.Add("StartTimer", new ScriptFunction(StartTimer));
        Functions.Add("BuyTapperButtonUpgrade", new ScriptFunction<int, int>(BuyTapperButtonUpgrade));
        Functions.Add("AddToGameTime", new ScriptFunction<int>(AddToGameTime));

        TimeLeft = GameTime + WarmupTime;

        Upgrades[0] = [0, 0, 0, 0, 0];
        Upgrades[1] = [0, 0, 0, 0, 0];
        Upgrades[2] = [0, 0, 0, 0, 0];
        Upgrades[3] = [0, 0, 0, 0, 0];
        UpgradesPreviousTexture[0] = [0, 0, 0, 0, 0];
        UpgradesPreviousTexture[1] = [0, 0, 0, 0, 0];
        UpgradesPreviousTexture[2] = [0, 0, 0, 0, 0];
        UpgradesPreviousTexture[3] = [0, 0, 0, 0, 0];
        ButtonUpgrades[0] = [false, false, false];
        ButtonUpgrades[1] = [false, false, false];
        ButtonUpgrades[2] = [false, false, false];
        ButtonUpgrades[3] = [false, false, false];
        ButtonUpgradesPreviousTexture[0] = [0, 0, 0];
        ButtonUpgradesPreviousTexture[1] = [0, 0, 0];
        ButtonUpgradesPreviousTexture[2] = [0, 0, 0];
        ButtonUpgradesPreviousTexture[3] = [0, 0, 0];

        UpgradeCost[0] = [20, 23, 26, 29, 33, 37, 42, 48, 55, 9999999];
        UpgradeCost[1] = [100, 115, 132, 151, 173, 198, 227, 261, 300, 9999999];
        UpgradeCost[2] = [500, 575, 661, 760, 874, 1005, 1155, 1328, 1527, 9999999];
        UpgradeCost[3] = [2700, 3105, 3570, 4105, 4720, 5428, 6242, 7178, 8254, 9999999];
        UpgradeCost[4] = [14250, 16387, 18845, 21671, 24921, 28659, 32957, 37900, 43585, 9999999];

        ScorePositions[0] = new Vector(4033, 1292, -96);
        ScorePositions[1] = new Vector(4033, 1516, -96);
        ScorePositions[2] = new Vector(4033, 1740, -96);
        ScorePositions[3] = new Vector(4033, 1964, -96);
        ScorePositionsOuter[0] = new Vector(4257, 1292, -155);
        ScorePositionsOuter[1] = new Vector(4257, 1516, -155);
        ScorePositionsOuter[2] = new Vector(4257, 1740, -155);
        ScorePositionsOuter[3] = new Vector(4257, 1964, -155);

        EntityNames = ["tapper_tap", "tapper_tap_upgrade", "tapper_tap_buttonupgrade", "tapper_rank_text", "tapper_platform", "tapper_timer_text", "tapper_score_outer", "tapper_score", "tapper_autotapper", "tapper_start_sound", "tapper_warmup_sound", "tapper_tap_upgrade", "tapper_tap_buttonupgrade"];

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
    }

    private Timer? Timer = null;
    private int TimeLeft;
    private int GameTime = 40;
    private float StartAllowedTime = 0.0f;
    private float GameEndedAt = -900.0f;
    private int ScorePerTap = 10;
    private bool TimerActive = false;
    private bool BetweenGames = true;
    private const int NumBooths = 4;
    private const int MaxScore = 999999999;
    private const int MaxUpgrades = 9;
    private const int MaxGameTime = 60;
    private const int MinGameTime = 10;
    private const int WarmupTime = 4;
    private const float PlayerWidthHalf = 16.0f;

    private int[] Scores = [0, 0, 0, 0];
    private int[] Ranks = [0, 0, 0, 0];
    private string[] ScoreText = ["$0", "$0", "$0", "$0"];

    private int[][] Upgrades = new int[NumBooths][];
    private int[][] UpgradesPreviousTexture = new int[NumBooths][];
    private bool[][] ButtonUpgrades = new bool[NumBooths][];
    private int[][] ButtonUpgradesPreviousTexture = new int[NumBooths][];

    private static int[] UpgradePower = [1, 5, 20, 100, 500];
    private static int[][] UpgradeCost = new int[5][];
    private static int[] ButtonUpgradeCost = [50, 400, 1000]; //decoy, defuse kit, kevlar
    private static int[] ButtonUpgradeMultiplier = [2, 4, 4];

    private static string[] RankTexts = ["1st Place", "2nd Place", "3rd Place", "4th Place"];

    private static Vector[] ScorePositions = new Vector[NumBooths];
    private static Vector[] ScorePositionsOuter = new Vector[NumBooths];
    private static Vector BoothAreaMin = new(4032.0f, 1200.0f, -161.0f);
    private static Vector BoothAreaMax = new(4255.0f, 2064.0f, 90.0f);
    private static Vector RandomizerAreaMin = new(4000.0f, 864.0f, -161.0f);
    private static Vector RandomizerAreaMax = new(4256.0f, 1168.0f, 96.0f);

    private static Vector[] TapperLocations = [new(4140f, 1296f, -144f), new(4140f, 1520f, -144f), new(4140f, 1744f, -144f), new(4140f, 1968f, -144f)];

    private bool DEBUG = false;
    private void debugprint(string text)
    {
        if (!DEBUG) return;
        Console.WriteLine($"************* {text}");
    }

    public void AddToTapperScore(int booth, int amount, bool automatic)
    {
        int adjusted_amount = amount;

        //Don't multiply amount for purchases
        if (amount > 0)
        {
            adjusted_amount = adjusted_amount * ScorePerTap;
        }

        //Adjust by multiplier if this is a manual button press
        if (!automatic)
        {
            for (int upgrade = 0; upgrade <= 2; upgrade++)
            {
                if (ButtonUpgrades[booth][upgrade])
                {
                    adjusted_amount *= ButtonUpgradeMultiplier[upgrade];
                }
            }
        }
        //Add to the score
        Scores[booth] += adjusted_amount;

        //Bound score at 0 <= score <= MAX_SCORE
        if (Scores[booth] < 0)
        {
            Scores[booth] = 0;
        }
        if (Scores[booth] > MaxScore)
        {
            Scores[booth] = MaxScore;
        }

        //Display score for booth
        //DisplayTapperScore(booth)
        DisplayTapperScoreText(booth, $"{Scores[booth]}");

        //Update buttons in case an upgrade is now available
        for (int upgrade = 0; upgrade <= 4; upgrade++)
        {
            DisplayUpgrade(booth, upgrade);
        }
        for (int upgrade = 0; upgrade < 3; upgrade++)
        {
            DisplayButtonUpgrade(booth, upgrade);
        }
    }

    //ent_fire tapper_script runscriptcode "SetTapperScore(0,100000000)
    private void SetTapperScore(int booth, int amount)
    {
        Scores[booth] = amount;
    }

    public void BuyTapperUpgrade(int booth, int upgrade)
    {
        if (CanAffordUpgrade(booth, upgrade))
        {
            AddToTapperScore(booth, -GetCostOfUpgrade(booth, upgrade), true);
            Upgrades[booth][upgrade]++;
            debugprint("Booth " + booth.ToString() + " bought upgrade " + upgrade.ToString() + " (total: " + Upgrades[booth][upgrade].ToString() + ")");
            DisplayUpgrade(booth, upgrade);

            return;
        }
        debugprint("Booth " + booth.ToString() + " was unable to buy upgrade " + upgrade.ToString() + " ($" + Scores[booth].ToString() + " available)");
    }

    public void BuyTapperButtonUpgrade(int booth, int upgrade)
    {
        if (CanAffordButtonUpgrade(booth, upgrade))
        {
            AddToTapperScore(booth, -ButtonUpgradeCost[upgrade], true);
            ButtonUpgrades[booth][upgrade] = true;
            DisplayButtonUpgrade(booth, upgrade);
        }
    }

    private bool CanAffordUpgrade(int booth, int upgrade)
    {
        return Scores[booth] >= GetCostOfUpgrade(booth, upgrade) && Upgrades[booth][upgrade] < MaxUpgrades;
    }

    private bool CanAffordButtonUpgrade(int booth, int upgrade)
    {
        return Scores[booth] >= ButtonUpgradeCost[upgrade] && !ButtonUpgrades[booth][upgrade];
    }

    private int GetCostOfUpgrade(int booth, int upgrade)
    {
        return UpgradeCost[upgrade][Upgrades[booth][upgrade]];
    }

    public void AutoTap()
    {
        if (!TimerActive) return;
        for (int booth = 0; booth < NumBooths; booth++)
        {
            int boothTaps = 0;
            for (int upgrade = 0; upgrade < 5; upgrade++)
            {
                boothTaps += Upgrades[booth][upgrade] * UpgradePower[upgrade];
            }
            if (boothTaps > 0)
            {
                AddToTapperScore(booth, boothTaps, true);
            }
        }
    }

    private void SetButtonLockState(string state)
    {
        foreach (CBaseDoor tapperTap in TapperTap)
        {
            tapperTap.AddEntityIOEvent(inputName: state);
        }
        foreach (CBaseButton tapperTapUpgrade in TapperTapUpgrades)
        {
            tapperTapUpgrade.AddEntityIOEvent(inputName: state);
        } 
        foreach (CBaseButton tapperTapButtonUpgrade in TapperTapButtonUpgrades)
        {
            tapperTapButtonUpgrade.AddEntityIOEvent(inputName: state);
        } 
    }

    public void ClearTapperScoreTexts()
    {
        for (int i = 0; i < NumBooths; i++)
        {
            DisplayTapperScoreText(i, "");
            DisplayRank(i, -1);
        }
    }

    public void AddToGameTime(int time)
    {
        if (!BetweenGames) return;
        GameTime += time;
        if (GameTime > MaxGameTime) GameTime = MaxGameTime;
        if (GameTime < MinGameTime) GameTime = MinGameTime;
        if (!TimerActive)
        {
            TimeLeft = GameTime + WarmupTime;
            SetTimerSeconds(GameTime);
        }
    }

    private void SetGameTime(int time)
    {
        GameTime = time;
    }

    public void ResetTime()
    {
        StartAllowedTime = Server.CurrentTime + 1.0f;
        EndGame(false);
        BetweenGames = true;
        TimeLeft = GameTime + WarmupTime;
        SetTimerSeconds(GameTime);
        for (int booth = 0; booth < NumBooths; booth++)
        {
            Upgrades[booth] = [0, 0, 0, 0, 0];
            ButtonUpgrades[booth] = [false, false, false];
            AddToTapperScore(booth, -MaxScore, true);
        }
        ClearTapperScoreTexts();
        ResetPlatforms();
    }

    public void StartTimer()
    {
        if (TimerActive || TimeLeft <= 0 || Server.CurrentTime < StartAllowedTime) return;

        BetweenGames = false;
        TimerActive = true;
        Timer = new(1.0f, TimerTick);
        Ranks = [0, 0, 0, 0];
    }

    private void TimerTick()
    {
        if (!TimerActive) return;
        TimeLeft--;
        if (TimeLeft > 0)
        {
            Timer = new(1.0f, TimerTick);
            if (TimeLeft == GameTime)
            {
                //Warmup is over, start the game!
                TapperAutotapper.AddEntityIOEvent(inputName: "Enable");
                SetButtonLockState("Unlock");
                SetTimerTextAll("TAP!", Color.Green);
                TapperStartSound.AddEntityIOEvent(inputName: "PlaySound");
            }
            else if (TimeLeft > GameTime)
            {
                SetTimerTextAll(" " + (TimeLeft - GameTime).ToString() + "!", Color.Red);
                TapperWarmupSound.AddEntityIOEvent(inputName: "PlaySound");
            }
            else
            {
                SetTimerSeconds(TimeLeft);
            }
        }
        else
        {
            SetTimerSeconds(TimeLeft);
            GameEndedAt = Server.CurrentTime;
            EndGame(true);
        }
    }

    private void EndGame(bool rankPlayers)
    {
        TimerActive = false;
        SetButtonLockState("Lock");
        TapperAutotapper.AddEntityIOEvent(inputName: "Disable");
        if (rankPlayers)
        {
            RankPlayers();
        }
    }

    private void RankPlayers()
    {
        int[] scoresCopy = [Scores[0], Scores[1], Scores[2], Scores[3]];
        Dictionary<int, Dictionary<int, int>> rankedBooths = [];

        for (int rankIndex = 0; rankIndex < NumBooths; rankIndex++)
        {
            int currentMax = -1;
            Dictionary<int, int> currentBooths = [];
            int boothsInRank = 0;
            for (int booth = 0; booth < NumBooths; booth++)
            {
                if (scoresCopy[booth] > currentMax)
                {
                    currentBooths.Clear();
                    currentBooths[0] = booth;
                    boothsInRank = 1;
                    currentMax = scoresCopy[booth];
                }
                else if (scoresCopy[booth] == currentMax)
                {
                    currentBooths[boothsInRank] = booth;
                    boothsInRank++;
                }
            }
            rankedBooths[rankIndex] = currentBooths;
            for (int boothIndex = 0; boothIndex < boothsInRank; boothIndex++)
            {
                int booth = rankedBooths[rankIndex][boothIndex];
                scoresCopy[booth] = -999999;
                debugprint("Booth " + booth + " has rank " + (rankIndex + 1).ToString() + " with score " + Scores[booth]);
                if (booth >= NumBooths) continue;
                DisplayRank(booth, rankIndex);
                MovePlatform(booth, rankIndex);
                Ranks[booth] = rankIndex;
            }
        }
    }

    private void DisplayRank(int booth, int rank)
    {
        string text = (rank < 0) ? "" : RankTexts[rank];
        TapperRankText[booth].AddEntityIOEvent(inputName: "SetMessage", value: text);
    }

    private void ResetPlatforms()
    {
        for (int booth = 0; booth < NumBooths; booth++)
        {
            TapperPlatforms[booth].AddEntityIOEvent(inputName: "SetSpeed", value: "256", delay: 0.0f);
            MovePlatform(booth, 4);
            TapperPlatforms[booth].AddEntityIOEvent(inputName: "SetSpeed", value: "64", delay: 0.95f);
        }
    }

    private void MovePlatform(int booth, int rank)
    {
        TapperPlatforms[booth].MoveDistance = 192 - (rank * 64);
        TapperPlatforms[booth].AddEntityIOEvent(inputName: "SetPosition", value: $"{1.0f - (rank / 4.0f)}", delay: 0.1f);
    }

    private void SetTimerSeconds(int time)
    {
        int minutes = time / 60;
        int seconds = time % 60;
        string timeString = minutes.ToString() + ":" + (seconds < 10 ? "0" : "") + seconds;
        SetTimerTextAll(timeString, Color.White);
    }

    private void SetTimerTextAll(string text, Color color)
    {
        for (int i = 0; i < NumBooths + 1; i++)
        {
            SetTimerText(i, text, color);
        }
    }

    private void SetTimerText(int booth, string text, Color color)
    {
        TapperTimerText[booth].AddEntityIOEvent(inputName: "SetMessage", value: text);
        TapperTimerText[booth].Color = color;
    }

    private void DisplayTapperScoreText(int booth, string text)
    {
        //Add commas
        string scoreWithCommas = "";
        string remainingScore = text.ToString();
        while (remainingScore.Length > 0)
        {
            if (remainingScore.Length < 3)
            {
                scoreWithCommas = remainingScore + scoreWithCommas;
                remainingScore = "";
            }
            else
            {
                scoreWithCommas = remainingScore.Substring(remainingScore.Length - 3) + scoreWithCommas;
                remainingScore = remainingScore.Substring(0, remainingScore.Length - 3);
            }
            if (remainingScore.Length > 0)
            {
                scoreWithCommas = "," + scoreWithCommas;
            }
            else
            {
                scoreWithCommas = "$" + scoreWithCommas;
            }
        }

        //Set Text
        TapperScoreText[booth].AddEntityIOEvent(inputName: "SetMessage", value: scoreWithCommas);
        TapperScoreTextOuter[booth].AddEntityIOEvent(inputName: "SetMessage", value: scoreWithCommas);
        ScoreText[booth] = scoreWithCommas;

        //Reposition (if necessary)
        int yoffset = scoreWithCommas.Length * 3;
        Vector scorePosition = new(ScorePositions[booth].X, ScorePositions[booth].Y - yoffset, ScorePositions[booth].Z);
        TapperScoreText[booth].Teleport(scorePosition);
        scorePosition = new Vector(ScorePositionsOuter[booth].X, scorePosition.Y, ScorePositionsOuter[booth].Z);
        TapperScoreTextOuter[booth].Teleport(scorePosition);
    }

    private void DisplayUpgrade(int booth, int upgrade)
    {
        //Update button texture
        int textureindex = Upgrades[booth][upgrade] * 2 + (CanAffordUpgrade(booth, upgrade) ? 1 : 0);
        if (textureindex == UpgradesPreviousTexture[booth][upgrade]) return;
        UpgradesPreviousTexture[booth][upgrade] = textureindex;
        TapperTapUpgradeModels[booth][upgrade].AddEntityIOEvent(inputName: "Skin", value: $"{textureindex}");
    }

    private void DisplayButtonUpgrade(int booth, int upgrade)
    {
        //Update button texture
        int textureindex = ButtonUpgrades[booth][upgrade] ? 2 : (CanAffordButtonUpgrade(booth, upgrade) ? 1 : 0);
        if (textureindex == ButtonUpgradesPreviousTexture[booth][upgrade]) return;
        ButtonUpgradesPreviousTexture[booth][upgrade] = textureindex;
        TapperTapButtonUpgradeModels[booth][upgrade].AddEntityIOEvent(inputName: "Skin", value: $"{textureindex}");
    }

    private bool IsPlayerInBooth(Vector playerOrigin)
    {
        return playerOrigin.X >= BoothAreaMin.X && playerOrigin.X <= BoothAreaMax.X && playerOrigin.Y >= BoothAreaMin.Y && playerOrigin.Y <= BoothAreaMax.Y && playerOrigin.Z >= BoothAreaMin.Z && playerOrigin.Z < BoothAreaMax.Z;
    }

    private int GetPlayerBooth(CCSPlayerController player)
    {
        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return -1;

        Vector playerOrigin = pawn.AbsOrigin!;
        float gameY = playerOrigin.Y - BoothAreaMin.Y;
        float boothRatio = gameY / (BoothAreaMax.Y - BoothAreaMin.Y);
        float booth = boothRatio * 4.0f;
        return (int)booth;
    }

    private void DisplayScoresHudText(CCSPlayerController player)
    {
        string text = "";
        int playerBooth = GetPlayerBooth(player);
        for (int booth = 0; booth < 4; booth++)
        {
            if (Scores[booth] == 0)
            {
                ScoreText[booth] = "$0";
            }

            if (booth == playerBooth)
            {
                text += "YOU - ";
            }
            else
            {
                text += "TEAM " + (booth + 1) + " - ";
            }
            text += ScoreText[booth] + " (";
            text += RankTexts[Ranks[booth]] + ")<br>";
        }
        DisplayText(player, text);
    }

    public void DisplayHelpText()
    {
        if (TimerActive) return;
        
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            Vector playerOrigin = pawn.AbsOrigin!;
            if (pawn.Health > 0 && IsPlayerInBooth(playerOrigin))
            {
                if (Server.CurrentTime < GameEndedAt + 20.0f)
                {
                    DisplayScoresHudText(player);
                }
                else
                {
                    DisplayText(player, "1. Tap your use key while looking at the $ button to gain money.<br>2. Buy upgrades on the left and right.<br>3. Left upgrades tap for you.<br>4. Right upgrades improve your own tapping.<br>5. Whoever has the most money wins!");
                }
            }
        }
    }

    public void RandomizePlayers()
    {
        if (!BetweenGames) return;
        List<CCSPlayerPawn> players = [];
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            Vector playerOrigin = pawn.AbsOrigin!;
            if (playerOrigin.X >= RandomizerAreaMin.X - PlayerWidthHalf && playerOrigin.X <= RandomizerAreaMax.X + PlayerWidthHalf &&
                playerOrigin.Y >= RandomizerAreaMin.Y - PlayerWidthHalf && playerOrigin.Y <= RandomizerAreaMax.Y + PlayerWidthHalf &&
                playerOrigin.Z >= RandomizerAreaMin.Z && playerOrigin.Z < RandomizerAreaMax.Z && pawn.Health > 0)
            {
                players.Add(pawn);
            }
        }

        int NUM_TEAMS = 4;
        if (players.Count % 4 == 0)
        {
            NUM_TEAMS = 4;
        }
        else if (players.Count % 3 == 0)
        {
            NUM_TEAMS = 3;
        }
        else if (players.Count % 2 == 0)
        {
            NUM_TEAMS = 2;
        }
        debugprint("NUM_TEAMS: " + NUM_TEAMS);

        int location = 0;
        while (players.Count > 0)
        {
            int playerIndex = RandomInt(0, players.Count - 1);
            CCSPlayerPawn player = players[playerIndex];
            players.RemoveAt(playerIndex);
            player.Teleport(TapperLocations[location++]);
            if (location >= NUM_TEAMS)
            {
                location = 0;
            }
        }
    }

    private void DisplayText(CCSPlayerController player, string text)
    {
        SetMessage(player.Slot, text, 4.0f);
    }


    public override void Remove()
    {
        Timer?.Kill();
        Timer = null;

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
    }

}
