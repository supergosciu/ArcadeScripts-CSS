using CounterStrikeSharp.API.Core;
using static ArcadeScripts.Random;

namespace ArcadeScripts.Scripts;

public class Rogue : ScriptBase
{
    public Rogue(CLogicScript owner) : base(owner)
    {
        Functions.Add("Setup", new ScriptFunction(Setup));
        Functions.Add("Move", new ScriptFunction<CEntityInstance, int>(Move));

        TrapPatterns = [TrapPattern0, TrapPattern1];

        EntityNames = ["secret_teleportation_template", "rogue_text"];

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
    }

    private enum Directions
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 3,
        Right = 4
    }

    private enum TileType
    {
        Player,
        Wall,
        DoorClosed,
        DoorOpen,
        DoorAndPlayer,
        Trap,
        LaserFiring,
        LaserFired,
        Treasure
    }

    private string[] TrapPattern0 =
    [
        "^...^..",
        "^.^.^.^",
        "^.^.^.^",
        "^.^.^.^",
        "..^...^"
    ];

    private string[] TrapPattern1 =
    [
        "..........",
        ".^^^^^^^^.",
        ".^$.......",
        ".^^^^^^^^.",
        ".........."
    ];

    // Game state
    private int PlayerX = 3;
    private int PlayerY = 3;
    private bool GameWon = false;
    private const int GameSize = 64;
    private const int GameHeight = 7;
    private const int DrawX = 15;
    private const int DrawY = 7;
    private const int LaserStartX = 26;
    private const int LaserEndX = 37;

    private Dictionary<TileType, string> TileTypeToChar = [];
    private TileType?[][] GameBoard = new TileType?[GameSize][];
    private string[][] TrapPatterns;

    private CPointWorldText[] GameText = new CPointWorldText[GameHeight];
    private CPointTemplate SecretTeleportationTemplate = null!;

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

            if (targetname == "secret_teleportation_template")
            {
                SecretTeleportationTemplate = entities[0].As<CPointTemplate>();
            }
            else if (targetname.StartsWith("rogue_text"))
            {
                if (int.TryParse(targetname.Replace("rogue_text", ""), out int number))
                {
                    GameText[number - 1] = entities[0].As<CPointWorldText>();
                }
            }
        }
    }

    public void Setup()
    {
        PlayerX = 3;
        PlayerY = 3;
        for (int i = 0; i < GameSize; i++)
        {
            GameBoard[i] = new TileType?[GameHeight];
            for (int j = 0; j < GameHeight; j++)
            {
                GameBoard[i][j] = TileType.Wall;
            }
        }

        GenerateRoom(0, 6, 0, 6);
        GenerateRoom(11, 20, 0, 6);
        GenerateRoom(25, 38, 0, 6);
        GenerateRoom(45, 56, 0, 6);
        GenerateHallway(6, 11, 5);
        GenerateHallway(20, 25, 5);
        GenerateHallway(38, 45, 3);
        GenerateTraps(12, 0);
        GenerateTraps(46, 1);
        GameBoard[PlayerX][PlayerY] = TileType.Player;

        TileTypeToChar[TileType.Player] = "@";
        TileTypeToChar[TileType.Wall] = "#";
        TileTypeToChar[TileType.DoorClosed] = "+";
        TileTypeToChar[TileType.DoorOpen] = "'";
        TileTypeToChar[TileType.DoorAndPlayer] = "@";
        TileTypeToChar[TileType.Trap] = "^";
        TileTypeToChar[TileType.LaserFiring] = "v";
        TileTypeToChar[TileType.LaserFired] = "^";
        TileTypeToChar[TileType.Treasure] = "$";

        LogGameBoard();
        DisplayGameBoard();
    }

    private void GenerateRoom(int xMin, int xMax, int yMin, int yMax)
    {
        // Draw walls
        for (int x = xMin; x <= xMax; x++)
        {
            GameBoard[x][yMin] = TileType.Wall;
            GameBoard[x][yMax] = TileType.Wall;
        }
        for (int y = yMin + 1; y < yMax; y++)
        {
            GameBoard[xMin][y] = TileType.Wall;
            GameBoard[xMax][y] = TileType.Wall;
        }
        // Draw floors
        for (int y = yMin + 1; y < yMax; y++)
        {
            for (int x = xMin + 1; x < xMax; x++)
            {
                GameBoard[x][y] = null!;
            }
        }
    }

    private void GenerateHallway(int xMin, int xMax, int y)
    {
        for (int x = xMin; x <= xMax; x++)
        {
            GameBoard[x][y] = null!;
        }
        GameBoard[xMin][y] = TileType.DoorClosed;
        GameBoard[xMax][y] = TileType.DoorClosed;
    }

    private void GenerateTraps(int xStart, int pattern)
    {
        debugprint("TRAP_PATTERN[0].len() = " + TrapPatterns[pattern][0].Length);
        for (int y = 0; y < GameHeight - 2; y++)
        {
            for (int x = xStart; x < xStart + TrapPatterns[pattern][0].Length; x++)
            {
                switch (TrapPatterns[pattern][y][x - xStart])
                {
                    case '^':
                        GameBoard[x][y + 1] = TileType.Trap;
                        break;
                    case '$':
                        if (!GameWon)
                        {
                            GameBoard[x][y + 1] = TileType.Treasure;
                        }
                        break;
                    case '.':
                        GameBoard[x][y + 1] = null!;
                        break;
                    default:
                        break;
                }
            }
        }
    }

    private void LogGameBoard()
    {
        for (int y = 0; y < GameHeight; y++)
        {
            string line = "";
            for (int x = 0; x < GameSize; x++)
            {
                if (GameBoard[x][y] == null)
                {
                    line += ".";
                }
                else
                {
                    line += TileTypeToChar[(TileType)GameBoard[x][y]!];
                }
            }
            debugprint(line);
        }
    }

    private void DisplayGameBoard()
    {
        int yMin = PlayerY - DrawY / 2;
        int yMax = PlayerY + DrawY / 2;
        int xMin = PlayerX - DrawX / 2;
        int xMax = PlayerX + DrawX / 2;
        int line = 0;
        for (int y = 0; y < DrawY; y++)
        {
            string text = "";
            for (int x = xMin; x < xMax; x++)
            {
                if (x < 0 || x >= GameSize || y < 0 || y >= GameHeight)
                {
                    text += TileTypeToChar[TileType.Wall];
                }
                else if (GameBoard[x][y] == null)
                {
                    text += ".";
                }
                else
                {
                    text += TileTypeToChar[(TileType)GameBoard[x][y]!];
                }
            }
            debugprint("Line " + line + " is " + text);
            GameText[line++].AddEntityIOEvent(inputName: "SetMessage", value: text);
        }
    }

    public void Move(CEntityInstance? activator, int direction)
    {
        if (activator == null) return;

        switch (direction)
        {
            case (int)Directions.Up:
                AttemptMoveTo(activator, PlayerX, PlayerY - 1);
                break;
            case (int)Directions.Down:
                AttemptMoveTo(activator, PlayerX, PlayerY + 1);
                break;
            case (int)Directions.Left:
                AttemptMoveTo(activator, PlayerX - 1, PlayerY);
                break;
            case (int)Directions.Right:
                AttemptMoveTo(activator, PlayerX + 1, PlayerY);
                break;
            case (int)Directions.None:
                AttemptMoveTo(activator, PlayerX, PlayerY);
                break;
        }
        LogGameBoard();
        DisplayGameBoard();
    }

    private void AttemptMoveTo(CEntityInstance activator, int x, int y)
    {
        if (GameBoard[x][y] != null && (TileType)GameBoard[x][y]! == TileType.Treasure)
        {
            SecretTeleportationTemplate.AddEntityIOEvent(inputName: "ForceSpawn", activator: activator);
            GameWon = true;
        }

        if (GameBoard[x][y] == null || 
            (GameBoard[x][y] != null && (TileType)GameBoard[x][y]! == TileType.DoorOpen) || 
            (GameBoard[x][y] != null && (TileType)GameBoard[x][y]! == TileType.Treasure) || 
            (x == PlayerX && y == PlayerY))
        {
            if (GameBoard[PlayerX][PlayerY] != null && (TileType)GameBoard[PlayerX][PlayerY]! == TileType.DoorAndPlayer)
            {
                GameBoard[PlayerX][PlayerY] = TileType.DoorOpen;
            }
            else
            {
                GameBoard[PlayerX][PlayerY] = null!;
            }
            PlayerX = x;
            PlayerY = y;
            if (GameBoard[PlayerX][PlayerY] != null && (TileType)GameBoard[PlayerX][PlayerY]! == TileType.DoorOpen)
            {
                GameBoard[PlayerX][PlayerY] = TileType.DoorAndPlayer;
            }
            else
            {
                GameBoard[PlayerX][PlayerY] = TileType.Player;
            }
            AdvanceLasers();
        }
        else if (GameBoard[x][y] != null && (TileType)GameBoard[x][y]! == TileType.DoorClosed)
        {
            GameBoard[x][y] = TileType.DoorOpen;
            AdvanceLasers();
        }
        else if (GameBoard[x][y] != null && (TileType)GameBoard[x][y]! == TileType.Trap)
        {
            Setup();
        }
    }

    private void AdvanceLasers()
    {
        // Build list of potential laser locations that are not next to fired or firing lasers
        List<int> potentialLaserLocations = [];
        for (int x = LaserStartX; x <= LaserEndX; x++)
        {
            if (GameBoard[x][0] != null && (TileType)GameBoard[x][0]! == TileType.Wall && 
                GameBoard[x - 1][0] != null && (TileType)GameBoard[x - 1][0]! == TileType.Wall && 
                GameBoard[x + 1][0] != null && (TileType)GameBoard[x + 1][0]! == TileType.Wall)
            {
                potentialLaserLocations.Add(x);
                debugprint("potentialLaserLocations: pushed " + x);
            }
        }
        debugprint("potentialLaserLocations.len(): " + potentialLaserLocations.Count);

        // Remove old lasers and fire new ones
        for (int x = LaserStartX; x <= LaserEndX; x++)
        {
            if (GameBoard[x][0] != null && (TileType)GameBoard[x][0]! == TileType.LaserFired)
            {
                GameBoard[x][0] = TileType.Wall;
                for (int y = 1; y < GameHeight - 1; y++)
                {
                    GameBoard[x][y] = null!;
                }
            }
            if (GameBoard[x][0] != null && (TileType)GameBoard[x][0]! == TileType.LaserFiring)
            {
                GameBoard[x][0] = TileType.LaserFired;
                for (int y = 1; y < GameHeight - 1; y++)
                {
                    if (GameBoard[x][y] != null && (TileType)GameBoard[x][y]! == TileType.Player)
                    {
                        Setup();
                        return;
                    }
                    GameBoard[x][y] = TileType.Trap;
                }
            }
        }

        // Setup next lasers
        if (potentialLaserLocations.Count > 0)
        {
            int[] laserLocations =
            [
                potentialLaserLocations[RandomInt(0, potentialLaserLocations.Count - 1)], 
                potentialLaserLocations[RandomInt(0, potentialLaserLocations.Count - 1)] 
            ];
            // Prevent auto-lose scenario where two lasers at the end of the hallway fire at the same time
            if ((laserLocations[0] == LaserEndX && laserLocations[1] == LaserEndX - 1) || 
                (laserLocations[1] == LaserEndX && laserLocations[0] == LaserEndX - 1))
            {
                debugprint("preventing laserLocation " + laserLocations[0].ToString() + " and laserLocation " + laserLocations[1].ToString() + " from firing at the same time");
                laserLocations[1] = laserLocations[0];
            }

            foreach (int laserLocation in laserLocations)
            {
                debugprint("Chose laser location " + laserLocation);
                GameBoard[laserLocation][0] = TileType.LaserFiring;
            }
        }
    }

    public override void Remove()
    {
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
    }
}