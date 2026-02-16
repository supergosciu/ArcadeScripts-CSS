using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

using ArcadeScripts.Scripts;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;

namespace ArcadeScripts;

public partial class ArcadeScripts
{
    public delegate void OnAllEntitiesSpawnedDelegate();
    public event OnAllEntitiesSpawnedDelegate? OnAllEntitiesSpawnedEvent;
    public delegate void OnEntitySpawnedDelegate(CEntityInstance entity);
    public event OnEntitySpawnedDelegate? OnEntitySpawnedEvent;
    public delegate void OnEntityRemovedDelegate(CEntityInstance entity);
    public event OnEntityRemovedDelegate? OnEntityRemovedEvent;

    private List<CEntityInstance> Entities = [];

    private bool IsRoundStart = false;
    private bool WarmuedUp = false;

    private void OnPrecacheResources(ResourceManifest manifest)
    {
        if (Server.MapName != "jb_arcade_b6") return;

        manifest.AddResource("panorama/images/icons/equipment/lightning.vsvg");
        manifest.AddResource("panorama/images/icons/equipment/nuclear.vsvg");
        manifest.AddResource("panorama/images/icons/equipment/poison.vsvg");
        manifest.AddResource("panorama/images/icons/equipment/wreckingball.vsvg");
    }
    
    private HookResult OnAcceptInput(DynamicHook hook)
    {
        if (Server.MapName != "jb_arcade_b6") return HookResult.Continue;

        string input = hook.GetParam<CUtlSymbolLarge>(1).String;
        if (!string.Equals(input.ToLower(), "runscriptcode")) return HookResult.Continue;

        string? value = hook.GetParam<CVariant>(4).FieldType == fieldtype_t.FIELD_CSTRING ? NativeAPI.GetStringFromSymbolLarge(hook.GetParam<CVariant>(4).Handle) : null;
        if (value == null) return HookResult.Continue;

        if (!WarmuedUp && Scripts.Count != 0)
        {
            foreach (ScriptBase script in Scripts.Values)
            {
                script.Warmup();
            }
            WarmuedUp = true;
            return HookResult.Continue;
        }

        CEntityInstance target = hook.GetParam<CEntityIdentity>(0).EntityInstance;
        if (target.DesignerName != "logic_script") return HookResult.Continue;

        CEntityInstance activator = hook.GetParam<CEntityInstance>(2);
        CEntityInstance caller = hook.GetParam<CEntityInstance>(3);

        InputData inputData = new(target.As<CLogicScript>(), value, activator, caller);
        RunScriptCode(inputData);

        return HookResult.Continue;
    }

    private HookResult OnTakeDamage(DynamicHook hook)
    {
        if (Server.MapName != "jb_arcade_b6") return HookResult.Continue;

        CTakeDamageInfo info = hook.GetParam<CTakeDamageInfo>(1);
        CBaseEntity? attacker = info.Attacker.Value ?? info.Ability.Value;
        if (attacker != null && (attacker.DesignerName == "weapon_nuclear" || attacker.DesignerName == "weapon_poison" || attacker.DesignerName == "weapon_lightning" || attacker.DesignerName == "weapon_wreckingball"))
        {
            CBaseEntity? owner = attacker.OwnerEntity.Value;
            if (owner != null && owner.IsValid)
            {
                info.Inflictor.Raw = attacker.EntityHandle.Raw;
                info.Ability.Raw = attacker.EntityHandle.Raw;
                info.Attacker.Raw = attacker.OwnerEntity;
            }
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundPrestart(EventRoundPrestart @event, GameEventInfo info)
    {
        if (Server.MapName != "jb_arcade_b6") return HookResult.Continue;

        foreach (KeyValuePair<CLogicScript, ScriptBase> script in Scripts.ToDictionary())
        {
            script.Value.Remove();
            Scripts[script.Key] = null!;
        }
        Scripts.Clear();

        IsRoundStart = true;

        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (Server.MapName != "jb_arcade_b6") return HookResult.Continue;

        if (Entities.Count == 0) return HookResult.Continue;

        foreach (CEntityInstance entity in Entities)
        {
            if (entity.DesignerName == "logic_script")
            {
                CLogicScript script = entity.As<CLogicScript>();
                if (entity.PrivateVScripts == "playsoundat.nut")
                {
                    Scripts.Add(script, new PlaySoundAt(script));
                }
                else if (entity.PrivateVScripts == "colors.nut")
                {
                    Scripts.Add(script, new Colors(script));
                }
                else if (entity.PrivateVScripts == "blockjump.nut")
                {
                    Scripts.Add(script, new BlockJump(script));
                }
                else if (entity.PrivateVScripts == "darude.nut")
                {
                    Scripts.Add(script, new Darude(script));
                }
                else if (entity.PrivateVScripts == "item_in_area.nut")
                {
                    Scripts.Add(script, new ItemInArea(script));
                }
                else if (entity.PrivateVScripts == "car_secret.nut")
                {
                    Scripts.Add(script, new CarSecret(script));
                }
                else if (entity.PrivateVScripts == "decathlon.nut")
                {
                    Scripts.Add(script, new Decathlon(script));
                }
                else if (entity.PrivateVScripts == "surf.nut")
                {
                    Scripts.Add(script, new Surf(script));
                }
                else if (entity.PrivateVScripts == "bee.nut")
                {
                    Scripts.Add(script, new Bee(script));
                }
                else if (entity.PrivateVScripts == "tapper.nut")
                {
                    Scripts.Add(script, new Tapper(script));
                }
                else if (entity.PrivateVScripts == "strafe_jump.nut")
                {
                    Scripts.Add(script, new StrafeJump(script));
                }
                else if (entity.PrivateVScripts == "lightduel.nut")
                {
                    Scripts.Add(script, new LightDuel(script));
                }
                else if (entity.PrivateVScripts == "blob.nut")
                {
                    Scripts.Add(script, new Blob(script));
                }
                else if (entity.PrivateVScripts == "fidgetspinner.nut")
                {
                    Scripts.Add(script, new FidgetSpinner(script));
                }
                else if (entity.PrivateVScripts == "rocketjump.nut")
                {
                    Scripts.Add(script, new RocketJump(script));
                }
                else if (entity.PrivateVScripts == "follower.nut")
                {
                    Scripts.Add(script, new Follower(script));
                }
                else if (entity.PrivateVScripts == "cell_instructions.nut")
                {
                    Scripts.Add(script, new CellInstructions(script));
                }
                else if (entity.PrivateVScripts == "cellrewards")
                {
                    Scripts.Add(script, new CellRewards(script));
                }
                else if (entity.PrivateVScripts == "decoys.nut")
                {
                    Scripts.Add(script, new Decoys(script));
                }
                else if (entity.PrivateVScripts == "has_weapon_noname.nut")
                {
                    Scripts.Add(script, new HasWeapon(script));
                }
                else if (entity.PrivateVScripts == "hurt.nut")
                {
                    Scripts.Add(script, new Hurt(script));
                }
                else if (entity.PrivateVScripts == "lightning.nut")
                {
                    Scripts.Add(script, new Lightning(script));
                }
                else if (entity.PrivateVScripts == "lunge.nut")
                {
                    Scripts.Add(script, new Lunge(script));
                }
                else if (entity.PrivateVScripts == "poison.nut")
                {
                    Scripts.Add(script, new Poison(script));
                }
                else if (entity.PrivateVScripts == "rogue.nut")
                {
                    Scripts.Add(script, new Rogue(script));
                }
                else if (entity.PrivateVScripts == "timer.nut")
                {
                    Scripts.Add(script, new SurfTimer(script));
                }
                if (entity.PrivateVScripts == "grab.nut")
                {
                    Scripts.Add(script, new Grab(script));
                }
                else if (entity.PrivateVScripts == "compass.nut")
                {
                    Scripts.Add(script, new Compass(script));
                }
                else if (entity.PrivateVScripts == "flight.nut")
                {
                    Scripts.Add(script, new Flight(script));
                }
            }
        }

        foreach (CEntityInstance entity in Entities)
        {
            string? targetname = entity.Entity?.Name;
            if (string.IsNullOrEmpty(targetname)) continue;

            foreach (ScriptBase script in Scripts.Values)
            {
                foreach (string name in script.EntityNames)
                {
                    if (targetname.Contains(name))
                    {
                        if (!script.EntityList.TryGetValue(targetname, out List<CEntityInstance>? entityList))
                        {
                            entityList = [];
                            script.EntityList[targetname] = entityList;
                        }

                        entityList.Add(entity);
                    }
                }
            }
        }

        OnAllEntitiesSpawnedEvent?.Invoke();
        IsRoundStart = false;

        return HookResult.Continue;
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (Server.MapName != "jb_arcade_b6") return;

        if (!entity.IsValid) return;
        if (!IsRoundStart) OnEntitySpawnedEvent?.Invoke(entity);
        if (string.IsNullOrEmpty(entity.Entity?.Name)) return;

        Entities.Add(entity);
    }

    private void OnEntityRemoved(CEntityInstance entity)
    {
        if (Server.MapName != "jb_arcade_b6") return;

        if (!IsRoundStart) OnEntityRemovedEvent?.Invoke(entity);
        Entities.Remove(entity);
    }
}