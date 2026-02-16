using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace ArcadeScripts.Scripts;

public class RocketJump : ScriptBase
{
    public RocketJump(CLogicScript owner) : base(owner)
    {
        Functions.Add("SetRocketJumper", new ScriptFunction<CEntityInstance, CEntityInstance>(SetRocketJumper));
        Functions.Add("DoRocketJump", new ScriptFunction<CEntityInstance>(DoRocketJump));

        EntityNames = ["rocketjump_sound", "rocketjump_origin"];

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
    }

    private float LastRocket = 0;
    private const float Boost = 600.0f;

    private CWeaponSawedoff Item = null!;
    private CCSPlayerPawn SpecialPlayer = null!;
    private CDynamicProp ShotDirection = null!;
    private CAmbientGeneric Sound = null!;

    private void OnAllEntitiesSpawned()
    {
        Sound = EntityList["rocketjump_sound"][0].As<CAmbientGeneric>();
        ShotDirection = EntityList["rocketjump_origin"][0].As<CDynamicProp>();
    }

    public void SetRocketJumper(CEntityInstance? activator, CEntityInstance? caller)
    {
        if (activator == null || caller == null) return;

        Item = caller.As<CWeaponSawedoff>();
        SpecialPlayer = activator.As<CCSPlayerPawn>();
    }

    public void DoRocketJump(CEntityInstance? activator)
    {
        //If the shotgun was thrown or stripped, don't do anything
        if (activator == null || SpecialPlayer != activator || Item == null || !Item.IsValid || Item.OwnerEntity.Value != activator) return;

        float time = Server.CurrentTime;
        if (time < LastRocket + 0.7f) return;

        Sound.Teleport(Item.AbsOrigin);
        Sound.AddEntityIOEvent(inputName: "PlaySound", delay: 0.0f, activator: Sound, caller: Sound);
        LastRocket = time;
        Vector directionFacing = ShotDirection.AbsRotation!.GetForwardVector();
        Vector directionOfMovement = new(-directionFacing.X, -directionFacing.Y, -directionFacing.Z);
        Vector speedboost = directionOfMovement.Scale(Boost);
        activator.As<CCSPlayerPawn>().AbsVelocity.X += speedboost.X;
        activator.As<CCSPlayerPawn>().AbsVelocity.Y += speedboost.Y;
        activator.As<CCSPlayerPawn>().AbsVelocity.Z += speedboost.Z;
    }

    public override void Remove()
    {
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
    }
}