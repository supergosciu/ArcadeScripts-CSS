using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static ArcadeScripts.TextDisplayHelper;

namespace ArcadeScripts.Scripts;

public class Flight : ScriptBase
{
    public Flight(CLogicScript owner) : base(owner)
    {
        Functions.Add("StartFlight", new ScriptFunction(StartFlight));
        Functions.Add("PickUp", new ScriptFunction<CEntityInstance>(PickUp));
        Functions.Add("RegisterItem", new ScriptFunction(RegisterItem));
        Functions.Add("ToggleEnabled", new ScriptFunction<CEntityInstance>(ToggleEnabled));

        EntityNames = ["secret_flight_origin"];

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent += OnEntitySpawned;
    }

    private bool Enabled = false;
    private float NextToggleAllowed = 0.0f;
    private const float ToggleTime = 6.0f;
    private const float SecretSpeed = 0.5f;

    private static Vector StrafeMin = new(1024.0f, -1312.0f, -16072.0f);
    private static Vector StrafeMax = new(1536.0f, -800.0f, -511.0f);
    private static Vector StrafeHardMin = new(1088.0f, -6720.0f, -16072.0f);
    private static Vector StrafeHardMax = new(1600.0f, -6208.0f, 16000.0f);
    private static Vector DodgeMin = new(2176.0f, -1824.0f, -1024.0f);
    private static Vector DodgeMax = new(2688.0f, 3744.0f, -800.0f);

    private CC4 Item = null!;
    private CCSPlayerPawn SecretPlayer = null!;
    private CDynamicProp OriginEntity = null!;

    private bool DEBUG = false;
    private void debugprint(string text)
    {
        if (!DEBUG) return;
        Console.WriteLine($"************* {text}");
    }

    private void OnAllEntitiesSpawned()
    {
        OriginEntity = EntityList["secret_flight_origin"][0].As<CDynamicProp>();
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        string? targetname = entity.Entity?.Name;

        if (targetname == "secret_flight_c4")
        {
            Item = entity.As<CC4>();
        }
    }

    private void RegisterItem()
    {
        NextToggleAllowed = Server.CurrentTime;
    }

    private void ToggleEnabled(CEntityInstance activator)
    {
        if (!(SecretPlayer == activator) || Server.CurrentTime < NextToggleAllowed) return;

        NextToggleAllowed = Server.CurrentTime + ToggleTime;
        if (IsWithinDisabledAreaFlight(SecretPlayer)) return;

        Enabled = !Enabled;
        if (!Enabled) Disable();
        else Enable();
    }

    private void Enable()
    {
        ModifySpeed(SecretSpeed);
        SecretPlayer.GravityScale = 0.01f;
        SecretPlayer.ActualGravityScale = 0.01f;
        Utilities.SetStateChanged(SecretPlayer, "CBaseEntity", "m_flGravityScale");
        UpdateHUDText();
    }

    private void Disable()
    {
        ModifySpeed(1.0f);
        SecretPlayer.GravityScale = 1.0f;
        SecretPlayer.ActualGravityScale = 1.0f;
        Utilities.SetStateChanged(SecretPlayer, "CBaseEntity", "m_flGravityScale");
        UpdateHUDText();
    }

    private void PickUp(CEntityInstance? activator)
    {
        if (activator != null)
        {
            SecretPlayer = activator.As<CCSPlayerPawn>();
            Enabled = true;
            Enable();
            UpdateHUDText();
        }
    }

    private void UpdateHUDText()
    {
        if (SecretPlayer == null || Item == null || !Item.IsValid || Item.OwnerEntity.Value == null) return;
        string toggleAvailabilityText = "(Ready!)";
        if (Server.CurrentTime < NextToggleAllowed)
        {
            toggleAvailabilityText = $"({(int)(NextToggleAllowed - Server.CurrentTime + 1.01f)}s)";
        }

        if (!Enabled)
        {
            DisplayText(SecretPlayer.OriginalController.Value!, $"Right click to toggle flight on {toggleAvailabilityText}");
        }
        else
        {
            DisplayText(SecretPlayer.OriginalController.Value!, $"Right click to toggle flight off {toggleAvailabilityText}");
        }
    }

    private void StartFlight()
    {
        UpdateHUDText();
        if (SecretPlayer != null && Enabled)
        {
            if (Item != null)
            {
                if (Item.OwnerEntity.Value != SecretPlayer)
                {
                    debugprint("Disabling flight item");
                    ModifySpeed(1.0f);
                    Disable();
                    SecretPlayer = null!;
                    return;
                }
                if (IsWithinDisabledAreaFlight(SecretPlayer))
                {
                    debugprint("Disabling flight within restricted area");
                    Enabled = false;
                    Disable();
                    return;
                }
            }

            Vector forwardVector = OriginEntity.AbsRotation!.GetForwardVector();
            debugprint("forwardVector: " + forwardVector);
            Vector newVelocity = forwardVector.Scale(600.0f);
            newVelocity.Z = newVelocity.Z + 10.0f;
            SecretPlayer.AbsVelocity.X = newVelocity.X;
            SecretPlayer.AbsVelocity.Y = newVelocity.Y;
            SecretPlayer.AbsVelocity.Z = newVelocity.Z;
            ModifySpeed(SecretSpeed);
        }
    }

    private void ModifySpeed(float speed)
    {
        if (SecretPlayer != null)
        {
            SecretPlayer.VelocityModifier = speed;
            Utilities.SetStateChanged(SecretPlayer, "CCSPlayerPawn", "m_flVelocityModifier");
        }
    }

    private bool IsWithinDisabledAreaFlight(CCSPlayerPawn pawn)
    {
        Vector playerOrigin = pawn.AbsOrigin!;
        if (playerOrigin.X >= StrafeMin.X && playerOrigin.X <= StrafeMax.X &&
            playerOrigin.Y >= StrafeMin.Y && playerOrigin.Y <= StrafeMax.Y &&
            playerOrigin.Z >= StrafeMin.Z && playerOrigin.Z < StrafeMax.Z)
        {
            return true;
        }
        else if (playerOrigin.X >= StrafeHardMin.X && playerOrigin.X <= StrafeHardMax.X &&
            playerOrigin.Y >= StrafeHardMin.Y && playerOrigin.Y <= StrafeHardMax.Y &&
            playerOrigin.Z >= StrafeHardMin.Z && playerOrigin.Z < StrafeHardMax.Z)
        {
            return true;
        }
        else if (playerOrigin.X >= DodgeMin.X && playerOrigin.X <= DodgeMax.X &&
            playerOrigin.Y >= DodgeMin.Y && playerOrigin.Y <= DodgeMax.Y &&
            playerOrigin.Z >= DodgeMin.Z && playerOrigin.Z < DodgeMax.Z)
        {
            return true;
        }
        else if (pawn.PrivateVScripts == "inGame")
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void DisplayText(CCSPlayerController player, string text)
    {
        SetMessage(player.Slot, text, 4.0f);
    }

    public override void Remove()
    {
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent -= OnEntitySpawned;
    }
}