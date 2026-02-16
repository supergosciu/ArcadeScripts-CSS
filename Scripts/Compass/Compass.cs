using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static ArcadeScripts.Random;
using static ArcadeScripts.TextDisplayHelper;

namespace ArcadeScripts.Scripts;

public class Compass : ScriptBase
{
    public Compass(CLogicScript owner) : base(owner)
    {
        Functions.Add("AdjustNeedle", new ScriptFunction(AdjustNeedle));
        Functions.Add("PickUp", new ScriptFunction<CEntityInstance>(PickUp));
        Functions.Add("RegisterItem", new ScriptFunction(RegisterItem));

        EntityNames = ["secret_compass_timer", "rocketjump_entity_maker"];

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent += OnEntitySpawned;
    }

    private int CurrentTargetRoom = 9999;
    private int TargetsInspected = 0;
    private float SecondsAtTarget = 0;
    private bool AtTarget = false;
    private bool QuestOver = false;
    private const int TotalTargets = 1;

    private static Vector[] MainLobby = [new Vector(1152.0f, -612.0f, 0.0f), new Vector(3648.0f, 800.0f, 0.0f)];
    private static Vector[] CellsArea = [new Vector(-564.0f, -564.0f, 0.0f), new Vector(448.0f, 504.0f, 0.0f)];
    private static Vector[] SpriteRoom = [new Vector(1536.0f, -2287.0f, 0.0f), new Vector(3072.0f, -800.0f, 0.0f)];
    private static Vector[] Disco = [new Vector(3712.0f, -736.0f, 0.0f), new Vector(5760.0f, 800.0f, 0.0f)];
    private static Vector[] Tapper = [new Vector(4000.0f, 864.0f, 0.0f), new Vector(4736.0f, 2400.0f, 0.0f)];
    private static Vector[] Decathlon = [new Vector(1152.0f, 2528.0f, 0.0f), new Vector(3136.0f, 4512.0f, 0.0f)];
    private static Vector[] Bee = [new Vector(1152.0f, 864.0f, 0.0f), new Vector(3936.0f, 2400.0f, 0.0f)];
    private static Vector[] ClimbA = [new Vector(3712.0f, -2848.0f, 0.0f), new Vector(4672.0f, -800.0f, 0.0f)];
    private static Vector[] ClimbB = [new Vector(4672.0f, -1824.0f, 0.0f), new Vector(5760.0f, -800.0f, 0.0f)];
    private static Vector[][] Rooms = [MainLobby, CellsArea, SpriteRoom, Disco, Tapper, Decathlon, Bee, ClimbA, ClimbB];
    private Vector CurrentTarget = null!;

    private CC4 Item = null!;
    private CFuncRotating Needle = null!;
    private CDynamicProp CompassProp = null!;
    private CTimerEntity Timer = null!;
    private CEnvEntityMaker EntityMaker = null!;

    private bool DEBUG = false;
    private void debugprint(string text)
    {
        if (!DEBUG) return;
        Console.WriteLine($"************* {text}");
    }

    private void OnAllEntitiesSpawned()
    {
        Timer = EntityList["secret_compass_timer"][0].As<CTimerEntity>();
        EntityMaker = EntityList["rocketjump_entity_maker"][0].As<CEnvEntityMaker>();
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        string? targetname = entity.Entity?.Name;

        if (targetname == "secret_compass_c4")
        {
            Item = entity.As<CC4>();
        }
        if (targetname == "secret_compass_needle")
        {
            Needle = entity.As<CFuncRotating>();
        }
        if (targetname == "secret_compass_base")
        {
            CompassProp = entity.As<CDynamicProp>();
        }
    }

    public void RegisterItem()
    {
        if (CurrentTarget == null)
        {
            ChooseNewTarget();
        }
    }

    private void ChooseNewTarget()
    {
        int roomNumber = RandomInt(0, Rooms.Length - 2);
        if (roomNumber >= CurrentTargetRoom) roomNumber++; //ignore current room
        CurrentTargetRoom = roomNumber;
        CurrentTarget = new Vector(RandomFloat(Rooms[CurrentTargetRoom][0].X, Rooms[CurrentTargetRoom][1].X), RandomFloat(Rooms[CurrentTargetRoom][0].Y, Rooms[CurrentTargetRoom][1].Y), 0.0f);
        debugprint("Set target to " + CurrentTarget + ", a point in room " + roomNumber);
    }

    private void TriggerLocation()
    {
        if (Item == null || !Item.IsValid || Item.OwnerEntity.Value == null || !AtTarget || QuestOver) return;
        TargetsInspected++;
        debugprint("Compass holder inspected their weapon at the target, targetsInspected = " + TargetsInspected);
        if (TargetsInspected >= TotalTargets)
        {
            QuestOver = true;
            CompassProp.AddEntityIOEvent(inputName: "Skin", value: "0");
            Timer.AddEntityIOEvent(inputName: "Kill");
            Needle.AddEntityIOEvent(inputName: "Kill");
            Vector spawnLocation = Item.OwnerEntity.Value.AbsOrigin!.Clone();
            spawnLocation.Z += 32.0f;
            EntityMaker.Teleport(spawnLocation, new QAngle(0, 0, 0));
            EntityMaker.AddEntityIOEvent(inputName: "ForceSpawn");
            debugprint("spawning rocketjump shotgun at " + spawnLocation);
        }
        else
        {
            ChooseNewTarget();
        }
    }

    private void CheckIfAtTarget()
    {
        if (CompassProp == null || Item == null || Item.OwnerEntity.Value == null) return;
        Vector originXY = CompassProp.AbsOrigin!.Clone();
        originXY.Z = 0;
        if ((originXY - CurrentTarget).LengthSqr() <= 4096.0f) //64^2
        {
            if (!AtTarget)
            {
                AtTarget = true;
                CompassProp.AddEntityIOEvent(inputName: "Skin", value: "1");
            }
            SecondsAtTarget += 0.015625f;
        }
        else
        {
            SecondsAtTarget = 0;
            if (AtTarget)
            {
                CompassProp.AddEntityIOEvent(inputName: "Skin", value: "0");
                AtTarget = false;
            }
        }
    }

    public void PickUp(CEntityInstance? activator)
    {
        if (activator == null) return;

        if (!QuestOver)
        {
            CCSPlayerController? player = activator.As<CCSPlayerPawn>().OriginalController.Value;
            if (player != null)
                SetMessage(player.Slot, "Stand where the compass turns green for 3 seconds to uncover the secret.", 15.0f);
        }
    }

    private float YawFromVector(Vector vector)
    {
        return (float)Math.Atan2(vector.X, -vector.Y);
    }

    private float GetYawBetweenPoints(Vector start, Vector end)
    {
        float yawInRads = YawFromVector(start - end) + ((float)Math.PI / 2.0f);
        float targetYawInDegrees = yawInRads / (float)Math.PI * 180.0f;
        // float yawInDegrees = targetYawInDegrees - CompassProp.AbsRotation!.Y;
        return targetYawInDegrees;
    }

    public void AdjustNeedle()
    {
        if (Item == null) return;
        if (Item.OwnerEntity.Value != null)
        {
            float yawInDegrees = GetYawBetweenPoints(Needle.AbsOrigin!, CurrentTarget);
            Needle.Teleport(null, new QAngle(0.0f, yawInDegrees, 0.0f));
            CheckIfAtTarget();
            if (SecondsAtTarget >= 3)
            {
                SecondsAtTarget = 0;
                TriggerLocation();
            }
        }
    }

    public override void Remove()
    {
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent -= OnEntitySpawned;
    }
}