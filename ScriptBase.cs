using CounterStrikeSharp.API.Core;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace ArcadeScripts;

public abstract class ScriptBase(CLogicScript owner)
{
    public CLogicScript Owner { get; init; } = owner;
    public CBaseEntity[] EntityGroup { get; set; } = new CBaseEntity[16];
    public List<string> EntityNames { get; set; } = [];
    public Dictionary<string, List<CEntityInstance>> EntityList { get; set; } = [];
    public string[] EntityGroupNames { get; set; } = new string[16];
    public Dictionary<string, ScriptFunctionBase> Functions = [];
    public List<Timer> Timers = [];

    public virtual void Remove() {}
    public virtual void Warmup() {}
}