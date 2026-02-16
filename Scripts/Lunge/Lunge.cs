using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static ArcadeScripts.TextDisplayHelper;

namespace ArcadeScripts.Scripts;

public class Lunge : ScriptBase
{
    public Lunge(CLogicScript owner) : base(owner)
    {
        Functions.Add("UpdateHUD", new ScriptFunction(UpdateHUD));
        Functions.Add("PickUp", new ScriptFunction<CEntityInstance>(PickUp));
        Functions.Add("DoLunge", new ScriptFunction(DoLunge));

        EntityNames = ["secret_lunge_timer", "secret_lunge_origin"];

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent += OnEntitySpawned;
    }

    private static Vector StrafeMin = new(1024.0f, -1312.0f, -16072.0f);
    private static Vector StrafeMax = new(1536.0f, -800.0f, -511.0f);
    private static Vector StrafeHardMin = new(1088.0f, -6720.0f, -16072.0f);
    private static Vector StrafeHardMax = new(1600.0f, -6208.0f, 16000.0f);

    private float NextTimeAllowed = 0.0f;
    private const float LungeStrength = 900.0f;
    private const float Cooldown = 3.0f;

    private CC4 Item = null!;
    private CCSPlayerPawn SecretPlayer = null!;
    private CDynamicProp OriginEntity = null!;
    private CTimerEntity SecretLungeTimer = null!;

    private bool DEBUG = false;
    private void debugprint(string text)
    {
        if (!DEBUG) return;
        Console.WriteLine($"************* {text}");
    }

    private void OnAllEntitiesSpawned()
    {
        OriginEntity = EntityList["secret_lunge_origin"][0].As<CDynamicProp>();
        SecretLungeTimer = EntityList["secret_lunge_timer"][0].As<CTimerEntity>();
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        string? targetname = entity.Entity?.Name;

        if (targetname == "secret_lunge_item")
        {
            Item = entity.As<CC4>();
        }
    }

    private void ResetName(CCSPlayerPawn? pawn)
    {
        if (pawn != null && pawn.IsValid && pawn.Entity?.Name == "secret_lunger")
        {
            pawn.AddEntityIOEvent(inputName: "KeyValue", value: "targetname default");
            // WHAT THE FUCKING FUCK VALVE
            // pawn.Entity.Name = "";
        }
    }

    private void Disable(CCSPlayerPawn? pawn, bool disableTimer)
    {
        debugprint("Disabling lunge item for " + pawn);
        ResetName(pawn);
        SecretPlayer = null!;
        if (disableTimer)
        {
            SecretLungeTimer.AddEntityIOEvent(inputName: "Disable");
        }
    }

    public void PickUp(CEntityInstance? activator)
    {
        if (activator != null)
        {
            CCSPlayerPawn pawn = activator.As<CCSPlayerPawn>();
            if (activator != SecretPlayer)
            {
                Disable(SecretPlayer, false);
            }
            SecretPlayer = pawn;
            NextTimeAllowed = Server.CurrentTime + 0.5f;
        }
    }

    public void DoLunge()
    {
        if (Server.CurrentTime < NextTimeAllowed || SecretPlayer == null || Item == null) return;

        if (Item.OwnerEntity.Value != SecretPlayer)
        {
            Disable(SecretPlayer, true);
            return;
        }

        if (IsWithinDisabledArea(SecretPlayer))
        {
            debugprint("Disallowing lunge flight within restricted area");
            return;
        }

        NextTimeAllowed = Server.CurrentTime + Cooldown;
        Vector forwardVector = OriginEntity.AbsRotation!.GetForwardVector();

        debugprint("forwardVector: " + forwardVector);

        Vector newVelocity = forwardVector.Scale(LungeStrength);
        newVelocity.Z = newVelocity.Z + 10.0f;
        SecretPlayer.AbsVelocity.X += newVelocity.X;
        SecretPlayer.AbsVelocity.Y += newVelocity.Y;
        SecretPlayer.AbsVelocity.Z += newVelocity.Z;
    }

    public void UpdateHUD()
    {
        if (Item == null || SecretPlayer == null || SecretPlayer.LifeState != (byte)LifeState_t.LIFE_ALIVE || Item.OwnerEntity.Value != SecretPlayer)
        {
            debugprint("Disabling secret_lunge_timer because item no longer exists or is no longer held");
            Disable(SecretPlayer, true);
            return;
        }

        string text = "Lunge ready!";
        if (Server.CurrentTime < NextTimeAllowed)
        {
            text = $"{(int)(NextTimeAllowed - Server.CurrentTime + 1.01f)}s until lunge ready.";
        }
        CCSPlayerController? player = SecretPlayer.OriginalController.Value;
        if (player != null)
        {
            SetMessage(player.Slot, text, 2.0f);
        }
    }

    private bool IsWithinDisabledArea(CCSPlayerPawn pawn)
    {
        Vector playerOrigin = pawn.AbsOrigin!;
        if (playerOrigin.X >= StrafeMin.X && playerOrigin.X <= StrafeMax.X && playerOrigin.Y >= StrafeMin.Y && playerOrigin.Y <= StrafeMax.Y && playerOrigin.Z >= StrafeMin.Z && playerOrigin.Z < StrafeMax.Z) return true;
        else if (playerOrigin.X >= StrafeHardMin.X && playerOrigin.X <= StrafeHardMax.X && playerOrigin.Y >= StrafeHardMin.Y && playerOrigin.Y <= StrafeHardMax.Y && playerOrigin.Z >= StrafeHardMin.Z && playerOrigin.Z < StrafeHardMax.Z) return true;
        else if (pawn.PrivateVScripts == "inGame") return true;
        else return false;
    }

    public override void Remove()
    {
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent -= OnEntitySpawned;
    }
}