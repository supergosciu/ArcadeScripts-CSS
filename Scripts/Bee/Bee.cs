using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

using RayTraceAPI;

using static ArcadeScripts.Random;
using static ArcadeScripts.TextDisplayHelper;

namespace ArcadeScripts.Scripts;

public partial class Bee : ScriptBase
{
    public Bee(CLogicScript owner) : base(owner)
    {
        Functions.Add("AddToTime", new ScriptFunction<int>(AddToTime));
        Functions.Add("SetGame", new ScriptFunction<int>(SetGame));
        Functions.Add("AddToDamage", new ScriptFunction<int>(AddToDamage));
        Functions.Add("ClearAllGameTexts", new ScriptFunction(ClearAllGameTexts));
        Functions.Add("PressButton", new ScriptFunction<CEntityInstance, int>(PressButton));
        Functions.Add("DisplayHelpText", new ScriptFunction(DisplayHelpText));
        Functions.Add("ChooseNewWordOrProblem", new ScriptFunction<int>(ChooseNewWordOrProblem));
        Functions.Add("StartTimer", new ScriptFunction(StartTimer));
        Functions.Add("PauseTimer", new ScriptFunction(PauseTimer));

        EntityNames = ["bee_keyboard_model", "bee_keyboard", "bee_origin", "bee_measure", "bee_input", "bee_currentdefinition", "bee_hurt", "bee_damage_sign_model", "bee_starttimer_model", "bee_pausetimer_model", "bee_randomword_easy_model", "bee_randomword_medium_model", "bee_randomword_hard_model", "bee_spell_model", "bee_math_model", "bee_timer", "bee_currentword", "bee_start_sound"];
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;

        ClearInputs();
        Timers.Add(new(0.5f, Setup));
    }

    private int CurrentGame = 0;
    public const int GAME_SPELLING = 0;
    public const int GAME_MATH = 1;
    private int TimeLeft = 0;
    private int GameNumber = 0;
    private bool TimerActive = false;
    private bool GameStarted = false;

    private int Damage = 0;
    private const int MinDamage = 0;
    private const int MaxDamage = 100;
    private const int MinTime = 5;
    private const int MaxTime = 40;

    private const int MaxWordSize = 15;
    private const int MaxNumberSize = 4;
    private const int MaxDefinitionLineSize = 52;
    private const int MaxDefinitionLines = 5;
    private const int NumBooths = 16;
    private const float KeyboardHeight = 64.0f;
    private const float KeyboardWidth = 160.0f;
    private const float KeySize = 16.0f;

    private Dictionary<int, Vector> KeyboardLocation = []; // Top left vertex of each keyboard
    private static string[] KeyboardLineOne = ["q", "w", "e", "r", "t", "y", "u", "i", "o", "p"];
    private static string[] KeyboardLineTwo = ["a", "s", "d", "f", "g", "h", "j", "k", "l", ""];
    private static string[] KeyboardLineThree = ["z", "x", "c", "v", "b", "n", "m", "", "del", "del"];
    private static string[] KeyboardLineFour = ["-1", " ", " ", " ", " ", " ", " ", "", "", ""];
    private static string[][] KeyboardLines = [KeyboardLineOne, KeyboardLineTwo, KeyboardLineThree, KeyboardLineFour];
    private static Dictionary<int, int> DamageToTexture = new() { { 0, 0 }, { 5, 1 }, { 10, 2 }, { 15, 3 }, { 20, 4 }, { 25, 5 }, { 50, 6 }, { 75, 7 }, { 100, 8 } };

    private static Vector BoothAreaMin = new(3807.0f, 865.0f, -511.0f);
    private static Vector BoothAreaMax = new(3935.0f, 2400.0f, -215.0f);
    private static Vector ControlsAreaMin = new(3392.0f, 1504.0f, -161.0f);
    private static Vector ControlsAreaMax = new(3518.0f, 1728.0f, 32.0f);

    private string SpellWord = "";
    private Dictionary<int, string> BoothMode = [];
    private Dictionary<int, string> BoothInput = [];
    private Dictionary<int, Dictionary<string, string>> Words = [];
    private static int[] NumWords = [450, 450, 450];
    private static int[] TimePerDifficulty = [15, 20, 25];

    private static string[] NumpadLineOne = ["", "", "", "7", "8", "9", "", "", "-2", "-2"]; //mode
    private static string[] NumpadLineTwo = ["", "", "", "4", "5", "6", "", "", "", ""];
    private static string[] NumpadLineThree = ["", "", "", "1", "2", "3", "", "", "0", "0"]; //delete
    private static string[] NumpadLineFour = ["-1", "", "", "", "0", "", "", "", "", ""]; //clear
    private static string[][] NumpadLines = [NumpadLineOne, NumpadLineTwo, NumpadLineThree, NumpadLineFour];

    private bool DEBUG = false;
    private void debugprint(string text)
    {
        if (!DEBUG) return;
        Console.WriteLine($"************* {text}");
    }

    private void Setup()
    {
        Damage = 0;
        DamageSignModel.AddEntityIOEvent(inputName: "Skin", value: $"{DamageToTexture[Damage]}");
        CurrentGameButtonModels[CurrentGame].AddEntityIOEvent(inputName: "Skin", value: "1");
    }

    public void SetGame(int game)
    {
        if (game == CurrentGame) return;

        TimerActive = false;
        GameStarted = false;
        CurrentProblem = "";
        CurrentAnswer = "";
        SpellWord = "";

        CurrentGameButtonModels[CurrentGame].AddEntityIOEvent(inputName: "Skin", value: "0");
        CurrentGameButtonModels[game].AddEntityIOEvent(inputName: "Skin", value: "1");

        CurrentGame = game;

        DisplayCurrentTargetWord("");
        DisplayDefinition(new Dictionary<int, string> { { 0, "" }, { 1, "" }, { 2, "" }, { 3, "" }, { 4, "" } });

        for (int i = 0 ; i < 16 ; i++)
        {
            KeyboardButtonModels[i].AddEntityIOEvent(inputName: "Skin", value: $"{game}");
        }
        StartButtonModel.AddEntityIOEvent(inputName: "Skin", value: "0");
        PauseButtonModel.AddEntityIOEvent(inputName: "Skin", value: "0");
        EasyButtonModel.AddEntityIOEvent(inputName: "Skin", value: "0");
        MediumButtonModel.AddEntityIOEvent(inputName: "Skin", value: "0");
        HardButtonModel.AddEntityIOEvent(inputName: "Skin", value: "0");
    }

    public void ChooseNewWordOrProblem(int difficulty)
    {
        if (CurrentGame == GAME_SPELLING)
        {
            ChooseNewWord(difficulty);
        }
        else
        {
            ChooseNewProblem(difficulty);
        }
        StartButtonModel.AddEntityIOEvent(inputName: "Skin", value: "0");
        PauseButtonModel.AddEntityIOEvent(inputName: "Skin", value: "0");
    }

    public static TraceResult? GetEyePosition(CCSPlayerPawn pawn)
    {
        CRayTraceInterface? rayTrace = ArcadeScripts.RayTraceInterface.Get();
        if (rayTrace == null)
            return null;
        Vector absOrigin = pawn.AbsOrigin!;
        CNetworkViewOffsetVector viewOffset = pawn.ViewOffset;
        Vector eyePosition = new(absOrigin.X + viewOffset.X, absOrigin.Y + viewOffset.Y, absOrigin.Z + viewOffset.Z);
        ulong mask = pawn.Collision.CollisionAttribute.InteractsWith;
        ulong contents = pawn.Collision.CollisionGroup;
        QAngle eyeAngles = pawn.EyeAngles;
        TraceOptions options = new()
        {
            InteractsWith = mask
        };

        rayTrace.TraceShape(eyePosition, eyeAngles, pawn, options, out TraceResult result);
        return result;
    }

    public void PressButton(CEntityInstance? activator, int booth)
    {
        //Set up keyboard location if first press
        if (!KeyboardLocation.TryGetValue(booth, out Vector? value))
        {
            CBaseButton keyboard = KeyboardButtons[booth - 1];
            Vector keyboardOrigin = keyboard.AbsOrigin!;
            debugprint("Found keyboard at " + keyboardOrigin + "(corner at " + new Vector(keyboardOrigin.X, keyboardOrigin.Y + KeyboardWidth/2, keyboardOrigin.Z + KeyboardWidth/2)+")");
            value = new Vector(keyboardOrigin.X, keyboardOrigin.Y + KeyboardWidth / 2, keyboardOrigin.Z + KeyboardHeight / 2);
            KeyboardLocation[booth] = value;
        }

        if (activator == null) return;

        if (activator.Entity?.Name != $"bee_player_{booth}")
        {
            activator.AddEntityIOEvent(inputName: "KeyValue", value: $"targetname bee_player_{booth}");
        }
        MeasureEntities[booth - 1].AddEntityIOEvent(inputName: "SetMeasureTarget", value: $"bee_player_{booth}", delay: 0.01f, activator: activator);

        //Cast a ray from the player's eyes in the direction they are looking
        TraceResult? Hit = GetEyePosition(activator.As<CCSPlayerPawn>());

        //Get the key corresponding to the hit
        int column = (int)((value.Y - Hit!.Value.EndPos.Y) / KeySize);
        int row = (int)((value.Z - Hit.Value.EndPos.Z) / KeySize);
        if (column < 0) column = 0;
        if (column > 9) column = 9;
        if (row < 0) row = 0;
        if (row > 3) row = 3;
        debugprint("Row " + row + " column " + column +  " key " + KeyboardLines[row][column]);

        if (!BoothInput.TryGetValue(booth, out string? input))
        {
            input = "";
            BoothInput[booth] = input;
        }
        if (!BoothMode.ContainsKey(booth))
        {
            BoothMode[booth] = "L";
        }

        string key = (CurrentGame == GAME_SPELLING) ? KeyboardLines[row][column] : NumpadLines[row][column];

        if (key == "del")
        {
            //Special case: delete last char
            if (BoothInput[booth].Length > 0)
            {
                if (CurrentGame == GAME_SPELLING)
                {
                    BoothInput[booth] = BoothInput[booth].Substring(0, BoothInput[booth].Length - 1);
                }
                else
                {
                    BoothInput[booth] = BoothInput[booth].Substring(1);
                }
            }
        }
        else if (key == "-1")
        {
            //Special case: clear entire word
            BoothInput[booth] = "";
        }
        else if (key == "-2")
        {
            //Special case: switch mode in math
            BoothMode[booth] = BoothMode[booth] == "L" ? "R" : "L";
        }
        else if (key.Equals(" ") && BoothInput[booth] == "")
        {
            //Special case: don't allow space at the start of the word
            debugprint("Ignoring space at start of word");
        }
        else if (BoothInput[booth].Length < ((CurrentGame == GAME_SPELLING) ? MaxWordSize : MaxNumberSize))
        {
            //Add the key to the booth's input
            if (CurrentGame == GAME_SPELLING || BoothMode[booth] == "L")
            {
                BoothInput[booth] += key.ToString();
            }
            else
            {
                BoothInput[booth] = key.ToString() + BoothInput[booth];
            }
        }

        debugprint("New value of BoothInput: " + value);

        UpdateInputEntity(booth, Color.White);
        if (CurrentGame == GAME_SPELLING)
        {
            DisplayWord(booth);
        }
        else
        {
            DisplayProblem(booth);
        }
    }

    //         0  1   2    3     4      5       6        7         8          9           10           11            12             13              14               15                16                 17
    private static string[] SPACES = ["", " ", "  ", "   ", "    ", "     ", "      ", "       ", "        ", "         ", "          ", "           ", "            ", "             ", "              ", "               ", "                ", "                 "];
    private string RightAlignText(string text, string textToAppend, int stringSize)
    {
        if (textToAppend == "0") return text;

        int numSpaces = stringSize - text.Length - textToAppend.Length;
        if (numSpaces < 0) return text;

        return text + SPACES[numSpaces] + textToAppend;
    }

    private void UpdateInputEntity(int booth, Color color)
    {
        // //Set up input entity if first update
        // if (!BoothInputEntities.ContainsKey(booth))
        // {
        //     BoothInputEntities[booth] = Entities.FindByName(null, "bee_input" + booth);
        //     debugprint("Found " + BoothInputEntities[booth] + " entity for " + "bee_input" + (booth));
        // }
        if (!BoothInput.TryGetValue(booth, out string? input)) return;

        string cursor = (TimeLeft > 0 && BoothInput[booth].Length < MaxNumberSize) ? "_" : "";
        if (GameStarted)
        {
            if (CurrentGame == GAME_SPELLING)
            {
                input = BoothInput[booth] + cursor;
            }
            else
            {
                if (BoothMode.TryGetValue(booth, out string? value) && value == "R")
                {
                    input = string.Format("{0}{1,4}", CurrentProblem, cursor + BoothInput[booth]);
                }
                else
                {
                    input = string.Format("{0}{1}{2}", CurrentProblem, cursor, BoothInput[booth]);
                }
            }
        }
        string inputAndTimer = RightAlignText(input, TimeLeft.ToString(), MaxWordSize + 3);
        BoothInputEntities[booth].AddEntityIOEvent(inputName: "SetMessage", value: inputAndTimer);
        BoothInputEntities[booth].Color = color;
    }

    public void ClearAllGameTexts()
    {
        for (int booth = 1; booth <= NumBooths; booth++)
        {
            BoothInput[booth] = "";
            UpdateInputEntity(booth, Color.White);
        }
        DisplayDefinition(new Dictionary<int, string> { { 0, "" }, { 1, "" }, { 2, "" }, { 3, "" }, { 4, "" } });
        DisplayCurrentTargetWord("");
        BeeTimer.AddEntityIOEvent(inputName: "SetMessage", value: "");
    }

    private void UpdateAllGameTexts()
    {
        for (int booth = 1; booth <= NumBooths; booth++)
        {
            UpdateInputEntity(booth, Color.White);
        }
    }

    private void ClearInputs()
    {
        for (int booth = 1; booth <= NumBooths; booth++)
        {
            debugprint("Setting BoothInput[" + booth + "] to blank string"); //TODO remove
            BoothInput[booth] = "";
        }
    }

    private void DisplayCurrentTargetWord(string word)
    {
        CurrentWordEntity.AddEntityIOEvent(inputName: "SetMessage", value: word.ToLower());
    }

    private void DisplayWord(int booth)
    {
        CCSPlayerController? player = Utilities.GetPlayers().Where(p => p.PlayerPawn.Value?.Entity?.Name == $"bee_player_{booth}").FirstOrDefault();
        if (player != null)
        {
            SetMessage(player.Slot, $"{BoothInput[booth]}", 15.0f);
            // game_txt.__KeyValueFromString("message", BoothInput[booth]);
            // EntFireByHandle(game_txt, "Display", "", 0.05f, player, player);
        }
    }

    private void ChooseNewWord(int difficulty)
    {
        TimerActive = false;
        GameStarted = false;
        if (!Words.TryGetValue(difficulty, out Dictionary<string, string>? value))
        {
            value = SetupWords(difficulty);
            Words[difficulty] = value;
        }
        KeyValuePair<string, string> nextWord = value.ElementAt(RandomInt(0, NumWords[difficulty] - 1));
        SetSpellWord(nextWord.Key.ToLower());
        SetDefinition(nextWord.Value);

        DisplayCurrentTargetWord(nextWord.Key);
        TimeLeft = TimePerDifficulty[difficulty];
        SetTimerSeconds(TimeLeft);

        ClearInputs();
        UpdateAllGameTexts();
    }

    private void SetDefinition(string definition)
    {
        Dictionary<int, string> CurrentWordDefinitionLines = [];
        for (int i = 0; i < MaxDefinitionLines; i++)
        {
            CurrentWordDefinitionLines[i] = "";
        }
        string remainingDefinition = definition;
        int line = 0;
        while (remainingDefinition.Length > 0 && line < MaxDefinitionLines)
        {
            debugprint("On line " + line);
            if (remainingDefinition.Length <= MaxDefinitionLineSize)
            {
                CurrentWordDefinitionLines[line] = remainingDefinition;
                break;
            }
            for (int i = MaxDefinitionLineSize >= remainingDefinition.Length ? remainingDefinition.Length - 1 : MaxDefinitionLineSize; i >= 0; i--)
            {
                debugprint("On index " + i + " which corresponds to character " + remainingDefinition[i]);
                if (remainingDefinition[i] == ' ')
                {
                    debugprint("Found space at index " + i + " in " + remainingDefinition);
                    CurrentWordDefinitionLines[line] = remainingDefinition.Substring(0, i);
                    remainingDefinition = remainingDefinition.Substring(i + 1);
                    debugprint("Setting line " + line + " to: " + CurrentWordDefinitionLines[line]);
                    break;
                }
                if (i == 0)
                {
                    CurrentWordDefinitionLines[line] = remainingDefinition;
                    remainingDefinition = "";
                    break;
                }
            }
            line++;
        }
        DisplayDefinition(CurrentWordDefinitionLines);
    }

    private void DisplayDefinition(Dictionary<int, string> definitionLines)
    {
        for (int line = 0; line < MaxDefinitionLines; line++)
        {
            debugprint("Line " + line + ":   " + definitionLines[line]);
            CurrentWordDefinition[line].AddEntityIOEvent(inputName: "SetMessage", value: definitionLines[line]);
        }
    }

    //For debugging purposes only
    private void SetRandomWord(int length)
    {
        SpellWord = "";
        string alphabet = "abcdefghijklmnopqrstuvwxyz";
        for (int i = 0; i < length; i++)
        {
            SpellWord += alphabet[RandomInt(0, 25)];
        }
        debugprint("Setting spell word to " + SpellWord);
    }

    private void SetSpellWord(string word)
    {
        SpellWord = word;
        debugprint("Setting spell word to " + word);
    }

    private KeyValuePair<string, string> CreateWord(string spelling, string definition)
    {
        KeyValuePair<string, string> word = new(spelling, definition);
        return word;
    }

    private void CheckSpelling(int booth)
    {
        debugprint("checking booth " + booth);
        if (BoothInput.ContainsKey(booth))
        {
            string correctAnswer = CurrentGame == GAME_SPELLING ? SpellWord : CurrentAnswer;
            if (correctAnswer == BoothInput[booth])
            {
                debugprint("booth " + booth + " has correct answer " + BoothInput[booth]);
                UpdateInputEntity(booth, Color.Green);
            }
            else
            {
                debugprint("booth " + booth + " has incorrect answer " + BoothInput[booth]);
                UpdateInputEntity(booth, Color.Red);
                BeeHurt[booth - 1].AddEntityIOEvent(inputName: "Enable", delay: 0.0f);
                BeeHurt[booth - 1].AddEntityIOEvent(inputName: "Disable", delay: 0.2f);
            }
        }
    }

    private void CheckSpellingAll()
    {
        foreach (CBaseButton keyboard in KeyboardButtons)
        {
            keyboard.AddEntityIOEvent(inputName: "Lock", delay: 0.0f);
        }
        string answerText = CurrentGame == GAME_SPELLING ? ("Correct spelling of word: \x05" + SpellWord + "\x01 ") : ("Correct solution: \x05" + CurrentProblem + CurrentAnswer + "\x01 ");
        Server.PrintToChatAll(answerText);
        if ((CurrentGame == GAME_SPELLING && SpellWord.Length > 0) || (CurrentGame == GAME_MATH && CurrentAnswer.Length > 0))
        {
            for (int booth = 1; booth <= NumBooths; booth++)
            {
                CheckSpelling(booth);
            }
        }
    }

    public void StartTimer()
    {
        if (TimerActive || (SpellWord == "" && CurrentProblem == "")) return;
        GameNumber++;
        GameStarted = true;
        TimerActive = true;
        Timers.Add(new(1.0f, () => TimerTick(GameNumber)));
        StartSound.AddEntityIOEvent(inputName: "PlaySound", delay: 0.0f);
        for (int booth = 1; booth <= NumBooths; booth++)
        {
            BoothInput[booth] = "";
            UpdateInputEntity(booth, Color.White);
        }
        foreach (CBaseButton keyboard in KeyboardButtons)
        {
            keyboard.AddEntityIOEvent(inputName: "Unlock", delay: 0.0f);
        }
        StartButtonModel.AddEntityIOEvent(inputName: "Skin", value: "1");
        PauseButtonModel.AddEntityIOEvent(inputName: "Skin", value: "0");
    }

    public void PauseTimer()
    {
        if (TimeLeft <= 0 || !GameStarted) return;

        if (TimerActive)
        {
            TimerActive = false;
            foreach (CBaseButton keyboard in KeyboardButtons)
            {
                keyboard.AddEntityIOEvent(inputName: "Lock", delay: 0.0f);
            }
            StartButtonModel.AddEntityIOEvent(inputName: "Skin", value: "0");
            PauseButtonModel.AddEntityIOEvent(inputName: "Skin", value: "1");
        }
    }

    private void TimerTick(int gameNumberOfTick)
    {
        if (!TimerActive || gameNumberOfTick != GameNumber) return;
        TimeLeft--;
        SetTimerSeconds(TimeLeft);
        if (TimeLeft > 0)
        {
            Timers.Add(new(1.0f, () => TimerTick(GameNumber)));
            UpdateAllGameTexts();
        }
        else
        {
            CheckSpellingAll();
            StartButtonModel.AddEntityIOEvent(inputName: "Skin", value: "0");
            PauseButtonModel.AddEntityIOEvent(inputName: "Skin", value: "0");
        }
    }

    private void SetTimerSeconds(int time)
    {
        int minutes = time / 60;
        int seconds = time % 60;
        BeeTimer.AddEntityIOEvent(inputName: "SetMessage", value: $"{minutes}:" + (seconds < 10 ? "0" : "") + seconds);
    }

    public void AddToTime(int toAdd)
    {
        if (toAdd < 0 && TimeLeft + toAdd < MinTime) return;

        TimeLeft += toAdd;
        if (TimeLeft > MaxTime) TimeLeft = MaxTime;
        if (TimeLeft <= MinTime) TimeLeft = MinTime;
        SetTimerSeconds(TimeLeft);
    }

    public void AddToDamage(int toAdd)
    {
        Damage += toAdd;
        if (Damage > MaxDamage) Damage = MaxDamage;
        if (Damage < MinDamage) Damage = MinDamage;

        DamageSignModel.AddEntityIOEvent(inputName: "Skin", value: $"{DamageToTexture[Damage]}");
        for (int i = 0 ; i < 16 ; i++)
        {
            BeeHurt[i].AddEntityIOEvent(inputName: "SetDamage", value: (Damage * 2).ToString(), delay: 0.0f);
        }
    }

    private string RemoveSpaces(string str)
    {
        string returnstring = "";
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] != 32)
            {
                returnstring += str[i];
            }
        }
        return returnstring;
    }

    private Dictionary<string, string> SetupWords(int difficulty)
    {
        switch (difficulty)
        {
            case 0:
                return SetupEasyWords();
            case 1:
                return SetupMediumWords();
            case 2:
                return SetupHardWords();
            default:
                debugprint("ERROR: unknown difficulty " + difficulty + " in SetupWords");
                return [];
        }
    }

    public void DisplayHelpText()
    {
        if (TimerActive) return;
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;
            if (pawn == null) continue;

            Vector playerOrigin = pawn.AbsOrigin!;
            debugprint("Checking location of player " + player + " with origin " + playerOrigin);
            if (playerOrigin.X >= BoothAreaMin.X && playerOrigin.X <= BoothAreaMax.X &&
                playerOrigin.Y >= BoothAreaMin.Y && playerOrigin.Y <= BoothAreaMax.Y &&
                playerOrigin.Z >= BoothAreaMin.Z && playerOrigin.Z < BoothAreaMax.Z)
            {
                if (CurrentGame == GAME_SPELLING)
                {
                    DisplayText(player, "1. Listen to the word and definition.<br>2. When the bell rings you can begin.<br>3. Use the keyboard in your booth to enter the word.");
                }
                else
                {
                    DisplayText(player, "1. Once the bell rings, solve the math problem.<br>2. Use the keypad in your booth to enter the answer.");
                }
            }
            if (playerOrigin.X >= ControlsAreaMin.X && playerOrigin.X <= ControlsAreaMax.X &&
                playerOrigin.Y >= ControlsAreaMin.Y && playerOrigin.Y <= ControlsAreaMax.Y &&
                playerOrigin.Z >= ControlsAreaMin.Z && playerOrigin.Z < ControlsAreaMax.Z)
            {
                if (CurrentGame == GAME_SPELLING)
                {
                    DisplayText(player, "1. Select damage for wrong answers<br>2. Choose difficulty to get a word<br>3. Read out the word and definition clearly<br>4. Start the timer AFTER reading the definition");
                }
                else
                {
                    DisplayText(player, "1. Select damage for wrong answers<br>2. Choose difficulty to get a math problem<br>3. Start the timer and the players will see the problem");
                }
            }
        }
    }

    private void DisplayText(CCSPlayerController player, string text)
    {
        SetMessage(player.Slot, $"{text}", 4.0f);
    }

    //////////////////////////
    //        MATH          //
    //////////////////////////

    private string CurrentProblem = "";
    private string CurrentAnswer = "";

    private void DisplayProblem(int booth)
    {
        CCSPlayerController? player = Utilities.GetPlayers().Where(p => p.PlayerPawn.Value?.Entity?.Name == $"bee_player_{booth}").FirstOrDefault();
        if (player != null)
        {
            SetMessage(player.Slot, CurrentProblem + BoothInput[booth], 15.0f);
        }
    }

    private void GenerateEasyProblem()
    {
        int problemType = RandomInt(0, 2);
        switch (problemType)
        {
            case 0:
                int firstNumber = RandomInt(10, 99);
                int secondNumber = RandomInt(10, 99);
                CurrentProblem = firstNumber.ToString() + "+" + secondNumber.ToString();
                CurrentAnswer = (firstNumber + secondNumber).ToString();
                break;
            case 1:
                firstNumber = RandomInt(3, 5);
                secondNumber = RandomInt(20, 99);
                CurrentProblem = firstNumber.ToString() + "*" + secondNumber.ToString();
                CurrentAnswer = (firstNumber * secondNumber).ToString();
                break;
            case 2:
                firstNumber = RandomInt(3, 9);
                secondNumber = RandomInt(4, 9);
                int dividend = firstNumber * secondNumber;
                CurrentProblem = dividend.ToString() + "/" + secondNumber.ToString();
                CurrentAnswer = firstNumber.ToString();
                break;
            default:
                debugprint("ERROR: unknown problem type " + problemType + " in GenerateEasyProblem");
                return;
        }
    }

    private void GenerateMediumProblem(bool includeEasy)
    {
        int problemType = RandomInt(includeEasy ? 0 : 1, 2);
        switch (problemType)
        {
            case 0:
                GenerateEasyProblem();
                int thirdNumber = RandomInt(10, 99);
                CurrentProblem = "(" + CurrentProblem + ")+" + thirdNumber.ToString();
                CurrentAnswer = (int.Parse(CurrentAnswer) + thirdNumber).ToString();
                break;
            case 1:
                int firstNumber = RandomInt(100, 999);
                int secondNumber = RandomInt(100, 999);
                CurrentProblem = firstNumber.ToString() + "+" + secondNumber.ToString();
                CurrentAnswer = (firstNumber + secondNumber).ToString();
                break;
            case 2:
                firstNumber = RandomInt(21, 85);
                secondNumber = RandomInt(21, 85);
                if (firstNumber < secondNumber)
                {
                    int tmp = firstNumber;
                    firstNumber = secondNumber;
                    secondNumber = tmp;
                }
                if ((firstNumber - secondNumber) < 10)
                {
                    secondNumber -= 2;
                    firstNumber += 2;
                }

                if ((firstNumber % 10) - (secondNumber % 10) >= 0)
                {
                    secondNumber -= (secondNumber % 10) + 1; //last digit is now 9
                    firstNumber += 10 - (firstNumber % 10); //last digit is now 0

                    secondNumber -= RandomInt(0, 4); //last digit is now between 9 and 5
                    firstNumber += RandomInt(0, 4); //last digit is now between 0 and 4;
                }
                CurrentProblem = firstNumber.ToString() + "-" + secondNumber.ToString();
                CurrentAnswer = (firstNumber - secondNumber).ToString();
                break;
            default:
                debugprint("ERROR: unknown problem type " + problemType + " in GenerateMediumProblem");
                return;
        }
    }

    private void GenerateHardProblem()
    {
        int problemType = RandomInt(0, 2);
        switch (problemType)
        {
            case 0:
                GenerateMediumProblem(false);
                int thirdNumber = RandomInt(10, 99);
                CurrentProblem = "" + CurrentProblem + "+" + thirdNumber.ToString();
                CurrentAnswer = (int.Parse(CurrentAnswer) + thirdNumber).ToString();
                break;
            case 1:
                int firstNumber = RandomInt(2, 9);
                int secondNumber = RandomInt(100, 999);
                CurrentProblem = firstNumber.ToString() + "*" + secondNumber.ToString();
                CurrentAnswer = (firstNumber * secondNumber).ToString();
                break;
            case 2:
                firstNumber = RandomInt(121, 985);
                secondNumber = RandomInt(21, 89);

                if ((firstNumber % 10) - (secondNumber % 10) >= 0)
                {
                    secondNumber -= (secondNumber % 10) + 1; //last digit is now 9
                    firstNumber += 10 - (firstNumber % 10); //last digit is now 0

                    secondNumber -= RandomInt(0, 4); //last digit is now between 9 and 5
                    firstNumber += RandomInt(0, 4); //last digit is now between 0 and 4;
                }
                CurrentProblem = firstNumber.ToString() + "-" + secondNumber.ToString();
                CurrentAnswer = (firstNumber - secondNumber).ToString();
                break;
            default:
                debugprint("ERROR: unknown problem type " + problemType + " in GenerateHardProblem");
                return;
        }
    }

    private void ChooseNewProblem(int difficulty)
    {
        TimerActive = false;
        GameStarted = false;
        switch (difficulty)
        {
            case 0:
                GenerateEasyProblem();
                break;
            case 1:
                GenerateMediumProblem(true);
                break;
            case 2:
                GenerateHardProblem();
                break;
            default:
                debugprint("ERROR: unknown difficulty " + difficulty + " in GenerateEasyProblem");
                return;
        }

        CurrentProblem += "=";
        DisplayCurrentTargetWord(CurrentProblem);
        TimeLeft = 15 + difficulty * 5;
        SetTimerSeconds(TimeLeft);
        ClearInputs();
        UpdateAllGameTexts();
        debugprint("Answer: " + CurrentAnswer);
    }

    private Dictionary<string, string> SetupEasyWords()
    {
        List<KeyValuePair<string, string>> words = 
        [
            CreateWord("stalk", "v. to seek out game for food or sport; n. the stem of a plant"),
            CreateWord("fault", "n. a defect in character; responsibility for wrongdoing or failure"),
            CreateWord("pause", "v. to stop for a short time before going on; n. a short break or rest from what has been going on."),
            CreateWord("cause", "n. something or someone that brings about a result or effect; an idea or goal that many are interested in; v. to make happen; be the cause of"),
            CreateWord("vault", "n. a strongroom or compartment often made of steel for safekeeping of valuables; v. and adj. 1 jump across or leap over; 2 having a hemispherical vault or dome"),
            CreateWord("because", "conj. or prep. on account of"),
            CreateWord("fawn", "n. a young deer less than one year old"),
            CreateWord("draw", "v. 1 make a mark or lines on a surface; 2 cause to move by pulling; n. a gully that is shallower than a ravine"),
            CreateWord("thaw", "v. to go from a solid to a liquid state"),
            CreateWord("yawn", "n. an opening of the mouth wide while taking a deep breath often as an involuntary reaction; v. to open the mouth wide and take a deep breath"),
            CreateWord("dawn", "n. the first light of day; v. become clear or enter one's consciousness or emotions"),
            CreateWord("lawn", "n. a plot of grass usually tended or mowed as one around a residence or in a park or estate"),
            CreateWord("straw", "n. 1 hollow stalks or stems of grain after threshing; 2 a thin paper or plastic tube used to suck liquids into the mouth"),
            CreateWord("chalk", "n. a hard powdery substance that is used as a blackboard crayon; v. to record or note something"),
            CreateWord("haunt", "v. 1 to stay in ones mind continually; 2 to visit frequently 3 to appear in the form of a ghost."),
            CreateWord("collect", "v. get or gather together"),
            CreateWord("animal", "n. a living organism characterized by voluntary movement; adj. marked by the appetites and passions of the body"),
            CreateWord("angry", "adj. feeling or showing an intense emotional state of displeasure"),
            CreateWord("announcer", "n. a person who ann.ces; specif; one who introduces radio or television programs; identifies the station; reads the news; etc."),
            CreateWord("angle", "n. and v. 1 the figure made by two lines or rays coming from a single point; 2.the space between such lines measured in degrees."),
            CreateWord("cousin", "n. the child of your aunt or uncle"),
            CreateWord("count", "v. to find the sum of by noting each one as it is being added; to place reliance or trust"),
            CreateWord("anchor", "n. a device usually of metal that is attached to a boat by a cable and that when thrown overboard digs into the earth and holds the boat in place"),
            CreateWord("coast", "n. the shore of a sea or ocean; v. move effortlessly by force of gravity"),
            CreateWord("coil", "v. to wind or move in a spiral course; n. a structure consisting of something wound in a continuous series of loops"),
            CreateWord("couch", "n. an upholstered seat for more than one person; v. formulate in a particular style or language"),
            CreateWord("cough", "v. and n. to force air from the lungs with a sharp short noise or series of noises"),
            CreateWord("angel", "n. 1.a spiritual being who acts as a servant or messenger of God; 2.a person who has great goodness and kindness."),
            CreateWord("collar", "n. the part of a garment at the neck especially of a shirt or jacket; v. to take physical control or possession of something suddenly or forcibly"),
            CreateWord("answered", "v. something spoken or written in reaction especially to a question; reply or respond to"),
            CreateWord("Tuesday", "n. the third day of the week; abbrev. Tue;Tues; Tu; or T"),
            CreateWord("Wednesday", "n. the fourth day of the week. Wednesday comes between Tuesday and Thursday"),
            CreateWord("Thursday", "n. the fifth day of the week; abbrev. Thur; Thurs; Thu; Th; or T"),
            CreateWord("yesterday", "adv. on the day before today; n. 1 the day before today; 2 the recent past"),
            CreateWord("holiday", "n. a day on which most people do not work in honor or celebration of some person or event"),
            CreateWord("pray", "v. 1 to ask for something in a serious or sincere manner; 2 to thank ask or speak to God or some other spiritual being."),
            CreateWord("betray", "v. to be disloyal to; to show; to reveal."),
            CreateWord("flight", "n. 1 an act of passing through air or space by flying; 2 a trip on a plane from one place to another; 3 an act or instance of fleeing."),
            CreateWord("height", "n. the distance of something or someone from bottom to top; the highest part or point; the most extreme or advanced point"),
            CreateWord("eight", "adj. or n. totaling one more than seven"),
            CreateWord("delight", "n. great pleasure or joy; v. 1 to give great pleasure or joy; 2 to take or find pleasure"),
            CreateWord("slight", "adj. of a size that is less than average; lacking bodily strength; small in degree; v. to cause hurt feelings or deep resentment in"),
            CreateWord("weight", "n. the amount that something weighs; a heavy object -as a metal ball- used in athletic exercises and contests"),
            CreateWord("fright", "n. 1 strong fear caused by sudden danger; 2 something or someone that causes such fear."),
            CreateWord("afternoon", "n. the time of day from noon to evening any period of beginning decline"),
            CreateWord("anyone", "any person; anybody."),
            CreateWord("asleep", "adj. sleeping; lacking in sensation or feeling"),
            CreateWord("cupboard", "n. a closet or cabinet with shelves for holding cups; plates; food; and the like"),
            CreateWord("downstairs", "n. the lower floor or floors of a building; adj. on or of lower floors of a building; adv. down the flight of steps;"),
            CreateWord("everybody", "pron. every person"),
            CreateWord("everyone", "pron. every person"),
            CreateWord("maybe", "adv. it is possible"),
            CreateWord("myself", "pron. 1 used as an intensive of me or I; 2 used reflexively in place of me as the object of a preposition or as the direct or indirect object of a verb"),
            CreateWord("overboard", "adv. over the side of a ship into the water"),
            CreateWord("sailboat", "n. a boat having a sail or sails by means of which it is propelled"),
            CreateWord("snowstorm", "n. a storm with widespread snowfall accompanied by strong winds"),
            CreateWord("sometimes", "plural adv. on some occasions"),
            CreateWord("surfboard", "n. a long; narrow board used in the sport of surfing"),
            CreateWord("upstairs", "n. and adj and adv. the part of a building above the ground floor"),
            CreateWord("gentle", "adj. soft and mild; not harsh or stern; v. to tame or calm"),
            CreateWord("taught", "v. impart skills or knowledge to"),
            CreateWord("caught", "v. to take physical control or possession of"),
            CreateWord("wear", "v. 1 be dressed in; 2 have on one's person; n. impairment resulting from long use"),
            CreateWord("weigh", "v. 1 to measure the weight of by using a scale; 2 to think about carefully before making a decision. 3 to have a particular amount of weight."),
            CreateWord("through", "adj. having completed an action or process; prep. in at one end or side and out at the other"),
            CreateWord("enough", "adj. adequate for the want or need; pron. an adequate quantity."),
            CreateWord("field", "n. a piece of land cleared of trees and usually enclosed; v. catch or pick up balls in baseball"),
            CreateWord("believe", "v. to regard as right or true; to have as an opinion"),
            CreateWord("favorite", "adj. best liked"),
            CreateWord("loose", "adj. not tightly fastened or tied or stretched; v. to set free"),
            CreateWord("scarf", "n. a broad band of cloth worn about the shoulders or around the neck or over the head or about the waist"),
            CreateWord("surprise", "n. something that makes a strong impression because it is so unexpected; v. come upon or take unawares"),
            CreateWord("whether", "conj. if; used to introduce two or more situations of which only one can occur"),
            CreateWord("wrestle", "v. to take part in the sport of wrestling or to struggle to throw and hold another to the ground; to struggle or fight usually followed by with"),
            CreateWord("busy", "adj. 1 occupied in some activity; at work; 2 crowded with or characterized by much activity"),
            CreateWord("video", "n. the picture part of television; adj. having to do with the picture part of television or any images shown on a screen."),
            CreateWord("gold", "symbol-Au; atomic number-79; a precious yellow metallic element highly malleable and ductile and not subject to oxidation or corrosion"),
            CreateWord("safe", "adj. free from danger or the risk of harm; n. strongbox where valuables can be safely kept"),
            CreateWord("hello", "Interjection. an expression of greeting"),
            CreateWord("secret", "adj. not known or meant to be known by the general population; n. information shared only with another or with a select few"),
            CreateWord("Maine", "n. a state in New England"),
            CreateWord("spray", "v. to cover by or as if by scattering something over or on"),
            CreateWord("coach", "n. a person who trains performers or athletes; v. to give advice and instruction to someone regarding the course or process to be followed"),
            CreateWord("week", "n. 1 a period of seven consecutive days starting on Sunday; 2 any period of seven consecutive days"),
            CreateWord("seem", "v. 1 to appear to be or do; 2 to appear to be real"),
            CreateWord("tight", "adj. 1 snug; trim; close in fit or timing; 2.fastened or shut in a secure way; fixed in place."),
            CreateWord("load", "n. 1 a quantity that can be processed or transported at one time; 2 a considerable amount; v. fill or place a burden on"),
            CreateWord("toast", "n. 1 slices of bread that have been browned; 2 a drink in honor of or to the health of a person or event; v. make brown and crisp by heating"),
            CreateWord("why", "adv. for what? for what reason cause or purpose?; n. the cause or intention underlying an action or situation"),
            CreateWord("dismiss", "v. 1 to send away or allow to go away; 2.to remove from a job; fire. 3 to reject as not worth considering."),
            CreateWord("disgrace", "n. a cause of shame"),
            CreateWord("disable", "v. 1 make unable to perform a certain action; 2 injure permanently"),
            CreateWord("disapprove", "v. consider bad or wrong or inappropriate"),
            CreateWord("disrepair", "n. the condition of needing repairs; state of neglect; dilapidation"),
            CreateWord("disregard", "v. to pay no attention to; ignore; n. lack of attention or respect"),
            CreateWord("display", "n. a public show or exhibition; v. to show or make visible or apparent"),
            CreateWord("disappear", "v. to cease to be seen"),
            CreateWord("displaced", "v. 1 to force to leave an area; 2 to change the place or position of"),
            CreateWord("disbelief", "n. a condition of being unable or not willing to believe."),
            CreateWord("dishonor", "v. bring shame upon; n. a state of shame or disgrace"),
            CreateWord("discuss", "v. to talk together about; to consider in writing or speech"),
            CreateWord("discussion", "n. an exchange of views on some topic"),
            CreateWord("disconnect", "v. to break off or stop the connection of or between; to break the flow of electric current to; separate from the power source"),
            CreateWord("discard", "v. to throw away or abandon; get rid of as having no value"),
            CreateWord("checking", "v. to make sure that something is correct or satisfactory"),
            CreateWord("lightning", "n. a brilliant electric spark discharge in the atmosphere occurring within a thundercloud between clouds or between a cloud and the ground"),
            CreateWord("something", "pron. 1 a certain undetermined or unspecified thing; 2 an unspecified or unknown amount; 3 an impressive or important person or thing or event"),
            CreateWord("hoping", "v. expect and wish"),
            CreateWord("smiling", "n. a facial expression characterized by turning up the corners of the mouth; usually shows pleasure or amusement; v. change one's facial expression by turning up the lips"),
            CreateWord("smelling", "v. 1 inhale the odor of; 2 emit an odor; n. the act of perceiving the odor of something"),
            CreateWord("lying", "v. to occupy a place or location; v. or n. not telling the truth"),
            CreateWord("writing", "to form letters or words on a surface with an instrument -as a pen or pencil-"),
            CreateWord("drawing", "n. the art of representing something by lines made on a surface with a pencil or pen; v. cause to move by pulling"),
            CreateWord("hurrying", "v. to move or act with speed; to rush"),
            CreateWord("studying", "v. contemplate; learn; n. learning; analysis"),
            CreateWord("anything", "pron. a thing of any kind"),
            CreateWord("swimming", "v. travel through water; n. the act of traveling through water; adj. filled or brimming with tears"),
            CreateWord("hiking", "v. 1 walk a long way; 2 increase"),
            CreateWord("worrying", "v. to experience concern or anxiety; n. an uneasy state of mind usually over the possibility of an anticipated misfortune"),
            CreateWord("happiness", "n. the fact or condition of being glad."),
            CreateWord("happen", "v. to take place; occur; befall to be or occur by chance or without plan; to have the luck or occasion; chance"),
            CreateWord("arrival", "n. 1 coming to stopping-place or destination; 2 accomplishment of an objective"),
            CreateWord("arrive", "v. reach a destination"),
            CreateWord("earrings", "n. a ring or other small ornament for the lobe of the ear; either passed through a hole pierced in the lobe or fastened with a screw or clip"),
            CreateWord("carry", "v. to support and take from one place to another"),
            CreateWord("hopped", "v. jump lightly"),
            CreateWord("stripped", "v. 1 to remove; 2 make naked; undress to deprive or disposses"),
            CreateWord("shipped", "v. transport commercially"),
            CreateWord("skipped", "v. 1 jump lightly; 2 bypass; 3 intentionally fail to attend"),
            CreateWord("supplies", "plural v. to make available for use; n. an amount of something available for use"),
            CreateWord("squirrel", "n. a small rodent with a long tail"),
            CreateWord("mirror", "n. a reflecting surface; a pattern for imitation"),
            CreateWord("tomorrow", "adv. on or during the day after today at some time in the indefinite future"),
            CreateWord("worry", "v. to experience concern or anxiety; n. an uneasy state of mind usually over the possibility of an anticipated misfortune"),
            CreateWord("minute", "n. a unit of time equal to sixty seconds; adj. very small in size"),
            CreateWord("excuse", "n. 1 a reason offered to explain or ask for pardon for a fault; 2 a reason or explanation used to escape blame; v. to allow someone to leave"),
            CreateWord("due", "adj. owed and payable immediately or on demand; n. that which is deserved or owed; adv. directly or exactly"),
            CreateWord("tale", "n. 1 a trivial lie; narrative a literary composition in narrative form a piece of gossip a falsehood; 2 a message that tells the particulars of an act or occurrence"),
            CreateWord("soar", "v. to fly high"),
            CreateWord("sauce", "n. flavorful relish or dressing or topping served as an accompaniment to food; v. add zest or flavor to"),
            CreateWord("special", "adj. being unusual and especially better in some way"),
            CreateWord("dream", "n. 1 a series of mental images and emotions occurring during sleep; someone or something wonderful; 2 a cherished desire; v. indulge in a fantasy"),
            CreateWord("fought", "v. to oppose someone in physical conflict; to try hard"),
            CreateWord("thought", "v. to have as an opinion; n. something imagined or pictured in the mind"),
            CreateWord("friend", "n. a person who has a strong liking for and trust in another"),
            CreateWord("screech", "n. a shrill harsh cry usually expressing pain or terror"),
            CreateWord("stitches", "plural n. a single complete in-and-out movement of the threaded needle in sewing; v. fasten by sewing"),
            CreateWord("clothes", "plural n. a covering designed to be worn on a person's body"),
            CreateWord("wrote", "v. to form letters or words on a surface with an instrument -as a pen or pencil"),
            CreateWord("bank", "n. 1 the land around a body of water; 2 a place of business that lends or exchanges or takes care of or issues money"),
            CreateWord("theater", "n. a building for housing dramatic presentations or stage entertainments or motion-picture shows; place in which military operations are in progress"),
            CreateWord("feather", "n. one of the things that grow from a bird's skin that form the covering of its body; v. to line or cover or decorate with feathers"),
            CreateWord("weather", "n. the meteorological conditions - temperature and wind and clouds and precipitation; v. face or endure with courage"),
            CreateWord("death", "n. the act or fact of dying; permanent ending of all life in a person or animal"),
            CreateWord("breath", "n. 1 the air that flows into and out of the lungs during breathing; respiration; 2 a slight movement of the air"),
            CreateWord("beat", "v. 1 to hit again and again; 2 to win against; defeat; 3 to stir rapidly; n. 1 a hit or blow; 2 musical rhythm; adj. very tired; exhausted"),
            CreateWord("speed", "n. 1 distance travelled per unit time; 2 changing location rapidly; v. step on it"),
            CreateWord("greed", "n. excessive desire for getting or having; esp. wealth; desire for more than one needs or deserves; avarice; cupidity"),
            CreateWord("needle", "n. 1 a slender pointed steel instrument used in sewing or piercing tissues; 2 the leaf of a conifer; v. goad or provoke as by constant criticism"),
            CreateWord("hook", "n. a curved or bent tool for catching or holding or pulling; v. to catch -a fish- with a hook; v. to hold hang or pull with a hook; to bend sparply"),
            CreateWord("shook", "v. move or cause to move back and forth; stir one's feelings or emotions"),
            CreateWord("brook", "n. a small stream"),
            CreateWord("crook", "n. and v. 1 someone who has committed or been legally convicted of a crime; 2 a circular segment of a curve; 3 a long staff with one end being hook shaped"),
            CreateWord("thank", "v. 1 to say that one is grateful to; 2.used ironically to assign blame or responsibility for something; 3 to hold responsible"),
            CreateWord("almost", "adv. very close to but not completely"),
            CreateWord("again", "adv. anew"),
            CreateWord("airplane", "n. an aircraft that has a fixed wing and is powered by propellers or jets"),
            CreateWord("alarm", "n. a warning of danger; v. to strike with fear"),
            CreateWord("around", "adv. 1 in a circle; 2 in the area or vicinity"),
            CreateWord("aware", "adj. knowing or realizing"),
            CreateWord("area", "n. 1 a particular geographical region of indefinite boundary; 2 a particular geographical region of indefinite boundary"),
            CreateWord("always", "adv. at all times"),
            CreateWord("agree", "v. to have or come to the same opinion or point of view"),
            CreateWord("alley", "n. a narrow street or passageway between buildings"),
            CreateWord("along", "adv. 1 in accompaniment or as a companion"),
            CreateWord("alone", "adj. or adv. not being in the company of others"),
            CreateWord("author", "n. a person who creates a written work"),
            CreateWord("alphabet", "n. the letters of a written language given in proper order."),
            CreateWord("attic", "n. floor consisting of open space at the top of a house just below roof"),
            CreateWord("scared", "v. 1 to fill with fear or terror; 2 cause to lose courage; adj. made afraid"),
            CreateWord("laughed", "v. the manifestation of joy or mirth or scorn"),
            CreateWord("planned", "v. think out; prepare in advance"),
            CreateWord("named", "v. assign a specified usually proper proper name to"),
            CreateWord("hundred", "adj. being ten more than ninety; ten tens"),
            CreateWord("hoped", "v. expect and wish"),
            CreateWord("turned", "v. change orientation or direction"),
            CreateWord("spied", "v. to watch or observe closely and secretly"),
            CreateWord("fried", "v. cook on a hot surface using fat; adj. cooked in fat"),
            CreateWord("cried", "v. 1 shed tears; 2 utter a sudden loud noise"),
            CreateWord("tried", "v. make an effort or attempt"),
            CreateWord("studied", "v. contemplate; learn"),
            CreateWord("dried", "v. remove the moisture from; adj. not still wet"),
            CreateWord("scrubbed", "v. to clean or wash by rubbing or brushing hard"),
            CreateWord("watched", "v. 1 to look closely or carefully; 2.to look or wait in anticipation; 3 to be careful."),
            CreateWord("habit", "n. 1 something that a person does so often that it is done without thinking; 2 a special kind of clothing worn by certain groups."),
            CreateWord("split", "v. 1.to divide along the length or in layers; 2.to break up or separate by force or as though by force; tear apart; 3.to divide into parts"),
            CreateWord("rabbit", "n. a small mammal with long ears and long back legs for running or jumping; also called a hare"),
            CreateWord("credit", "n. 1 honor or praise; a way of expressing thanks; 2 a way of buying things and paying for them later -He has a lot of good credit.-"),
            CreateWord("practice", "v. to do over and over so as to become skilled; n. working at a profession or occupation"),
            CreateWord("price", "n. the sum or amount of money or its equivalent for which anything is bought sold or offered for sale."),
            CreateWord("slice", "n. 1 a serving that has been cut from a larger portion; 2 a share of something; v. make a clean cut through"),
            CreateWord("voice", "n. the sounds from the mouth made in speaking or singing; v. to make known"),
            CreateWord("choice", "n. an act of choosing or selecting; selection; adj. of high quality; excellent."),
            CreateWord("twice", "adv. on two occasions or in two instances two times two times as much or as many; twofold; doubly"),
            CreateWord("police", "n. the governmental department organized for keeping order and enforcing the law; v. maintain the security of by carrying out a control"),
            CreateWord("nice", "adj. 1 pleasant or pleasing or agreeable in nature or appearance; 2 done with delicacy and skill"),
            CreateWord("knit", "v. 1 to make by joining together loops of yarn by hand with long needles or by machine; 2 to bring into a whole; unite"),
            CreateWord("visit", "v. and n. 1 to go or come to see; 2.to stay with for a short time as a guest."),
            CreateWord("quit", "v. to give up or stop doing something"),
            CreateWord("people", "n. human beings in general"),
            CreateWord("custom", "n. a long-standing tradition; adj. made specially for individual customers"),
            CreateWord("praise", "v. 1 to speak well of; 2 to honor with words or song; n. words that show admiration or respect."),
            CreateWord("raisin", "n. dried grape"),
            CreateWord("stair", "n. a flight of steps; stairway a single step; esp. one of a series forming a stairway"),
            CreateWord("stare", "v. look at with fixed eyes; n. a fixed look with eyes open wide"),
            CreateWord("hare", "n. swift timid long-eared mammal larger than a rabbit having a divided upper lip and long hind legs"),
            CreateWord("laugh", "n. the sound of the manifestation of joy or mirth; v. the manifestation of joy or mirth or scorn"),
            CreateWord("survey", "v. to look over; to examine; n. a study designed to gather information about a subject."),
            CreateWord("August", "n. the eighth month of the year. It has thirty-one days."),
            CreateWord("guess", "n. an opinion or judgment based on little or no evidence; v. to form an opinion from little or no evidence"),
            CreateWord("grieve", "v. to feel deep sadness or mental pain"),
            CreateWord("together", "adv. 1 in each other's company; 2 at the same time"),
            CreateWord("build", "v. 1 make by putting together materials and parts; 2 to produce or bring about especially by long or repeated effort"),
            CreateWord("bruise", "v. to wound or damage without causing a break in the skin or bone; n. an injury due to bruising; contusion."),
            CreateWord("fair", "adj. free from favoritism or self-interest; attractive in appearance; n. a traveling show with rides and games"),
            CreateWord("pair", "n. and v. 1 two things that are alike and meant to be used together; 2.a single object made up of two parts joined together. 3 two persons together like a couple"),
            CreateWord("hair", "n. any of the cylindrical filaments characteristically growing from the epidermis of a mammal"),
            CreateWord("stairs", "n. a flight of steps; stairway a single step; esp. one of a series forming a stairway"),
            CreateWord("near", "not far; close;"),
            CreateWord("clear", "adj. free from clouds; v. remove"),
            CreateWord("heard", "v. to take in through the ear"),
            CreateWord("fear", "n. an unpleasant often strong emotion caused by expectation or awareness of danger; v. concern about what may happen"),
            CreateWord("search", "v. try to locate or discover; n. the activity of looking thoroughly in order to find something"),
            CreateWord("steer", "v. to guide or control the course of; n. a type of male cow raised to produce beef"),
            CreateWord("stereo", "n. a stereophonic high-fidelity sound reproduction device; adj. designating sound transmission from two sources through two channels"),
            CreateWord("turkey", "n. any of a family -Meleagrididae- of large; gallinaceous North American birds with a small; naked head and spreading tail"),
            CreateWord("third", "adj. coming next after the second and just before the fourth in position; n. one of three equal parts of a divisible whole"),
            CreateWord("nurse", "n. a person who is trained to care for sick and injured people v. 1 to give medical care to; 2 to feed from a breast."),
            CreateWord("world", "n. 1 the universe; everything that exists; 2.the earth and all those who live on it. 3 the whole human race; all people. 4 a great amount."),
            CreateWord("unfold", "v. 1 extend or stretch out to a greater or the full length; 2 develop or come to a promising stage"),
            CreateWord("insect", "n. small animals without backbones -as spiders or flies- that have bodies made up of segments"),
            CreateWord("invite", "v. to ask in a polite way to come somewhere or do something; to welcome or ask for; to bring about by doing or saying; to risk causing"),
            CreateWord("unusual", "adj. noticeably different from what is generally found or experienced"),
            CreateWord("uncommon", "adj. unusual or rare"),
            CreateWord("unafraid", "adj. 1 oblivious of dangers or perils or calmly resolute in facing them; 2 free from fear or doubt; easy in mind"),
            CreateWord("unlucky", "adj. unfortunate"),
            CreateWord("instead", "adv. in place of the person or thing mentioned"),
            CreateWord("untie", "v. 1 undo the ties of; 2 cause to become loose"),
            CreateWord("unload", "v. 1 to remove the load from; 2 to remove; 3 to remove the ammunition from."),
            CreateWord("inventor", "n. a person who invents; esp; one who devises a new contrivance; method; etc."),
            CreateWord("initial", "adj. coming before all others in time or order; v. mark with the first letters of one's name"),
            CreateWord("until", "prep. up to the time of"),
            CreateWord("unsure", "adj. lacking or indicating lack of confidence or assurance"),
            CreateWord("unwrap", "v. to remove or open the wrapping of"),
            CreateWord("tired", "v. 1 to become in need of rest; 2 become bored or impatient; adj. depleted of strength or energy"),
            CreateWord("dyed", "v. to give color or a different color to"),
            CreateWord("mixed", "v. to put different things together so that the parts become one; 2.to put together in a confused way; 3.to meet and greet other people"),
            CreateWord("listened", "v. to try to hear; to pay attention to"),
            CreateWord("shouted", "v. utter in a loud voice; talk in a loud voice"),
            CreateWord("returned", "v. 1 to go back or come back; 2.to send put give or take back to an earlier place"),
            CreateWord("acted", "v. to give the impression of being; to present a portrayal or performance of"),
            CreateWord("stated", "v. express in words; adj. declared as fact"),
            CreateWord("raced", "v. to engage in a contest -especially of speed-"),
            CreateWord("remarked", "v. make mention of; make or write a comment on"),
            CreateWord("fed", "v. to provide food or meals for"),
            CreateWord("pierced", "v. 1 make a hole into; 2 cut or make a way through; 3 move or affect deeply or sharply"),
            CreateWord("died", "v. pass from physical life and lose all bodily attributes and functions necessary to sustain life"),
            CreateWord("delighted", "v. 1 to give great pleasure or joy; 2 to take or find pleasure"),
            CreateWord("smiled", "v. change one's facial expression by turning up the corners of the mouth; usually shows pleasure or amusement"),
            CreateWord("happily", "adv. merrily or in a joyous manner"),
            CreateWord("barrel", "n. 1 a cylindrical container that holds liquids; 2 a tube through which a bullet travels when a gun is fired; 3 a bulging cylindrical shape; hollow with flat ends"),
            CreateWord("rearrange", "v. put into a new order or arrangement"),
            CreateWord("dripped", "v. fall in drops; n. flowing in drops"),
            CreateWord("terrace", "n. 1.a flat paved surface outside of a house or other building; patio; 2.a flat raised section of ground; 3.a small balcony with a roof."),
            CreateWord("puppies", "n. a young dog"),
            CreateWord("happier", "adj. merrily or in a joyous manner"),
            CreateWord("applied", "v. to occupy oneself diligently or with close attention"),
            CreateWord("supplying", "n. an amount of something available for use"),
            CreateWord("sorry", "adj. 1 feeling regret sympathy or sadness; 2 of low quality; terrible; poor; 3 without honor; low; Interjection. used as an expression of apology"),
            CreateWord("correct", "adj. being in agreement with the truth or a fact or a standard ; v. to point out or mark the errors in"),
            CreateWord("hurry", "verb and noun. to move or act with speed; to rush"),
            CreateWord("surrounds", "plural v. to enclose on all sides"),
            CreateWord("purr", "n. and v. a low; vibratory sound made by a cat when it seems to be pleased any sound like this"),
            CreateWord("currency", "n. 1 something that is used as a medium of exchange; money; 2 general acceptance; prevalence; vogue"),
            CreateWord("giant", "adj. unusually large"),
            CreateWord("ninety", "adj. or n. nine times ten"),
            CreateWord("want", "v. to have an earnest wish to own or enjoy; n. the state of lacking sufficient money or material possessions"),
            CreateWord("sequel", "n. something that follows"),
            CreateWord("trade", "n. and v. 1 the act of exchanging or buying and selling goods; 2 a job that involves a particular skill"),
            CreateWord("said", "v. express in words; adj. being the one previously mentioned or spoken of"),
            CreateWord("wreck", "n. an action or event that results in great or total destruction; v. 1 to ruin or destroy; 2 to tear down or apart."),
            CreateWord("bridge", "n. a structure built over something so people can cross; v. to make a passage over"),
            CreateWord("sneak", "v. to go stealthily or furtively; n. person who is very dishonest; adj. marked by quiet and caution and secrecy"),
            CreateWord("bought", "v. obtain by purchase"),
            CreateWord("soak", "v. submerge in a liquid; adj. wet through and through"),
            CreateWord("lose", "v. to be unable to find or have at hand; to fail to win or gain or obtain"),
            CreateWord("doubt", "n. the state of being unsure of something; v. consider unlikely or lack confidence in"),
            CreateWord("sure", "adj. exercising or taking care great enough to bring assurance; adv. definitely or positively"),
            CreateWord("whirl", "to turn or spin quickly on a central point; to turn or change direction suddenly; wheel about; to have a feeling of spinning quickly; reel;"),
            CreateWord("full", "adj. containing as much or as many as is possible or normal; adv. to the greatest degree or extent"),
            CreateWord("puddle", "n. a small pool of water; v. wade in or dabble in a small pool of wtaer"),
            CreateWord("illness", "n. impairment of normal physiological function affecting part or all of an organism"),
            CreateWord("gallon", "n. abbrev. gal. a unit of liquid measure; equal to four liquid quarts"),
            CreateWord("smell", "v. 1 inhale the odor of; 2 emit an odor; n. any property detected by the olfactory system"),
            CreateWord("odd", "adj. 1 miscellaneous; 2 not divisible by two; 3 not easily explained"),
            CreateWord("million", "n. the number that is represented as a one followed by six zeros; a very large indefinite number"),
            CreateWord("paddle", "n. and v. 1 an oar with a wide flat blade and long handle; 2 a similar smaller device with a short handle used to hit the ball in table tennis."),
            CreateWord("village", "n. a community of people smaller than a town"),
            CreateWord("dollar", "n. the basic monetary unit of the U.S; equal to 100 cents; symbol - $"),
            CreateWord("ladder", "n. a framework consisting of two parallel sidepieces connected by a series of rungs or crosspieces on which a person steps in climbing up or down"),
            CreateWord("collie", "n. any of a breed of large; long-haired dog with a long; narrow head; first bred in Scotland for herding sheep"),
            CreateWord("balloon", "n. 1 small thin inflatable rubber bag with narrow neck; 2 large tough nonrigid bag filled with gas or heated air; v. ride in a hot-air blimp"),
            CreateWord("yellow", "the color of an egg yolk or ripe lemon; the color between orange and green on the color spectrum; cowardly."),
            CreateWord("hollow", "empty out; make concave; an empty space inside something; hole; cavity; gap."),
            CreateWord("recycle", "v. to obtain a raw material by separating it from a by-product or waste product"),
            CreateWord("refill", "v. make full again; n. a prescription drug that is provided again"),
            CreateWord("recess", "n. a momentary halt in an activity; a period during which the usual routine of school or work is suspended; a hollowed-out space in a wall"),
            CreateWord("rebel", "n. a person who rises up against authority"),
            CreateWord("remain", "v. to go on being; continue in a particular way without a change; to stay or be left in the same place after others have gone"),
            CreateWord("recipe", "n. a list of ingredients and instructions for making a food dish"),
            CreateWord("ready", "v. to cause to be ready or to make ready; prepare; adj. 1 prepared; 2 able to perform or be used; fit; 3 quick to answer; 4 willing."),
            CreateWord("reread", "v. read again"),
            CreateWord("really", "adv. in fact; actually; certainly; truly; interjection . used to show disgust surprise or doubt."),
            CreateWord("reduce", "v. make less; decrease; to lower in degree intensity"),
            CreateWord("rewrite", "v. revise"),
            CreateWord("review", "v. to examine or look over again; to give a report on the strengths and weaknesses of; to look back over; think back on; n. a looking back over past"),
            CreateWord("record", "v. set down in permanent form; n. the sum of recognized accomplishments"),
            CreateWord("report", "n. a written document or verbal account describing the findings of some individual or group; v. to give an account or representation of in words"),
            CreateWord("resource", "n. a new or a reserve source of supply or support; the ability to meet and deal with difficult situations"),
            CreateWord("graceful", "adj. moving easily; having beauty of movement."),
            CreateWord("lively", "adj. full of life and energy"),
            CreateWord("suddenly", "adv. happening without warning or in a short space of time"),
            CreateWord("finally", "adv. after a long time"),
            CreateWord("helpful", "adj. giving help or aid"),
            CreateWord("careful", "adj. taking care in one's actions; cautious; done with care and effort."),
            CreateWord("beautiful", "adj. very pleasing to look at ; of weather highly enjoyable"),
            CreateWord("early", "before the usual or expected time; in the first part of something; near the beginning of something"),
            CreateWord("wonderful", "adj. extraordinarily good; used especially as intensifiers"),
            CreateWord("quietly", "adv. free of noise or uproar; refraining or free from activity"),
            CreateWord("noisily", "adv. with much noise or loud and unpleasant sound"),
            CreateWord("slowly", "adv. not moving or not able to move quickly; taking a long time;"),
            CreateWord("silly", "adj. without good sense; foolish."),
            CreateWord("multiply", "v. to increase the number degree or amount of;"),
            CreateWord("surely", "adv. definitely or positively"),
            CreateWord("wagon", "n. any of various kinds of wheeled vehicles drawn by a horse or tractor"),
            CreateWord("final", "adj. not to be altered or undone; n. an examination administered at the end of an academic term"),
            CreateWord("travel", "v. to take a trip especially of some distance"),
            CreateWord("cents", "plural n. a bronze coin of the U.S. the 100th part of a U.S. dollar; a monetary coin"),
            CreateWord("garden", "n. a plot of ground where plants are cultivated; v. work in above"),
            CreateWord("sugar", "n. a sweet substance that is made up wholly or mostly of sucrose is colorless or white when pure is obtained from plants and is used as a sweetener"),
            CreateWord("chance", "n. a possibility due to a favorable conditions; a risk involving danger"),
            CreateWord("circle", "a closed curve made up of points that are all the same distance from a fixed center point; a group of people who are related by blood"),
            CreateWord("pencil", "n. a thin cylindrical pointed writing implement; v. write or draw or trace with a pencil"),
            CreateWord("circus", "n. a traveling show that usually includes performances by acrobats and clowns and trained animals"),
            CreateWord("nickel", "Symbol-Ni; atomic number-28; a silver-white hard malleable ductile metallic element capable of a high polish and resistant to corrosion"),
            CreateWord("center", "n. the middle point; the object upon which interest and attention focuses"),
            CreateWord("erase", "v. to rub or scrape or wipe out -esp. written or engraved letters-"),
            CreateWord("paper", "n. 1 a thin flexible material made usually in sheets from a pulp prepared from rags or wood or other fibrous material; 2 an essay"),
            CreateWord("since", "adv. 1 from then until now; 2 at some or any time between then and now; subsequently; 3 before the present time; before now; ago"),
            CreateWord("place", "1. a certain area of space that is taken up by something; 2 a duty or job 3 one's home 4 a point in a series 5 a proper position"),
            CreateWord("pretty", "adj. pleasing or attractive to the eyes or ears; adv. somewhat or fairly; very; quite"),
            CreateWord("wife", "n. a married woman; a man's partner in marriage"),
            CreateWord("pale", "adj. lacking a healthy skin color"),
            CreateWord("cheek", "n. 1 either side of the face between the nose and ear and below the eye; 2 either of the two large fleshy masses of muscular tissue that form the human rump"),
            CreateWord("cheese", "n. a food made by pressing together a mass of soft thick soured milk solids."),
            CreateWord("chocolate", "n. candy made from the seeds of cacao roasted and husked and ground; a dark brown color"),
            CreateWord("knuckle", "n. a joint of the finger"),
            CreateWord("breakfast", "n. the first meal of the day"),
            CreateWord("wheel", "n. and v. 1 a round frame that turns on the axle; 2.any instrument or device that looks or acts like such a frame. 3 plural moving forces."),
            CreateWord("sought", "v. 1 try to locate or discover; 2 try to get or reach"),
            CreateWord("foal", "n. a young horse; mule; donkey; etc; colt or filly"),
            CreateWord("foam", "n. a light mass of fine bubbles formed in or on a liquid"),
            CreateWord("would", "v. past tense of will that is used to express desire or intent"),
            CreateWord("shrink", "v. to become smaller in size or volume through the drawing together of particles of matter"),
            CreateWord("began", "v. 1 to do the first step in a process; 2 set in motion cause to start"),
            CreateWord("woman", "n. an adult female human being"),
            CreateWord("gain", "v. 1 obtain; 2 derive a benefit from; 3 increase in; n. a quantity that is added"),
            CreateWord("pain", "n. a sharp unpleasant sensation usually felt in some specific part of the body"),
            CreateWord("brain", "n. 1.the organ inside the skull of humans and animals; 2 intelligence."),
            CreateWord("stain", "v. 1 make dirty or spotty; 2 color with a liquid dye or tint; n. 1 a soiled or discolored appearance; 2 a dye or other coloring material"),
            CreateWord("chain", "n. 1 a flexible series of joined links; usually of metal; 2 a series of things depending on each other as if linked together; v. fasten or secure with above"),
            CreateWord("plain", "adj. 1 not complicated or fancy; without anything extra; 2 easily seen or heard; n. a large flat area of land without trees;"),
            CreateWord("certain", "adj. having or showing a mind free from doubt"),
            CreateWord("grain", "n. 1 a small hard seed or seedlike fruit; esp. that of any cereal plant; 2 a small hard particle; 3 the direction or texture of fibers found in wood or leather"),
            CreateWord("ocean", "n. the whole body of salt water that covers nearly three-fourths of the earth"),
            CreateWord("clean", "v. to remove dirt or stains from; adj. 1 not dirty or stained; 2 pure; free from pollution"),
            CreateWord("groan", "v. and n. to make a deep sound made to show pain grief or sadness."),
            CreateWord("moan", "n. a long low sound of pain grief or sorrow; v. to make a moan or a similar sound; to express unhappiness; complain"),
            CreateWord("loan", "n. the temporary provision of money usually at interest; v. give temporarily"),
            CreateWord("onion", "n. a round bulb vegetable with a sharp taste and smell"),
            CreateWord("once", "adv. one time only"),
            CreateWord("oatmeal", "n. oats ground or rolled into meal or flakes a porridge made from such oats"),
            CreateWord("dimmed", "v and adj. 1.not well lighted; dark; 2.faint or dull; 3.not clear to the senses or mind; 4.not able to see or understand clearly."),
            CreateWord("different", "adj. being not of the same kind"),
            CreateWord("dirt", "n. 1 any unclean or soiling matter; 2 earth or garden soil"),
            CreateWord("dirty", "adj. soiled or likely to soil with dirt or grim"),
            CreateWord("odor", "n. any property detected by the olfactory system"),
            CreateWord("order", "n. a tidy state; v. to give a request or demand for; to put in order; organize"),
            CreateWord("Oregon", "NW coastal state of the U.S; admitted in 1859; 95,997 sq mi 248,631 sq km; pop. 3,421,000; capital Salem"),
            CreateWord("orange", "n. or adj. of the color between red and yellow; n. round fruit of any of several citrus trees"),
            CreateWord("owl", "n. nocturnal bird of prey with hawk-like beak and claws and large head with front-facing eyes"),
            CreateWord("division", "n. the act or process of a whole separating into two or more parts or pieces"),
            CreateWord("dinosaur", "n. any of numerous extinct terrestrial reptiles of the Mesozoic era"),
            CreateWord("our", "pron. that which belongs to us"),
            CreateWord("losing", "v. to fail to win or gain or obtain; to be unable to find or have at hand"),
            CreateWord("freezing", "n. the withdrawal of heat to change something from a liquid to a solid; v. 1 change to ice; 2 be cold"),
            CreateWord("crying", "v. 1 shed tears; 2 utter aloud; n. the process of shedding tears; adj. noisy with or as if with loud weeping and shouts"),
            CreateWord("frying", "v. cook on a hot surface using fat; n. cooking in fat or oil in a pan or griddle"),
            CreateWord("dropping", "v. 1 fall to a lower place or level; 2 let fall to the ground"),
            CreateWord("surviving", "v. stay alive or continue to exist"),
            CreateWord("hopping", "v. jump lightly"),
            CreateWord("stopping", "v. come to a halt"),
            CreateWord("planning", "v. think out; prepare in advance; n. an act of formulating a program for a definite course of action"),
            CreateWord("snapping", "v. 1 separate; break 2 to snatch or grasp quickly or eagerly; 3 utter in an angry sharp or abrupt tone; 4 click"),
            CreateWord("dancing", "v. to move the body and feet in rhythm; ordinarily to music; n. moving feet and body to music"),
            CreateWord("happening", "n. something that happens; occurrence; incident; event a theatrical performance of unrelated and bizarre or ludicrous actions"),
            CreateWord("morning", "n. 1 the first part of the day ending at or around noon; 2 sunrise; daybreak; dawn"),
            CreateWord("using", "v. 1 to bring into service; 2 to spend; consume."),
            CreateWord("pleasing", "v. to give pleasure or satisfaction to; adj. giving pleasure and satisfaction"),
            CreateWord("salmon", "n. any of various large food and game fishes of northern waters; adj. of orange tinged with pink"),
            CreateWord("common", "adj. 1 widely existing; 2 belonging to or participated in by a community as a whole"),
            CreateWord("lesson", "n. something to be learned; an exercise or assignment that a student is to prepare or learn within a given time; unit of instruction"),
            CreateWord("cannon", "n. a large heavy gun usually mounted on wheels"),
            CreateWord("dragon", "n. an imaginary monster that looks like a giant lizard with wings claws and a long tail"),
            CreateWord("crayon", "n. a stick of colored wax or charcoal or chalk used for drawing or writing"),
            CreateWord("lick", "v. 1 to pass the tongue over 2 beat thoroughly and conclusively in a competition or fight"),
            CreateWord("thick", "adj. having or being of great depth or extent from one surface to its opposite"),
            CreateWord("quick", "adj. or adv. rapid or swift"),
            CreateWord("dead", "adj.1. no longer living; 2 not working"),
            CreateWord("spread", "v. distribute or disperse widely; n. process or result of distributing or extending over a wide expanse of space"),
            CreateWord("head", "n. 1 the top or leading part of an animal body; 2.mind; intellect; understanding. 3 a position of leadership or authority or the person in such a position."),
            CreateWord("thread", "n. a thin string made of strands twisted together; v. 1 to go along on carefully or with difficulty; 2 pass a cord of fibers through"),
            CreateWord("vegetation", "n. green leaves or plants; dull or inactive living"),
            CreateWord("subtraction", "n. 1 the taking away of a part of something; 2.an operation that finds the difference between two numbers or how many are left when some are taken away."),
            CreateWord("globe", "n. the planet Earth; a sphere on which is depicted a map of the earth"),
            CreateWord("stole", "v. 1 to take the property of another without permission or right; 2 move stealthily"),
            CreateWord("central", "adj. coming before all others in importance"),
            CreateWord("perch", "v. to come to rest after descending from the air"),
            CreateWord("speech", "n. a usually formal discourse delivered to an audience; communication by word of mouth"),
            CreateWord("launch", "v. to hurl or discharge or send off; to get started or to make a move"),
            CreateWord("squeak", "n. a short high shrill sound or cry; v. to give off a high shrill sound"),
            CreateWord("sweep", "v. to remove from a surface with or as if with a broom or brush"),
            CreateWord("frighten", "v. to cause fear in; scare"),
            CreateWord("climb", "v. and n. 1 to move upward; go towards the top; rise; 2 to grow upward on a tall support; 3 to go up by foot."),
            CreateWord("float", "v. to rest or move along the surface of a liquid or in the air; n. a drink with ice cream in it"),
            CreateWord("survivor", "n. a person who lives through a difficult event or experience; someone or something that survives"),
            CreateWord("crowd", "n. a great number of persons or things gathered together; v. to gather into a closely packed group"),
            CreateWord("truth", "n. agreement with fact or reality"),
            CreateWord("measure", "v. to find out the size of ; n. something set up as an example against which others of the same type are compared")
        ];

        return new Dictionary<string, string>(words);
    }

    private Dictionary<string, string> SetupMediumWords()
    {
        List<KeyValuePair<string, string>> words = 
        [
            CreateWord("stunned", "v. 1 surprise greatly; 2 make senseless or dizzy by or as if by a blow; adj. filled with the emotional impact of overwhelming surprise or shock"),
            CreateWord("happened", "v. to take place; occur; befall to be or occur by chance or without plan; to have the luck or occasion; chance"),
            CreateWord("transferred", "v. to convey or carry or remove or send from one person or place or position to another"),
            CreateWord("acquitted", "v. to free from a charge of wrongdoing"),
            CreateWord("referred", "v. 1 to send or direct to a source for help; 2 to pass or hand over for advice or help; 3 to speak of; mention."),
            CreateWord("committed", "v. to carry through as a process to completion"),
            CreateWord("omitted", "v. leave undone or leave out"),
            CreateWord("permitted", "v. to give permission for or to approve of"),
            CreateWord("propeller", "n. a mechanical device consisting of revolving blades that rotates to push against air or water"),
            CreateWord("spinning", "v. revolve quickly and repeatedly around one's own axis; n. creating thread"),
            CreateWord("knitting", "v. 1 to make by joining together loops of yarn by hand with long needles or by machine; 2 to bring into a whole; unite"),
            CreateWord("upsetting", "v. 1 to disturb mentally or emotionally or physically; 2 to overturn; 3 defeat suddenly and unexpectedly; adj. causing an emotional disturbance"),
            CreateWord("excelling", "v. to be better or greater than; superior to"),
            CreateWord("occurring", "v. 1 to take place or happen; 2 come to one's mind; 3 to be found to exist"),
            CreateWord("submitting", "v. to present for the approval or consideration or decision of another or others; to give over or yield to the power or authority of another;"),
            CreateWord("airmail", "n. mail transported by air; esp; in the U.S; mail going overseas by air a system for transporting mail by air -example-send it by airmail- Also"),
            CreateWord("skateboard", "n. a board with wheels that is ridden in a standing or crouching position and propelled by foot; v. ride on a flat board with rollers attached to the bottom"),
            CreateWord("quarterback", "n. the position of the football player in the backfield who directs the offensive play of his team"),
            CreateWord("fainthearted", "adj. lacking courage or conviction; timid."),
            CreateWord("overdue", "adj. past due as a delayed train or a bill not paid by the assigned date; late"),
            CreateWord("viewpoint", "n. 1 a mental position from which things are viewed; 2 a place from which something can be viewed"),
            CreateWord("afterthought", "n. a later or second thought"),
            CreateWord("thoroughbred", "n. a pedigreed animal of unmixed lineage; adj. having a list of ancestors as proof of being a purebred animal"),
            CreateWord("eyesight", "n. the power of seeing; sight; vision the range of vision"),
            CreateWord("outstanding", "adj. standing apart from others due to being excellent; unpaid"),
            CreateWord("headphones", "n. an earphone held over the ear by a band worn on the head"),
            CreateWord("xylophone", "n. a musical instrument consisting of a series of wooden bars varying in length and sounded by striking with two wooden hammers"),
            CreateWord("postpone", "v. to assign to a later time"),
            CreateWord("furthermore", "adv. in addition; besides; moreover"),
            CreateWord("yourselves", "pronoun. 1 used to emphasize you; your own self; 2.your usual or healthy self."),
            CreateWord("wolves", "plural n. any of various predatory carnivorous canine mammals that usually hunt in packs"),
            CreateWord("dungarees", "n. pants made of this cloth; blue jeans; a heavy cotton fabric; blue denim"),
            CreateWord("measles", "n. an acute; infectious; communicable disease caused by a paramyxovirus and characterized by small; red spots on the skin; high fever; nasal discharge; etc. and occurring most frequently in childhood"),
            CreateWord("abilities", "plural n. the physical or mental power to do something; talents or special skills or aptitudes"),
            CreateWord("factories", "plural n. a building or set of buildings where products are made by machines"),
            CreateWord("memories", "plural n. the power or process of recalling what has been previously learned or experienced"),
            CreateWord("butterflies", "plural n. diurnal insect typically having a slender body with knobbed antennae and broad colorful wings"),
            CreateWord("hobbies", "plural n. an interest or activity that one does for pleasure in one's spare time."),
            CreateWord("groceries", "plural n. n. a store where food and other household supplies are sold; grocery store; a place to buy groceries"),
            CreateWord("volcanoes", "n. a vent in the earth's crust from which melted or hot rock and steam come out"),
            CreateWord("buffaloes", "plural n. large shaggy-haired brown bison of North American plains; v. intimidate or overawe"),
            CreateWord("dominoes", "n. 1 any of several games played with small rectangular blocks; 2 a loose hooded cloak worn with a half mask as part of a masquerade costume"),
            CreateWord("coordinates", "plural n. a set of numbers or a single number that locates a point on a line on a plane or in space; v. v. to arrange to work well together"),
            CreateWord("businesses", "plural n. 1 an occupation or profession or trade; 2 a commercial or industrial enterprise and the people who constitute it"),
            CreateWord("temperatures", "n. the degree of hotness or coldness of a body or environment corresponding to its molecular activity"),
            CreateWord("disseminate", "v. to cause to be known over a considerable area or by many people"),
            CreateWord("distress", "n. a state of physical or mental suffering v. to cause sorrow or misery"),
            CreateWord("disability", "n. lack of ability or power or fitness to do something"),
            CreateWord("disdainfully", "adv. feeling or showing scorn contempt or aloofness."),
            CreateWord("disguised", "v. to change the dress or looks of so as to conceal true identity"),
            CreateWord("disagreeable", "adj. having or showing a habitually bad temper"),
            CreateWord("disobedience", "n. failure or refusal to obey"),
            CreateWord("disobedient", "adj. refusing to obey; naughty"),
            CreateWord("disastrous", "adj. bringing about ruin or misfortune"),
            CreateWord("discontinue", "v. to stop or put an end to"),
            CreateWord("disciple", "n. one who follows the opinions or teachings of another"),
            CreateWord("dissipate", "v. to disperse or disappear"),
            CreateWord("disinfectant", "n. a substance used to destroy the germs of infectious diseases"),
            CreateWord("distinctive", "adj. of a feature that helps to distinguish a person or thing"),
            CreateWord("disturbance", "n. a disturbing or being disturbed any departure from normal anything that disturbs the state of being worried; troubled; or anxious commotion; disorder"),
            CreateWord("immigrant", "n. one that leaves one place to settle in another"),
            CreateWord("hangar", "n. a shelter for housing and repairing aircraft"),
            CreateWord("fare", "n. the price of a journey on a train or bus or ship; substances intended to be eaten; v. proceed or get along"),
            CreateWord("magician", "n. a person who has skill in magic and entertains people with magic tricks."),
            CreateWord("cathedral", "n. a large and important church"),
            CreateWord("guacamole", "n. mashed avocado that is seasoned with condiments and often served as a spread or dip"),
            CreateWord("tongue", "n. 1 the movable muscular structure attached to the floor of the mouth in most vertebrates; 2 a human written or spoken language used by a community"),
            CreateWord("noticeable", "adj. easy to see; likely to be observed; worthy of attention"),
            CreateWord("dilemma", "n. a situation in which one has to choose between two or more equally unsatisfactory choices"),
            CreateWord("colonel", "n. a military officer ranking above a lieutenant colonel and below a brigadier general; and corresponding to a captain in the navy"),
            CreateWord("symphony", "n. a cominposition for a full ochestra; an elaborate instrumental composition in three or more movements"),
            CreateWord("consistent", "adj. 1 in agreement or reliable; 2 the same throughout in structure or composition"),
            CreateWord("illustration", "n. an illustrating or being illustrated an example; story; analogy; etc. used to help explain or make something clear a picture; design; diagram; etc. used to decorate or explain something"),
            CreateWord("identical", "adj. resembling another in every respect; being one and not another"),
            CreateWord("resourceful", "adj. capable of dealing with difficult situations quickly and imaginatively."),
            CreateWord("assure", "v. to make sure or certain or safe"),
            CreateWord("bureau", "n. 1 furniture with drawers for keeping clothes; 2 an administrative unit of government"),
            CreateWord("bureaucratic", "n. an official in a government office esp. one who follows rules and routines rather than personal judgment"),
            CreateWord("cure", "n. and v. something that makes a sick person healthy or well; v. to preserve by salting or smoking or drying."),
            CreateWord("enclosure", "n. 1 an area that is fenced off and used for a special purpose; 2 something -usually a supporting document- that is included in an envelope with a covering letter"),
            CreateWord("ensure", "v. to make sure or certain or safe"),
            CreateWord("immature", "adj. not completely grown or developed"),
            CreateWord("lecture", "n. a talk given before an audience or class especially for instruction; v. to criticize someone severely or angrily especially for personal failings"),
            CreateWord("leisure", "n. freedom from activity or labor"),
            CreateWord("manufacture", "v. 1 to make from raw materials by hand or by machinery; 2 to make into a product suitable for use; n. the making of something especially using machinery"),
            CreateWord("mature", "adj. fully grown or developed; v. to bring or come to full development"),
            CreateWord("miniature", "adj. being on a small or greatly reduced scale; n. a representation or image of something on a small or reduced scale"),
            CreateWord("pertinent", "adj. having to do with the matter at hand"),
            CreateWord("pressure", "n. the burden on one's emotional or mental well-being created by demands on one's time; v. to force someone toward a particular end"),
            CreateWord("temperature", "n. the degree of hotness or coldness of a body or environment corresponding to its molecular activity"),
            CreateWord("indefinitely", "adv. in a vague or uncertain way"),
            CreateWord("illustrate", "v. 1 depict with pictures or drawings; 2 clarify by giving an example of"),
            CreateWord("elude", "v. to keep away from"),
            CreateWord("illegible", "adj. not able to be read"),
            CreateWord("equivalent", "adj. equal in some way"),
            CreateWord("exhilaration", "n. a pleasurably intense stimulation of the feelings"),
            CreateWord("electrician", "n. a person whose work is the construction; repair; or installation of electric apparatus"),
            CreateWord("immediate", "adj. happening right away; instant; happening right away; instant; close in space or time; near"),
            CreateWord("interrupt", "v. to cause to stop; break off; to begin to speak over in the middle of in a way that breaks off."),
            CreateWord("irrelevant", "adj. not having anything to do with the matter being considered or talked about."),
            CreateWord("enormous", "adj. very large"),
            CreateWord("extraordinary", "adj. beyond what is usual or ordinary; rare"),
            CreateWord("irresistible", "adj. too strong or delightful or tempting to be resisted"),
            CreateWord("expression", "n. facial appearance regarded as an indication of mood or feeling"),
            CreateWord("essential", "adj. impossible to do without; n. something necessary or indispensable or unavoidable"),
            CreateWord("hazardous", "adj. involiving risk or danger"),
            CreateWord("treacherous", "adj. dangerous or hazardous; deceptive or untrustworthy or unreliable"),
            CreateWord("precipitous", "adj. very steep or sudden; rising or dropping abruptly."),
            CreateWord("atrocious", "adj. outrageously or wantonly wicked or criminal or vile or cruel"),
            CreateWord("ferocious", "adj. violently unfriendly or aggressive in disposition; extreme in degree or power or effect"),
            CreateWord("conspicuous", "adj. likely to attract attention"),
            CreateWord("monotonous", "adj. 1 dull as a result of not changing in any way; 2 causing weariness or restlessness or lack of interest"),
            CreateWord("pretentious", "adj. showy; pompous; claiming unjustified distinction"),
            CreateWord("fictitious", "adj. not real and existing only in the imagination"),
            CreateWord("numerous", "adj. a large number; very many; being of a large but indefinite number"),
            CreateWord("posthumous", "adj. occurring after one's death"),
            CreateWord("preposterous", "adj. utterly ridiculous or absurd"),
            CreateWord("ridiculous", "adj. silly; foolish; laughable."),
            CreateWord("continuous", "adj. going on and on without any interruptions"),
            CreateWord("superstitious", "adj. showing ignorance or the laws of nature and faith in magic or chance"),
            CreateWord("appropriate", "adj. suitable or fitting for a particular purpose or person or occasion; v. to take or make use of without authority or right"),
            CreateWord("sloppiness", "n. 1 a lack of order and tidiness; not cared for; 2 the wetness of ground that is covered or soaked with water"),
            CreateWord("approximate", "adj. not quite exact or correct; v. to estimate; be close or similar;"),
            CreateWord("supplement", "n. 1 something necessary to complete a whole or make up for a deficiency; 2 something extra or additional; tr. v. provide an addition to."),
            CreateWord("apparel", "n. the things that are worn by a person; clothing"),
            CreateWord("applaud", "v. to declare enthusiastic approval of"),
            CreateWord("appreciation", "n. 1 a feeling of thanks; 2 the act of judging worth or quality; 3 a rise in value."),
            CreateWord("appreciative", "adj. feeling or expressive of gratitude"),
            CreateWord("appeal", "n. the power of irresistible attraction; v. take a court case to a higher court for review; to make an earnest request."),
            CreateWord("slippery", "adj. 1.having a slick surface that is difficult to move upon without sliding; 2.difficult to grasp because of a slick surface; 3.not to be trusted"),
            CreateWord("appoint", "v. to name or assign to a position an office or the like; designate"),
            CreateWord("oppose", "v. to be or act against; to refuse to give in to; to strive to reduce or eliminate"),
            CreateWord("opposite", "adj. radically different or contrary in action or movement"),
            CreateWord("Mississippi", "n. 1 a state in the Deep South on the gulf of Mexico; one of the Confederate States during the American Civil War; 2 a major North American river"),
            CreateWord("appetizer", "n. a small portion of a tasty food or a drink to stimulate the appetite at the beginning of a meal a bit of something that excites a desire for more"),
            CreateWord("broccoli", "n. an open branching form of cauliflower that bears young green flowering shoots used as a vegetable"),
            CreateWord("marriage", "n. a union representing a special kind of social and legal partnership between two people"),
            CreateWord("macaroni", "n. pasta in the form of tubes or in various other shapes; often baked with cheese; ground meat; etc. pl. an English dandy in the 18th cent. who affected foreign mannerisms and fashions"),
            CreateWord("commercial", "n. an advertisement on television or radio. adj. 1 having to do with trade or business; 2 having to do with making money."),
            CreateWord("beneficial", "adj. having a good or favorable effect; helpful"),
            CreateWord("plateau", "n. a broad flat area of high ground"),
            CreateWord("drought", "n. 1 a long time without rain; 2 a prolonged shortage"),
            CreateWord("cologne", "n. a liquid similar to perfume but not as strongly scented or as long-lasting"),
            CreateWord("monologue", "n. a long speech or reading given by a single speaker; a speech in a play given by an actor alone -From the Greek root 'mono' meaning One-"),
            CreateWord("intrigue", "v. to draw the strong interest of; puzzle; fascinate; n. a secret plot or scheme."),
            CreateWord("bungalow", "n. a small house with a single story"),
            CreateWord("cantaloupe", "n. 1 a variety of muskmelon vine having fruit with a tan rind and orange flesh; 2 the fruit of the above vine"),
            CreateWord("inexpensive", "adj. relatively low in price or charging low prices"),
            CreateWord("extension", "n. an extra part or addition; something that extends another thing; an extra telephone line connected to the main line"),
            CreateWord("courtesy", "n. polite or thoughtful or considerate behavior; a polite act; a favor"),
            CreateWord("cookware", "n. cooking utensils; pots; pans; etc."),
            CreateWord("career", "n. activity pursued as a livelihood."),
            CreateWord("proceeding", "n. 1 a particular action or course or manner of action; 2 doing a series of activities or events; happenings. -Law- a legal step or measure"),
            CreateWord("tepee", "n. a cone-shaped tent of animal skins or bark used by North American Indian peoples of the plains and Great Lakes regions a similarly shaped Indian dwelling of other materials; such as canvas"),
            CreateWord("heel", "n. 1 the rounded back part of the human foot or a part like it in an animal or shoe; 2 the end piece of something; v. 1 to follow closely behind."),
            CreateWord("Tennessee", "n. 1 a state in east central United States; 2 a river formed by the confluence of two other rivers near Knoxville; it follows a U-shaped course"),
            CreateWord("neighborhood", "n. the people living in a particular area"),
            CreateWord("bookkeeper", "n. the person who keeps the financial records for a business."),
            CreateWord("roommate", "n. one of two or more persons sharing a room or dwelling"),
            CreateWord("cooperative", "adj. done with or working with others for a common purpose or benefit; n. an association formed and operated for the benefit of those using it"),
            CreateWord("whoosh", "v. to move swiftly with a gushing or hissing noise; n. a loud rushing noise"),
            CreateWord("macaroon", "n. a cookie made chiefly of egg whites and sugar and coconut or almond paste; a small cake composed chiefly of the white of eggs and almonds and sugar"),
            CreateWord("coordination", "n. harmonious combination or interaction; the working together of different muscles to carry out a complicated movement"),
            CreateWord("moose", "n. a deer -Alces alces- of N regions; the male of which has huge spatulate antlers and weighs up to 815 kg -c. 1;800 lb-; it is the largest of the deer family"),
            CreateWord("indiscreet", "adj. showing poor judgment especially in personal relationships or social situations"),
            CreateWord("pretzel", "n. a brown cracker that is salted and usually hard and shaped like a loose knot"),
            CreateWord("presence", "n. 1 the state or condition of being in a place at a certain time; 2.the condition of being near in time and space."),
            CreateWord("prepare", "v. to make ready in advance"),
            CreateWord("precipitation", "n. water or the amount of water that falls to the earth as hail or mist or rain or sleet or snow"),
            CreateWord("prearrange", "v. to arrange beforehand"),
            CreateWord("preceding", "v. to go or come before in time; rank or position."),
            CreateWord("preserved", "v. 1 to keep in good condition; 2 to protect; 3 to keep from rotting or spoiling"),
            CreateWord("preserve", "v. 1 to keep in good condition; 2 to protect; 3 to keep from rotting or spoiling"),
            CreateWord("prefer", "v. to like better; to choose first."),
            CreateWord("prey", "n. an animal that is hunted and eaten by another animal; v. to seize and eat"),
            CreateWord("prejudice", "n. an attitude that always favors one way of feeling or acting especially without considering any other possibilities"),
            CreateWord("prevailed", "v. 1 to emerge as dominant; 2 to successfully encourage; 3 to exist widely or generally; 4 to be currently in effect"),
            CreateWord("precursor", "n. something belonging to an earlier time from which something else was later developed"),
            CreateWord("preposition", "n. a word that shows a connection or relation between a noun or pronoun and some other word. In the sentence -example in; on; by; to; since-"),
            CreateWord("premonition", "n. an advance sign or warning; forewarning; an intuition of a future occurrence; presentiment."),
            CreateWord("indolence", "n. indulging in ease; avoiding exertion; lazy."),
            CreateWord("diligence", "n. careful and persevering effort to accomplish what is undertaken"),
            CreateWord("sentence", "n. 1 string of words satisfying the grammatical rules of a language; 2 a final judgment of guilty in a criminal case and the punishment that is imposed"),
            CreateWord("brilliance", "n. the fact of being brilliant; great brightness; radiance; intensity; splendor; intelligence"),
            CreateWord("hindrance", "n. something that makes movement or progress more difficult"),
            CreateWord("elegance", "n. the quality of refinement; taste and grace"),
            CreateWord("intelligence", "n. capacity to know or understand"),
            CreateWord("lance", "v. to pierce or strike with or as if with a spear; n. a long metal-tipped weapon"),
            CreateWord("sequence", "n. serial arrangement in which things follow in a logical order"),
            CreateWord("arrogance", "n. a feeling of too much pride in oneself."),
            CreateWord("patience", "n. the capacity quality or fact of being patient"),
            CreateWord("ordinance", "n. a rule of conduct or action laid down by a governing authority and especially a legislator"),
            CreateWord("announce", "v. to make known; declare"),
            CreateWord("conscience", "n. the faculty in man by which he distinguishes between right and wrong in character and conduct"),
            CreateWord("significance", "n. importance; meaning."),
            CreateWord("electric", "adj. 1 using or providing or producing or transmitting or operated by electricity; 2 affected by emotion as if by electricity; thrilling"),
            CreateWord("abbreviate", "v. to shorten the time or length of"),
            CreateWord("envelop", "v. to cover wrap enclose or surround"),
            CreateWord("accompaniment", "n. a subordinate part or parts enriching or supporting the leading part"),
            CreateWord("assent", "v. to give ones consent; to agree; n. an act of agreeing or acceptance."),
            CreateWord("entertaining", "v. to interest and amuse; to have guests; to have in mind; consider"),
            CreateWord("artificial", "adj. being such in appearance only and made with or manufactured from usually cheaper materials; lacking in natural or spontaneous quality"),
            CreateWord("assault", "n. the act or action of setting upon with force or violence; v. to take sudden violent action against"),
            CreateWord("exercised", "v. 1 energetic movement of the body for the sake of physical fitnes; 2 an act of putting into practice; use."),
            CreateWord("illiterate", "adj. unable to read or write."),
            CreateWord("efficient", "adj. operating or working in a way that gets results with little wasted effort"),
            CreateWord("equator", "n. an imaginary circle around the earth everywhere equally distant from the north pole and the south pole"),
            CreateWord("encounter", "v. to meet unexpectedly; to be faced with; n. a chance meeting; a battle or fight"),
            CreateWord("insincere", "adj. showing false or dishonest feelings or opinions; phony"),
            CreateWord("inconsiderate", "adj. not thinking of other people's feelings; thoughtless or rude"),
            CreateWord("debt", "n. something owed by one person to another or others an obligation or liability to pay or return something the condition of owing"),
            CreateWord("solemn", "adj. having or showing a serious and reserved manner"),
            CreateWord("barricade", "n. a physical object that blocks the way"),
            CreateWord("pedestal", "n. a columnlike stand for displaying a piece of sculpture or other decorative article"),
            CreateWord("mediocre", "adj. of average to below average quality"),
            CreateWord("imperfect", "n. and adj. 1 not perfect; defective or inadequate; 2 having the attributes of man as opposed to"),
            CreateWord("ceremony", "n. a formal act or set of formal acts established by custom or authority as proper to a special occasion"),
            CreateWord("phonics", "n. a method of teaching reading and spelling based upon the phonetic interpretation of ordinary spelling"),
            CreateWord("susceptible", "adj. readily taken advantage of"),
            CreateWord("schedule", "n. a plan of when certain actions or events will be carried out; v. to set the date or time of; table of events and times they happen"),
            CreateWord("descendant", "n. a person who is an offspring -however remote- of a certain ancestor or family or group"),
            CreateWord("commotion", "n. a state of noisy and confused activity"),
            CreateWord("antibiotic", "n. a chemical substance derivable from a mold or bacterium that kills microorganisms and cures infections"),
            CreateWord("concentration", "n. giving total attention to something; consolidation of effort"),
            CreateWord("inaccurate", "adj. 1 not correct; 2 not exact; in error"),
            CreateWord("manipulate", "v. handled skillfully or cleverly"),
            CreateWord("eliminate", "v. 1 do away with; 2 terminate or take out; 3 dismiss from consideration or a contest"),
            CreateWord("propel", "v. to push or drive forward"),
            CreateWord("condemn", "v. to declare to be morally wrong or evil; to express one's unfavorable opinion of the worth or quality of"),
            CreateWord("strategy", "n. a method worked out in advance for achieving some objective; the means or procedure for doing something"),
            CreateWord("companion", "n. 1 a person frequently seen in the company of another; 2 either of a pair of something matched in one or more qualities"),
            CreateWord("boulevard", "n. a broad avenue in a city usually having areas at the sides or center for trees or grass or flowers."),
            CreateWord("laughter", "n. the sound of the manifestation of joy or mirth"),
            CreateWord("chauffeur", "n. a person hired to drive a private automobile for someone else"),
            CreateWord("beautician", "n. a person who does hair styling; manicures; etc. in a beauty salon; cosmetologist"),
            CreateWord("civilian", "n. a person not on active duty in the armed services or not on a police or firefighting force; adj. performed by persons who are not active military"),
            CreateWord("neutral", "adj. not favoring or joined to either side in a quarrel or contest or war"),
            CreateWord("lasagna", "n. 1 large flat rectangular strips of pasta; 2 baked dish of layers of the above pasta with sauce and cheese and meat or vegetables"),
            CreateWord("guardian", "n. one who protects; one who legally has the care of another person"),
            CreateWord("interior", "adj. situated farther in; n. an internal part"),
            CreateWord("colossal", "adj. amazingly large or great or powerful in size or degree"),
            CreateWord("unnecessary", "adj. not needed or required"),
            CreateWord("associate", "n. a person frequently seen in the company of another; a fellow worker; v. to come or be together as friends"),
            CreateWord("embassy", "n. 1 an ambassador and his staff; 2 the official headquarters or residence of an ambassador."),
            CreateWord("ambassador", "n. 1.a person who is sent by the government of one country to be its official representative in another country; anyone who is sent as a messenger"),
            CreateWord("croissant", "n. a flaky rich crescent-shaped roll"),
            CreateWord("commissioner", "n. an official in charge of a government department; a member of a group of persons directed to perform some duty"),
            CreateWord("stubbornness", "adj. unreasonably obstinate; obstinately unmoving"),
            CreateWord("permission", "n. the approval by someone in authority for the doing of something"),
            CreateWord("assist", "v. to give help or support. n. the act of helping; a pass as in basketball or ice hockey that enables the receiver to score a goal"),
            CreateWord("compression", "n. the act or process of reducing the size or volume of something"),
            CreateWord("aggressive", "adj. having or showing a bold forcefulness in the pursuit of a goal; feeling or displaying eagerness to fight"),
            CreateWord("necessity", "n. something that cannot be avoided or done without; great need"),
            CreateWord("compassion", "n. feeling of sorrow or pity caused by the suffering or misfortune of another"),
            CreateWord("pessimism", "n. the belief that events will turn out badly; tendency to expect the worst."),
            CreateWord("cactus", "n. any of a family of flowering plants able to live in dry regions and having fleshy stems and branches that bear scales or prickles instead of leaves"),
            CreateWord("platypus", "n. small densely furred aquatic monotreme of Australia and Tasmania having a broad bill and tail and webbed feet; only species in the family Ornithorhynchidae"),
            CreateWord("focus", "n. the center of the activity or interest; v. to adjust a camera or binoculars in order to get a clear picture; direct one's attention on something"),
            CreateWord("genius", "n. a very smart person; a special and usually inborn ability"),
            CreateWord("spacious", "n. very large in expanse or scope"),
            CreateWord("beauteous", "adj. very pleasing to look at"),
            CreateWord("zealous", "adj. marked by active interest and enthusiasm"),
            CreateWord("asparagus", "n. any of a genus -Asparagus- of plants of the lily family; with small; scalelike leaves; many flat or needlelike branches; and whitish flowers; including several plants -asparagus fernshaving fleshy roots and fine fernlike leaves the tender shoot"),
            CreateWord("ambitious", "adj. eagerly desirous and aspiring"),
            CreateWord("ingenious", "adj. showing a use of the imagination and creativity especially in inventing"),
            CreateWord("unanimous", "adj. having or marked by agreement in feeling or action -From the Latin root 'unus' meaning ONE-"),
            CreateWord("pious", "adj. having or showing religious devotion"),
            CreateWord("oblivious", "adj. unmindful; unconscious; unaware"),
            CreateWord("industrious", "adj. involved in often constant activity"),
            CreateWord("devious", "adj. clever at attaining one's ends by indirect and often deceptive means"),
            CreateWord("accidentally", "adv. not happening or done on purpose"),
            CreateWord("attention", "n. a focusing of the mind on something"),
            CreateWord("anticipate", "v. to believe in the future occurrence of"),
            CreateWord("apologize", "v. to say you are sorry and ask pardon"),
            CreateWord("admiration", "n. a feeling of great approval and liking"),
            CreateWord("access", "n. a way of approaching or coming to a place; v. to get to; reach; to obtain or reach on a computer"),
            CreateWord("autonomy", "n. the state of being free from the control or power of another; freedom and independence; self-governance"),
            CreateWord("assume", "v. to take as true or as a fact without actual proof ; to take to or upon oneself"),
            CreateWord("ascent", "n. the act or an instance of rising or climbing up"),
            CreateWord("allude", "v. to mention usually followed by to; hint at"),
            CreateWord("affect", "v. to act upon so as to cause a response; to be the business or affair of"),
            CreateWord("annual", "adj. occurring or performed once a year; covering the period of a year; n. a plant that completes the life cycle in one growing season or single year"),
            CreateWord("accomplish", "v. to bring about by effort"),
            CreateWord("acquire", "v. come into the possession of something; to gain gradually over time"),
            CreateWord("allocate", "v. 1 to set aside for a specific purpose; to allot; 2 distribute according to a plan."),
            CreateWord("traitor", "n. a person who is disloyal to his or her country his or her friends or another group"),
            CreateWord("terrain", "n. the surface features of an area of land"),
            CreateWord("barbecue", "n. 1 a cookout in which food is cooked over an open fire; 2 meat that has been grilled in a highly seasoned sauce; v. cook outdoors on a grill"),
            CreateWord("quarantine", "n. isolation to prevent the spread of infectious disease; v. place into enforced isolation"),
            CreateWord("stationary", "adj. fixed in a place or position; not undergoing a change in condition"),
            CreateWord("peculiar", "adj. odd or curious; not like the normal or usual; belonging to a particular group or person or place or thing"),
            CreateWord("interpret", "v. to decide on or explain the meaning of; to understand in a particular way; to change or translate from one language into another."),
            CreateWord("permanent", "adj. lasting forever"),
            CreateWord("verdict", "n. a position arrived at after consideration; an idea that is believed to be true or valid without positive knowledge"),
            CreateWord("attorney", "n. any person legally empowered to act as agent for; or in behalf of; another; esp; a lawyer"),
            CreateWord("exterior", "n. 1 the region that is outside of something; 2 the outer side or surface of something; adj. situated in or suitable for the outdoors or outside of a building"),
            CreateWord("deodorant", "n. a product or preparation that destroys or masks unpleasant odors"),
            CreateWord("alligator", "n. 1 either of two amphibious reptiles related to crocodiles but with shorter broader snouts; 2 leather made from the above's hide"),
            CreateWord("temporary", "adj. lasting only for a short time"),
            CreateWord("similar", "adj. having resemblance or likeness."),
            CreateWord("collide", "v. to bump into hard; come together with force; to clash"),
            CreateWord("gallant", "adj. large and impressive in size or grandeur or extent or conception; feeling or displaying no fear by temperament"),
            CreateWord("illogical", "adj. using or based on or caused by faulty reasoning"),
            CreateWord("vanilla", "n. and adj. 1 Vanilla having fleshy leaves and clusters of large waxy highly fragrant white or green or topaz flowers 2 a flavoring prepared from vanilla beans"),
            CreateWord("attend", "v. 1 be present at; 2 take charge of or deal with"),
            CreateWord("rebuttal", "n. a statement or contention as in a debate or legal case that is intended to disprove or confute another."),
            CreateWord("illuminate", "v. to provide cover or fill with light"),
            CreateWord("attain", "v. to obtain -as a goal- through effort"),
            CreateWord("latter", "adj. of relating to or being the second of two things referred to; more recent"),
            CreateWord("forgotten", "v. to be unable to recall or think of; adj. left unoccupied or unused"),
            CreateWord("attractive", "adj. pleasing to the eye or mind; charming"),
            CreateWord("attitude", "n. a person' manner of acting or his feelings; position or posture of the body appropriate to or expressive of an action or emotion"),
            CreateWord("attraction", "n. something that draws attention"),
            CreateWord("cancellation", "n. the act of omission deletion or invalidation; something that has been done away with abolished withdrawn or annulled such as a hotel reservation."),
            CreateWord("settlement", "n. an arrangement about action to be taken"),
            CreateWord("exactly", "adv. as stated or indicated without the slightest difference"),
            CreateWord("definitely", "adv. without any question"),
            CreateWord("unusually", "adv. noticeably different from what is generally found or experienced"),
            CreateWord("absolutely", "adv. having no exceptions or restrictions; without flaws or imperfections; without doubt"),
            CreateWord("truly", "adv. 1 in a truthful manner; 2 authentically; genuinely; 3 of course; indeed. 4 sincerely; honestly."),
            CreateWord("approximately", "adv. reasonably close to"),
            CreateWord("unbearably", "adj. so unpleasant distasteful or painful as to be intolerable"),
            CreateWord("undoubtedly", "adv. not called in question; accepted as beyond doubt; undisputed."),
            CreateWord("neighborly", "adj. exhibiting the qualities expected in a friendly person who lives or is located near another"),
            CreateWord("formally", "adv. of or according to prescribed or fixed customs or rules or ceremonies; n. a gown for evening wear"),
            CreateWord("formerly", "adv. having been such at some previous time"),
            CreateWord("sincerely", "adv. without any attempt to impress by deception or exaggeration"),
            CreateWord("hastily", "adv. in a hurried manner"),
            CreateWord("relatively", "adv. in comparison to something else."),
            CreateWord("naturally", "adv. 1 according to nature; without artificial help; 2 as might be expected"),
            CreateWord("reluctant", "adj. unwilling; disinclined; struggling in opposition"),
            CreateWord("recommend", "v. to put forward as one's choice for a wise or proper course of action"),
            CreateWord("repellent", "adj. serving or tending to drive away or ward off"),
            CreateWord("reign", "n. the authority or rule of a monarch; v. to govern as a monarch; to be usual or widespread"),
            CreateWord("receipt", "n. a written statement saying that money or goods have been received"),
            CreateWord("received", "v. to take or get something that is given or paid or sent"),
            CreateWord("recognize", "v. to identify as something or someone previously seen or known"),
            CreateWord("retrieval", "n. the act or process of retrieving possibility of recovery or restoration"),
            CreateWord("refrigerator", "n. a box or room or cabinet in which food and drink are kept cool by means of ice or mechanical cooling methods"),
            CreateWord("remorseful", "adj. filled with remorse; marked or caused by deep regret."),
            CreateWord("resign", "v. to give up deliberately"),
            CreateWord("relative", "n. a person connected with another by blood or marriage; adj. being such only when compared to something else"),
            CreateWord("resolution", "n. 1 strong purpose or determination; 2 something officially decided upon by a group or organization; 3 a solution or end to a conflict"),
            CreateWord("repetition", "n. the act of saying or doing over again"),
            CreateWord("reception", "n. a receiving or being received the manner of this -example-a friendly reception- a social function; often formal; for the receiving of guests response or reaction"),
            CreateWord("conceal", "v. to put into a hiding place; to keep secret or shut off from view"),
            CreateWord("nuclear", "adj. operated or powered by atomic energy"),
            CreateWord("weary", "adj. worn out in strength or energy or freshness"),
            CreateWord("heir", "n. a person who has the right to inherit property"),
            CreateWord("neither", "adj. not one or the other of two; not either -usually paired with nor in a sentence-"),
            CreateWord("receiver", "n. 1 a person who receives; 2 earphone that converts electrical signals into sounds 3 a football player who catches -or is supposed to catch- a forward pass"),
            CreateWord("vein", "n. 1.a small vessel that carries blood to the heart; 2.one of a series of thin ribs that form the structure of a leaf or insect wing"),
            CreateWord("interview", "n. a meeting of people face to face -as to evaluate or question a job applicant-"),
            CreateWord("yield", "v. to give up and cease resistance; n. something produced by physical or intellectual effort"),
            CreateWord("impatient", "adj. not patient; not willing or able to wait calmly; showing a lack of patience"),
            CreateWord("shield", "n. a protective covering or structure; v. protect or hide or conceal from danger or harm"),
            CreateWord("orientation", "n. an introduction as to guide one in new surroundings or activities; introductory instruction concerning a new situation"),
            CreateWord("siege", "n. the cutting off of an area by military means to stop the flow of people or supplies"),
            CreateWord("diesel", "n. a fuel designed for use in diesel engines"),
            CreateWord("endeavor", "v. to devote serious and sustained effort"),
            CreateWord("ballet", "n. a theatrical representation of a story that is performed to music by trained dancers"),
            CreateWord("individual", "n. a human regarded as a unique personality; a person distinguished from others by a special quality; a member of a collection or set; specimen"),
            CreateWord("sprawl", "v. to lie or sit with arms and legs spread out; to spread out in an uneven or awkward way; n. the spreading of urban structures into areas surrounding"),
            CreateWord("immaterial", "adj. of no essential consequence"),
            CreateWord("desperate", "adj. being beyond or almost beyond hope"),
            CreateWord("universe", "n. everything that exists anywhere; the whole world"),
            CreateWord("vague", "adj. 1 inexact unclear or indistinct in form or character; 2 not clearly expressed understood or perceived; 3 unclear in conveying; uncertain."),
            CreateWord("glacier", "n. a slowly moving mass of ice"),
            CreateWord("juvenile", "adj. having or showing the annoying qualities - as silliness - associated with children; n. a young person who is between infancy and adulthood"),
            CreateWord("crocodile", "n. a large reptile that is found in tropical swamps. It has a thick tough skin a long tail and a long pointed snout."),
            CreateWord("dungeon", "n. a dark usually underground prison"),
            CreateWord("roam", "v. to move about from place to place aimlessly"),
            CreateWord("informal", "adj. not officially recognized or controlled"),
            CreateWord("invasion", "n. a sudden attack on and entrance into hostile territory"),
            CreateWord("sincere", "adj. free from any intent to deceive or impress others"),
            CreateWord("exhibit", "v. to show by outward signs 2 to put on display 3 n. a public showing of objects of interest"),
            CreateWord("definite", "adj. 1 clear or exact; 2 known without a doubt; certain; sure."),
            CreateWord("impolite", "adj. not showing regard for others in manners or speech or behavior"),
            CreateWord("equality", "n. the quality of being the same in quantity or measure or value or status"),
            CreateWord("inability", "n. the quality or state of being unable; lack of ability; capacity; means; or power"),
            CreateWord("arthritis", "n. inflammation of the joints"),
            CreateWord("criticize", "v. 1 to find fault with; 2 to make judgments as to merits and faults"),
            CreateWord("inherit", "v. to receive by legal right from a person at the person's death"),
            CreateWord("exploit", "n. 1 a deed of daring or courage; v. 1 to make full use of and gain from; 2 to use for one's own advantage and in a way that is unfair"),
            CreateWord("positive", "adj.expressing approval; having or showing a mind free from doubt"),
            CreateWord("position", "n. the manner in which a person or thing is placed or arranged"),
            CreateWord("hesitant", "adj. not feeling sure; in doubt."),
            CreateWord("exquisite", "adj. finely done or made; very beautiful"),
            CreateWord("audition", "n. a short performance to test the talents of a musician singer dancer or actor; v. to test or try out in an audition"),
            CreateWord("aptitude", "n. an inherent ability as for learning; a talent"),
            CreateWord("accumulate", "v. 1 to pile up collect or gather; 2 to grow in amount or mass"),
            CreateWord("abrupt", "adj. sudden; being or characterized by direct and brief and potentially rude speech or manner"),
            CreateWord("address", "n. a usually formal discourse delivered to an audience; a place where a person or organization can usually be reached"),
            CreateWord("ascertain", "v. to find out for certain."),
            CreateWord("archaeologist", "n. an anthropologist who studies prehistoric people and their culture"),
            CreateWord("alternate", "n. someone who takes the place of another person; v. to do or use by turns; adj. occurring or following by turns"),
            CreateWord("average", "n. The usual amount or kind of something; The result of adding a set of numbers and then dividing the total by the number in the set; adj. ordinary"),
            CreateWord("awhile", "adv. for a short time"),
            CreateWord("ancestor", "n. a family member who lived at an earlier time; something from an earlier time from which something else was later developed"),
            CreateWord("accountable", "adj. subject to the obligation to report explain or justify something; responsible; answerable."),
            CreateWord("amphibian", "n. a small animal that spends part of its life cycle in water and part of its life cycle on land; they hatch in water and breathe with gills."),
            CreateWord("autobiography", "n. the story of one's life written by himself"),
            CreateWord("adolescent", "n. a boy or a girl from puberty to adulthood"),
            CreateWord("antique", "adj. belonging to earlier periods; n. an object of an earlier period"),
            CreateWord("aquatic", "adj. growing or living in or on water; 2 done in or upon water."),
            CreateWord("tomato", "n. edible fleshy usually red round fruit"),
            CreateWord("subtle", "adj. so slight as to be not easily seen or understood; 2 able to understand fine shades of meaning"),
            CreateWord("spectacle", "n. something to look at -esp. some strange or remarkable sight-"),
            CreateWord("isle", "n. a fairly small area of land completely surrounded by water"),
            CreateWord("gauge", "v. to find out the size or extent or amount of; n. an instrument for measuring testing or registering; thickness of something -as wire or screw-"),
            CreateWord("poncho", "n. garment with a slit in the center for the head."),
            CreateWord("cleanse", "v. 1 remove dirt or stains from; 2 remove unwanted substances from"),
            CreateWord("desperado", "n. a bold or reckless criminal"),
            CreateWord("fatigue", "n. a complete depletion of energy or strength; v. to use up all the physical energy of"),
            CreateWord("league", "n. 1 an association of sports teams; 2 an association of organizations for common action; 3 unit of distance of variable length usually 3 miles"),
            CreateWord("license", "n. a document or printed tag or permit showing approval by someone in authority for the doing of something"),
            CreateWord("privilege", "n. something granted as a special favor"),
            CreateWord("fascinate", "v. to attract and hold attentively by a unique power personal charm unusual nature or some other special quality; enthrall"),
            CreateWord("irresponsible", "adj. not having or showing responsibility; not able to be counted on or trusted"),
            CreateWord("genuine", "adj. real; actually being what something appears to be; true or reliable; sincere or honest"),
            CreateWord("communicate", "v. 1 to engage in an exchange of information or ideas; 2 to cause something to pass from one to another"),
            CreateWord("college", "n. a school of higher learning that one attends after high school. Most college programs require four years of study."),
            CreateWord("toboggan", "n. a long narrow sled without runners; boards curve upward in front; v. move along on a luge"),
            CreateWord("banana", "n. any of a genus -Musa- of treelike tropical plants of the banana family; with long; broad leaves and large clusters of edible fruit; esp; any of the various hybrids widely cultivated in the Western Hemisphere the long; curved fruit of these plant"),
            CreateWord("realistic", "adj. willing to see things as they really are and deal with them sensibly"),
            CreateWord("earnest", "adj. not joking or playful in mood or manner"),
            CreateWord("ingredient", "n. one of the parts of a mixture"),
            CreateWord("grief", "n. deep sadness especially for the loss of someone or something loved"),
            CreateWord("playwright", "n. a person who writes plays"),
            CreateWord("fragile", "adj. easily broken; easily injured without careful handling"),
            CreateWord("irrational", "adj. not possessed of reasoning powers or understanding"),
            CreateWord("saxophone", "n. a musical wind instrument consisting of a conical usually brass tube with keys or valves and a mouthpiece with one reed"),
            CreateWord("enforce", "v. to carry out effectively"),
            CreateWord("informative", "adj. providing information or adding to one's knowledge or understanding; educational"),
            CreateWord("sponsor", "n. a person who takes the responsibility for some other person or item; v. to take on financial responsibility as a form of advertising or charity"),
            CreateWord("construct", "v. to build; to make by fitting the parts together"),
            CreateWord("contribute", "v. to make a donation as part of a group effort to make a donation as part of a group effort"),
            CreateWord("consent", "n. the approval by someone in authority for the doing of something; v. to permit or approve or agree"),
            CreateWord("concentrate", "v. to fix one's powers or efforts or attention on one thing"),
            CreateWord("contradict", "v. to make an assertion that is contrary to one made by another"),
            CreateWord("conclude", "v. to come to an end; to come to a judgment after discussion or consideration"),
            CreateWord("condominium", "n. an individually owned unit in a structure as an apartment building with many units"),
            CreateWord("conserve", "v. to keep safe from loss destruction or waste."),
            CreateWord("controversy", "n. a dispute or an argument"),
            CreateWord("congruent", "adj. having the same size and shape"),
            CreateWord("confiscate", "v. to take ownership or control of something by right of one's authority"),
            CreateWord("concussion", "n. injury to the brain or spinal cord due to jarring from a blow or fall or the like"),
            CreateWord("consist", "v. to be made up of"),
            CreateWord("confetti", "n. small bits of brightly colored paper made for throwing"),
            CreateWord("convention", "n. a formal meeting or gathering where people discuss shared interests.")
        ];

        return new Dictionary<string, string>(words);
    }

    private Dictionary<string, string> SetupHardWords()
    {
        List<KeyValuePair<string, string>> words = 
        [
            CreateWord("Aardwolf", "a hyena-like animal of southern and eastern Africa"),
            CreateWord("Aberration", "departing from the usual course"),
            CreateWord("Abridgment", "a shortened form of a book"),
            CreateWord("Abscission", "sudden termination"),
            CreateWord("Acerbate", "to make sour or bitter"),
            CreateWord("Aficionado", "a devotee of something"),
            CreateWord("Algorithm", "a set of rules for solving a problem"),
            CreateWord("Alignment", "arrangement in a straight line"),
            CreateWord("Allocution", "a formal speech"),
            CreateWord("Ancillary", "a subordinate or subsidiary"),
            CreateWord("Apocalypse", "widespread destruction or disaster"),
            CreateWord("Applique", "ornamentation applied to a material"),
            CreateWord("Archetype", "the original model"),
            CreateWord("Avenge", "to exact satisfaction for"),
            CreateWord("Babushka", "a woman's scarf"),
            CreateWord("Baccalaureate", "a religious service held before commencement day"),
            CreateWord("Balalaika", "a Russian musical instrument"),
            CreateWord("Baroque", "pertains to architecture and art from 17th century Italy"),
            CreateWord("Barracuda", "a long, predaceous fish"),
            CreateWord("Bayou", "a marshy arm of a river, usually sluggish or stagnant"),
            CreateWord("Beleaguer", "to surround with troubles"),
            CreateWord("Belligerence", "a hostile attitude"),
            CreateWord("Beret", "a soft, visorless cap"),
            CreateWord("Bivouac", "a military encampment"),
            CreateWord("Blithe", "joyous, glad or cheerful"),
            CreateWord("Boatswain", "a warrant officer on a warship"),
            CreateWord("Bourgeois", "to be a member of the middle class"),
            CreateWord("Boutique", "a small shop within a larger store"),
            CreateWord("Boutonniere", "a flower worn by a man in his lapel"),
            CreateWord("Boysenberry", "a blackberry-like fruit"),
            CreateWord("Buoy", "a float used to mark a water channel"),
            CreateWord("Cabaret", "a restaurant providing food, drink and music"),
            CreateWord("Calisthenics", "are gymnastic exercises"),
            CreateWord("Callous", "hard or indifferent"),
            CreateWord("Camouflage", "hiding oneself from one's enemy"),
            CreateWord("Cannoneer", "an artilleryman"),
            CreateWord("Cantankerous", "disagreeable to deal with"),
            CreateWord("Cardiopulmonary", "pertaining to the heart and lungs"),
            CreateWord("Carnivorous", "flesh-eating"),
            CreateWord("Catastrophe", "a sudden and widespread disaster"),
            CreateWord("Celerity", "swiftness, speed"),
            CreateWord("Censer", "a container in which incense is burned"),
            CreateWord("Changeable", "liable to change or to be changed"),
            CreateWord("Chaparral", "a dense growth of shrubs or trees in the southwest US"),
            CreateWord("Commemorate", "to serve as a reminder of"),
            CreateWord("Committal", "an act or instance of committing"),
            CreateWord("Connoisseur", "a person competent to pass critical judgment"),
            CreateWord("Convalescence", "the gradual recovery to health after illness"),
            CreateWord("Cornucopia", "the horn of plenty in mythology"),
            CreateWord("Corruptible", "that which can be corrupted"),
            CreateWord("Crevasse", "a fissure in ice or the earth"),
            CreateWord("Croissant", "a rich, buttery crescent-shaped roll"),
            CreateWord("Curmudgeon", "a bad-tempered, cantankerous person"),
            CreateWord("Cynic", "a person who believes in selfishness as prime motivation"),
            CreateWord("Dachshund", "a long, German dog"),
            CreateWord("Decaffeinate", "to extract caffeine from"),
            CreateWord("Deliverance", "an act or instance of delivering"),
            CreateWord("Denouement", "the final resolution of the intricacies of a plot"),
            CreateWord("Diaphragm", "a part of the human body"),
            CreateWord("Dichotomy", "division into two parts"),
            CreateWord("Dietitian", "a person who is an expert on nutrition"),
            CreateWord("Diphthong", "an unsegmented gliding speech soun"),
            CreateWord("Docile", "easily handled or manageable"),
            CreateWord("Eczema", "an inflammatory condition of the skin"),
            CreateWord("Effervescent", "bubbling, vivacious or gay"),
            CreateWord("Eloquence", "using language with fluency and aptness"),
            CreateWord("Encumbrance", "something burdensome"),
            CreateWord("Exquisite", "of special beauty or charm"),
            CreateWord("Extemporaneous", "done without special preparation"),
            CreateWord("Facsimile", "an exact copy"),
            CreateWord("Fallacious", "logically unsound"),
            CreateWord("Fascinate", "to attract and hold attentively"),
            CreateWord("Fauna", "the animals of a region considered as a whole"),
            CreateWord("Flocculent", "like a clump of wool"),
            CreateWord("Foliage", "the leaves of a plant"),
            CreateWord("Forage", "food for cattle or horses"),
            CreateWord("Forsythia", "a shrub of the olive family"),
            CreateWord("Fraught", "full of or accompanied by"),
            CreateWord("Fuchsia", "a bright, purplish-red color"),
            CreateWord("Gauche", "lacking in social grace"),
            CreateWord("Genre", "a class of artistic endeavor having a particular form"),
            CreateWord("Germane", "relevant"),
            CreateWord("Gerrymander", "dividing election districts to suit one group or party"),
            CreateWord("Glockenspiel", "a musical instrument"),
            CreateWord("Gnash", "to grind or strike the teeth together"),
            CreateWord("Granary", "a storehouse for grain"),
            CreateWord("Grippe", "the former name for influenza"),
            CreateWord("Guillotine", "a device for execution"),
            CreateWord("Hallelujah", "praise ye the Lord"),
            CreateWord("Handwrought", "formed or shaped by hand, especially metal objects"),
            CreateWord("Harebrained", "giddy or reckless"),
            CreateWord("Harpsichord", "a keyboard instrument, precursor of the piano"),
            CreateWord("Haughty", "disdainfully proud"),
            CreateWord("Heir", "a person who inherits"),
            CreateWord("Hemorrhage", "a profuse discharge of blood"),
            CreateWord("Heterogeneous", "different in kind, unlike"),
            CreateWord("Hoard", "a supply that is carefully guarded or hidden"),
            CreateWord("Holocaust", "a great or complete destruction"),
            CreateWord("Homogenize", "to form by blending unlike elements"),
            CreateWord("Homonym", "a word the same in spelling and sound, but different in meaning"),
            CreateWord("Horde", "a large group, a multitude"),
            CreateWord("Humoresque", "a musical composition of humorous character"),
            CreateWord("Hydraulic", "employing water or other liquids in motion"),
            CreateWord("Hydrolysis", "chemical decomposition by reacting with water"),
            CreateWord("Hypothesis", "a proposition set forth to explain some occurrence"),
            CreateWord("Hysterical", "of or pertaining to hysteria"),
            CreateWord("Idyll", "a composition, usually describing pastoral scenes or any appealing incident, or the like"),
            CreateWord("Iguana", "a large lizard native to Central and South America"),
            CreateWord("Imperceptible", "very slight, gradual or subtle"),
            CreateWord("Impetuous", "characterized by sudden or rash action"),
            CreateWord("Impromptu", "done without previous preparation"),
            CreateWord("Incidence", "the rate of change or occurrence"),
            CreateWord("Indicator", "a person or thing that indicates"),
            CreateWord("Infallible", "absolutely trustworthy or sure"),
            CreateWord("Inferior", "lower in station, rank or degree"),
            CreateWord("Insurgence", "an act of rebellion"),
            CreateWord("Interfere", "to meddle in the affairs of others"),
            CreateWord("Invoice", "an itemized bill for goods or services"),
            CreateWord("Iridescent", "displaying a play of bright colors, like a rainbow"),
            CreateWord("Isle", "a small island"),
            CreateWord("Isthmus", "a narrow strip of land with water on both sides, connecting two larger strips of land"),
            CreateWord("Jackal", "a wild dog of Asia and Africa"),
            CreateWord("Jacuzzi", "a trade name for a whirlpool bath and related products"),
            CreateWord("Joist", "a beam used to support ceilings or floors or the like"),
            CreateWord("Juxtaposition", "the act of placing close together"),
            CreateWord("Kaiser", "a German or Austrian emperor"),
            CreateWord("Kaleidoscope", "a continually shifting pattern or scene"),
            CreateWord("Ketch", "a two-masted sailing vessel"),
            CreateWord("Knave", "an unprincipled or dishonest person"),
            CreateWord("Knell", "the sound made by a bell rung slowly, at a death"),
            CreateWord("Knoll", "a small, rounded hill"),
            CreateWord("Labyrinth", "an intricate combination of paths in which it is difficult to find the exit"),
            CreateWord("Laconic", "using few words, being concise"),
            CreateWord("Laggard", "a lingerer or loiterer"),
            CreateWord("Lagoon", "an area of shallow water separated from the sea by sandy dunes"),
            CreateWord("Laryngitis", "the inflammation of the larynx"),
            CreateWord("Larynx", "the structure in which the vocal cords are located"),
            CreateWord("Lavender", "a pale bluish purple"),
            CreateWord("Legionnaire", "a member of any legion"),
            CreateWord("Leprechaun", "a dwarf or sprite in Ireland"),
            CreateWord("Liege", "a Feudal lord entitled to allegiance or service"),
            CreateWord("Luau", "a feast of Hawaiian food"),
            CreateWord("Luscious", "highly pleasing to the taste or smell"),
            CreateWord("Lyre", "a musical instrument of ancient Greece, harp-like"),
            CreateWord("Lymphatic", "pertaining to, containing or conveying lymph"),
            CreateWord("Magnanimous", "generous in forgiving insult or injury"),
            CreateWord("Magnify", "to increase the apparent size of, as does a lens"),
            CreateWord("Malfeasance", "wrongdoing by a public official"),
            CreateWord("Maneuver", "a planned movement of troops or warships, etc"),
            CreateWord("Mantle", "a loose, sleeveless cloak or cape"),
            CreateWord("Marquee", "a projection above a theater entrance, usually containing the name of the feature at the theater"),
            CreateWord("Masquerade", "a party of people wearing masks and other disguises"),
            CreateWord("Maul", "a heavy hammer"),
            CreateWord("Melee", "a confused, hand-to-hand fight among several people"),
            CreateWord("Memento", "a keepsake or souvenir"),
            CreateWord("Mercenary", "working or acting merely for money or reward"),
            CreateWord("Mesquite", "a spiny tree found in western North America"),
            CreateWord("Mettle", "courage or fortitude"),
            CreateWord("Minuscule", "very small"),
            CreateWord("Momentous", "of great or far-reaching importance"),
            CreateWord("Monastery", "a house occupied by usually monks"),
            CreateWord("Monocle", "an eyeglass for one eye"),
            CreateWord("Morgue", "a place in which bodies are kept"),
            CreateWord("Morphine", "a narcotic used as a pain-killer or sedative"),
            CreateWord("Mosque", "a Muslim temple or place of public worship"),
            CreateWord("Motif", "a recurring subject, theme or idea"),
            CreateWord("Mousse", "a sweetened dessert with whipped cream as a base"),
            CreateWord("Mozzarella", "a mild, white, semi-soft Italian cheese"),
            CreateWord("Muenster", "a white cheese made from whole milk"),
            CreateWord("Municipal", "of or pertaining to a town or city or its government"),
            CreateWord("Mysterious", "full of or involving mystery"),
            CreateWord("Mystique", "an aura of mystery or mystical power surrounding a particular occupation or pursuit"),
            CreateWord("Naughty", "disobedient or mischievous"),
            CreateWord("Neuter", "gender that is neither masculine nor feminine"),
            CreateWord("Nickel", "a coin of the U.S., 20 of which make a dollar"),
            CreateWord("Nickelodeon", "an early motion-picture theater"),
            CreateWord("Nomenclature", "are names or terms comprising a set or system"),
            CreateWord("Nonchalant", "coolly unconcerned, unexcited"),
            CreateWord("Nonpareil", "having no equal"),
            CreateWord("Noxious", "harmful or injurious to health"),
            CreateWord("Nuance", "a subtle difference in meaning"),
            CreateWord("Nucleus", "the core"),
            CreateWord("Nuisance", "an obnoxious or annoying person"),
            CreateWord("Nuptial", "of or pertaining to marriage or the ceremony"),
            CreateWord("Nylons", "are stockings worn by women"),
            CreateWord("Obnoxious", "highly objectionable or offensive"),
            CreateWord("Obsolescent", "passing out of use, as a word"),
            CreateWord("Occurrence", "the action, fact or instance of happening"),
            CreateWord("Ocelot", "a spotted, leopard-like cat, ranging from Texas to South America"),
            CreateWord("Ogre", "a monster in fairy tales"),
            CreateWord("Onyx", "black"),
            CreateWord("Ophthalmology", "the branch of medicine dealing with anatomy, functions and diseases of the eye"),
            CreateWord("Ordnance", "cannon or artillery"),
            CreateWord("Orphan", "a child who has lost both parents through death"),
            CreateWord("Oscillate", "to swing or move to and fro, as a pendulum"),
            CreateWord("Overwrought", "extremely excited or agitated"),
            CreateWord("Oxygen", "the element constituting about one-fifth of the atmosphere"),
            CreateWord("Pacifist", "a person who is opposed to war or to violence of any kind"),
            CreateWord("Palette", "a board with a thumb hole, used by painters to mix colors"),
            CreateWord("Palomino", "a horse with a golden coat, and a white mane and tail"),
            CreateWord("Pamphlet", "a short essay, generally controversial, on some subject of contemporary interest"),
            CreateWord("Pantomime", "the art of conveying things through gestures, without speech"),
            CreateWord("Papacy", "the office, dignity or jurisdiction of the pope"),
            CreateWord("Parable", "a short story designed to illustrate some truth"),
            CreateWord("Paralysis", "a loss of movement in a body part, caused by disease or injury"),
            CreateWord("Paraphernalia", "apparatus necessary for a particular activity"),
            CreateWord("Parishioner", "one of the inhabitants of a parish"),
            CreateWord("Parochial", "of or pertaining to a parish or parishes"),
            CreateWord("Parody", "a humorous imitation of a serious piece of literature"),
            CreateWord("Parquet", "a floor composed of strips or blocks of wood forming a pattern"),
            CreateWord("Partition", "a division into portions or shares"),
            CreateWord("Pasture", "grass used to feed livestock"),
            CreateWord("Patriarch", "the male head of a family or tribal line"),
            CreateWord("Patrician", "a person of noble rank or an aristocrat"),
            CreateWord("Paunchy", "having a large and protruding belly"),
            CreateWord("Pause", "a temporary stop or rest"),
            CreateWord("Pavilion", "a building used for shelter, concerts, or exhibits"),
            CreateWord("Peak", "the pointed top of a mountain"),
            CreateWord("Penchant", "a strong inclination or liking for something"),
            CreateWord("Penguin", "a flightless bird of the Southern Hemisphere"),
            CreateWord("Penicillin", "an antibiotic of low toxicity"),
            CreateWord("Penitentiary", "a prison maintained for serious offenders"),
            CreateWord("Perennial", "lasting for a long time; enduring"),
            CreateWord("Periphery", "the external boundary of any area"),
            CreateWord("Perjury", "lying under oath"),
            CreateWord("Perseverance", "doggedness, steadfastness"),
            CreateWord("Persuade", "to prevail on a person to do something"),
            CreateWord("Peruse", "to read through with care"),
            CreateWord("Pesticide", "a chemical preparation to destroy pests"),
            CreateWord("Petition", "a formally drawn request"),
            CreateWord("Phalanx", "a body of troops in close array"),
            CreateWord("Phenomenon", "a fact or occurrence observed or observable"),
            CreateWord("Philosopher", "one who offers views on profound subjects"),
            CreateWord("Phoenix", "a mythical bird able to rise from its own ashes"),
            CreateWord("Physics", "the science that deals with matter, energy, motion and force"),
            CreateWord("Picturesque", "visually charming or quaint"),
            CreateWord("Peace", "a country's condition when not involved in war"),
            CreateWord("Pinnacle", "a lofty peak"),
            CreateWord("Pinafore", "a child's apron"),
            CreateWord("Pixie", "a fairy or sprite, especially a mischievous one"),
            CreateWord("Placard", "a paperboard sign or notice"),
            CreateWord("Placebo", "a pill with no medicine but used to soothe a patient"),
            CreateWord("Plaid", "any fabric woven of differently colored yarns in a cross-barred pattern"),
            CreateWord("Plight", "a condition or situation especially an unfavorable one"),
            CreateWord("Plumber", "a person who installs and repairs piping, fixtures, etc"),
            CreateWord("Pneumonia", "inflammation of the lungs with congestion"),
            CreateWord("Poignant", "keenly distressing to the feelings"),
            CreateWord("Poinsettia", "sometimes called the Christmas flower"),
            CreateWord("Politicize", "to bring a political flavor to"),
            CreateWord("Populous", "heavily populated"),
            CreateWord("Porridge", "a food made of cereal, boiled to a thick consistency in water or milk"),
            CreateWord("Posse", "a force armed with legal authority"),
            CreateWord("Posthumous", "arising, occurring, or continuing after one's death"),
            CreateWord("Potpourri", "any mixture of unrelated objects, subjects, etc"),
            CreateWord("Practitioner", "a person engaged in the practice of a profession or occupation"),
            CreateWord("Prairie", "a tract of grassland or meadow"),
            CreateWord("Precise", "definitely or strictly stated"),
            CreateWord("Prerogative", "an exclusive right or privilege"),
            CreateWord("Prestigious", "having a high reputation"),
            CreateWord("Prey", "an animal hunted or seized for food"),
            CreateWord("Principle", "an accepted or professed rule of action or conduct"),
            CreateWord("Pronunciation", "an accepted standard of the sound and stress patterns of a syllable or word"),
            CreateWord("Psalm", "a sacred song or hymn"),
            CreateWord("Psychology", "the science of the mind or of mental states and processes"),
            CreateWord("Purge", "to cleanse or to purify"),
            CreateWord("Quaff", "to drink a beverage"),
            CreateWord("Quandary", "a state of uncertainty"),
            CreateWord("Quarantine", "a strict isolation"),
            CreateWord("Questionnaire", "a list of questions submitted for replies"),
            CreateWord("Queue", "a braid of hair or a line of people"),
            CreateWord("Quiche", "a dish with cheeses and other vegetables"),
            CreateWord("Quintessence", "the pure and concentrated essence of a substance"),
            CreateWord("Rabble", "a disorderly crowd or a mob"),
            CreateWord("Raffle", "a form of a lottery"),
            CreateWord("Rambunctious", "difficult to control or handle"),
            CreateWord("Rancid", "having an unpleasant smell or taste"),
            CreateWord("Raspberry", "the fruit of a shrub"),
            CreateWord("Ratchet", "a tool"),
            CreateWord("Rationale", "the fundamental reason serving to account for something"),
            CreateWord("Recede", "to go or move away"),
            CreateWord("Recluse", "a person who lives apart or in seclusion"),
            CreateWord("Reconnaissance", "the act of reconnoitering"),
            CreateWord("Rectify", "to make or set right"),
            CreateWord("Recurrence", "an act of something happening again"),
            CreateWord("Reggae", "a style of Jamaican popular music"),
            CreateWord("Rehearse", "to practice"),
            CreateWord("Reign", "the period during which a sovereign sits on a throne"),
            CreateWord("Rein", "the leather strap used to control a horse"),
            CreateWord("Remembrance", "a memory"),
            CreateWord("Reminiscence", "the process of recalling experiences"),
            CreateWord("Requisition", "the act of requiring or demanding"),
            CreateWord("Rescind", "to annul or repeal"),
            CreateWord("Respondent", "a person who responds or makes replies"),
            CreateWord("Resume", "a summing up, a summary"),
            CreateWord("Resurrection", "the act of rising from the dead"),
            CreateWord("Revise", "to amend or alter"),
            CreateWord("Rhapsodic", "ecstatic or extravagantly enthusiastic"),
            CreateWord("Rhetoric", "bombast or the undue use of exaggeration or display"),
            CreateWord("Rhubarb", "a plant of the buckwheat family"),
            CreateWord("Rigor", "strictness, severity or hardness"),
            CreateWord("Rotor", "a rotating part of a machine"),
            CreateWord("Rouge", "any of various red cosmetics for cheek and lips"),
            CreateWord("Roulette", "a game of chance"),
            CreateWord("Rubella", "a disease also called German measles"),
            CreateWord("Sable", "an Old World weasel-like animal"),
            CreateWord("Sachet", "a small bag containing perfuming powder or the like"),
            CreateWord("Sacrilegious", "pertaining to the violation of anything sacred"),
            CreateWord("Saffron", "a crocus having showy, purple flowers"),
            CreateWord("Salutatorian", "the person ranking second in the graduating class"),
            CreateWord("Sanctimonious", "making a hypocritical show of religious devotion"),
            CreateWord("Sapphire", "a gem with a blue color"),
            CreateWord("Sarcasm", "harsh or bitter derision or irony"),
            CreateWord("Satellite", "a body that revolves around a planet, a moon"),
            CreateWord("Sauerkraut", "cabbage allowed to ferment until sour"),
            CreateWord("Sauna", "a bath that uses dry heat to induce perspiration"),
            CreateWord("Scandalous", "disgraceful or shocking behavior"),
            CreateWord("Scarab", "a beetle regarded as sacred by the ancient Egyptians"),
            CreateWord("Scenario", "the outline of a plot of a dramatic work"),
            CreateWord("Scepter", "a rod held as an emblem of regal or imperial power"),
            CreateWord("Schizophrenia", "a severe mental disorder"),
            CreateWord("Schnauzer", "a German breed of medium-sized dogs"),
            CreateWord("Sciatic", "pertaining to the back of the hip"),
            CreateWord("Scour", "to remove dirt by hard scrubbing"),
            CreateWord("Scourge", "a cause of affliction or calamity"),
            CreateWord("Scrod", "a young Atlantic codfish or haddock"),
            CreateWord("Scruple", "a moral standard that acts as a restraining force"),
            CreateWord("Sculptor", "a person who practices the art of sculpture"),
            CreateWord("Seance", "a meeting in which people try to communicate with spirits"),
            CreateWord("Seclude", "to withdraw into solitude"),
            CreateWord("Seine", "a fishing net"),
            CreateWord("Semaphore", "an apparatus for conveying visual signals"),
            CreateWord("Sensuous", "pertaining to or affecting the senses"),
            CreateWord("Separate", "to keep apart or divide"),
            CreateWord("Sepulcher", "a tomb, grave or burial place"),
            CreateWord("Sequoia", "a large tree, aka redwood"),
            CreateWord("Sergeant", "a noncommissioned officer above the rank of corporal"),
            CreateWord("Serial", "anything published in short installments at regular intervals"),
            CreateWord("Sew", "to join or attach by stitches"),
            CreateWord("Shackle", "something used to secure the wrist, leg, etc"),
            CreateWord("Sheathe", "to put a sword into a sheath"),
            CreateWord("Sheen", "luster, brightness, radiance"),
            CreateWord("Shrew", "a woman of violent temper and speech"),
            CreateWord("Sierra", "a chain of hills or mountains, the peaks of which suggest the teeth of a saw"),
            CreateWord("Silhouette", "a two-dimensional representation of the outline of an object"),
            CreateWord("Simile", "a figure of speech in which two unlike things are compared, as in she is like a rose"),
            CreateWord("Simultaneous", "occurring or operating at the same time"),
            CreateWord("Singe", "to burn slightly, to scorch"),
            CreateWord("Siphon", "a tube bent into legs of unequal length, for getting liquid from one container to another"),
            CreateWord("Skeptic", "a person who questions the validity of something"),
            CreateWord("Skew", "to turn aside or swerve"),
            CreateWord("Slaughter", "the killing of cattle, etc., for food"),
            CreateWord("Sleigh", "a vehicle on runners, especially used over snow or ice"),
            CreateWord("Sleight", "skill or dexterity"),
            CreateWord("Sleuth", "a detective"),
            CreateWord("Slough", "(sloo) is an area of soft, muddy ground"),
            CreateWord("Sojourn", "a temporary stay"),
            CreateWord("Solder", "an alloy fused and applied to the joint between metal objects to unite them"),
            CreateWord("Solemn", "grave or sober or mirthless"),
            CreateWord("Sovereign", "a monarch or a king"),
            CreateWord("Spasm", "a sudden involuntary muscular contraction"),
            CreateWord("Specter", "a ghost, phantom or apparition"),
            CreateWord("Sponsor", "a person who vouches for or is responsible for a person"),
            CreateWord("Squabble", "to engage in a petty quarrel"),
            CreateWord("Squeak", "a short, sharp, shrill cry"),
            CreateWord("Squint", "to look with the eyes partly closed"),
            CreateWord("Stationery", "writing paper"),
            CreateWord("Stimulus", "something that incites to action or exertion"),
            CreateWord("Strait", "a narrow passage of water between 2 larger bodies of water"),
            CreateWord("Straitjacket", "a garment made of strong material and designed to bind the arms"),
            CreateWord("Stroganoff", "a dish of meet sauteed with onions and cooked in a sauce of sour cream"),
            CreateWord("Suave", "smoothly agreeable or polite"),
            CreateWord("Subpoena", "the usual writ for the summoning of witnesses"),
            CreateWord("Subtle", "thin, tenuous or delicate in meaning"),
            CreateWord("Succinct", "expressed in few words, concise, terse"),
            CreateWord("Sufficiency", "adequacy"),
            CreateWord("Suite", "a number of things forming a set"),
            CreateWord("Supersede", "to replace in power, or acceptance"),
            CreateWord("Supposition", "something that is supposed; assumption"),
            CreateWord("Surety", "security against loss or damage"),
            CreateWord("Surrey", "a light carriage for four persons"),
            CreateWord("Surrogate", "a person appointed to act for another; a deputy"),
            CreateWord("Surveillance", "a watch kept over a person or group"),
            CreateWord("Swerve", "to turn aside abruptly"),
            CreateWord("Symposium", "a meeting to discuss some subject"),
            CreateWord("Synod", "an assembly of church delegates"),
            CreateWord("Synonym", "a word having nearly the same meaning as another"),
            CreateWord("Syntax", "the study of the rules for the formation of grammatical sentences in a language"),
            CreateWord("Tabernacle", "a place or house of worship"),
            CreateWord("Tableau", "a picture of a scene"),
            CreateWord("Tabular", "arranged into a table"),
            CreateWord("Tachometer", "a machine to measure velocity or speed"),
            CreateWord("Tacky", "not tasteful or fashionable"),
            CreateWord("Tact", "a sense of what to say without raising offense"),
            CreateWord("Taffy", "a chewy candy"),
            CreateWord("Taint", "a trace of something bad or harmful"),
            CreateWord("Tally", "an account or reckoning"),
            CreateWord("Tambourine", "a small drum consisting of a circular frame with skin stretched over it and several pairs of metal jingles attached"),
            CreateWord("Tandem", "one following or behind the other"),
            CreateWord("Tangible", "capable of being touched"),
            CreateWord("Tantalize", "to torment with"),
            CreateWord("Tapestry", "a fabric used for wall hangings or furniture coverings"),
            CreateWord("Tassel", "an ornament consisting of a bunch of threads hanging from a round knob, used on clothing or jewelry"),
            CreateWord("Taught", "the past participle of teach"),
            CreateWord("Taunt", "to mock"),
            CreateWord("Tawdry", "showy or cheap"),
            CreateWord("Technique", "the manner in which the technical skills of a particular art or field of endeavor are used"),
            CreateWord("Tedious", "long and tiresome"),
            CreateWord("Teeter", "to move unsteadily"),
            CreateWord("Telegraph", "an apparatus to send messages to a distant place"),
            CreateWord("Telepathy", "communication between minds"),
            CreateWord("Temblor", "a tremor; earthquake"),
            CreateWord("Tempt", "to entice or allure to do something often considered wrong"),
            CreateWord("Tenor", "the meaning that runs through something written or spoken"),
            CreateWord("Tense", "stretched tight; high-strung or nervous"),
            CreateWord("Terrain", "a tract of land"),
            CreateWord("Terse", "neatly or effectively concise; brief and pithy"),
            CreateWord("Tetanus", "a disease, commonly called lockjaw"),
            CreateWord("Thatch", "a material used to cover roofs"),
            CreateWord("Thermometer", "a device for measuring temperature"),
            CreateWord("Thesaurus", "a dictionary of synonyms and antonyms"),
            CreateWord("Thesis", "a proposition put forth to be considered"),
            CreateWord("Thigh", "between the hip and the knee"),
            CreateWord("Thimble", "a small cap, worn over the fingertip to protect it when pushing a needle through a cloth in sewing"),
            CreateWord("Thistle", "a prickly plant"),
            CreateWord("Thorough", "executed without negligence or omissions"),
            CreateWord("Thumb", "the short, thick inner digit of the human hand"),
            CreateWord("Tier", "one of a series of rows rising one behind or above another"),
            CreateWord("Tinsel", "a glittering, metallic substance, usually in strips"),
            CreateWord("Titanic", "gigantic"),
            CreateWord("Titlist", "a titleholder, champion"),
            CreateWord("Tobacco", "the plant used in making cigarettes"),
            CreateWord("Tongue", "the movable organ in the mouth of humans"),
            CreateWord("Tonsillectomy", "the operation removing one or both tonsils"),
            CreateWord("Topaz", "a mineral used as a gem"),
            CreateWord("Torque", "something that produces rotation"),
            CreateWord("Tout", "to solicit business"),
            CreateWord("Toxicity", "the degree of being poisonous"),
            CreateWord("Traceable", "capable of being traced"),
            CreateWord("Trachea", "the windpipe"),
            CreateWord("Trait", "a distinguishing characteristic or quality"),
            CreateWord("Tranquil", "calm or peaceful"),
            CreateWord("Transcend", "to rise above or go beyond"),
            CreateWord("Transient", "not lasting or enduring"),
            CreateWord("Translucent", "letting pass through, but not clearly"),
            CreateWord("Trapeze", "an apparatus consisting of a horizontal bar attached to two suspending ropes"),
            CreateWord("Trauma", "a body wound or shock produced by sudden physical injury"),
            CreateWord("Trestle", "a type of frame, used in railroad spans"),
            CreateWord("Trichotomy", "divided into three parts"),
            CreateWord("Trivial", "of little significance or importance"),
            CreateWord("Trough", "a receptacle, usually for drinking from"),
            CreateWord("Troupe", "a group of actors or performers, especially travelers"),
            CreateWord("Truancy", "the act of being truant or late"),
            CreateWord("Tyrannize", "to exercise absolute control or power"),
            CreateWord("Ulcer", "a sore on the skin"),
            CreateWord("Uncollectible", "it can't be collected"),
            CreateWord("Unkempt", "disheveled or messy"),
            CreateWord("Vaccinal", "pertaining to vaccine or vaccination"),
            CreateWord("Vague", "not clearly expressed or identified"),
            CreateWord("Vaudeville", "a theatrical entertainment"),
            CreateWord("Vehemence", "ardor or fervor"),
            CreateWord("Veneer", "a thin layer of wood"),
            CreateWord("Vengeance", "violent revenge or getting back"),
            CreateWord("Vermicelli", "a form of pasta"),
            CreateWord("Victuals", "are food supplies"),
            CreateWord("Viscount", "a nobleman just below an earl or count"),
            CreateWord("Vogue", "something in fashion"),
            CreateWord("Vying", "competing or contending"),
            CreateWord("Waive", "to give up or to forgo"),
            CreateWord("Whack", "to strike with a sharp blow or blows"),
            CreateWord("Wheelwright", "a person whose trade is to make wheels"),
            CreateWord("Wherever", "in, at or to whatever place"),
            CreateWord("Wince", "to draw back or tense the body"),
            CreateWord("Wrack", "wreck or wreckage"),
            CreateWord("Wreak", "to inflict or execute as punishment or vengeance"),
            CreateWord("Wren", "a small, active songbird"),
            CreateWord("Yeoman", "a petty officer in a navy"),
            CreateWord("Zeppelin", "a rigid airship or dirigible"),
            CreateWord("Zoological", "of or pertaining to zoology"),
            CreateWord("Zucchini", "a variety of summer squash.")
        ];

        return new Dictionary<string, string>(words);
    }

    public override void Warmup()
    {
        ChooseNewWord(0);
        ChooseNewWord(1);
        ChooseNewWord(2);
        ClearAllGameTexts();
        PressButton(null, 1);
        DisplayWord(1);
        DisplayProblem(1);

        CRayTraceInterface? rayTrace = ArcadeScripts.RayTraceInterface.Get();
        rayTrace?.TraceShape(Vector.Zero, QAngle.Zero, null, new(), out TraceResult result);
    }

    public override void Remove()
    {
        foreach (Timer timer in Timers) timer?.Kill();
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
    }
}
