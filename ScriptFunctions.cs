namespace ArcadeScripts;

public abstract class ScriptFunctionBase(Type[] parameters)
{
    public Type[] ParameterTypes { get; init; } = parameters;
    public abstract void Invoke(object?[] args);
}

public class ScriptFunction(Action action) : ScriptFunctionBase([])
{
    private Action _Action = action;

    public override void Invoke(object?[] args)
    {
        _Action();
    }
}

public class ScriptFunction<T>(Action<T> action) : ScriptFunctionBase([typeof(T)])
{
    private Action<T> _Action = action;
    public override void Invoke(object?[] args)
    {
        _Action((T)args[0]!);
    }
}

public class ScriptFunction<T1, T2>(Action<T1, T2> action) : ScriptFunctionBase([typeof(T1), typeof(T2)])
{
    private Action<T1, T2> _Action = action;

    public override void Invoke(object?[] args)
    {
        _Action((T1)args[0]!, (T2)args[1]!);
    }
}

public class ScriptFunction<T1, T2, T3>(Action<T1, T2, T3> action) : ScriptFunctionBase([typeof(T1), typeof(T2), typeof(T3)])
{
    private Action<T1, T2, T3> _Action = action;

    public override void Invoke(object?[] args)
    {
        _Action((T1)args[0]!, (T2)args[1]!, (T3)args[2]!);
    }
}

public class ScriptFunction<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action) : ScriptFunctionBase([typeof(T1), typeof(T2), typeof(T3), typeof(T4)])
{
    private Action<T1, T2, T3, T4> _Action = action;

    public override void Invoke(object?[] args)
    {
        _Action((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!);
    }
}

public class ScriptFunction<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action) : ScriptFunctionBase([typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)])
{
    private Action<T1, T2, T3, T4, T5> _Action = action;

    public override void Invoke(object?[] args)
    {
        _Action((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!);
    }
}