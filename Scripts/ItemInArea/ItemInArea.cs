using CounterStrikeSharp.API.Core;

namespace ArcadeScripts.Scripts;

public class ItemInArea : ScriptBase
{
    public ItemInArea(CLogicScript owner) : base(owner)
    {
        Functions.Add("CheckForItem", new ScriptFunction<CEntityInstance, CEntityInstance>(CheckForItem));

        EntityNames = ["secret_concussion_trigger", "secret_concussion_template"];
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
    }

    private CTriggerMultiple SecretConcussionTrigger = null!;
    private CPointTemplate SecretConcussionTemplate = null!;

    private void OnAllEntitiesSpawned()
    {
        SecretConcussionTrigger = EntityList["secret_concussion_trigger"][0].As<CTriggerMultiple>();
        SecretConcussionTemplate = EntityList["secret_concussion_template"][0].As<CPointTemplate>();
    }

    public void CheckForItem(CEntityInstance? caller, CEntityInstance? activator)
    {
        if (activator == null || caller != SecretConcussionTrigger) return;

        if (activator.DesignerName == "weapon_deagle")
        {
            SecretConcussionTrigger.AddEntityIOEvent(inputName: "Disable");
            SecretConcussionTemplate.AddEntityIOEvent(inputName: "ForceSpawn");
        }
    }

    public override void Remove()
    {
        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
    }
}