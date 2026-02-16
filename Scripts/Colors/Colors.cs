using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static ArcadeScripts.Random;
using static ArcadeScripts.TextDisplayHelper;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace ArcadeScripts.Scripts;

public partial class Colors : ScriptBase
{
    public Colors(CLogicScript owner) : base(owner)
    {
        Functions.Add("AddToDamage", new ScriptFunction<int>(AddToDamage));
        Functions.Add("SetAuto", new ScriptFunction<bool>(SetAuto));
        Functions.Add("SetNextDifficulty", new ScriptFunction<int>(SetNextDifficulty));
        Functions.Add("DisplayHelpText", new ScriptFunction(DisplayHelpText));
        Functions.Add("StartGame", new ScriptFunction(StartGame));
        Functions.Add("EndGame", new ScriptFunction(EndGame));
        Functions.Add("PlayerJumped", new ScriptFunction<CEntityInstance>(PlayerJumped));
        Functions.Add("ChooseRandomCommand", new ScriptFunction(ChooseRandomCommand));
        Functions.Add("Setup", new ScriptFunction(Setup));

        EntityNames = ["colors_line_easy_horizontal", "colors_line_medium_horizontal", "colors_line_hard_horizontal", "colors_line_easy_vertical", "colors_line_medium_vertical", "colors_line_hard_vertical", "colors_circle_easy", "colors_circle_medium", "colors_circle_hard", "colors_commands_middle_model", "colors_commands_left_model", "colors_commands_right_model", "colors_ladder", "colors_ladder_brush_easy", "colors_ladder_brush_medium", "colors_ladder_brush_hard", "colors_jump_trigger", "colors_timer", "colors_damage_sign_model", "colors_easy_button_model", "colors_medium_button_model", "colors_hard_button_model", "colors_stop_button_model"];

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
    }

    private enum ColorsCommands
    {
        Corners,
        Side,
        Colors,
        Direction,
        Line,
        Circle,
        Jump,
        Crouch,
        Ladders,
        Walk,
        Run
    }

    private static Dictionary<int, bool> ColorsNorth = new() { { 6, true }, { 7, true }, { 8, true } };
    private static Dictionary<int, bool> ColorsEast = new() { { 2, true }, { 5, true }, { 8, true } };
    private static Dictionary<int, bool> ColorsSouth = new() { { 0, true }, { 1, true }, { 2, true } };
    private static Dictionary<int, bool> ColorsWest = new() { { 0, true }, { 3, true }, { 6, true } };
    private static Dictionary<int, bool>[] SideColors = [ColorsNorth, ColorsEast, ColorsSouth, ColorsWest];
    private string[] Directions = ["Face North", "Face East", "Face South", "Face West"];
    private string[] DirectionsAbbreviated = ["N", "E", "S", "W"];
    private string[] Sides = ["Go North", "Go East", "Go South", "Go West"];
    private string[] ColorNames = ["Orange", "Purple", "Green", "White", "Red", "Black", "Blue", "Yellow", "Brown"];
    private float[] LinesWidth = [64.0f, 32.0f, 16.0f];
    private float[] RoundTimes = [4.5f, 4.0f, 3.5f];

    private static Vector GameMin = new(1232.0f, 1440.0f, -513.0f);
    private static Vector GameMax = new(1616.0f, 1824.0f, -192.0f);
    private Vector LinePosition = new(0.0f, 0.0f, 0.0f);
    private Vector CirclePosition = new(0.0f, 0.0f, 0.0f);

    private static Vector[] ColorsMin = [
        new(GameMin.X, GameMin.Y, 0),
        new(GameMin.X + ColorsWidthThird, GameMin.Y, 0),
        new(GameMax.X - ColorsWidthThird, GameMin.Y, 0),
        new(GameMin.X, GameMin.Y + ColorsWidthThird, 0),
        new(GameMin.X + ColorsWidthThird, GameMin.Y + ColorsWidthThird, 0),
        new(GameMax.X - ColorsWidthThird, GameMin.Y + ColorsWidthThird, 0),
        new(GameMin.X, GameMax.Y - ColorsWidthThird, 0),
        new(GameMin.X + ColorsWidthThird, GameMax.Y - ColorsWidthThird, 0),
        new(GameMax.X - ColorsWidthThird, GameMax.Y - ColorsWidthThird, 0)
    ];
    private Vector[] ColorsMax = [
        new(GameMin.X + ColorsWidthThird, GameMin.Y + ColorsWidthThird, 0),
        new(GameMax.X - ColorsWidthThird, GameMin.Y + ColorsWidthThird, 0),
        new(GameMax.X, GameMin.Y + ColorsWidthThird, 0),
        new(GameMin.X + ColorsWidthThird, GameMax.Y - ColorsWidthThird, 0),
        new(GameMax.X - ColorsWidthThird, GameMax.Y - ColorsWidthThird, 0),
        new(GameMax.X, GameMax.Y - ColorsWidthThird, 0),
        new(GameMin.X + ColorsWidthThird, GameMax.Y, 0),
        new(GameMax.X - ColorsWidthThird, GameMax.Y, 0),
        new(GameMax.X, GameMax.Y, 0)
    ];

    private Dictionary<int, int> DamageToTexture = new() { { 0, 0 }, { 5, 1 }, { 10, 2 }, { 15, 3 }, { 20, 4 }, { 25, 5 }, { 50, 6 }, { 75, 7 }, { 100, 8 } };
    private Dictionary<ColorsCommands, string> CommandToTexture = new() { { ColorsCommands.Corners, "4" }, { ColorsCommands.Side, "-1" }, { ColorsCommands.Colors, "-1" }, { ColorsCommands.Direction, "-1" }, { ColorsCommands.Line, "1" }, { ColorsCommands.Circle, "2" }, { ColorsCommands.Jump, "13" }, { ColorsCommands.Crouch, "14" }, { ColorsCommands.Ladders, "3" }, { ColorsCommands.Walk, "24" }, { ColorsCommands.Run, "25" } };
    private Dictionary<int, string> GoDirectionToTexture = new() { { 0, "12" }, { 1, "10" }, { 2, "11" }, { 3, "9" } }; // NESW
    private Dictionary<int, string> FaceDirectionToTexture = new() { { 0, "8" }, { 1, "6" }, { 2, "7" }, { 3, "5" } }; // NESW
    private Dictionary<int, string> ColorToTexture = new() { { 0, "21" }, { 1, "15" }, { 2, "22" }, { 3, "20" }, { 4, "23" }, { 5, "19" }, { 6, "18" }, { 7, "16" }, { 8, "17" } };
    //["Orange",   "Purple",   "Green",    "White",    "Red",      "Black",    "Blue",     "Yellow",   "Brown"];
    
    private const float MinRoundTime = 2.0f;
    private const float RoundEndTime = 0.5f;
    private const int RoundsPerDifficulty = 6;
    private const float ColorsWidth = 384.0f;
    private const float ColorsWidthThird = 128.0f;
    private const float PlayerWidthHalf = 16.0f;
    private const float PlayerHeightCrouched = 54.0f;
    private const float PlayerJumpHeight = 64.0f;
    private const int NumColors = 9;
    private const int MinDamage = 0;
    private const int MaxDamage = 100;
    private const int WalkingSpeedSquared = 16900;
    private const int RunningSpeedSquared = 62500;

    //Configuration
    private int Damage = 50;
    private int Difficulty = 0;
    private int NextDifficulty = 0;

    //State
    private bool GameOn = false;
    private int GameCount = 0;
    private int RoundNumber = 0;
    private bool Auto = true;
    private List<ColorsCommands>? LastCommands = null;
    private int LineHorizontal = 0;
    private int Direction = -1;
    private int Side = -1;

    private Dictionary<int, bool> PickedColors = [];
    private float[] CircleRadius = [ColorsWidth / 4.0f, ColorsWidth / 8.0f, ColorsWidth / 16.0f];
    private string ColorString = "";
    private List<CCSPlayerController> FrozenPlayers = [];
    private int ExpectedPlayerSpeed = 0;

    private bool DEBUG = false;
    private void debugprint(string text)
    {
        if (!DEBUG) return;
        Console.WriteLine($"************* {text}");
    }

    public void Setup()
    {
        foreach (CFuncBrush circle in CircleEntities)
        {
            circle.AddEntityIOEvent(inputName: "Disable");
        }
        foreach (CFuncBrush line in LineEntitiesHorizontal)
        {
            line.AddEntityIOEvent(inputName: "Disable");
        }
        foreach (CFuncBrush line in LineEntitiesVertical)
        {
            line.AddEntityIOEvent(inputName: "Disable");
        }

        UpdateButtonTextures(Difficulty);
        AddToDamage(0); // refresh damage sign

        ColorsLadder.AddEntityIOEvent(inputName: "Disable", delay: 0.0f);
        LadderEntities[0].AddEntityIOEvent(inputName: "Disable", delay: 0.0f);
        LadderEntities[1].AddEntityIOEvent(inputName: "Disable", delay: 0.0f);
        LadderEntities[2].AddEntityIOEvent(inputName: "Disable", delay: 0.0f);
        ColorsEndButtonModel.AddEntityIOEvent(inputName: "Skin", value: "1", delay: 0.0f);
        foreach (CDynamicProp instruction in InstructionRightEntities)
        {
            instruction.AddEntityIOEvent(inputName: "Skin", value: "0", delay: 0.0f);
        }
        foreach (CDynamicProp instruction in InstructionMiddleEntities)
        {
            instruction.AddEntityIOEvent(inputName: "Skin", value: "0", delay: 0.0f);
        }
        foreach (CDynamicProp instruction in InstructionLeftEntities)
        {
            instruction.AddEntityIOEvent(inputName: "Skin", value: "0", delay: 0.0f);
        }

        SetDifficulty(Difficulty);
    }

    private void SetInstructionsMiddle(string text)
    {
        foreach (CDynamicProp instructionEntity in InstructionMiddleEntities)
        {
            //instructionEntity.__KeyValueFromString("message", text);
            instructionEntity.AddEntityIOEvent(inputName: "Skin", value: text, activator: instructionEntity, caller: instructionEntity);
        }
        if (text != "0")
        {
            SetInstructionsLeft("0");
            SetInstructionsRight("0");
        }
    }

    private void SetInstructionsLeft(string text)
    {
        foreach (CDynamicProp instructionEntity in InstructionLeftEntities)
        {
            //instructionEntity.__KeyValueFromString("message", text);
            instructionEntity.AddEntityIOEvent(inputName: "Skin", value: text, activator: instructionEntity, caller: instructionEntity);
        }
        if (text != "0")
        {
            SetInstructionsMiddle("0");
        }
    }

    private void SetInstructionsRight(string text)
    {
        foreach (CDynamicProp instructionEntity in InstructionRightEntities)
        {
            //instructionEntity.__KeyValueFromString("message", text);
            instructionEntity.AddEntityIOEvent(inputName: "Skin", value: text, activator: instructionEntity, caller: instructionEntity);
        }
    }

    private List<CCSPlayerController> GetGamePlayers()
    {
        return GetGamePlayersWithLeeway(PlayerWidthHalf);
    }

    private List<CCSPlayerController> GetGamePlayersWithLeeway(float leeway)
    {
        List<CCSPlayerController> gameplayers = [];
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            Vector? origin = pawn.AbsOrigin;
            if (origin != null && origin.X >= GameMin.X - leeway && origin.X <= GameMax.X + leeway &&
                origin.Y >= GameMin.Y - leeway && origin.Y <= GameMax.Y + leeway &&
                origin.Z >= GameMin.Z && origin.Z < GameMax.Z)
            {
                gameplayers.Add(player);
            }
        }
        return gameplayers;
    }

    // Get the union of two sets
    private List<CCSPlayerController> Union(List<CCSPlayerController> A, List<CCSPlayerController> B)
    {
        List<CCSPlayerController> union = [];

        foreach (CCSPlayerController playerA in A)
        {
            union.Add(playerA);
        }
        foreach (CCSPlayerController playerB in B)
        {
            union.Add(playerB);
        }

        return union;
    }

    private void UpdateButtonTextures(int button)
    {
        ButtonModelEntities[button].AddEntityIOEvent(inputName: "Skin", value: "1");
        ButtonModelEntities[(button + 1) % 3].AddEntityIOEvent(inputName: "Skin", value: "0");
        ButtonModelEntities[(button + 2) % 3].AddEntityIOEvent(inputName: "Skin", value: "0");
    }

    public void SetNextDifficulty(int difficulty)
    {
        NextDifficulty = difficulty;
        UpdateButtonTextures(NextDifficulty);
        if (!GameOn)
        {
            SetDifficulty(difficulty);
        }
    }

    private void SetTimer(float timerTime)
    {
        ColorsTimer.AddEntityIOEvent(inputName: "RefireTime", value: (timerTime + 0.1f).ToString().Replace(",", "."), delay: 0.0f);
    }

    private void SetDifficulty(int difficulty)
    {
        Difficulty = difficulty;
        SetTimer(RoundTimes[difficulty] + RoundEndTime);
    }

    private void PunishPlayers(List<CCSPlayerController> gameplayers)
    {
        foreach (CCSPlayerController gameplayer in gameplayers)
        {
            CCSPlayerPawn? pawn = gameplayer.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            pawn.AddEntityIOEvent(inputName: "SetHealth", value: $"{pawn.Health - Damage}");
        }
    }

    private void TeleportPlayers(List<CCSPlayerController> gameplayers)
    {
        Vector gameCenter = new((GameMax.X + GameMin.X) / 2, (GameMax.Y + GameMin.Y) / 2, GameMin.Z + 4.0f);
        foreach (CCSPlayerController gameplayer in gameplayers)
        {
            CCSPlayerPawn? pawn = gameplayer.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            pawn.Teleport(gameCenter, null, Vector.Zero);
        }
    }

    public void StartGame()
    {
        if (GameOn) return;

        GameOn = true;
        GameCount++;
        ColorsTimer.AddEntityIOEvent(inputName: "Enable", value: "0.0", delay: 0.0f);
    }

    private void EndRound()
    {
        if (GameOn)
        {
            SetInstructionsLeft("0");
            SetInstructionsMiddle("0");
            SetInstructionsRight("0");
        }
    }

    public void EndGame()
    {
        if (!GameOn) return;

        GameOn = false;
        RoundNumber = 0;
        ColorsTimer.AddEntityIOEvent(inputName: "Disable", value: "0.0", delay: 0.0f);
        SetInstructionsLeft("0");
        SetInstructionsMiddle("0");
        SetInstructionsRight("0");

        if (LastCommands == null) return;

        foreach (ColorsCommands command in LastCommands)
        {
            switch (command)
            {
                case ColorsCommands.Corners:
                    ChooseCorners(false);
                    break;
                case ColorsCommands.Direction:
                    ChooseRandomDirection(false);
                    break;
                case ColorsCommands.Line:
                    ChooseRandomLinePosition(false);
                    break;
                case ColorsCommands.Circle:
                    ChooseRandomCirclePosition(false);
                    break;
                case ColorsCommands.Side:
                    ChooseRandomSide(false);
                    break;
                case ColorsCommands.Colors:
                    ChooseRandomColors(0);
                    break;
                case ColorsCommands.Jump:
                    SetJump(false);
                    break;
                case ColorsCommands.Crouch:
                    SetCrouch(false);
                    break;
                case ColorsCommands.Ladders:
                    SetLadders(false);
                    TeleportPlayers(GetGamePlayersWithLeeway(96.0f));
                    break;
                case ColorsCommands.Walk:
                    SetMovement(false);
                    break;
                case ColorsCommands.Run:
                    SetMovement(false);
                    break;
                default:
                    debugprint(" Selected command out of bounds in EndGame: " + command);
                    break;
            }
        }
        LastCommands = null;
    }

    public void ChooseRandomCommand()
    {
        float roundTime = RoundTimes[Difficulty];
        PickedColors.Clear();
        if (NextDifficulty != Difficulty)
        {
            SetDifficulty(NextDifficulty);
            RoundNumber = 0;
            return;
        }
        if (RoundNumber >= RoundsPerDifficulty && Auto && Difficulty < 2)
        {
            debugprint("Setting Difficulty to " + (Difficulty + 1));
            RoundNumber = 0;
            SetDifficulty(Difficulty + 1);
            NextDifficulty = Difficulty;
            UpdateButtonTextures(Difficulty);
            return;
        }
        if (Difficulty == 2)
        {
            roundTime = RoundTimes[Difficulty] - (RoundNumber * 0.1f);
            if (roundTime < MinRoundTime)
            {
                roundTime = MinRoundTime;
            }
            else
            {
                SetTimer(roundTime + RoundEndTime);
            }
        }
        debugprint("Now on RoundNumber " + RoundNumber + " where roundTime = " + roundTime);

        if (Difficulty == 0)
        {
            ColorsCommands command = (ColorsCommands)RandomInt((int)ColorsCommands.Corners, 10);
            LastCommands = [command];
            switch (command)
            {
                case ColorsCommands.Corners:
                    ChooseCorners(true);
                    SetInstructionsMiddle(CommandToTexture[ColorsCommands.Corners]);
                    DisplayTextToGamePlayers("Go to the corners");
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersNotOnColors(GameCount))));
                    break;
                case ColorsCommands.Side:
                    ChooseRandomSide(true);
                    SetInstructionsMiddle(GoDirectionToTexture[Side]);
                    DisplayTextToGamePlayers(Sides[Side]);
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersNotOnColors(GameCount))));
                    break;
                case ColorsCommands.Colors:
                    ChooseRandomColors(2);
                    string colorString = "Go to ";
                    bool first = true;
                    foreach (KeyValuePair<int, bool> color in PickedColors)
                    {
                        if (first)
                        {
                            first = false;
                            SetInstructionsLeft(ColorToTexture[color.Key]);
                            colorString += ColorNames[color.Key];
                        }
                        else
                        {
                            SetInstructionsRight(ColorToTexture[color.Key]);
                            colorString += " OR " + ColorNames[color.Key];
                        }
                    }
                    DisplayTextToGamePlayers(colorString);
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersNotOnColors(GameCount))));
                    break;
                case ColorsCommands.Direction:
                    ChooseRandomDirection(true);
                    SetInstructionsMiddle(FaceDirectionToTexture[Direction]);
                    DisplayTextToGamePlayers(Directions[Direction]);
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersNotFacingDirection(GameCount))));
                    break;
                case ColorsCommands.Line:
                    ChooseRandomLinePosition(true);
                    SetInstructionsMiddle(CommandToTexture[ColorsCommands.Line]);
                    DisplayTextToGamePlayers("Go to the line");
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersNotOnLine(GameCount))));
                    break;
                case ColorsCommands.Circle:
                    ChooseRandomCirclePosition(true);
                    SetInstructionsMiddle(CommandToTexture[ColorsCommands.Circle]);
                    DisplayTextToGamePlayers("Go to the circle");
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersNotInCircle(GameCount))));
                    break;
                case ColorsCommands.Jump:
                    SetJump(true);
                    SetInstructionsMiddle(CommandToTexture[ColorsCommands.Jump]);
                    DisplayTextToGamePlayers("Jump");
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersWhoDidNotJump(GameCount))));
                    break;
                case ColorsCommands.Crouch:
                    SetCrouch(true);
                    SetInstructionsMiddle(CommandToTexture[ColorsCommands.Crouch]);
                    DisplayTextToGamePlayers("Crouch");
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersWhoDidNotCrouch(GameCount))));
                    break;
                case ColorsCommands.Ladders:
                    SetLadders(true);
                    SetInstructionsMiddle(CommandToTexture[ColorsCommands.Ladders]);
                    DisplayTextToGamePlayers("Go up a ladder");
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersNotOnLadders(GameCount))));
                    break;
                case ColorsCommands.Walk:
                    ExpectedPlayerSpeed = WalkingSpeedSquared;
                    SetInstructionsMiddle(CommandToTexture[ColorsCommands.Walk]);
                    DisplayTextToGamePlayers("Shift walk around");
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersNotAtSpeed(GameCount))));
                    break;
                case ColorsCommands.Run:
                    ExpectedPlayerSpeed = RunningSpeedSquared;
                    SetInstructionsMiddle(CommandToTexture[ColorsCommands.Run]);
                    DisplayTextToGamePlayers("Run around");
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersNotAtSpeed(GameCount))));
                    break;
                default:
                    debugprint("Selected command out of bounds in ChooseRandomCommand: " + command);
                    break;
            }
        }
        else if (Difficulty == 1)
        {
            ColorsCommands command = (ColorsCommands)RandomInt((int)ColorsCommands.Colors, 8);
            switch (command)
            {
                case ColorsCommands.Colors:
                    LastCommands = [ColorsCommands.Colors];
                    ChooseRandomColors(1);
                    foreach (KeyValuePair<int, bool> color in PickedColors)
                    {
                        SetInstructionsMiddle(ColorToTexture[color.Key]);
                        DisplayTextToGamePlayers("Go to " + ColorNames[color.Key]);
                    }
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersNotOnColors(GameCount))));
                    break;
                case ColorsCommands.Direction:
                    LastCommands = [ColorsCommands.Direction, ColorsCommands.Side];
                    ChooseRandomDirection(true);
                    ChooseRandomSide(true);
                    SetInstructionsLeft(GoDirectionToTexture[Side]);
                    SetInstructionsRight(FaceDirectionToTexture[Direction]);
                    DisplayTextToGamePlayers(Sides[Side] + " AND " + Directions[Direction]);
                    Timers.Add(new(roundTime, () => PunishPlayers(Union(GetPlayersNotFacingDirection(GameCount), GetPlayersNotOnColors(GameCount)))));
                    break;
                case ColorsCommands.Line:
                    LastCommands = [ColorsCommands.Line];
                    ChooseRandomLinePosition(true);
                    SetInstructionsMiddle(CommandToTexture[ColorsCommands.Line]);
                    DisplayTextToGamePlayers("Go to the line");
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersNotOnLine(GameCount))));
                    break;
                case ColorsCommands.Circle:
                    LastCommands = [ColorsCommands.Circle];
                    ChooseRandomCirclePosition(true);
                    SetInstructionsMiddle(CommandToTexture[ColorsCommands.Circle]);
                    DisplayTextToGamePlayers("Go to the circle");
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersNotInCircle(GameCount))));
                    break;
                case ColorsCommands.Jump:
                    LastCommands = [ColorsCommands.Jump, ColorsCommands.Side];
                    SetJump(true);
                    ChooseRandomSide(true);
                    SetInstructionsLeft(GoDirectionToTexture[Side]);
                    SetInstructionsRight(CommandToTexture[ColorsCommands.Jump]);
                    DisplayTextToGamePlayers(Sides[Side] + " AND Jump");
                    Timers.Add(new(roundTime, () => PunishPlayers(Union(GetPlayersWhoDidNotJump(GameCount), GetPlayersNotOnColors(GameCount)))));
                    break;
                case ColorsCommands.Crouch:
                    LastCommands = [ColorsCommands.Crouch, ColorsCommands.Side];
                    SetCrouch(true);
                    ChooseRandomSide(true);
                    SetInstructionsLeft(GoDirectionToTexture[Side]);
                    SetInstructionsRight(CommandToTexture[ColorsCommands.Crouch]);
                    DisplayTextToGamePlayers(Sides[Side] + " AND Crouch");
                    Timers.Add(new(roundTime, () => PunishPlayers(Union(GetPlayersWhoDidNotCrouch(GameCount), GetPlayersNotOnColors(GameCount)))));
                    break;
                case ColorsCommands.Ladders:
                    LastCommands = [ColorsCommands.Ladders];
                    SetLadders(true);
                    SetInstructionsMiddle(CommandToTexture[ColorsCommands.Ladders]);
                    DisplayTextToGamePlayers("Go up a ladder");
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersNotOnLadders(GameCount))));
                    break;
                default:
                    debugprint("Selected command out of bounds in ChooseRandomCommand: " + command);
                    break;
            }
        }
        else if (Difficulty == 2)
        {
            ColorsCommands command = (ColorsCommands)RandomInt((int)ColorsCommands.Direction, 8);
            switch (command)
            {
                case ColorsCommands.Direction:
                    LastCommands = [ColorsCommands.Direction, ColorsCommands.Colors];
                    ChooseRandomColors(1);
                    ChooseRandomDirection(true);
                    foreach (KeyValuePair<int, bool> color in PickedColors)
                    {
                        SetInstructionsLeft(ColorToTexture[color.Key]);
                        DisplayTextToGamePlayers("Go to " + ColorNames[color.Key] + " AND " + Directions[Direction]);
                    }
                    SetInstructionsRight(FaceDirectionToTexture[Direction]);
                    Timers.Add(new(roundTime, () => PunishPlayers(Union(GetPlayersNotFacingDirection(GameCount), GetPlayersNotOnColors(GameCount)))));
                    break;
                case ColorsCommands.Line:
                    LastCommands = [ColorsCommands.Line];
                    ChooseRandomLinePosition(true);
                    SetInstructionsMiddle(CommandToTexture[ColorsCommands.Line]);
                    DisplayTextToGamePlayers("Go to the line");
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersNotOnLine(GameCount))));
                    break;
                case ColorsCommands.Circle:
                    LastCommands = [ColorsCommands.Circle];
                    ChooseRandomCirclePosition(true);
                    SetInstructionsMiddle(CommandToTexture[ColorsCommands.Circle]);
                    DisplayTextToGamePlayers("Go to the circle");
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersNotInCircle(GameCount))));
                    break;
                case ColorsCommands.Jump:
                    LastCommands = [ColorsCommands.Jump, ColorsCommands.Colors];
                    SetJump(true);
                    ChooseRandomColors(1);
                    foreach (KeyValuePair<int, bool> color in PickedColors)
                    {
                        SetInstructionsLeft(ColorToTexture[color.Key]);
                        DisplayTextToGamePlayers("Go to " + ColorNames[color.Key] + " AND Jump");
                    }
                    SetInstructionsRight(CommandToTexture[ColorsCommands.Jump]);
                    Timers.Add(new(roundTime, () => PunishPlayers(Union(GetPlayersWhoDidNotJump(GameCount), GetPlayersNotOnColors(GameCount)))));
                    break;
                case ColorsCommands.Crouch:
                    LastCommands = [ColorsCommands.Crouch, ColorsCommands.Colors];
                    SetCrouch(true);
                    ChooseRandomColors(1);
                    foreach (KeyValuePair<int, bool> color in PickedColors)
                    {
                        SetInstructionsLeft(ColorToTexture[color.Key]);
                        DisplayTextToGamePlayers("Go to " + ColorNames[color.Key] + " AND Crouch");
                    }
                    SetInstructionsRight(CommandToTexture[ColorsCommands.Crouch]);
                    Timers.Add(new(roundTime, () => PunishPlayers(Union(GetPlayersWhoDidNotCrouch(GameCount), GetPlayersNotOnColors(GameCount)))));
                    break;
                case ColorsCommands.Ladders:
                    LastCommands = [ColorsCommands.Ladders];
                    SetLadders(true);
                    SetInstructionsMiddle(CommandToTexture[ColorsCommands.Ladders]);
                    DisplayTextToGamePlayers("Go up a ladder");
                    Timers.Add(new(roundTime, () => PunishPlayers(GetPlayersNotOnLadders(GameCount))));
                    break;
                default:
                    debugprint("Selected command out of bounds in ChooseRandomCommand: " + command);
                    break;
            }
        }

        RoundNumber++;
    }

    private void ToggleAuto()
    {
        Auto = !Auto;
    }

    public void SetAuto(bool newAuto)
    {
        Auto = newAuto;
    }

    public void AddToDamage(int toAdd)
    {
        Damage += toAdd;
        if (Damage > MaxDamage) Damage = MaxDamage;
        if (Damage < MinDamage) Damage = MinDamage;

        ColorsDamageSignModel.AddEntityIOEvent(inputName: "Skin", value: $"{DamageToTexture[Damage]}");
    }

    private void ChooseRandomDirection(bool enabled)
    {
        if (enabled)
        {
            Direction = RandomInt(0, 3);
        }
        else
        {
            EndRound();
        }
    }

    private List<CCSPlayerController> GetPlayersNotFacingDirection(int startingGameCount)
    {
        List<CCSPlayerController> failedPlayers = [];
        if (!GameOn || GameCount != startingGameCount) return failedPlayers;

        List<CCSPlayerController> gameplayers = GetGamePlayers();

        foreach (CCSPlayerController gameplayer in gameplayers)
        {
            CCSPlayerPawn? pawn = gameplayer.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            bool correctDirection = false;
            QAngle angles = pawn.EyeAngles;

            if (angles.Y <= 100 && angles.Y >= 80 && Direction == 0)
            {
                debugprint("Facing North");
                correctDirection = true;
            }
            else if (angles.Y >= -100 && angles.Y <= -80 && Direction == 2)
            {
                debugprint("Facing South");
                correctDirection = true;
            }
            else if (angles.Y >= -10 && angles.Y <= 10 && Direction == 1)
            {
                debugprint("Facing East");
                correctDirection = true;
            }
            else if ((angles.Y <= -170 || angles.Y >= 170) && Direction == 3)
            {
                debugprint("Facing West");
                correctDirection = true;
            }

            if (!correctDirection)
            {
                failedPlayers.Add(gameplayer);
            }
        }

        Timers.Add(new(RoundEndTime, () => ChooseRandomDirection(false)));
        return failedPlayers;
    }

    private void ChooseRandomLinePosition(bool enabled)
    {
        if (enabled)
        {
            LineHorizontal = RandomInt(0, 1);
            if (LineHorizontal == 0)
            {
                LinePosition = new Vector(GameMin.X, RandomFloat(GameMin.Y, GameMax.Y - LinesWidth[Difficulty]), GameMin.Z + 1.0f);
            }
            else
            {
                LinePosition = new Vector(RandomFloat(GameMin.X, GameMax.X - LinesWidth[Difficulty]), GameMin.Y, GameMin.Z + 1.0f);
            }
            debugprint("LineHorizontal: " + LineHorizontal);
            debugprint("LinePosition: " + LinePosition);
            debugprint("LineEntities[LineHorizontal][Difficulty]: " + LineEntities[LineHorizontal][Difficulty]);
            LineEntities[LineHorizontal][Difficulty].Teleport(LinePosition);
            LineEntities[LineHorizontal][Difficulty].AddEntityIOEvent(inputName: "Enable", caller: LineEntities[LineHorizontal][Difficulty]);
        }
        else
        {
            LineEntities[LineHorizontal][Difficulty].AddEntityIOEvent(inputName: "Disable", caller: LineEntities[LineHorizontal][Difficulty]);
            EndRound();
        }
    }

    private List<CCSPlayerController> GetPlayersNotOnLine(int startingGameCount)
    {
        List<CCSPlayerController> failedPlayers = [];
        if (!GameOn || GameCount != startingGameCount) return failedPlayers;

        List<CCSPlayerController> gameplayers = GetGamePlayers();
        bool isLineHorizontal = LineHorizontal == 0;

        foreach (CCSPlayerController gameplayer in gameplayers)
        {
            CCSPlayerPawn? pawn = gameplayer.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            Vector playerOrigin = pawn.AbsOrigin!;
            debugprint("x bounds: " + (LinePosition.X - PlayerWidthHalf) + " to " + (LinePosition.X + (isLineHorizontal ? ColorsWidth : LinesWidth[Difficulty]) + PlayerWidthHalf));
            debugprint("y bounds: " + (LinePosition.Y - PlayerWidthHalf) + " to " + (LinePosition.Y + (isLineHorizontal ? LinesWidth[Difficulty] : ColorsWidth) + PlayerWidthHalf));
            if (playerOrigin.X < LinePosition.X - PlayerWidthHalf ||
                playerOrigin.X > LinePosition.X + (isLineHorizontal ? ColorsWidth : LinesWidth[Difficulty]) + PlayerWidthHalf ||
                playerOrigin.Y < LinePosition.Y - PlayerWidthHalf ||
                playerOrigin.Y > LinePosition.Y + (isLineHorizontal ? LinesWidth[Difficulty] : ColorsWidth) + PlayerWidthHalf)
            {
                failedPlayers.Add(gameplayer);
                debugprint("Player not on line: " + gameplayer.PlayerName);
            }
            else
                debugprint("Player on line: " + gameplayer.PlayerName);
        }

        Timers.Add(new(RoundEndTime, () => ChooseRandomLinePosition(false)));
        return failedPlayers;
    }

    private void ChooseRandomCirclePosition(bool enabled)
    {
        if (enabled)
        {
            float radius = CircleRadius[Difficulty];
            CirclePosition = new Vector(RandomFloat(GameMin.X + (float)radius, GameMax.X - (float)radius), RandomFloat(GameMin.Y + (float)radius, GameMax.Y - (float)radius), GameMin.Z + 1.0f);
            debugprint("CirclePosition: " + CirclePosition);
            CircleEntities[Difficulty].Teleport(CirclePosition);
            CircleEntities[Difficulty].AddEntityIOEvent(inputName: "Enable", caller: CircleEntities[Difficulty]);
        }
        else
        {
            CircleEntities[Difficulty].AddEntityIOEvent(inputName: "Disable", caller: CircleEntities[Difficulty]);
            EndRound();
        }
    }

    private List<CCSPlayerController> GetPlayersNotInCircle(int startingGameCount)
    {
        List<CCSPlayerController> failedPlayers = [];
        if (!GameOn || GameCount != startingGameCount) return failedPlayers;

        List<CCSPlayerController> gameplayers = GetGamePlayers();

        float radiusSquared = CircleRadius[Difficulty] * CircleRadius[Difficulty];
        foreach (CCSPlayerController gameplayer in gameplayers)
        {
            CCSPlayerPawn? pawn = gameplayer.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            Vector vectorToCenter = pawn.AbsOrigin! - CirclePosition;
            if (vectorToCenter.VecLength2DSqr() > radiusSquared)
            {
                failedPlayers.Add(gameplayer);
                debugprint("Player not in circle: " + gameplayer.PlayerName);
            }
            else
                debugprint("Player in circle: " + gameplayer.PlayerName);
        }

        Timers.Add(new(RoundEndTime, () => ChooseRandomCirclePosition(false)));
        return failedPlayers;
    }

    private void ChooseRandomSide(bool enabled)
    {
        if (enabled)
        {
            Side = RandomInt(0, 3);
            PickedColors = SideColors[Side];
        }
        else
        {
            PickedColors.Clear();
            EndRound();
        }
    }

    private void ChooseRandomColors(int quantity)
    {
        if (quantity == 0)
        {
            PickedColors.Clear();
            EndRound();
            return;
        }
        debugprint(ColorsMin[0].ToString());
        PickedColors.Clear();
        List<int> colorOptions = [0, 1, 2, 3, 4, 5, 6, 7, 8];
        ColorString = "";
        for (int i = 0; i < quantity; i++)
        {
            int colorIndex = RandomInt(0, colorOptions.Count - 1);
            PickedColors[colorOptions[colorIndex]] = true;
            debugprint("Color chosen: " + ColorNames[colorOptions[colorIndex]] + " (" + colorIndex + ")");
            ColorString += ColorNames[colorOptions[colorIndex]] + ((i == quantity - 1) ? "" : " or ");
            colorOptions.RemoveAt(colorIndex);
        }
    }

    private void ChooseCorners(bool enabled)
    {
        if (!enabled)
        {
            PickedColors.Clear();
            EndRound();
            return;
        }

        PickedColors = new Dictionary<int, bool>
        {
            { 0, true },
            { 2, true },
            { 6, true },
            { 8, true }
        };
    }

    private bool IsPlayerOnColors(CCSPlayerController gameplayer)
    {
        if (PickedColors.Count == 0) return true;

        CCSPlayerPawn? pawn = gameplayer.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return false;

        Vector playerOrigin = pawn.AbsOrigin!;
        foreach (KeyValuePair<int, bool> color in PickedColors)
        {
            if (playerOrigin.X >= ColorsMin[color.Key].X - PlayerWidthHalf &&
                playerOrigin.X <= ColorsMax[color.Key].X + PlayerWidthHalf &&
                playerOrigin.Y >= ColorsMin[color.Key].Y - PlayerWidthHalf &&
                playerOrigin.Y <= ColorsMax[color.Key].Y + PlayerWidthHalf)
            {
                debugprint("Player on color " + ColorNames[color.Key] + ": " + gameplayer.PlayerName);
                return true;
            }
            else
            {
                debugprint("Player not on color " + ColorNames[color.Key] + ": " + gameplayer.PlayerName);
            }
        }

        return false;
    }

    private List<CCSPlayerController> GetPlayersNotOnColors(int startingGameCount)
    {
        List<CCSPlayerController> failedPlayers = [];
        if (!GameOn || GameCount != startingGameCount) return failedPlayers;

        List<CCSPlayerController> gameplayers = GetGamePlayers();

        foreach (CCSPlayerController gameplayer in gameplayers)
        {
            if (!IsPlayerOnColors(gameplayer))
            {
                failedPlayers.Add(gameplayer);
            }
        }

        PickedColors.Clear();

        Timers.Add(new(RoundEndTime, EndRound));
        return failedPlayers;
    }

    private void SetJump(bool enabled)
    {
        if (enabled)
        {
            ColorsJumpTrigger.AddEntityIOEvent(inputName: "Enable", delay: 0.0f);
        }
        else
        {
            EndRound();
            ColorsJumpTrigger.AddEntityIOEvent(inputName: "Disable", delay: 0.0f);
            foreach (CCSPlayerController gameplayer in FrozenPlayers)
            {
                CCSPlayerPawn? pawn = gameplayer.PlayerPawn.Value;
                if (pawn != null && pawn.IsValid && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                {
                    pawn.MoveType = MoveType_t.MOVETYPE_WALK;
                    pawn.ActualMoveType = MoveType_t.MOVETYPE_WALK;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
                }
            }
            FrozenPlayers.Clear();
        }
    }

    public void PlayerJumped(CEntityInstance activator)
    {
        CCSPlayerPawn pawn = activator.As<CCSPlayerPawn>();
        CCSPlayerController? player = pawn.OriginalController.Value;
        if (player == null || !player.IsValid || !IsPlayerOnColors(player)) return;

        FrozenPlayers.Add(player);
        pawn.MoveType = MoveType_t.MOVETYPE_OBSOLETE;
        pawn.ActualMoveType = MoveType_t.MOVETYPE_OBSOLETE;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
    }

    private List<CCSPlayerController> GetPlayersWhoDidNotJump(int startingGameCount)
    {
        List<CCSPlayerController> failedPlayers = [];
        if (!GameOn || GameCount != startingGameCount) return failedPlayers;

        List<CCSPlayerController> gameplayers = GetGamePlayers();

        foreach (CCSPlayerController gameplayer in gameplayers)
        {
            CCSPlayerPawn? pawn = gameplayer.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            Vector playerOrigin = pawn.AbsOrigin!;
            if (playerOrigin.Z < GameMin.Z + 5)
            {
                failedPlayers.Add(gameplayer);
                debugprint("Player did not jump: " + gameplayer.PlayerName);
            }
            else
                debugprint("Player jumped: " + gameplayer.PlayerName);
        }

        Timers.Add(new(RoundEndTime, () => SetJump(false)));
        return failedPlayers;
    }

    private void SetCrouch(bool enabled)
    {
        if (!enabled)
        {
            EndRound();
        }
    }

    private List<CCSPlayerController> GetPlayersWhoDidNotCrouch(int startingGameCount)
    {
        List<CCSPlayerController> failedPlayers = [];
        if (!GameOn || GameCount != startingGameCount) return failedPlayers;

        List<CCSPlayerController> gameplayers = GetGamePlayers();

        foreach (CCSPlayerController gameplayer in gameplayers)
        {
            CCSPlayerPawn? pawn = gameplayer.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            if (pawn.Collision.Maxs.Z > PlayerHeightCrouched + 1.0)
            {
                failedPlayers.Add(gameplayer);
                debugprint("Player did not crouch: " + gameplayer.PlayerName);
            }
            else
                debugprint("Player crouched: " + gameplayer.PlayerName);
        }

        Timers.Add(new(RoundEndTime, () => SetCrouch(false)));
        return failedPlayers;
    }

    private void SetLadders(bool enabled)
    {
        if (enabled)
        {
            LadderEntities[Difficulty].AddEntityIOEvent(inputName: "Enable", delay: 0.0f);
            ColorsLadder.AddEntityIOEvent(inputName: "Enable", delay: 0.0f);
        }
        else
        {
            LadderEntities[Difficulty].AddEntityIOEvent(inputName: "Disable", delay: 0.0f);
            ColorsLadder.AddEntityIOEvent(inputName: "Disable", delay: 0.0f);
            EndRound();
        }
    }

    private List<CCSPlayerController> GetPlayersNotOnLadders(int startingGameCount)
    {
        List<CCSPlayerController> failedPlayers = [];
        if (!GameOn || GameCount != startingGameCount) return failedPlayers;

        List<CCSPlayerController> gameplayers = GetGamePlayers();

        float zMin = GameMin.Z + PlayerJumpHeight + 1.0f;
        foreach (CCSPlayerController gameplayer in gameplayers)
        {
            CCSPlayerPawn? pawn = gameplayer.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            Vector playerOrigin = pawn.AbsOrigin!;
            if (playerOrigin.Z < zMin)
            {
                failedPlayers.Add(gameplayer);
                debugprint("Player not on ladder: " + gameplayer.PlayerName);
            }
            else
                debugprint("Player on ladder: " + gameplayer.PlayerName);
        }

        Timers.Add(new(RoundEndTime, () => SetLadders(false)));
        return failedPlayers;
    }

    private void SetMovement(bool enabled)
    {
        if (enabled)
        {

        }
        else
        {
            ExpectedPlayerSpeed = 0;
            EndRound();
        }
    }

    private List<CCSPlayerController> GetPlayersNotAtSpeed(int startingGameCount)
    {
        List<CCSPlayerController> failedPlayers = [];
        if (!GameOn || GameCount != startingGameCount) return failedPlayers;

        List<CCSPlayerController> gameplayers = GetGamePlayers();
        int minSpeed = 10000;

        foreach (CCSPlayerController gameplayer in gameplayers)
        {
            CCSPlayerPawn? pawn = gameplayer.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            float playerSpeed = pawn.AbsVelocity.VecLength2DSqr();
            bool isWalking = gameplayer.Buttons.HasFlag(PlayerButtons.Speed);
            if (playerSpeed < minSpeed || (ExpectedPlayerSpeed == WalkingSpeedSquared && !isWalking))
            {
                failedPlayers.Add(gameplayer);
                debugprint("Player not going proper speed: " + gameplayer.PlayerName + "(" + playerSpeed.ToString() + " vs " + ExpectedPlayerSpeed.ToString() + ")");
            }
            else
                debugprint("Player going proper speed: " + gameplayer.PlayerName + "(" + playerSpeed.ToString() + " vs " + ExpectedPlayerSpeed.ToString() + ")");
        }

        Timers.Add(new(RoundEndTime, () => SetMovement(false)));
        return failedPlayers;
    }

    public void DisplayHelpText()
    {
        if (GameOn) return;

        DisplayTextToGamePlayers("1. Follow the instruction(s) shown.<br>2. If two instructions are shown, you must complete both.<br>3. If you stop completing the instruction before it<br>disappears, you will take damage.");
    }

    private void DisplayTextToGamePlayers(string text)
    {
        List<CCSPlayerController> gameplayers = GetGamePlayers();
        foreach (CCSPlayerController gameplayer in gameplayers)
        {
            SetMessage(gameplayer.Slot, text, 4.0f);
        }
    }

    public override void Remove()
    {
        foreach (Timer timer in Timers) timer?.Kill();
        
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
    }
}