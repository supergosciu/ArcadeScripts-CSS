using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Core.Capabilities;
using RayTraceAPI;

namespace ArcadeScripts;

public class InputData(CLogicScript target, string value, CEntityInstance? activator = null, CEntityInstance? caller = null)
{
    public CLogicScript Target { get; init; } = target;
    public string Value { get; init; } = value;
    public CEntityInstance? Activator { get; init; } = activator;
    public CEntityInstance? Caller { get; init; } = caller;
}

public partial class ArcadeScripts : BasePlugin
{
    public override string ModuleName => "jb_arcade_b6 Scripts";
    public override string ModuleAuthor => "Supergosciuツ";
    public override string ModuleVersion => "1.0.2";

    internal static PluginCapability<CRayTraceInterface> RayTraceInterface { get; } = new("raytrace:craytraceinterface");

    public static ArcadeScripts Instance = null!;
    private Dictionary<CLogicScript, ScriptBase> Scripts = [];

    private static readonly MemoryFunctionVoid<CEntityIdentity, CUtlSymbolLarge, CEntityInstance, CEntityInstance, CVariant, int> CEntityIdentity_AcceptInputFunc = new(GameData.GetSignature("CEntityIdentity_AcceptInput"));

    public override void Load(bool hotReload)
    {
        if (hotReload) throw new InvalidOperationException("Hot reload is not supported.");

        Instance = this;

        RegisterEventHandler<EventRoundPrestart>(OnRoundPrestart, HookMode.Pre);
        RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Post);
        RegisterListener<Listeners.OnServerPrecacheResources>(OnPrecacheResources);
        RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
        RegisterListener<Listeners.OnEntityDeleted>(OnEntityRemoved);
        RegisterListener<Listeners.OnTick>(TextDisplayHelper.OnTick);
        CEntityIdentity_AcceptInputFunc.Hook(OnAcceptInput, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
    }

    public override void Unload(bool hotReload)
    {
        DeregisterEventHandler<EventRoundPrestart>(OnRoundPrestart, HookMode.Pre);
        DeregisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Post);
        RemoveListener<Listeners.OnServerPrecacheResources>(OnPrecacheResources);
        RemoveListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
        RemoveListener<Listeners.OnEntityDeleted>(OnEntityRemoved);
        RemoveListener<Listeners.OnTick>(TextDisplayHelper.OnTick);
        CEntityIdentity_AcceptInputFunc.Unhook(OnAcceptInput, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
    }

    public void RunScriptCode(InputData data)
    {
        int open = data.Value.IndexOf('(');
        int close = data.Value.LastIndexOf(')');

        if (open <= 0 || close < open) throw new ArgumentException("Invalid code format.");

        string name = data.Value[..open];
        string argsRaw = data.Value.Substring(open + 1, close - open - 1);
        string[] argStrings = SplitArguments(argsRaw);
        CLogicScript scriptEntity = data.Target;

        if (Scripts.TryGetValue(scriptEntity, out ScriptBase? script))
        {
            ScriptFunctionBase function = script.Functions[name];
            Type[] parameterTypes = function.ParameterTypes;

            if (argStrings.Length != parameterTypes.Length) throw new InvalidOperationException($"Function {name} expects {parameterTypes.Length} parameters, got {argStrings.Length} instead.");

            object?[] parameters = new object?[argStrings.Length];
            if (argStrings.Length > 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    object? result = script.GetType()?.GetField(argStrings[i])?.GetValue(script);
                    if (result != null)
                    {
                        parameters[i] = result;
                        continue;
                    }

                    if (argStrings[i] == "!activator")
                    {
                        parameters[i] = data.Activator;
                        continue;
                    }

                    if (argStrings[i] == "!caller")
                    {
                        parameters[i] = data.Caller;
                        continue;
                    }

                    Type type = parameterTypes[i];
                    parameters[i] = Convert.ChangeType(argStrings[i], type);
                }
            }
            else
            {
                parameters = null!;
            }

            function.Invoke(parameters);
        }
    }

    private static string[] SplitArguments(string args)
    {
        if (string.IsNullOrWhiteSpace(args)) return [];

        return args.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToArray();
    }
}
