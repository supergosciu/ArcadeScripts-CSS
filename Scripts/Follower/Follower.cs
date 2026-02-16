using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static ArcadeScripts.Random;

namespace ArcadeScripts.Scripts;

public class Follower : ScriptBase
{
    public Follower(CLogicScript owner) : base(owner)
    {
        Functions.Add("ChooseRandomFollower", new ScriptFunction(ChooseRandomFollower));

        EntityNames = ["follower_train", "follower_track1", "follower_track2", "follower_sprite1", "follower_sprite2"];
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
        ArcadeScripts.Instance.RegisterListener<Listeners.OnTick>(Think);
    }

    private bool Stopped = true;
    private float NextTickTime = 0.0f;
    private const float MaxDistanceToFollow = 128.0f;
    private const float TimeBetweenTicks = 0.5f;

    private CCSPlayerPawn? CurrentTarget = null;
    private CPathTrack TrackEntityA = null!;
    private CPathTrack TrackEntityB = null!;
    private CFuncTrackTrain TrainEntity = null!;
    private List<CBreakable> FollowerSprites = [];

    private void OnAllEntitiesSpawned()
    {
        TrainEntity = EntityList["follower_train"][0].As<CFuncTankTrain>();
        TrackEntityA = EntityList["follower_track1"][0].As<CPathTrack>();
        TrackEntityB = EntityList["follower_track2"][0].As<CPathTrack>();
        FollowerSprites.Add(EntityList["follower_sprite1"][0].As<CBreakable>());
        FollowerSprites.Add(EntityList["follower_sprite2"][0].As<CBreakable>());
    }

    private void Think()
    {
        if (Server.CurrentTime < NextTickTime) return;
        NextTickTime = Server.CurrentTime + TimeBetweenTicks;

        if (CurrentTarget == null || !CurrentTarget.IsValid || CurrentTarget.Health <= 0)
        {
            SetNewRandomTarget();
            if (TrainEntity == null || !TrainEntity.IsValid) return;
            TrainEntity.AddEntityIOEvent(inputName: "Stop", activator: TrainEntity, caller: TrainEntity);
            Stopped = true;
            return;
        }
        if (Stopped)
        {
            if (TrainEntity == null || !TrainEntity.IsValid) return;
            if (GetDistanceFromTrainToTarget() < MaxDistanceToFollow) return;
            TrainEntity.AddEntityIOEvent(inputName: "StartForward", activator: TrainEntity, caller: TrainEntity);
            Stopped = false;
        }

        if (TrainEntity == null || !TrainEntity.IsValid) return;
        MoveTrack(CurrentTarget);
    }

    public void ChooseRandomFollower()
    {
        int random = RandomInt(0, 1);
        FollowerSprites[random].AddEntityIOEvent(inputName: "Kill");
        FollowerSprites.RemoveAt(random);
    }

    private void SetNewRandomTarget()
    {
        CurrentTarget = GetRandomLivingPlayer();
    }

    private CCSPlayerPawn GetRandomLivingPlayer()
    {
        List<CCSPlayerPawn> players = [];
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE || player.Team != CsTeam.CounterTerrorist && player.Team != CsTeam.Terrorist) continue;

            players.Add(pawn);
        }

        if (players.Count == 0) return null!;

        int plysIndex = RandomInt(0, players.Count - 1);
        return players[plysIndex];
    }

    private float GetDistanceFromTrainToTarget()
    {
        if (CurrentTarget == null || TrainEntity == null) return 0.0f;

        Vector vectorDifference = TrainEntity.AbsOrigin! - CurrentTarget.AbsOrigin!;
        return vectorDifference.Length();
    }

    private void MoveTrack(CCSPlayerPawn target)
    {
        if (target == null) return;

        if (TrainEntity == null || !TrainEntity.IsValid) return;

        if (GetDistanceFromTrainToTarget() < MaxDistanceToFollow)
        {
            TrainEntity.AddEntityIOEvent(inputName: "Stop", activator: TrainEntity, caller: TrainEntity);
            Stopped = true;
            return;
        }

        Vector targetOrigin = target.AbsOrigin!;
        TrackEntityA.Teleport(TrainEntity.AbsOrigin);
        TrackEntityB.Teleport(new Vector(targetOrigin.X, targetOrigin.Y, targetOrigin.Z + 30.0f));
    }

    public override void Remove()
    {
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
        ArcadeScripts.Instance.RemoveListener<Listeners.OnTick>(Think);
    }
}