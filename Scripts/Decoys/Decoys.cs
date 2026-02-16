using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace ArcadeScripts.Scripts;

public class Decoys : ScriptBase
{
    public Decoys(CLogicScript owner) : base(owner)
    {
        Functions.Add("CheckForNewDecoys", new ScriptFunction(CheckForNewDecoys));
        Functions.Add("PickUpConcussionItem", new ScriptFunction<CEntityInstance, CEntityInstance>(PickUpConcussionItem));
        Functions.Add("PickUpTeleportItem", new ScriptFunction<CEntityInstance, CEntityInstance>(PickUpTeleportItem));

        EntityNames = ["secret_decoy_timer", "secret_teleportation_target", "secret_concussion_target", "secret_teleportation_sound2"];
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent += OnEntitySpawned;
        ArcadeScripts.Instance.OnEntityRemovedEvent += OnEntityRemoved;
    }

    private Vector DodgeMin = new(2096.0f, -1824.0f, -1024.0f);
    private Vector DodgeMax = new(2688.0f, 3744.0f, -800.0f);

    private List<CDecoyProjectile> DecoysList = [];
    private CTimerEntity SecretDecoyTimer = null!;
    private CAmbientGeneric SecretConcussionSound = null!;
    private CAmbientGeneric SecretTeleportationSound1 = null!;
    private CAmbientGeneric SecretTeleportationSound2 = null!;

    private CC4 TeleportItem = null!;
    private CCSPlayerPawn TeleportSpecialPlayer = null!;
    private CInfoTarget TeleportationTarget = null!;
    private Vector OldPosition = null!;
    private Vector NewPosition = null!;

    private CC4 ConcussionItem = null!;
    private CCSPlayerPawn ConcussionSpecialPlayer = null!;
    private CInfoTarget ConcussionTarget = null!;

    private bool DEBUG = false;
    private void debugprintDecoy(string text)
    {
        if (!DEBUG) return;
        Console.WriteLine($"************* {text}");
    }

    private void OnAllEntitiesSpawned()
    {
        SecretDecoyTimer = EntityList["secret_decoy_timer"][0].As<CTimerEntity>();
        TeleportationTarget = EntityList["secret_teleportation_target"][0].As<CInfoTarget>();
        ConcussionTarget = EntityList["secret_concussion_target"][0].As<CInfoTarget>();
        SecretTeleportationSound2 = EntityList["secret_teleportation_sound2"][0].As<CAmbientGeneric>();
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        string? targetname = entity.Entity?.Name;
        string classname = entity.DesignerName;

        if (classname == "decoy_projectile")
        {
            DecoysList.Add(entity.As<CDecoyProjectile>());
        }
        else if (targetname == "secret_concussion_sound")
        {
            SecretConcussionSound = entity.As<CAmbientGeneric>();
        }
        else if (targetname == "secret_teleportation_sound")
        {
            SecretTeleportationSound1 = entity.As<CAmbientGeneric>();
        }
    }

    private void OnEntityRemoved(CEntityInstance entity)
    {
        if (entity.DesignerName == "decoy_projectile")
        {
            DecoysList.Remove(entity.As<CDecoyProjectile>());
        }
    }

    public void PickUpTeleportItem(CEntityInstance? activator, CEntityInstance? caller)
    {
        if (activator == null || caller == null) return;

        TeleportItem = caller.As<CC4>();
        TeleportSpecialPlayer = activator.As<CCSPlayerPawn>();
        SecretDecoyTimer.AddEntityIOEvent(inputName: "Enable");
    }

    public void PickUpConcussionItem(CEntityInstance? activator, CEntityInstance? caller)
    {
        if (activator == null || caller == null) return;

        ConcussionItem = caller.As<CC4>();
        ConcussionSpecialPlayer = activator.As<CCSPlayerPawn>();
        SecretDecoyTimer.AddEntityIOEvent(inputName: "Enable");
    }

    public void CheckForNewDecoys()
    {
        if (TeleportItem != null && (!TeleportItem.IsValid || TeleportItem.OwnerEntity.Value != TeleportSpecialPlayer))
        {
            TeleportSpecialPlayer = null!;
        }
        if (ConcussionItem != null && (!ConcussionItem.IsValid || ConcussionItem.OwnerEntity.Value != ConcussionSpecialPlayer))
        {
            ConcussionSpecialPlayer = null!;
        }

        if (TeleportSpecialPlayer == null && ConcussionSpecialPlayer == null)
        {
            SecretDecoyTimer.AddEntityIOEvent(inputName: "Disable");
            return;
        }

        foreach (CDecoyProjectile decoy in DecoysList)
        {
            // debugprintDecoy("decoy " + decoy + " has owner " + decoy.GetOwner());
            Vector location = decoy.AbsOrigin!;
            Vector velocity = decoy.AbsVelocity;
            if (TeleportSpecialPlayer != null && decoy.OwnerEntity.Value == TeleportSpecialPlayer)
            {
                if (velocity.X == 0 && velocity.Y == 0 && velocity.Z == 0)
                {
                    TeleportGrenade(decoy);
                }
            }
            else if (ConcussionSpecialPlayer != null && decoy.OwnerEntity.Value == ConcussionSpecialPlayer)
            {
                if (velocity.X == 0 && velocity.Y == 0 && velocity.Z == 0)
                {
                    ConcussionGrenade(decoy);
                }
            }
        }
    }

    private void TeleportGrenade(CDecoyProjectile decoy)
    {
        if (TeleportItem == null || !decoy.IsValid) return;
        CBaseEntity thrower = decoy.OwnerEntity.Value!;
        if (thrower == null || TeleportItem.OwnerEntity.Value != thrower) return;

        OldPosition = thrower.AbsOrigin!.Clone();
        Vector decoyPosition = decoy.AbsOrigin!;
        NewPosition = new Vector(decoyPosition.X, decoyPosition.Y, decoyPosition.Z + 0.5f);

        //Play sounds and sparks at start and ending positions
        TeleportationTarget.Teleport(OldPosition);
        SecretTeleportationSound1.AddEntityIOEvent(inputName: "PlaySound", delay: 0.1f, activator: thrower, caller: thrower);
        SecretTeleportationSound2.AddEntityIOEvent(inputName: "PlaySound", delay: 0.1f, activator: thrower, caller: thrower);
        debugprintDecoy("killing decoy: " + decoy);
        decoy.AddEntityIOEvent(inputName: "Kill", delay: 0.01f, activator: decoy, caller: decoy);
        //Remove z velocity so if they are midjump they don't get teleported back
        Vector vel = thrower.AbsVelocity;
        vel.Z = 0;
        thrower.Teleport(NewPosition, null, vel);
        debugprintDecoy("Old position: " + OldPosition);
        debugprintDecoy("New position: " + NewPosition);
        //Check if the player has fallen to the ground 0.2 seconds later
        Timers.Add(new(0.2f, () => CheckIfMoved(thrower)));
    }

    private void CheckIfMoved(CBaseEntity activator)
    {
        if (activator == null || activator.Health <= 0 || NewPosition == null || OldPosition == null)
        {
            return;
        }

        debugprintDecoy("player origin: " + activator.AbsOrigin);
        debugprintDecoy("new_teleportation_position: " + NewPosition);
        if (NewPosition.Z - activator.AbsOrigin!.Z < 1.5f)
        {
            debugprintDecoy("Player has not moved so must be stuck. moving back to " + OldPosition);
            activator.Teleport(OldPosition);
        }
    }

    private void ConcussionGrenade(CDecoyProjectile decoy)
    {
        if (ConcussionItem == null || !decoy.IsValid) return;
        CBaseEntity thrower = decoy.OwnerEntity.Value!;
        if (thrower == null || ConcussionItem.OwnerEntity.Value != thrower) return;
        int playerTeam = thrower.TeamNum;
        Vector decoyPosition = decoy.AbsOrigin!;

        //Play sound at grenade position
        ConcussionTarget.Teleport(new Vector(decoyPosition.X, decoyPosition.Y, decoyPosition.Z + 0.5f));
        SecretConcussionSound.AddEntityIOEvent(inputName: "PlaySound", delay: 0.05f, activator: thrower, caller: thrower);

        //Kill decoy
        debugprintDecoy("killing decoy: " + decoy);
        decoy.AddEntityIOEvent(inputName: "Kill", delay: 0.01f, activator: decoy, caller: decoy);

        //Push players
        Vector grenadePosition = new(decoyPosition.X, decoyPosition.Y, decoyPosition.Z);
        Vector grenadePositionUp = new(decoyPosition.X, decoyPosition.Y, decoyPosition.Z - 24.0f);
        foreach (CCSPlayerController player in Utilities.GetPlayers().Where(p => VectorExtensions.DistanceTo(p.PlayerPawn.Value!.AbsOrigin!, decoyPosition) <= 512.0f))
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;
            if (pawn == null) continue;

            if (player.TeamNum == playerTeam && pawn != thrower) continue;
            debugprintDecoy("found nearby player " + player);
            Vector posdif = pawn.AbsOrigin! - grenadePositionUp;
            Vector speedboost = posdif.Scale(600.0f);
            //debugprintDecoy(speedboost);
            if (speedboost.Z < 300.0f) speedboost.Z = 300.0f;
            //debugprintDecoy(speedboost);
            pawn.AbsVelocity.X += speedboost.X;
            pawn.AbsVelocity.Y += speedboost.Y;
            pawn.AbsVelocity.Z += speedboost.Z;
        }
    }

    public override void Remove()
    {
        foreach (Timer timer in Timers) timer?.Kill();

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent -= OnEntitySpawned;
        ArcadeScripts.Instance.OnEntityRemovedEvent -= OnEntityRemoved;
    }
}