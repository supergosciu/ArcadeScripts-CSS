using CounterStrikeSharp.API.Core;

namespace ArcadeScripts.Scripts;

public partial class BlockJump : ScriptBase
{
    public BlockJump(CLogicScript owner) : base(owner)
    {
        Functions.Add("DisableJumping", new ScriptFunction<CEntityInstance>(DisableJumping));
        Functions.Add("EnableJumping", new ScriptFunction<CEntityInstance>(EnableJumping));

        ArcadeScripts.Instance.RegisterListener<Listeners.OnTick>(OnTick);
    }

    private List<CCSPlayerPawn?> JumpDisabledPlayers = [];

    private void OnTick()
    {
        foreach (CCSPlayerPawn? pawn in JumpDisabledPlayers.ToList())
        {
            if (pawn == null || !pawn.IsValid)
            {
                JumpDisabledPlayers.Remove(pawn);
                continue;
            }

            if (pawn.AbsVelocity.Z > 0.0f) pawn.AbsVelocity.Z = 0.0f;
        }
    }

    public void DisableJumping(CEntityInstance? activator)
    {
        if (activator != null) JumpDisabledPlayers.Add(activator.As<CCSPlayerPawn>());
    }

    public void EnableJumping(CEntityInstance? activator)
    {
        if (activator != null) JumpDisabledPlayers.Remove(activator.As<CCSPlayerPawn>());
    }

    public override void Remove()
    {
        ArcadeScripts.Instance.RemoveListener<Listeners.OnTick>(OnTick);
    }
}