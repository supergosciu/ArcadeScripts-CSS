using System.Diagnostics.CodeAnalysis;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

public static class VectorExtensions
{
    public static float VecLength2DSqr(this Vector vector)
    {
        return vector.X * vector.X + vector.Y * vector.Y;
    }

    //Scales a vector
    public static Vector Scale(this Vector vector, float scalar)
    {
        float len = vector.Length();

        return len == 0 ? vector : new Vector(vector.X * scalar / len, vector.Y * scalar / len, vector.Z * scalar / len);
    }

    public static Vector Clone(this Vector vector)
    {
        Vector vec = new()
        {
            X = vector.X,
            Y = vector.Y,
            Z = vector.Z
        };
        return vec;
    }

    public static Vector GetForwardVector(this QAngle vector)
    {
        float pitch = vector.X * (MathF.PI / 180f);
        float yaw = vector.Y * (MathF.PI / 180f);

        float cp = MathF.Cos(pitch);
        float sp = MathF.Sin(pitch);
        float cy = MathF.Cos(yaw);
        float sy = MathF.Sin(yaw);

        return new Vector(cp * cy, cp * sy, -sp);
    }

    public static float DistanceTo(Vector a, Vector b)
    {
        return (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2) + Math.Pow(a.Z - b.Z, 2));
    }

    public static bool GetPlayerPawn(this CCSPlayerController? player, [NotNullWhen(true)] out CCSPlayerPawn? pawn)
    {
        pawn = player?.PlayerPawn.Value;
        return player != null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected && pawn != null && pawn.IsValid;
    }
}