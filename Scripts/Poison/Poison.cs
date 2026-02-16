using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using static ArcadeScripts.TextDisplayHelper;

namespace ArcadeScripts.Scripts;

public class Poison : ScriptBase
{
    public Poison(CLogicScript owner) : base(owner)
    {
        Functions.Add("TryPoison", new ScriptFunction(TryPoison));
        Functions.Add("PickUpSpecialItem", new ScriptFunction<CEntityInstance, CEntityInstance>(PickUpSpecialItem));

        EntityNames = ["hurt_script"];

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
    }

    private const int Ticks = 5;
    private const float Delay = 1.0f;
    private const float Cooldown = 5.0f;
    private float NextPoisonAvailable = 0.0f;
    private Dictionary<CCSPlayerPawn, int> PoisonCounterRemaining = [];
    private Timer? Timer;

    private CWeaponTec9 Item = null!;
    private CCSPlayerPawn SpecialPlayer = null!;
    private CLogicScript HurtScript = null!;

    private void OnAllEntitiesSpawned()
    {
        HurtScript = EntityList["hurt_script"][0].As<CLogicScript>();
    }

    public void PickUpSpecialItem(CEntityInstance? activator, CEntityInstance? caller)
    {
        if (activator == null || caller == null) return;

        CCSPlayerController? player = activator.As<CCSPlayerPawn>().OriginalController.Value;
        if (player == null || !player.IsValid) return;

        Item = caller.As<CWeaponTec9>();
        SpecialPlayer = activator.As<CCSPlayerPawn>();
        SetMessage(player.Slot, "Left click to attack nearby enemies with poison.", 15.0f);
    }

    public void TryPoison()
    {
        if (Server.CurrentTime < NextPoisonAvailable) return;

        if (Item != null && (!Item.IsValid || Item.OwnerEntity.Value != SpecialPlayer))
        {
            SpecialPlayer = null!;
        }

        if (SpecialPlayer == null) return;

        List<PlayerDistance> victim = GetClosestPlayers(SpecialPlayer.AbsOrigin!, 1, 1024, SpecialPlayer, SpecialPlayer.TeamNum);
        if (victim.Count == 0) return;

        NextPoisonAvailable = Server.CurrentTime + Cooldown;

        ArcadeScripts.Instance.RunScriptCode(new(HurtScript, "SetAttacker(!activator, HurterThree)", SpecialPlayer, SpecialPlayer));
        PoisonPlayer(victim[0].Pawn);
    }

    private void PoisonPlayer(CCSPlayerPawn pawn)
    {
        PoisonCounterRemaining.TryAdd(pawn, 0);
        if (PoisonCounterRemaining[pawn] > 0)
        {
            PoisonCounterRemaining[pawn] = Ticks;
            return;
        }

        PoisonCounterRemaining[pawn] = Ticks;
        Timer = new(Delay, () => PoisonTick(pawn));
    }

    private void PoisonTick(CCSPlayerPawn pawn)
    {
        if (PoisonCounterRemaining[pawn] <= 0)
        {
            PoisonCounterRemaining[pawn] = 0;
            return;
        }

        PoisonCounterRemaining[pawn] = PoisonCounterRemaining[pawn] - 1;
        if (pawn == null || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

        ArcadeScripts.Instance.RunScriptCode(new(HurtScript, "HurtActivator(!activator, HurterThree)", pawn, pawn));
        CCSPlayerController? player = pawn.OriginalController.Value;
        if (player == null) return;

        DisplayTextPoison(player);
        Timer = new(Delay, () => PoisonTick(pawn));
    }

    private void DisplayTextPoison(CCSPlayerController player)
    {
        SetMessage(player.Slot, "You are poisoned!", 2.0f);
    }

    private class PlayerDistance
    {
        public required CCSPlayerPawn Pawn;
        public required float Distance;
    }

    private List<PlayerDistance> GetClosestPlayers(Vector origin, int maxPlayers, float maxDistance, CCSPlayerPawn ignorePlayer, int ignoreTeam)
    {
        List<PlayerDistance> nearbyPlayers = [];
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;
            if (pawn == null || pawn == ignorePlayer || player.TeamNum == ignoreTeam || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;

            float distanceToOrigin = VectorExtensions.DistanceTo(pawn.AbsOrigin!, origin);
            if (distanceToOrigin > maxDistance) continue;

            PlayerDistance playerDistance = new() { Pawn = pawn, Distance = distanceToOrigin };
            nearbyPlayers.Add(playerDistance);
        }

        nearbyPlayers.Sort(ComparePlayersByDistance);
        int numPlayers = (maxPlayers < nearbyPlayers.Count) ? maxPlayers : nearbyPlayers.Count;

        return nearbyPlayers.GetRange(0, numPlayers);
    }

    //Lower is better
    private int ComparePlayersByDistance(PlayerDistance playerA, PlayerDistance playerB)
    {
        if (playerA.Distance > playerB.Distance) return 1;
        else if (playerA.Distance < playerB.Distance) return -1;
        return 0;
    }

    public override void Remove()
    {
        Timer?.Kill();
        Timer = null;

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
    }
}