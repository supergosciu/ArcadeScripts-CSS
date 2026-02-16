using System.Drawing;
using Vector3 = System.Numerics.Vector3;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

using static ArcadeScripts.Random;
using static ArcadeScripts.TextDisplayHelper;

namespace ArcadeScripts.Scripts;

public partial class Blob : ScriptBase
{
    public Blob(CLogicScript owner) : base(owner)
    {
        Functions.Add("Setup", new ScriptFunction(Setup));
        Functions.Add("DisplayHelpText", new ScriptFunction(DisplayHelpText));
        Functions.Add("StartTimer", new ScriptFunction(StartTimer));
        Functions.Add("AddToGameTime", new ScriptFunction<int>(AddToGameTime));
        Functions.Add("StopButton", new ScriptFunction(StopButton));
        Functions.Add("AddToWinnersSetting", new ScriptFunction<int>(AddToWinnersSetting));
        Functions.Add("RemovePlayerFromGameActivator", new ScriptFunction<CEntityInstance>(RemovePlayerFromGameActivator));
        Functions.Add("KillSkinSprites", new ScriptFunction(KillSkinSprites));
        Functions.Add("SetOtherGameStarted", new ScriptFunction<bool>(SetOtherGameStarted));

        TimeLeft = GameTime + WarmupTime;

        EntityGroupNames[0] = "blob_teleport1";
        EntityGroupNames[1] = "blob_teleport2";
        EntityGroupNames[2] = "blob_teleport3";
        EntityGroupNames[3] = "blob_teleport4";
        EntityGroupNames[4] = "blob_teleport5";

        EntityNames = ["blob_winners_sign_model", "blob_timer_text0", "blob_start_sound", "blob_warmup_sound", "draw_script", "cycle_start_model", "blob_start_model", "blob_reset_model", "sj_stop_relay", "blob_teleport1", "blob_teleport2", "blob_teleport3", "blob_teleport4", "blob_teleport5"];

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent += OnEntitySpawned;
        ArcadeScripts.Instance.RegisterListener<Listeners.OnPlayerButtonsChanged>(OnPlayerButtonsChanged);
        ArcadeScripts.Instance.RegisterListener<Listeners.OnTick>(Think);

    }

    private class BlobPlayer
    {
        public CCSPlayerController Player = null!;
        public CBreakable? Blob = null;
        public int SelectedSkin = 0;
        public bool SpeedBoostActive = false;
        public bool Active = false;
        public float Radius = 0.0f;
        public float Area = 0.0f;
        public float MaxArea = 0.0f;

        public void Remove()
        {
            if (Blob != null && Blob.IsValid)
            {
                Blob.Remove();
                Blob = null;
            }
            SpeedBoostActive = false;
            Active = false;
            Radius = 0.0f;
            Area = 0.00f;
            MaxArea = 0.0f;
        }
    }

    private static string[] SkinDisplayNames = ["white", "csgo", "thorgot", "steam", "white", "homer", "marge", "bart", "lisa", "maggie", "itchy", "scratchy", "doge", "thinking", "pokeball", "cs2"];
    private static Dictionary<string, string> BlobSkins = new()
    {
        {"white", "maps/jb_arcade_b6/entities/blob_sprite_unused_7063.vmdl"},
        {"csgo", "maps/jb_arcade_b6/entities/blob_sprite_unused_7066.vmdl"},
        {"thorgot", "maps/jb_arcade_b6/entities/blob_sprite_unused_7070.vmdl"},
        {"steam", "maps/jb_arcade_b6/entities/blob_sprite_unused_7090.vmdl"},
        {"white2", "maps/jb_arcade_b6/entities/blob_sprite_unused_7063.vmdl"},
        {"homer", "maps/jb_arcade_b6/entities/blob_sprite_unused_7072.vmdl"},
        {"marge", "maps/jb_arcade_b6/entities/blob_sprite_unused_7074.vmdl"},
        {"bart", "maps/jb_arcade_b6/entities/blob_sprite_unused_7078.vmdl"},
        {"lisa", "maps/jb_arcade_b6/entities/blob_sprite_unused_7080.vmdl"},
        {"maggie", "maps/jb_arcade_b6/entities/blob_sprite_unused_7082.vmdl"},
        {"itchy", "maps/jb_arcade_b6/entities/blob_sprite_unused_7084.vmdl"},
        {"scratchy", "maps/jb_arcade_b6/entities/blob_sprite_unused_7088.vmdl"},
        {"doge", "maps/jb_arcade_b6/entities/blob_sprite_unused_7092.vmdl"},
        {"thinking", "maps/jb_arcade_b6/entities/blob_sprite_unused_7086.vmdl"},
        {"pokeball", "maps/jb_arcade_b6/entities/blob_sprite_unused_7076.vmdl"},
        {"cs2", "maps/jb_arcade_b6/entities/blob_sprite_unused_7098.vmdl"}
    };

    private enum Directions
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 3,
        Right = 4,
    }

    private const int MinGameTime = 30;
    private const int MaxGameTime = 120;
    private const int WarmupTime = 4;
    private const int MinWinners = 1;
    private const int MaxWinners = 32;
    private const int NumberOfSkins = 16;

    private Dictionary<int, BlobPlayer> BlobPlayers = [];
    private Dictionary<CBreakable, BlobPlayer> BlobToPlayer = [];
    private Dictionary<CBreakable, float> SpriteToArea = [];
    private Dictionary<CBreakable, float> SpriteToRadius = [];

    private Dictionary<Directions, QAngle> DirectionToAngles = [];
    private Vector[] SpriteZoneCenters = new Vector[NumberOfSkins];
    private float[] SpriteZoneAngles = new float[NumberOfSkins];
    private float[] SpriteMinAngles = null!;
    private float[] SpriteMaxAngles = null!;
    private bool SpriteZonesEnabled = false;

    private const float NPCSpriteAreaSmall = (float)Math.PI / 4;
    private const float NPCSpriteAreaMedium = (float)Math.PI;
    private const float NPCSpriteAreaLarge = 4.0f * (float)Math.PI;
    private static float[] NPCSpriteSizes = [NPCSpriteAreaSmall, NPCSpriteAreaMedium, NPCSpriteAreaLarge];
    private int[] NPCBlobsToSpawn = [0, 0, 0];
    private int[] NPCBlobsToSpawnNegative = [0, 0, 0];

    private int GameTime = 90;
    private int TimeLeft;
    private bool TimerActive = false;
    private bool BetweenGames = true;
    private bool OtherGameStarted = false;
    private float StartAllowedTime = 0.0f;
    private int WinnersSettings = 4;
    private int GameNumber = 0;

    private float NextTickTime = 0.0f;
    private const float TimeBetweenTicks = 0.05f;

    private static Vector GameAreaMin = new(1280.0f, -3872.0f, -523.0f);
    private static Vector GameAreaMax = new(2816.0f, -2368.0f, -385.0f);
    private static Vector GameCenter = new(2048.0f, -3104.0f, -523.0f); // z was -444, so equivalent would be -523
    private const float GameZMax = -192;
    private const float GameRadius = 688.0f; //walls are 768 apart, width 32
    private const float GameRadiusPlayers = 768.0f;
    private const int NPCSpritesPositive = 256;
    private const int NPCSpritesNegative = 32;
    private const float SpriteZCorrection = 10.0f;
    private const float SpriteHeight = -512.0f;
    private const float BaseSpriteSize = 8.0f;
    private const float MaxSpriteScale = 16.0f;
    private const int MaxAbsorb = 8; // Max number of sprites you can absorb per tick
    private const float SpeedBoost = 1.5f;
    private const float SpeedBoostLength = 2.0f;
    private const float StartArea = (float)Math.PI;

    private List<CBreakable> Sprites = [];
    private List<CBreakable> SkinSprites = [];
    private CPointWorldText BlobTimeText = null!;
    private CAmbientGeneric BlobStartSound = null!;
    private CAmbientGeneric BlobWarmupSound = null!;
    private CDynamicProp WinnersSettingModel = null!;
    private CDynamicProp CycleStartModel = null!;
    private CDynamicProp BlobStartModel = null!;
    private CDynamicProp BlobResetModel = null!;
    private CLogicScript LightDuelScript = null!;
    private CLogicRelay StrafeJumpStopRelay = null!;

    private bool DEBUG = false;
    private void debugprint(string text)
    {
        if (!DEBUG) return;
        Console.WriteLine($"************* {text}");
    }

    public void Setup()
    {
        DirectionToAngles[Directions.Up] = new QAngle(0, 270, 0);
        DirectionToAngles[Directions.Down] = new QAngle(0, 90, 0);
        DirectionToAngles[Directions.Left] = new QAngle(0, 180, 0);
        DirectionToAngles[Directions.Right] = new QAngle(0, 0, 0);
        DirectionToAngles[Directions.None] = new QAngle(0, 270, 0);

        SetupSkinSprites();
    }

    private void OnAllEntitiesSpawned()
    {
        foreach (KeyValuePair<string, List<CEntityInstance>> kv in EntityList)
        {
            List<CEntityInstance> entities = kv.Value;
            string targetname = kv.Key;

            if (targetname == "blob_winners_sign_model")
            {
                WinnersSettingModel = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "blob_timer_text0")
            {
                BlobTimeText = entities[0].As<CPointWorldText>();
            }
            else if (targetname == "blob_start_sound")
            {
                BlobStartSound = entities[0].As<CAmbientGeneric>();
            }
            else if (targetname == "blob_warmup_sound")
            {
                BlobWarmupSound = entities[0].As<CAmbientGeneric>();
            }
            else if (targetname == "draw_script")
            {
                LightDuelScript = entities[0].As<CLogicScript>();
            }
            else if (targetname == "cycle_start_model")
            {
                CycleStartModel = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "blob_start_model")
            {
                BlobStartModel = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "blob_reset_model")
            {
                BlobResetModel = entities[0].As<CDynamicProp>();
            }
            else if (targetname == "sj_stop_relay")
            {
                StrafeJumpStopRelay = entities[0].As<CLogicRelay>();
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

        if (targetname == "blob_sprite_parented" || targetname == "blob_sprite" || targetname == "blob_sprite_negative")
        {
            Sprites.Add(entity.As<CBreakable>());
        }
    }

    private void SetupSkinSprites()
    {
        float r = GameRadius - 64;
        float oneWedge = 2.0f * (float)Math.PI / NumberOfSkins;
        for (int i = 0; i < NumberOfSkins; i++)
        {
            float angle = i * oneWedge;
            SpriteZoneCenters[i] = new Vector(r * (float)Math.Cos(angle) + GameCenter.X, r * (float)Math.Sin(angle) + GameCenter.Y, GameCenter.Z + SpriteHeight);
            SpriteZoneAngles[i] = i * (360 / NumberOfSkins);
        }
        oneWedge = (float)Math.PI / 16;
        SpriteMinAngles = [0, 0, 0, 0, 0, 0, 0, 0, -15 * oneWedge, -13 * oneWedge, -11 * oneWedge, -9 * oneWedge, -7 * oneWedge, -5 * oneWedge, -3 * oneWedge, -oneWedge];
        SpriteMaxAngles = [-oneWedge, oneWedge, 3 * oneWedge, 5 * oneWedge, 7 * oneWedge, 9 * oneWedge, 11 * oneWedge, 13 * oneWedge, 15 * oneWedge];
    }

    private void SpawnSkinSprites()
    {
        if (SpriteZonesEnabled) return;
        SpriteZonesEnabled = true;
        for (int i = 0; i < NumberOfSkins; i++)
        {
            if (i == 4) continue;
            CBreakable sprite = SpawnSprite(SpriteZoneCenters[i], Color.White, i, Directions.None, 2.0625f, "blob_sprite_skin");
            SkinSprites.Add(sprite);
            sprite.Teleport(SpriteZoneCenters[i], new QAngle(0, SpriteZoneAngles[i], 0), null);
        }
    }

    public void KillSkinSprites()
    {
        foreach (CBreakable sprite in SkinSprites)
        {
            sprite.AddEntityIOEvent(inputName: "Kill", delay: 0.0f);
        }
        SkinSprites.Clear();
        SpriteZonesEnabled = false;
    }

    private void SelectSkin(CCSPlayerController player, Vector playerOrigin)
    {
        if ((playerOrigin - GameCenter).Length() > GameRadius - 128)
        {
            float angle = (float)Math.Atan2(playerOrigin.Y - GameCenter.Y, playerOrigin.X - GameCenter.X);
            //debugprint("Angle of player is: " + angle);
            for (int i = 8; i < NumberOfSkins; i++)
            {
                if (angle <= SpriteMinAngles[i])
                {
                    //debugprint("skin selected: " + i);
                    BlobPlayers[player.Slot].SelectedSkin = i;
                    return;
                }
            }
            for (int i = 8; i >= 0; i--)
            {
                if (angle >= SpriteMaxAngles[i])
                {
                    if (i == 4) return; //Skip empty spot
                    //debugprint("skin selected: " + i);
                    BlobPlayers[player.Slot].SelectedSkin = i;
                    return;
                }
            }
            debugprint("Error: within range but no angle chosen.");
        }
    }

    private void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
    {
        int slot = player.Slot;
        if (!BlobPlayers.TryGetValue(slot, out BlobPlayer? blobPlayer)) return;

        if (pressed.HasFlag(PlayerButtons.Use))
        {
            ActivateSpeedBoost(blobPlayer);
        }
    }

    private void Think()
    {
        if (!TimerActive) return;
        
        foreach (CBreakable sprite in BlobToPlayer.Keys)
        {
            if (!sprite.IsValid) continue;

            Vector origin = BlobToPlayer[sprite].Player.PlayerPawn.Value!.AbsOrigin!;
            QAngle angles = BlobToPlayer[sprite].Player.PlayerPawn.Value!.EyeAngles;
            sprite.Teleport(new Vector3(origin.X, origin.Y, sprite.AbsOrigin!.Z), new Vector3(0, angles.Y, 0), null);
        }

        foreach (BlobPlayer blobPlayer in BlobPlayers.Values)
        {
            AdjustSpeed(blobPlayer, SizeToSpeed(blobPlayer.Radius));
        }

        if (Server.CurrentTime < NextTickTime) return;
        NextTickTime = Server.CurrentTime + TimeBetweenTicks;

        for (int spriteType = 0; spriteType < 3; spriteType++)
        {
            while (NPCBlobsToSpawn[spriteType] > 0)
            {
                SpawnNPCSprite(true, spriteType);
                NPCBlobsToSpawn[spriteType]--;
            }
            while (NPCBlobsToSpawnNegative[spriteType] > 0)
            {
                SpawnNPCSprite(false, spriteType);
                NPCBlobsToSpawnNegative[spriteType]--;
            }
        }

        for (int slot = 0; slot <= Server.MaxPlayers; slot++)
        {
            if (!BlobPlayers.TryGetValue(slot, out BlobPlayer? blobPlayer)) continue;

            CBreakable? blob = blobPlayer.Blob;
            CCSPlayerPawn? pawn = blobPlayer.Player.PlayerPawn.Value;
            if (blob != null && blob.IsValid && pawn != null && pawn.IsValid && blobPlayer.Active)
            {
                if (pawn.Health <= 0)
                {
                    RemovePlayerFromGame(blobPlayer.Player);
                    continue;
                }

                bool success = CheckForAbsorption(blobPlayer, blob);
                if (success)
                {
                    AdjustSpeed(blobPlayer, SizeToSpeed(blobPlayer.Radius));
                }
            }
        }
    }

    private bool CheckForAbsorption(BlobPlayer blobPlayer, CBreakable playerBlob)
    {
        bool success = false;
        int absorbed = 0;
        CBreakable[] absorbedSprites = new CBreakable[MaxAbsorb];
        foreach (CBreakable sprite in Sprites.Where(s => s.IsValid && VectorExtensions.DistanceTo(playerBlob.AbsOrigin!, s.AbsOrigin!) <= blobPlayer.Radius * BaseSpriteSize))
        {
            if (sprite == playerBlob) continue;
            bool isPlayerSprite = false;

            //Get size of intersecting sprite (whether NPC or human)
            float spriteSize = 0;
            float spriteArea = 0;
            if (SpriteToArea.TryGetValue(sprite, out float area) && SpriteToRadius.TryGetValue(sprite, out float radius))
            {
                spriteSize = radius;
                spriteArea = area;
            }
            else if (BlobToPlayer.TryGetValue(sprite, out BlobPlayer? enemyBlobPlayer) && enemyBlobPlayer.Active)
            {
                isPlayerSprite = true;
                //debugprint("Intersecting with player " + playerBlobsToIndices[sprite] + " who has radius " + playerRadii[playerBlobsToIndices[sprite]] + " and area " + playerAreas[playerBlobsToIndices[sprite]]);
                spriteSize = enemyBlobPlayer.Radius;
                spriteArea = enemyBlobPlayer.Area;
            }
            else
            {
                //Skip, non-game sprite
                continue;
            }

            //Absorb if size is 5% larger than the target sprite
            if (blobPlayer.Radius > spriteSize * 1.05f)
            {
                success = true;
                Absorb(blobPlayer, playerBlob, sprite, spriteArea);
                if (isPlayerSprite)
                {
                    KillPlayer(BlobToPlayer[sprite]);
                }
                else
                {
                    absorbedSprites[absorbed++] = sprite;
                    //Add to respawn queue
                    int spriteType = spriteArea == NPCSpriteAreaSmall ? 0 : (spriteArea == NPCSpriteAreaMedium ? 1 : 2);
                    if (sprite.Entity?.Name == "blob_sprite_negative")
                    {
                        NPCBlobsToSpawnNegative[spriteType]++;
                    }
                    else
                    {
                        NPCBlobsToSpawn[spriteType]++;
                    }
                }
                if (absorbed >= MaxAbsorb) break;
            }
        }
        for (absorbed = 0; absorbed < MaxAbsorb; absorbed++)
        {
            if (absorbedSprites[absorbed] != null && absorbedSprites[absorbed].IsValid)
            {
                absorbedSprites[absorbed].Render = Color.Blue;
                Utilities.SetStateChanged(absorbedSprites[absorbed], "CBaseModelEntity", "m_clrRender");
                Sprites.Remove(absorbedSprites[absorbed]);
                absorbedSprites[absorbed].Remove();
            }
        }
        return blobPlayer.Active && success;
    }

    private void Absorb(BlobPlayer blobPlayer, CBreakable playerBlob, CBreakable absorbedSprite, float spriteArea)
    {
        if (absorbedSprite.Entity?.Name == "blob_sprite_negative")
        {
            spriteArea *= -5.0f;
        }
        //debugprint("Player " + playerIndex + " of size " + playerRadii[playerIndex] + " absorbing sprite " + absorbedSprite.entindex() + " of area " + spriteArea);
        AddToArea(blobPlayer, spriteArea);
        //playerRadii[playerIndex] += spriteArea;
        if (blobPlayer.Radius > MaxSpriteScale)
        {
            blobPlayer.Radius = MaxSpriteScale;
        }
        if (blobPlayer.Radius < 1.0f)
        {
            KillPlayer(blobPlayer);
        }
        playerBlob.AddEntityIOEvent(inputName: "SetScale", value: (blobPlayer.Radius / 4).ToString().Replace(",", "."));
    }

    private void KillPlayer(BlobPlayer blobPlayer)
    {
        //debugprint("Player " + playerIndex + " has died. setting to blue for 5 seconds");
        SetArea(blobPlayer, StartArea);
        blobPlayer.Active = false;
        blobPlayer.SpeedBoostActive = false;
        blobPlayer.Blob!.Render = Color.Blue;
        Utilities.SetStateChanged(blobPlayer.Blob, "CBaseModelEntity", "m_clrRender");
        blobPlayer.Blob.AddEntityIOEvent(inputName: "SetScale", value: (blobPlayer.Radius / 4).ToString().Replace(",", "."));
        AdjustSpeed(blobPlayer, 1.5f);
        Timers.Add(new(5.0f, () => ReactivatePlayer(blobPlayer, GameNumber)));
    }

    private void ReactivatePlayer(BlobPlayer blobPlayer, int gameNumberWhenKilled)
    {
        if (!TimerActive || blobPlayer.Active || gameNumberWhenKilled != GameNumber) return;
        //debugprint("Player " + playerIndex + " returning to life");
        blobPlayer.Active = true;
        if (blobPlayer.Blob != null && blobPlayer.Blob.IsValid)
        {
            blobPlayer.Blob.Render = Color.White;
            Utilities.SetStateChanged(blobPlayer.Blob, "CBaseModelEntity", "m_clrRender");
        }
        AdjustSpeed(blobPlayer, SizeToSpeed(1.0f));
    }

    private void ActivateSpeedBoost(BlobPlayer blobPlayer)
    {
        if (!TimerActive) return;
        if (blobPlayer.Radius < 1.2f) return;

        if (blobPlayer.Active && !blobPlayer.SpeedBoostActive)
        {
            blobPlayer.SpeedBoostActive = true;
            AdjustSpeed(blobPlayer, SizeToSpeed(blobPlayer.Radius));
            Timers.Add(new(SpeedBoostLength, () => EndSpeedBoost(blobPlayer)));
            blobPlayer.Blob!.Render = Color.FromArgb(255, 200, 255, 200);
            Utilities.SetStateChanged(blobPlayer.Blob, "CBaseModelEntity", "m_clrRender");
            blobPlayer.Area *= 0.9f;
            SetArea(blobPlayer, blobPlayer.Area);
            blobPlayer.Blob.AddEntityIOEvent(inputName: "SetScale", value: (blobPlayer.Radius / 4).ToString().Replace(",", "."));
        }
    }

    private void EndSpeedBoost(BlobPlayer blobPlayer)
    {
        if (!TimerActive) return;

        if (blobPlayer.Active && blobPlayer.SpeedBoostActive)
        {
            blobPlayer.SpeedBoostActive = false;
            AdjustSpeed(blobPlayer, SizeToSpeed(blobPlayer.Radius));
            blobPlayer.Blob!.Render = Color.White;
            Utilities.SetStateChanged(blobPlayer.Blob, "CBaseModelEntity", "m_clrRender");
        }
    }

    //Range from 0.5 at size 1.0 to 0.06 at size MAX_SPRITE_SCALE
    private float SizeToSpeed(float size)
    {
        return (MaxSpriteScale - size + 1.0f) / MaxSpriteScale * 0.45f + 0.05f;
    }

    private void AdjustSpeed(BlobPlayer blobPlayer, float speed)
    {
        CCSPlayerPawn? pawn = blobPlayer.Player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;

        pawn.VelocityModifier = blobPlayer.SpeedBoostActive ? (speed * SpeedBoost) : speed;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
    }

    private void RemoveSpeed(BlobPlayer blobPlayer)
    {
        CCSPlayerPawn? pawn = blobPlayer.Player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid) return;

        pawn.VelocityModifier = 1.0f;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
    }

    private void RemoveSpeedFromAll()
    {
        foreach (KeyValuePair<int, BlobPlayer> player in BlobPlayers)
        {
            RemoveSpeed(player.Value);
        }
    }

    private float AreaToRadius(float area)
    {
        //area = 3.14*r*r
        //r^2 = area/(3.14)
        //r = sqrt(area/3.14);2
        //debugprint("radius for area " + area + " = " + ((area < 0) ? "0" : sqrt(area/Math.PI).ToString()));
        if (area < 0) return 0;
        return (float)Math.Sqrt(area / Math.PI);
    }

    private void AddToArea(BlobPlayer blobPlayer, float area)
    {
        SetArea(blobPlayer, blobPlayer.Area + area);
        //debugprint("Player " + playerIndex + " gained " + area + " area and now has area " + playerAreas[playerIndex]);
    }

    //3.7 radius: ent_fire blob_script runscriptcode "SetArea(1, 40)"
    //7.9 radius: ent_fire blob_script runscriptcode "SetArea(1, 195)"
    private void SetArea(BlobPlayer blobPlayer, float area)
    {
        blobPlayer.Area = area;
        blobPlayer.Radius = AreaToRadius(blobPlayer.Area);
        if (blobPlayer.MaxArea < blobPlayer.Area) blobPlayer.MaxArea = blobPlayer.Area;
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

    public void AddToWinnersSetting(int toAdd)
    {
        if (TimerActive) return;
        WinnersSettings += toAdd;
        if (WinnersSettings > MaxWinners) WinnersSettings = MaxWinners;
        if (WinnersSettings < MinWinners) WinnersSettings = MinWinners;

        WinnersSettingModel.AddEntityIOEvent(inputName: "Skin", value: $"{WinnersSettings}");
    }

    private CBreakable SpawnSprite(Vector position, Color color, int spriteNum, Directions direction, float scale, string targetname)
    {
        debugprint("In blob SpawnSprite");
        Vector correctedSpawnPosition = new(position.X, position.Y, position.Z);
        CBreakable sprite = Utilities.CreateEntityByName<CBreakable>("func_breakable")!;
        using CEntityKeyValues kv = new();
        kv.SetString("targetname", targetname);
        kv.SetVector("origin", correctedSpawnPosition);
        kv.SetAngle("angles", DirectionToAngles[direction]);
        kv.SetInt("health", 0);
        kv.SetColor("rendercolor", color);
        sprite.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(sprite.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
        sprite.DispatchSpawn(kv);
        sprite.SetModel(BlobSkins.ElementAt(spriteNum).Value);
        sprite.AddEntityIOEvent(inputName: "SetScale", value: scale.ToString().Replace(",", "."));

        return sprite;
    }

    private void SpawnAndAttachPlayerSprite(CCSPlayerController player)
    {
        int slot = player.Slot;
        BlobPlayer blobPlayer = BlobPlayers[player.Slot];
        blobPlayer.MaxArea = StartArea;

        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        Vector position = pawn.AbsOrigin!.Clone();
        position.Z += SpriteHeight - SpriteZCorrection;
        //debugprint("Spawning sprite for player " + ply + " who is at " + ply.GetOrigin());
        CBreakable sprite = SpawnSprite(position, Color.White, blobPlayer.SelectedSkin, Directions.None, AreaToRadius(StartArea) / 4, "blob_sprite_parented");
        blobPlayer.Blob = sprite;
        BlobToPlayer[sprite] = blobPlayer;
        SetArea(blobPlayer, StartArea); //Set area and radius
        blobPlayer.Active = false;
        blobPlayer.SpeedBoostActive = false;
    }

    private void SpawnAndAttachPlayerSprites()
    {
        float startSpeed = SizeToSpeed(1.0f);
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;
            if (pawn == null) continue;

            //debugprint("ply.GetOrigin().Z: " + ply.GetOrigin().Z.ToString() + " being compared to GAME_Z_MAX: " + GAME_Z_MAX);
            if ((pawn.AbsOrigin! - GameCenter).Length() > GameRadiusPlayers) continue;
            if (pawn.AbsOrigin!.Z > GameZMax || pawn.Health <= 0 || (player.Team != CsTeam.Terrorist && player.Team != CsTeam.CounterTerrorist)) continue;
            //debugprint("spawning sprite for player " + ply);
            SpawnAndAttachPlayerSprite(player);
            AdjustSpeed(BlobPlayers[player.Slot], startSpeed);
        }
    }

    private Vector GetRandomPointInCircle(Vector center, float radius)
    {
        for (int i = 0; i < 64; i++)
        {
            float randomX = RandomFloat(-radius, radius);
            float randomY = RandomFloat(-radius, radius);
            if (randomX * randomX + randomY * randomY > radius * radius)
            {
                //debugprint("Discarding point " + randomX + "," + randomY + " because it lies outside the circle: " + (randomX) + "^2 + " + (randomY) + "^2 > " + radius + "^2");
                continue;
            }
            return new Vector(randomX + center.X, randomY + center.Y, center.Z);
        }
        return null!;
    }

    public void SpawnNPCSprite(bool positive, int type)
    {
        // if (!TimerActive) return;
        Vector position = GetRandomPointInCircle(GameCenter, GameRadius);
        if (position == null) return;
        string spriteName = positive ? "blob_sprite" : "blob_sprite_negative";
        Color color = positive ? Color.Green : Color.Red;
        position.Z += SpriteHeight;
        float area = NPCSpriteSizes[type];
        float radius = AreaToRadius(area) / 4;
        CBreakable sprite = SpawnSprite(position, color, 0, Directions.None, radius, spriteName);
        SpriteToArea[sprite] = area;
        SpriteToRadius[sprite] = radius;
        //debugprint("Spawned NPC sprite at " + sprite.GetOrigin() + " of type " + type.ToString() + (positive ? "+" : "-").ToString());
    }

    private int GetRandomSpriteType()
    {
        int randomSize = RandomInt(0, 100);
        if (randomSize == 0)
        {
            return 2;
        }
        else if (randomSize <= 5)
        {
            return 1;
        }
        return 0;
    }

    private void SpawnAllNPCSprites()
    {
        SpawnNPCSprites(false, NPCSpritesNegative);
        Timers.Add(new(0.1f, () =>
        {
            for (int i = 1; i <= 8; i++)
            {
                SpawnNPCSprites(true, NPCSpritesPositive / 8);
            }
        }));
    }

    private void SpawnNPCSprites(bool positive, int quantity)
    {
        for (int i = 0; i < quantity; i++)
        {
            SpawnNPCSprite(positive, GetRandomSpriteType());
        }
    }

    public void StartTimer()
    {
        if (TimerActive || TimeLeft <= 0 || Server.CurrentTime < StartAllowedTime || OtherGameStarted) return;

        ArcadeScripts.Instance.RunScriptCode(new(LightDuelScript, "SetOtherGameStarted(true)", Owner, LightDuelScript));
        StrafeJumpStopRelay.AddEntityIOEvent(inputName: "Trigger"); //Close strafe jump to prevent easy cheating
        KillSkinSprites();
        CycleStartModel.AddEntityIOEvent(inputName: "Skin", value: "2");
        
        BlobResetModel.AddEntityIOEvent(inputName: "Skin", value: "0");
        BlobStartModel.AddEntityIOEvent(inputName: "Skin", value: "1");
        GameNumber++;
        BetweenGames = false;
        TimerActive = true;
        SpawnAllNPCSprites();
        SpawnAndAttachPlayerSprites();

        Timers.Add(new(1.0f, TimerTick));
    }

    private void TimerTick()
    {
        if (!TimerActive) return;
        TimeLeft--;
        if (TimeLeft > 0)
        {
            Timers.Add(new(1.0f, TimerTick));
            if (TimeLeft == GameTime)
            {
                //Warmup is over, start the game!
                SetTimerTextAll("GO!!", Color.Green);
                BlobStartSound.AddEntityIOEvent(inputName: "PlaySound");
                foreach (KeyValuePair<int, BlobPlayer> player in BlobPlayers)
                {
                    player.Value.Active = true;
                }
                DisplayHudTextToAll();
            }
            else if (TimeLeft > GameTime)
            {
                SetTimerTextAll(" " + (TimeLeft - GameTime).ToString() + "!", Color.Red);
                BlobWarmupSound.AddEntityIOEvent(inputName: "PlaySound");
            }
            else
            {
                SetTimerSeconds(TimeLeft);
                DisplayHudTextToAll();
            }
        }
        else
        {
            EndGame(true);
            ResetTime();
        }
    }

    private void EndGame(bool rankPlayers)
    {
        TimerActive = false;
        foreach (CBreakable sprite in Sprites.ToList())
        {
            Sprites.Remove(sprite);
            if (sprite.IsValid) sprite.AddEntityIOEvent(inputName: "Kill");
        }
        RemoveSpeedFromAll();
        if (rankPlayers)
        {
            RetrieveTopScores(WinnersSettings);
        }

        foreach (KeyValuePair<int, BlobPlayer> player in BlobPlayers)
        {
            RemoveSpeed(player.Value);
            BlobPlayers[player.Key].Remove();
            BlobPlayers.Remove(player.Key);
        }
        BlobToPlayer.Clear();
        SpriteToArea.Clear();
        SpriteToRadius.Clear();
        return;
    }

    private void SetGameTime(int time)
    {
        GameTime = time;
    }

    public void StopButton()
    {
        EndGame(false);
        ResetTime();
    }

    private void ResetTime()
    {
        StartAllowedTime = Server.CurrentTime + 1.0f;
        //debugprint("StartAllowedTime set to: " + StartAllowedTime);
        BetweenGames = true;
        TimeLeft = GameTime + WarmupTime;
        SetTimerSeconds(GameTime);
        if (!OtherGameStarted)
        {
            CycleStartModel.AddEntityIOEvent(inputName: "Skin", value: "0");
            ArcadeScripts.Instance.RunScriptCode(new(LightDuelScript, "SetOtherGameStarted(false)", Owner, LightDuelScript));
            BlobResetModel.AddEntityIOEvent(inputName: "Skin", value: "1");
            BlobStartModel.AddEntityIOEvent(inputName: "Skin", value: "0");
        }
        return;
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
        SetTimerText(text, color);
    }

    private void SetTimerText(string text, Color color)
    {
        BlobTimeText.AddEntityIOEvent(inputName: "SetMessage", value: text);
        BlobTimeText.Render = color;
        Utilities.SetStateChanged(BlobTimeText, "CBaseModelEntity", "m_clrRender");
    }

    private void RetrieveTopScores(int winners)
    {
        List<PlayerScore> winningPlayers = SortPlayersByScore();

        CInfoTeleportDestination[] teleportDesinationEntities = [EntityGroup[0].As<CInfoTeleportDestination>(), EntityGroup[1].As<CInfoTeleportDestination>(), EntityGroup[2].As<CInfoTeleportDestination>()];
        CInfoTeleportDestination winnersDestination = EntityGroup[3].As<CInfoTeleportDestination>();
        CInfoTeleportDestination losersDestination = EntityGroup[4].As<CInfoTeleportDestination>();
        for (int winningScores = 0; winningScores < winningPlayers.Count; winningScores++)
        {
            int slot = winningPlayers[winningScores].Slot;
            CInfoTeleportDestination teleportDestination = losersDestination;
            if (winningScores < 3 && winningScores < WinnersSettings)
            {
                debugprint("Player " + slot + " for podium " + (winningScores + 1));
                teleportDestination = teleportDesinationEntities[winningScores];
            }
            else if (winningScores < winners)
            {
                debugprint("Player " + slot + " for winners podium");
                teleportDestination = winnersDestination;
            }
            else
            {
                debugprint("Player " + slot + " for losers podium");
            }
            if (!BlobPlayers.TryGetValue(slot, out BlobPlayer? blobPlayer)) continue;

            blobPlayer.Active = false;
            CCSPlayerPawn? pawn = blobPlayer.Player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;
            pawn.Teleport((Vector3)teleportDestination.AbsOrigin!, null, Vector3.Zero);
        }
    }

    private string ArrayToString(PlayerScore[] _array, int length)
    {
        if (_array == null) return "";
        string returnString = "[";
        for (int i = 0; i < length; i++)
        {
            returnString += _array[i].ToString();
            if (i != length - 1) returnString += ", ";
        }
        return returnString + "]";
    }

    private class PlayerScore
    {
        public int Slot;
        public float Area;
        public float MaxArea;

        public override string ToString()
        {
            return $"{{index:{Slot}, area:{Area}, maxarea:{MaxArea}}}";
        }
    }

    private int ComparePlayersByScores(PlayerScore playerA, PlayerScore playerB)
    {
        //debugprint("comparing player " + playerA.index + " with player " + playerB.index);
        //debugprint("area/maxarea: " +playerA.area+","+playerA.maxarea+" to "+playerB.area+","+playerB.maxarea);
        if (playerA.Area < playerB.Area) return 1;
        else if (playerA.Area > playerB.Area) return -1;
        else if (playerA.MaxArea < playerB.MaxArea) return 1;
        else if (playerA.MaxArea > playerB.MaxArea) return -1;
        return 0;
    }

    private List<PlayerScore> SortPlayersByScore()
    {
        List<PlayerScore> activePlayers = [];
        foreach (KeyValuePair<int, BlobPlayer> player in BlobPlayers)
        {
            //if (!playerActive.ContainsKey(playerIndex) || !playerActive[playerIndex]) continue;
            //if (!playerEntities.ContainsKey(playerIndex) || playerEntities[playerIndex].GetHealth() <= 0) continue;
            //This should really be how the data is stored, but instead bad design decisions were made.
            PlayerScore activePlayer = new() { Slot = player.Key, Area = player.Value.Area, MaxArea = player.Value.MaxArea };
            activePlayers.Add(activePlayer);
            //debugprint("Adding player " + playerIndex + " with area " + playerAreas[playerIndex] + " to active players");
        }
        //debugprint("Players before sort: " + ArrayToString(activePlayers.ToArray(), activePlayers.Count));
        activePlayers.Sort((a, b) => ComparePlayersByScores(a, b));
        //debugprint("Players after sort: " + ArrayToString(activePlayers.ToArray(), activePlayers.Count));
        return activePlayers;
    }

    private class PlayerPosition
    {
        public int position = 1;
        public float next_score = 99999;
    }

    private PlayerPosition GetPlayerPosition(BlobPlayer blobPlayer)
    {
        float score = blobPlayer.Area;
        PlayerPosition scores = new();
        foreach (KeyValuePair<int, BlobPlayer> player in BlobPlayers)
        {
            if (player.Value == blobPlayer) continue;
            if (player.Value.Area > score)
            {
                scores.position++;
                if (player.Value.Area < scores.next_score)
                {
                    scores.next_score = player.Value.Area;
                }
            }
        }
        return scores;
    }

    private void DisplayHudTextToAll()
    {
        foreach (KeyValuePair<int, BlobPlayer> player in BlobPlayers)
        {
            if (player.Value.Player.PlayerPawn.Value != null && player.Value.Player.PlayerPawn.Value.IsValid && player.Value.Player.PlayerPawn.Value.Health > 0)
            {
                DisplayScore(player.Value, BlobPlayers.Count);
            }
        }
    }

    private void DisplayScore(BlobPlayer blobPlayer, int totalPlayers)
    {
        PlayerPosition scores = GetPlayerPosition(blobPlayer);

        string score_text = "Current Area: " + ((int)(blobPlayer.Area * 10.0f)).ToString() + //"(radius " + playerRadii[playerIndex] + ")" +
                        "<br>Time Left: " + TimeLeft + "s" +
                        "<br>Current Place: " + scores.position.ToString() + " out of " + totalPlayers.ToString() +
                        "<br>Next Place Score: " + ((scores.next_score < 99999) ? ((int)(scores.next_score * 10.0f)).ToString() : "N/A");
        SetMessage(blobPlayer.Player.Slot, score_text, 4.0f);
    }

    public void RemovePlayerFromGameActivator(CEntityInstance activator)
    {
        RemovePlayerFromGame(activator.As<CCSPlayerPawn>().OriginalController.Value!);
    }

    private void RemovePlayerFromGame(CCSPlayerController player)
    {
        if (player == null || !player.IsValid) return;
        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        Vector playerOrigin = pawn.AbsOrigin!;

        int slot = player.Slot;
        if (!BlobPlayers.TryGetValue(slot, out BlobPlayer? blobPlayer)) return;
        if (!blobPlayer.Active || blobPlayer.Blob == null) return;

        Vector vectorFromGameCenter = GameCenter - playerOrigin;
        //debugprint("vectorFromGameCenter: " + vectorFromGameCenter);
        //debugprint("vectorFromGameCenter.Length(): " + vectorFromGameCenter.Length().ToString() + " compared to GAME_RADIUS_PLAYERS: " + GAME_RADIUS_PLAYERS.ToString());
        if (player.Health > 0 && vectorFromGameCenter.Length() < GameRadiusPlayers && playerOrigin.Z < GameZMax)
        {
            //debugprint("Leaving player in game because player is still within GAME_RADIUS_PLAYERS of GAME_CENTER");
            return;
        }

        SetMessage(player.Slot, "You have exited the game.", 5.0f);

        RemoveSpeed(BlobPlayers[player.Slot]);
        BlobPlayers[player.Slot].Remove();
        BlobPlayers.Remove(player.Slot);
    }

    public void DisplayHelpText()
    {
        if (TimerActive) return;
        bool playersInGame = false;
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;
            if (pawn == null) continue;

            Vector playerOrigin = pawn.AbsOrigin!;
            if (playerOrigin.X >= GameAreaMin.X && playerOrigin.X <= GameAreaMax.X &&
                playerOrigin.Y >= GameAreaMin.Y && playerOrigin.Y <= GameAreaMax.Y &&
                playerOrigin.Z >= GameAreaMin.Z && playerOrigin.Z < GameAreaMax.Z)
            {
                if (!BlobPlayers.ContainsKey(player.Slot)) BlobPlayers.Add(player.Slot, new() { Player = player });
                SelectSkin(player, playerOrigin);
                
                SetMessage(player.Slot, $"<font class='fontSize-sm horizontal-center'>1. Look down and run around.<br>2. Absorb green orbs and smaller players to gain mass.<br>3. Red orbs make you lose mass.<br>4. Press use to boost for -10% mass.<br>5. If absorbed, you will turn blue and respawn after 5s.<br>6. Most mass at the end wins! (Tiebreaker: highest mass achieved.)<br><br>Skin Selected: {SkinDisplayNames[BlobPlayers[player.Slot].SelectedSkin]}</font>", 4.0f);
                playersInGame = true;
            }
        }
        if (playersInGame && !OtherGameStarted)
        {
            SpawnSkinSprites();
        }
    }

    public void SetOtherGameStarted(bool val)
    {
        OtherGameStarted = val;
        if (OtherGameStarted) KillSkinSprites();
    }

    public override void Warmup()
    {
        StopButton();
        StartTimer();
    }

    public override void Remove()
    {
        foreach (Timer timer in Timers) timer?.Kill();
        
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent -= OnEntitySpawned;
        ArcadeScripts.Instance.RemoveListener<Listeners.OnPlayerButtonsChanged>(OnPlayerButtonsChanged);
        ArcadeScripts.Instance.RemoveListener<Listeners.OnTick>(Think);
    }
}