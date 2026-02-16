using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace ArcadeScripts.Scripts;

public class CarSecret : ScriptBase
{
    public CarSecret(CLogicScript owner) : base(owner)
    {
        Functions.Add("SetItemLocation", new ScriptFunction(SetItemLocation));
        Functions.Add("Update", new ScriptFunction(Update));
        Functions.Add("PickUp", new ScriptFunction<CEntityInstance>(PickUp));

        EntityNames = ["secret_car_item", "secret_car_model"];
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
    }

    private Timer? Timer = null;
    private Vector ItemLocation = new(-63.231995f, -275.157990f, -698.593018f);
    private const float SpeedBonus = 1.4f;

    private CCSPlayerPawn? SecretPlayer = null;
    private CWeaponElite? Item = null;
    private CDynamicProp CarModel = null!;

    private void OnAllEntitiesSpawned()
    {
        Item = EntityList["secret_car_item"][0].As<CWeaponElite>();
        CarModel = EntityList["secret_car_model"][0].As<CDynamicProp>();
    }

    public void SetItemLocation()
    {
        if (Item != null)
        {
            Item.Teleport(ItemLocation);
            CarModel.AddEntityIOEvent(inputName: "Alpha", value: "255", delay: 0.0f);
        }
    }

    private void SetSpeed(CCSPlayerPawn? pawn, float speed)
    {
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;

        pawn.VelocityModifier = speed;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
    }

    public void PickUp(CEntityInstance? activator)
    {
        if (activator != null)
        {
            if (activator != SecretPlayer)
            {
                //New owner, disable speed on old owner
                SetSpeed(SecretPlayer, 1.0f);
            }
            SecretPlayer = activator.As<CCSPlayerPawn>();
            SetSpeed(SecretPlayer, SpeedBonus);
            Timer = new(0.2f, Update, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
        }
    }

    private void Update()
    {
        if (SecretPlayer == null)
        {
            Timer?.Kill();
            Timer = null;
            return;
        }
        
        //Item deleted or has no owner
        if (Item == null || !Item.IsValid || Item.OwnerEntity.Value == null || !Item.OwnerEntity.Value.IsValid)
        {
            Timer?.Kill();
            Timer = null;
            SetSpeed(SecretPlayer, 1.0f);
            SecretPlayer = null;
        }
    }

    public override void Remove()
    {
        Timer?.Kill();
        Timer = null;
        
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
    }
}