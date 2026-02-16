using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static ArcadeScripts.TextDisplayHelper;

namespace ArcadeScripts.Scripts;

public class Grab : ScriptBase
{
    public Grab(CLogicScript owner) : base(owner)
    {
        Functions.Add("PickUp", new ScriptFunction<CEntityInstance>(PickUp));
        Functions.Add("DoGrab", new ScriptFunction<CEntityInstance>(DoGrab));
        Functions.Add("PressButton", new ScriptFunction<CEntityInstance, int, bool>(PressButton));
        Functions.Add("Think", new ScriptFunction(Think));

        EntityNames = ["secret_grab_origin", "secret_grab_ui", "secret_grab_timer"];

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent += OnEntitySpawned;
    }

    private CC4 Item = null!;
    private CDynamicProp OriginEntity = null!;
    private CCSPlayerPawn GrabbedPlayer = null!;
    private CCSPlayerPawn SecretPlayer = null!;
    private CLogicCase GrabUI = null!;
    private CTimerEntity Timer = null!;

    private const float GrabDistance = 96.0f;
    private const float GrabVelocity = 10.0f;

    public const int BUTTON_LEFT = 0;
    public const int BUTTON_RIGHT = 1;
    private bool[] ButtonsPressed = [false, false];

    private void OnAllEntitiesSpawned()
    {
        OriginEntity = EntityList["secret_grab_origin"][0].As<CDynamicProp>();
        GrabUI = EntityList["secret_grab_ui"][0].As<CLogicCase>();
        Timer = EntityList["secret_grab_timer"][0].As<CTimerEntity>();
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        string? targetname = entity.Entity?.Name;

        if (targetname == "secret_grab_item")
        {
            Item = entity.As<CC4>();
        }
    }

    public void PickUp(CEntityInstance? activator)
    {
        if (activator != null)
        {
            if (activator != SecretPlayer)
            {
                Disable(SecretPlayer);
            }
            SecretPlayer = activator.As<CCSPlayerPawn>();
            SetMessage(activator.As<CCSPlayerPawn>().OriginalController.Value!.Slot, "Press left+right at the same time to grab a player", 15.0f);
            ButtonsPressed[0] = false;
            ButtonsPressed[1] = false;
        }
    }

    public void Think()
    {
        if (SecretPlayer == null || Item == null) return;

        if (!Item.IsValid)
        {
            Item = null!;
            Disable(SecretPlayer);
            return;
        }

        if (Item.OwnerEntity.Value != SecretPlayer || SecretPlayer.Health < 1)
        {
            Disable(SecretPlayer);
            return;
        }

        if (GrabbedPlayer == null) return;

        else if (!GrabbedPlayer.IsValid)
        {
            GrabbedPlayer = null!;
            return;
        }
        else if (GrabbedPlayer.LifeState != (byte)LifeState_t.LIFE_ALIVE)
        {
            GrabbedPlayer.GravityScale = 1.0f;
            GrabbedPlayer.ActualGravityScale = 1.0f;
            Utilities.SetStateChanged(GrabbedPlayer, "CBaseEntity", "m_flGravityScale");
            GrabbedPlayer = null!;
            return;
        }

        Vector forwardVector = OriginEntity.AbsRotation!.GetForwardVector();
        Vector absOrigin = SecretPlayer.AbsOrigin!;
        CNetworkViewOffsetVector viewOffset = SecretPlayer.ViewOffset;
        Vector grabLocation = new Vector(absOrigin.X + viewOffset.X, absOrigin.Y + viewOffset.Y, absOrigin.Z + viewOffset.Z) + forwardVector.Scale(64);
        grabLocation.Z -= 32.0f;

        Vector playerOrigin = GrabbedPlayer.AbsOrigin!;
        Vector vel = grabLocation - playerOrigin;
        // Release if grabbedPlayer is far away now
        if (vel.Length() > 512.0f)
        {
            FreeGrabbedPlayer();
            return;
        }
        vel.X *= 10.0f;
        vel.Y *= 10.0f;
        vel.Z *= 10.0f;
        GrabbedPlayer.AbsVelocity.X = vel.X;
        GrabbedPlayer.AbsVelocity.Y = vel.Y;
        GrabbedPlayer.AbsVelocity.Z = vel.Z;
    }

    private void ResetName(CCSPlayerPawn pawn)
    {
        if (pawn != null && pawn.IsValid && pawn.Entity?.Name == "secret_grabber")
        {
            pawn.AddEntityIOEvent(inputName: "KeyValue", value: "targetname default");
            // WHAT THE FUCKING FUCK VALVE
            // pawn.Entity.Name = "";
        }
    }

    private void Disable(CCSPlayerPawn pawn)
    {
        FreeGrabbedPlayer();
        ResetName(pawn);
        SecretPlayer = null!;
    }

    private void GrabPlayer(CCSPlayerPawn pawn)
    {
        FreeGrabbedPlayer();
        GrabbedPlayer = pawn;
        GrabbedPlayer.GravityScale = 0.01f;
        GrabbedPlayer.ActualGravityScale = 0.01f;
        Utilities.SetStateChanged(GrabbedPlayer, "CBaseEntity", "m_flGravityScale");
    }

    public void DoGrab(CEntityInstance? activator)
    {
        Deactivate(activator);

        if (SecretPlayer == null || SecretPlayer != activator || Item == null || Item.OwnerEntity.Value != SecretPlayer) return;

        if (GrabbedPlayer != null)
        {
            FreeGrabbedPlayer();
            return;
        }

        Vector forwardVector = OriginEntity.AbsRotation!.GetForwardVector();
        Vector absOrigin = SecretPlayer.AbsOrigin!;
        CNetworkViewOffsetVector viewOffset = SecretPlayer.ViewOffset;
        Vector grabLocation = new Vector(absOrigin.X + viewOffset.X, absOrigin.Y + viewOffset.Y, absOrigin.Z + viewOffset.Z) + forwardVector.Scale(64);
        CCSPlayerPawn closestLivingPlayer = null!;
        float closestDistance = 9999;
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;
            if (pawn == null) continue;

            if (pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE && pawn != SecretPlayer)
            {
                float distance = VectorExtensions.DistanceTo(pawn.AbsOrigin!, grabLocation);
                if (distance < closestDistance && distance < GrabDistance)
                {
                    closestDistance = distance;
                    closestLivingPlayer = pawn;
                }
            }
        }
        if (closestLivingPlayer != null)
        {
            GrabPlayer(closestLivingPlayer);
        }
    }

    private void FreeGrabbedPlayer()
    {
        if (GrabbedPlayer != null && GrabbedPlayer.IsValid)
        {
            GrabbedPlayer.GravityScale = 1.0f;
            GrabbedPlayer.ActualGravityScale = 1.0f;
            Utilities.SetStateChanged(GrabbedPlayer, "CBaseEntity", "m_flGravityScale");
            GrabbedPlayer = null!;
        }
    }

    public void PressButton(CEntityInstance? activator, int button, bool value)
    {
        Deactivate(activator);
        ButtonsPressed[button] = value;
        if (ButtonsPressed[0] && ButtonsPressed[1])
        {
            DoGrab(activator);
        }
    }

    private void Deactivate(CEntityInstance? activator)
    {
        if ((Item == null || !Item.IsValid || Item.OwnerEntity.Value == null) && activator != null)
        {
            GrabUI.AddEntityIOEvent(inputName: "Deactivate", activator: activator, caller: activator);
        }
    }

    public override void Remove()
    {
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent -= OnEntitySpawned;
        Item = null!;
        OriginEntity = null!;
        GrabbedPlayer = null!;
        SecretPlayer = null!;
        GrabUI = null!;
    }
}