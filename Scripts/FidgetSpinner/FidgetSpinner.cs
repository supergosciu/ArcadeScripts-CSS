using CounterStrikeSharp.API.Core;

namespace ArcadeScripts.Scripts;

public class FidgetSpinner : ScriptBase
{
    public FidgetSpinner(CLogicScript owner) : base(owner)
    {
        Functions.Add("Setup", new ScriptFunction(Setup));
        Functions.Add("UpdateSpeed", new ScriptFunction(UpdateSpeed));

        EntityNames = ["fidgetspinner_timer"];

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent += OnEntitySpawned;
    }

    private float SpinnerSpeed = 0.0f;
    private const int MinSpeedToSpin = 37500;
    private const float SpeedIncreasePerTic = 0.1f;
    private const float SpeedDecreasePerTick = -0.05f;
    private const float MinSpeed = 0.0f;
    private const float MaxSpeed = 1.0f;

    private CWeaponUSPSilencer Item = null!;
    private CFuncRotating Rotating = null!;
    private CTimerEntity Timer = null!;

    private void OnAllEntitiesSpawned()
    {
        Timer = EntityList["fidgetspinner_timer"][0].As<CTimerEntity>();
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        string? targetname = entity.Entity?.Name;

        if (targetname == "fidgetspinner_item")
        {
            Item = entity.As<CWeaponUSPSilencer>();
        }
        else if (targetname == "fidgetspinner_model")
        {
            Rotating = entity.As<CFuncRotating>();
        }
    }

    public void Setup()
    {
        Timer.AddEntityIOEvent(inputName: "Enable", delay: 1.0f);
    }

    private void AddToSpinnerSpeed(float speed)
    {
        if (speed == SpeedDecreasePerTick && SpinnerSpeed == MinSpeed) return;

        SpinnerSpeed += speed;

        if (SpinnerSpeed < MinSpeed)
        {
            SpinnerSpeed = MinSpeed;
        }

        if (SpinnerSpeed > MaxSpeed)
        {
            SpinnerSpeed = MaxSpeed;
        }

        Rotating.AddEntityIOEvent(inputName: "SetSpeed", value: SpinnerSpeed.ToString().Replace(",", "."), delay: 0.0f, activator: Rotating, caller: Rotating);
    }

    public void UpdateSpeed()
    {
        if (Item == null || !Item.IsValid)
        {
            Timer.AddEntityIOEvent(inputName: "Disable");
            return;
        }

        CBaseEntity? owner = Item.OwnerEntity.Value;

        if (owner == null || !owner.IsValid)
        {
            AddToSpinnerSpeed(SpeedDecreasePerTick);
            return;
        }

        float playerSpeed = owner.AbsVelocity.VecLength2DSqr();
        if (playerSpeed < MinSpeedToSpin)
        {
            AddToSpinnerSpeed(SpeedDecreasePerTick);
            return;
        }

        AddToSpinnerSpeed(SpeedIncreasePerTic);
    }

    public override void Remove()
    {
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent -= OnEntitySpawned;
    }
}