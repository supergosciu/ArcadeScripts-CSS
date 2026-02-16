using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

using static ArcadeScripts.Random;
using static ArcadeScripts.TextDisplayHelper;

namespace ArcadeScripts.Scripts;

public partial class LightDuel : ScriptBase
{
    public LightDuel(CLogicScript owner) : base(owner)
    {
        Functions.Add("DisplayHelpText", new ScriptFunction(DisplayHelpText));
        Functions.Add("FireRocket", new ScriptFunction<int>(FireRocket));
        Functions.Add("SetDirection", new ScriptFunction<int, int>(SetDirection));
        Functions.Add("SetPlayerActive", new ScriptFunction<CEntityInstance, int, bool>(SetPlayerActive));
        Functions.Add("SpawnPlayer", new ScriptFunction<int>(SpawnPlayer));
        Functions.Add("SpawnPlayers", new ScriptFunction(SpawnPlayers));
        Functions.Add("StopGame", new ScriptFunction(StopGame));
        Functions.Add("SetOtherGameStarted", new ScriptFunction<bool>(SetOtherGameStarted));

        DrawColor[0] = [255, 192, 192];
        DrawColor[1] = [192, 255, 192];
        DrawColor[2] = [192, 192, 255];
        DrawColor[3] = [255, 255, 255];
        DrawHeadColor[0] = [255, 1, 1];
        DrawHeadColor[1] = [1, 255, 1];
        DrawHeadColor[2] = [1, 1, 255];
        DrawHeadColor[3] = [200, 200, 200];

        SpriteToSpriteName[SpriteTypes.Player] = "cycle_sprite";
        SpriteToSpriteName[SpriteTypes.Rocket] = "cycle_sprite_rocket";
        SpriteToSpriteName[SpriteTypes.Explosion] = "cycle_sprite_explosion";

        DirectionToAngles[Directions.Up] = new QAngle(0, 90, 270);
        DirectionToAngles[Directions.Down] = new QAngle(0, 90, 90);
        DirectionToAngles[Directions.Left] = new QAngle(0, 90, 0);
        DirectionToAngles[Directions.Right] = new QAngle(0, 90, 180);
        DirectionToAngles[Directions.None] = new QAngle(0, 90, 0);

        StartPositions = [
            new Vector(MinEngineCoord.X + 280.0f, MinEngineCoord.Y + 2.1f, MinEngineCoord.Z + 280.0f),
            new Vector(MinEngineCoord.X + 280.0f, MinEngineCoord.Y + 2.1f, MinEngineCoord.Z + 40.0f),
            new Vector(MinEngineCoord.X + 40.0f, MinEngineCoord.Y + 2.1f, MinEngineCoord.Z + 280.0f),
            new Vector(MinEngineCoord.X + 40.0f, MinEngineCoord.Y + 2.1f, MinEngineCoord.Z + 40.0f)
        ];

        PrebuiltLevels[0] = [];
        PrebuiltLevels[1] = [new Vector(10, 0, 10), new Vector(10, 0, 11), new Vector(11, 0, 10), new Vector(11, 0, 11), new Vector(7, 0, 7), new Vector(6, 0, 7), new Vector(7, 0, 6), new Vector(7, 0, 14), new Vector(7, 0, 15), new Vector(6, 0, 14), new Vector(14, 0, 7), new Vector(15, 0, 7), new Vector(14, 0, 6), new Vector(14, 0, 14), new Vector(14, 0, 15), new Vector(15, 0, 14)];
        PrebuiltLevels[2] = [new Vector(9, 0, 9), new Vector(9, 0, 12), new Vector(12, 0, 9), new Vector(12, 0, 12), new Vector(1, 0, 2), new Vector(2, 0, 1), new Vector(1, 0, 19), new Vector(2, 0, 20), new Vector(19, 0, 1), new Vector(20, 0, 2), new Vector(19, 0, 20), new Vector(20, 0, 19)];
        PrebuiltLevels[3] = [new Vector(4, 0, 10), new Vector(4, 0, 11), new Vector(10, 0, 4), new Vector(11, 0, 4), new Vector(17, 0, 10), new Vector(17, 0, 11), new Vector(10, 0, 17), new Vector(11, 0, 17)];

        EntityGroupNames[0] = "cycle_kill0";
        EntityGroupNames[1] = "cycle_kill1";
        EntityGroupNames[2] = "cycle_kill2";
        EntityGroupNames[3] = "cycle_kill3";

        EntityNames = ["cycle_entity_maker", "cycle_entity_maker_rocket", "cycle_entity_maker_explosion", "cycle_start_sound", "cycle_warmup_sound", "sj_stop_relay", "blob_start_model", "blob_script", "cycle_start_model", "cycle_stop_model", "cycle_game", "cycle_kill0", "cycle_kill1", "cycle_kill2", "cycle_kill3"];

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent += OnEntitySpawned;
        ArcadeScripts.Instance.OnEntityRemovedEvent += OnEntityRemoved;
        ArcadeScripts.Instance.RegisterListener<Listeners.OnTick>(Think);
    }

    public enum Directions
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 3,
        Right = 4
    }

    public enum SpriteTypes
    {
        Player = 1,
        Rocket = 2,
        Explosion = 3
    }

    //Sprite Data
    private float RocketSize = DrawSize * 3.0f;
    private const float MovementDistance = 16.0f;
    private const float DrawTime = 0.2f;
    private const float DrawSize = 8.0f;
    private static int[][] DrawColor = new int[4][];
    private static int[][] DrawHeadColor = new int[4][];
    private static Dictionary<SpriteTypes, CEnvEntityMaker> SpriteToSpriteMaker = [];
    private static Dictionary<SpriteTypes, string> SpriteToSpriteName = [];
    private static Dictionary<Directions, QAngle> DirectionToAngles = [];

    //Movement and Game State
    private bool GameActive = false;
    private bool GameStarted = false;
    private bool GameEnded = false;
    private bool OtherGameStarted = false;
    private float LastSprite = 0.0f;
    private bool[] IsAlive = [false, false, false, false];
    private bool[] IsPlayerActive = [false, false, false, false];
    private Vector[] LastPosition = new Vector[4];
    private Vector[] RocketPosition = new Vector[4];
    private Directions?[] LastDirection = new Directions?[4];
    private Directions?[] NextDirection = new Directions?[4];
    private Directions?[] RocketDirection = new Directions?[4];

    private int CountDown = 0;
    private float NextCountdown = 0.0f;
    private float? TurnOffGameAt = null;
    private static Vector BoothAreaMin = new(1340.0f, -1965.0f, -511.0f);
    private static Vector BoothAreaMax = new(1658.0f, -1927.0f, -446.0f);

    // Board Data
    private static Directions[] StartDirections = [Directions.Right, Directions.Right, Directions.Left, Directions.Left];
    private static Vector[] StartPositions = null!;
    private static Vector MinEngineCoord = new(1339.0f, -2351.0f, -500.0f); // map coords
    private static Vector MaxEngineCoord = new(1659.0f, -2348.9f, -180.0f); // map coords

    // Prebuilt levels
    private static Vector[][] PrebuiltLevels = new Vector[4][];

    private List<CTriggerMultiple> StartTriggers = [null!, null!, null!, null!];
    private List<CLogicCase> CycleGameUI = [null!, null!, null!, null!];
    private List<CBreakable> Sprites = [];
    private CCSPlayerPawn[] PlayerEntities = new CCSPlayerPawn[4];
    private CBreakable[] PlayerSprites = new CBreakable[4];
    private CAmbientGeneric CycleStartSound = null!;
    private CAmbientGeneric CycleWarmupSound = null!;
    private CLogicScript BlobScript = null!;
    private CDynamicProp BlobStartModel = null!;
    private CDynamicProp CycleStartModel = null!;
    private CDynamicProp CycleStopModel = null!;
    private CLogicRelay StrafeJumpStopRelay = null!;

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

            if (targetname == "cycle_entity_maker")
            {
                SpriteToSpriteMaker[SpriteTypes.Player] = entities[0].As<CEnvEntityMaker>();
            }
            else if (targetname == "cycle_entity_maker_rocket")
            {
                SpriteToSpriteMaker[SpriteTypes.Rocket] = entities[0].As<CEnvEntityMaker>();
            }
            else if (targetname == "cycle_entity_maker_explosion")
            {
                SpriteToSpriteMaker[SpriteTypes.Explosion] = entities[0].As<CEnvEntityMaker>();
            }
            else if (targetname == "cycle_start_sound")
            {
                CycleStartSound = entities[0].As<CAmbientGeneric>();
            }
            else if (targetname == "cycle_warmup_sound")
            {
                CycleWarmupSound = entities[0].As<CAmbientGeneric>();
            }
            else if (targetname == "sj_stop_relay")
            {
                StrafeJumpStopRelay = entities[0].As<CLogicRelay>();
            }
            else if (targetname == "blob_start_model")
            {
                BlobStartModel = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "blob_script")
            {
                BlobScript = entities[0].As<CLogicScript>();
            }
            else if (targetname == "cycle_start_model")
            {
                CycleStartModel = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "cycle_stop_model")
            {
                CycleStopModel = entities[0].As<CDynamicProp>();
            }
            else if (targetname.StartsWith("cycle_game"))
            {
                if (int.TryParse(targetname.Replace("cycle_game", ""), out int number))
                {
                    CycleGameUI[number] = entities[0].As<CLogicCase>();
                }
            }

            int index = Array.IndexOf(EntityGroupNames, targetname);
            if (index != -1)
            {
                EntityGroup[index] = entities[0].As<CBaseEntity>();
            }
        }
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        string? targetname = entity.Entity?.Name;

        if (targetname == "cycle_sprite" || targetname == "cycle_sprite_rocket" || targetname == "cycle_sprite_explosion")
        {
            Sprites.Add(entity.As<CBreakable>());
        }
    }

    private void OnEntityRemoved(CEntityInstance entity)
    {
        Sprites.Remove(entity.As<CBreakable>());
    }

    private void Think()
    {
        if (GameActive && CountDown > 0 && Server.CurrentTime >= NextCountdown)
        {
            if (CountDown == 1)
            {
                CycleStartSound.AddEntityIOEvent(inputName: "PlaySound", delay: 0.0f);
            }
            else
            {
                CycleWarmupSound.AddEntityIOEvent(inputName: "PlaySound", delay: 0.0f);
            }
            CountDown--;
            NextCountdown = Server.CurrentTime + 1.0f;
        }

        // Only draw sprites at most 5 times per second
        if (GameActive && Server.CurrentTime >= LastSprite + DrawTime)
        {
            GameStarted = true;
            LastSprite = Server.CurrentTime;
            UpdateRockets();
            SpawnSprites();

            // End the game if there are 0 players remaining
            if (GetLivingPlayers() <= 0)
            {
                //StopGame()
                GameActive = false;
                GameStarted = false;
                GameEnded = true;
                TurnOffGameAt = Server.CurrentTime + 5.0f;
            }
        }
        if (TurnOffGameAt != null && Server.CurrentTime >= TurnOffGameAt)
        {
            StopGame();
        }
    }

    private void SpawnSprites()
    {
        int[] randomlyOrderedPlayers = ChooseRandomPlayerOrder(false);
        foreach (int player in randomlyOrderedPlayers)
        {
            if (!IsAlive[player]) continue;

            if (NextDirection[player] != null)
            {
                LastDirection[player] = NextDirection[player];
                NextDirection[player] = null;
            }

            Vector drawPosition = ChooseNewSpritePosition(LastPosition[player], LastDirection[player]!.Value);
            if (HasCollided(drawPosition))
            {
                KillPlayer(player);
                continue;
            }

            // Recolor previous sprite
            CBreakable? previousSprite = Sprites.FirstOrDefault(s => s.AbsOrigin!.ToString() == LastPosition[player].ToString());
            if (previousSprite != null)
            {
                previousSprite.Render = Color.FromArgb(255, DrawColor[player][0], DrawColor[player][1], DrawColor[player][2]);
                Utilities.SetStateChanged(previousSprite, "CBaseModelEntity", "m_clrRender");
            }

            CBreakable sprite = SpawnSprite(drawPosition, DrawHeadColor[player][0], DrawHeadColor[player][1], DrawHeadColor[player][2], SpriteTypes.Player, Directions.None);
            PlayerSprites[player] = sprite;
            LastPosition[player] = drawPosition;
        }
    }

    public CBreakable SpawnSprite(Vector position, int r, int g, int b, SpriteTypes spriteType, Directions direction)
    {
        if (!GameActive) return null!;

        string targetname = spriteType == SpriteTypes.Player ? "cycle_sprite" : (spriteType == SpriteTypes.Rocket ? "cycle_sprite_rocket" : "cycle_sprite_explosion");
        string model = spriteType == SpriteTypes.Player ? "maps/jb_arcade_b6/entities/cycle_sprite_7056.vmdl" : (spriteType == SpriteTypes.Rocket ? "maps/jb_arcade_b6/entities/cycle_sprite_rocket_7057.vmdl" : "maps/jb_arcade_b6/entities/cycle_sprite_explosion_7059.vmdl");

        CBreakable sprite = Utilities.CreateEntityByName<CBreakable>("func_breakable")!;
        using CEntityKeyValues kv = new();
        kv.SetString("targetname", targetname);
        kv.SetVector("origin", position);
        kv.SetAngle("angles", DirectionToAngles[direction]);
        kv.SetInt("health", 0);
        kv.SetColor("rendercolor", Color.FromArgb(255, r, g, b));
        sprite.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(sprite.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
        sprite.DispatchSpawn(kv);
        sprite.SetModel(model);

        return sprite;
    }

    private void KillAllSprites()
    {
        foreach (CBreakable sprite in Sprites)
        {
            sprite.AddEntityIOEvent(inputName: "Kill");
        }
        Sprites.Clear();
    }

    private void SpawnPlayers()
    {
        if (OtherGameStarted || GameActive || GameStarted || GameEnded) return;

        KillAllSprites();

        ArcadeScripts.Instance.RunScriptCode(new(BlobScript, "SetOtherGameStarted(true)", Owner, BlobScript));
        StrafeJumpStopRelay.AddEntityIOEvent(inputName: "Trigger"); //Close strafe jump to prevent easy cheating
        BlobStartModel.AddEntityIOEvent(inputName: "Skin", value: "2");
        CycleStartModel.AddEntityIOEvent(inputName: "Skin", value: "1");
        CycleStopModel.AddEntityIOEvent(inputName: "Skin", value: "0");
        GameActive = true;
        for (int player = 0; player < 4; player++)
        {
            if (IsPlayerActive[player])
            {
                SpawnPlayer(player);
            }
        }
        LastSprite = Server.CurrentTime + 3.0f;
        CountDown = 4;
        NextCountdown = 0.0f;

        SpawnArena(RandomInt(0, PrebuiltLevels.Length - 1));
    }

    //Spawn players after start button has been pressed but before game has started
    public void SpawnPlayer(int player)
    {
        if (!IsAlive[player] && GameActive && !GameStarted)
        {
            IsAlive[player] = true;
            LastPosition[player] = StartPositions[player];
            LastDirection[player] = StartDirections[player];
            NextDirection[player] = null;
            SpawnSprite(StartPositions[player], DrawHeadColor[player][0], DrawHeadColor[player][1], DrawHeadColor[player][2], SpriteTypes.Player, Directions.None);
        }
    }

    private void SpawnArena(int arena)
    {
        foreach (Vector coords in PrebuiltLevels[arena])
        {
            Vector engineCoords = GameCoordsToEngineCoords(coords);
            SpawnSprite(engineCoords, 64, 64, 64, SpriteTypes.Player, Directions.None);
        }
    }

    private void StopGame()
    {
        TurnOffGameAt = null;
        KillAllSprites();
        for (int player = 0; player < 4; player++)
        {
            IsAlive[player] = false;
            PlayerSprites[player] = null!;
            RocketPosition[player] = null!;
        }
        GameActive = false;
        GameStarted = false;
        GameEnded = false;
        if (!OtherGameStarted)
        {
            ArcadeScripts.Instance.RunScriptCode(new(BlobScript, "SetOtherGameStarted(false)", Owner, BlobScript));
            BlobStartModel.AddEntityIOEvent(inputName: "Skin", value: "0");
            CycleStartModel.AddEntityIOEvent(inputName: "Skin", value: "0");
            CycleStopModel.AddEntityIOEvent(inputName: "Skin", value: "1");
        }
    }

    private void KillPlayer(int player)
    {
        int livingPlayers = GetLivingPlayers();
        debugprint("There are " + livingPlayers + " livingPlayers");
        if (livingPlayers == 4) livingPlayers = 5;
        EntityGroup[player].Target = "blob_teleport" + livingPlayers;
        CycleGameUI[player].AddEntityIOEvent(inputName: "Deactivate", delay: 0.00f, activator: PlayerEntities[player]);
        EntityGroup[player].AddEntityIOEvent(inputName: "Enable", delay: 0.05f);
        EntityGroup[player].AddEntityIOEvent(inputName: "Disable", delay: 0.5f);
        IsAlive[player] = false;
        PlayerSprites[player] = null!;
        if (livingPlayers == 1)
        {
            GameActive = false;
            GameStarted = false;
            TurnOffGameAt = Server.CurrentTime + 5.0f;
        }
        debugprint("Finished KillPlayer");
    }

    private int GetLivingPlayers()
    {
        int livingPlayers = 0;
        foreach (bool living in IsAlive)
        {
            if (living)
            {
                livingPlayers++;
            }
        }
        return livingPlayers;
    }

    private Vector ChooseNewSpritePosition(Vector oldPosition, Directions direction)
    {
        return direction switch
        {
            Directions.Up => new Vector(oldPosition.X, oldPosition.Y, oldPosition.Z + MovementDistance),
            Directions.Down => new Vector(oldPosition.X, oldPosition.Y, oldPosition.Z - MovementDistance),
            Directions.Left => new Vector(oldPosition.X + MovementDistance, oldPosition.Y, oldPosition.Z),
            Directions.Right => new Vector(oldPosition.X - MovementDistance, oldPosition.Y, oldPosition.Z),
            _ => oldPosition,
        };
    }

    public void SetDirection(int player, int direction)
    {
        if ((LastDirection[player] == Directions.Up && (Directions)direction == Directions.Down) ||
            (LastDirection[player] == Directions.Down && (Directions)direction == Directions.Up) ||
            (LastDirection[player] == Directions.Left && (Directions)direction == Directions.Right) ||
            (LastDirection[player] == Directions.Right && (Directions)direction == Directions.Left))
        {
            return;
        }
        NextDirection[player] = (Directions)direction;
    }

    public void SetPlayerActive(CEntityInstance? activator, int player, bool isActive)
    {
        if (activator == null) return;

        IsPlayerActive[player] = isActive;
        PlayerEntities[player] = isActive ? activator.As<CCSPlayerPawn>() : null!;
    }

    private bool IsWithinPlayArea(float x, float y, float z)
    {
        return z < MaxEngineCoord.Z && z > MinEngineCoord.Z && x > MinEngineCoord.X && x < MaxEngineCoord.X;
    }

    private Vector GameCoordsToEngineCoords(Vector gameCoords)
    {
        return new Vector((gameCoords.X * 16) + MinEngineCoord.X - 8.0f, MaxEngineCoord.Y, (gameCoords.Z * 16) + MinEngineCoord.Z - 8.0f);
    }

    private bool HasCollided(Vector pos)
    {
        if (!IsWithinPlayArea(pos.X, pos.Y, pos.Z))
        {
            return true;
        }

        CBreakable? sprite = Sprites.FirstOrDefault(s => s.IsValid && s.AbsOrigin!.ToString() == pos.ToString());
        return sprite != null;
    }

    public void FireRocket(int player)
    {
        if (IsAlive[player] && GameStarted && RocketPosition[player] == null)
        {
            RocketPosition[player] = ChooseNewSpritePosition(LastPosition[player], LastDirection[player]!.Value);
            RocketDirection[player] = LastDirection[player];
            CheckRocketCollision(player);
        }
    }

    private void UpdateRockets()
    {
        UndrawRockets();
        int[] randomlyOrderedPlayersWithRockets = ChooseRandomPlayerOrder(true);
        foreach (int player in randomlyOrderedPlayersWithRockets)
        {
            for (int moves = 0; moves < 2; moves++)
            {
                RocketPosition[player] = ChooseNewSpritePosition(RocketPosition[player], RocketDirection[player]!.Value);
                if (CheckRocketCollision(player))
                {
                    break;
                }
            }
        }
        DrawRockets();
    }

    private bool CheckRocketCollision(int player)
    {
        if (HasCollided(RocketPosition[player]))
        {
            debugprint("Rocket has hit something and is exploding at " + RocketPosition[player]);
            Explode(RocketPosition[player]);
            RocketPosition[player] = null!;
            return true;
        }
        return false;
    }

    private void DrawRockets()
    {
        for (int player = 0; player < 4; player++)
        {
            if (RocketPosition[player] != null)
            {
                SpawnSprite(RocketPosition[player], DrawHeadColor[player][0], DrawHeadColor[player][1], DrawHeadColor[player][2], SpriteTypes.Rocket, RocketDirection[player]!.Value);
            }
        }
    }

    private void UndrawRockets()
    {
        for (int player = 0; player < 4; player++)
        {
            if (RocketPosition[player] != null)
            {
                CBreakable? sprite = Sprites.FirstOrDefault(s => s.Entity!.Name == "cycle_sprite_rocket" && s.AbsOrigin!.ToString() == RocketPosition[player].ToString());
                sprite?.AddEntityIOEvent(inputName: "Kill");
            }
        }
    }

    private void Explode(Vector position)
    {
        foreach (CBreakable sprite in Sprites.ToList().Where(s => VectorExtensions.DistanceTo(s.AbsOrigin!, position) <= RocketSize))
        {
            for (int player = 0 ; player < 4 ; player++)
            {
                if (PlayerSprites[player] == sprite)
                {
                    KillPlayer(player);
                }
            }
            sprite.AddEntityIOEvent(inputName: "Kill");
        }

        CBreakable? explosion = SpawnSprite(position, 255, 128, 128, SpriteTypes.Explosion, Directions.None);
        debugprint("Explosion sprite: " + explosion);
        explosion?.AddEntityIOEvent(inputName: "SetScale", value: "0.25", delay: 0.0f);
        explosion?.AddEntityIOEvent(inputName: "SetScale", value: "0.5", delay: 0.1f);
        explosion?.AddEntityIOEvent(inputName: "SetScale", value: "1.0", delay: 0.2f);
        explosion?.AddEntityIOEvent(inputName: "SetScale", value: "2.0", delay: 0.3f);
        explosion?.AddEntityIOEvent(inputName: "Kill", delay: 0.5f);
    }

    private int[] ChooseRandomPlayerOrder(bool rocket)
    {
        List<int> playerOptions = [0, 1, 2, 3];
        List<int> orderedPlayers = [];

        while (playerOptions.Count > 0)
        {
            int index = RandomInt(0, playerOptions.Count - 1);
            int player = playerOptions[index];
            playerOptions.RemoveAt(index);
            if ((!rocket && IsAlive[player]) || (rocket && RocketPosition[player] != null))
            {
                orderedPlayers.Add(player);
            }
        }
        return orderedPlayers.ToArray();
    }

    public void DisplayHelpText()
    {
        if (GameStarted) return;
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            Vector? playerOrigin = player.PlayerPawn.Value?.AbsOrigin;
            if (playerOrigin != null && playerOrigin.X >= BoothAreaMin.X && playerOrigin.X <= BoothAreaMax.X &&
                playerOrigin.Y >= BoothAreaMin.Y && playerOrigin.Y <= BoothAreaMax.Y &&
                playerOrigin.Z >= BoothAreaMin.Z && playerOrigin.Z < BoothAreaMax.Z)
            {
                DisplayText(player, "1. Look forward and press use.<br>2. WASD to turn.<br>3. Attack to fire rocket.");
            }
        }
    }

    private void DisplayText(CCSPlayerController player, string text)
    {
        SetMessage(player.Slot, text, 4.0f);
    }

    private void SetOtherGameStarted(bool val)
    {
        OtherGameStarted = val;
    }

    public override void Warmup()
    {
        GameEnded = true;
        ArcadeScripts.Instance.RunScriptCode(new(Owner, "SpawnPlayers()", null, null));
        GameEnded = false;
        ArcadeScripts.Instance.RunScriptCode(new(Owner, "SpawnPlayer(0)", null, null));
    }

    public override void Remove()
    {
        ArcadeScripts.Instance.RemoveListener<Listeners.OnTick>(Think);
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent -= OnEntitySpawned;
        ArcadeScripts.Instance.OnEntityRemovedEvent -= OnEntityRemoved;
    }
}