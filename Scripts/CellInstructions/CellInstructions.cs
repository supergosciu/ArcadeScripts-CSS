using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static ArcadeScripts.TextDisplayHelper;

namespace ArcadeScripts.Scripts;

public class CellInstructions : ScriptBase
{
    public CellInstructions(CLogicScript owner) : base(owner)
    {
        Functions.Add("DisableForAll", new ScriptFunction(DisableForAll));
        Functions.Add("DisplayHelpText", new ScriptFunction(DisplayHelpText));
    }

    private const int NoCell = -1;
    private const int StrafeClimb = 0;
    private const int Climb = 1;
    private const int IceClimb = 2;
    private const int WaterRace = 3;
    private const int Surf = 4;
    private const int NoJump = 5;
    private const int Lasers = 6;
    private const int Bhop = 7;
    private const int LongJump = 8;
    private const int NumCells = 9;

    private string[] CellInstrictonStrings = [
        "<font class='fontSize-sm horizontal-center'>Climb and strafe to reach the goal.</font>",
        "<font class='fontSize-sm horizontal-center'>Climb to reach the goal.</font>",
        "<font class='fontSize-sm horizontal-center'>Climb the ice to reach the goal.</font>",
        "<font class='fontSize-sm horizontal-center'>Swim and avoid the walls to reach the green wall at the end.</font>",
        "<font class='fontSize-sm horizontal-center'>Ski, surf and drop to reach the goal.</font>",
        "<font class='fontSize-sm horizontal-center'>Crouch over the gaps to reach the goal.<br>Jumping is disabled.</font>",
        "<font class='fontSize-sm horizontal-center'>Avoid the lasers to reach the goal.</font>",
        "<font class='fontSize-sm horizontal-center'>Bunny hop on the platforms to reach the goal.</font>",
        "<font class='fontSize-sm horizontal-center'>Jump and strafe in midair over the gaps to reach the goal.</font>"
    ];

    private static Vector StrafeAreaMin = new(452.0f, -2112.0f, 0.0f);
    private static Vector StrafeAreaMax = new(960.0f, -572.0f, 0.0f);
    private static Vector ClimbAreaMin = new(-60.0f, -2112.0f, 0.0f);
    private static Vector ClimbAreaMax = new(444.0f, -572.0f, 0.0f);
    private static Vector IceClimbAreaMin = new(-576.0f, -2112.0f, 0.0f);
    private static Vector IceClimbAreaMax = new(-68.0f, -572.0f, 0.0f);
    private static Vector WaterRaceAreaMin = new(-960.0f, -576.0f, 0.0f);
    private static Vector WaterRaceAreaMax = new(-572.0f, -224.0f, 0.0f);
    private static Vector SurfAreaMin = new(-960.0f, -216.0f, 0.0f);
    private static Vector SurfAreaMax = new(-572.0f, 152.0f, 0.0f);
    private static Vector NoJumpAreaMin = new(-960.0f, 160.0f, 0.0f);
    private static Vector NoJumpAreaMax = new(-572.0f, 512.0f, 0.0f);
    private static Vector LasersAreaMin = new(-576.0f, 508.0f, 0.0f);
    private static Vector LasersAreaMax = new(-68.0f, 2048.0f, 0.0f);
    private static Vector BhopAreaMin = new(-60.0f, 508.0f, 0.0f);
    private static Vector BhopAreaMax = new(444.0f, 2048.0f, 0.0f);
    private static Vector LongJumpAreaMin = new(452.0f, 508.0f, 0.0f);
    private static Vector LongJumpAreaMax = new(960.0f, 2048.0f, 0.0f);

    private static Vector[] AREA_MIN = [StrafeAreaMin, ClimbAreaMin, IceClimbAreaMin, WaterRaceAreaMin, SurfAreaMin, NoJumpAreaMin, LasersAreaMin, BhopAreaMin, LongJumpAreaMin];
    private static Vector[] AREA_MAX = [StrafeAreaMax, ClimbAreaMax, IceClimbAreaMax, WaterRaceAreaMax, SurfAreaMax, NoJumpAreaMax, LasersAreaMax, BhopAreaMax, LongJumpAreaMax];

    private bool Disabled = false;

    public void DisableForAll()
    {
        Disabled = true;
    }

    private int GetPlayerCell(CCSPlayerController player)
    {
        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) return NoCell;

        Vector playerOrigin = pawn.AbsOrigin!;
        for (int i = 0; i < NumCells; i++)
        {
            if (playerOrigin.X >= AREA_MIN[i].X && playerOrigin.X <= AREA_MAX[i].X && playerOrigin.Y >= AREA_MIN[i].Y && playerOrigin.Y <= AREA_MAX[i].Y)
                return i;
        }
        return NoCell;
    }

    public void DisplayHelpText()
    {
        if (Disabled) return;

        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            int cell = GetPlayerCell(player);
            if (cell != NoCell)
            {
                SetMessage(player.Slot, CellInstrictonStrings[cell], 4.0f);
            }
        }
    }
}