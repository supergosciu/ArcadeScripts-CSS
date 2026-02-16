namespace ArcadeScripts;

public static class Random
{
    private static System.Random _Random = new();

    public static float RandomFloat(float min, float max)
    {
        return (float)(_Random.NextDouble() * (max - min) + min);
    }

    public static int RandomInt(int min, int max)
    {
        return _Random.Next(min, max + 1);
    }
}