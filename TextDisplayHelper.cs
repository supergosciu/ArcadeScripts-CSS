using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace ArcadeScripts;

public static class TextDisplayHelper
{
    public static string[] MessageText = new string[Server.MaxPlayers];
    public static int[] HoldTimeTicks = new int[Server.MaxPlayers];

    public static void SetMessage(int index, string text, float time)
    {
        HoldTimeTicks[index] = (int)(time * 64);
        MessageText[index] = text;
    }

    public static void OnTick()
    {
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            int slot = player.Slot;
            if (player.PawnIsAlive && !string.IsNullOrEmpty(MessageText[slot]))
            {
                player.PrintToCenterHtml(MessageText[slot]);
            }
        }

        for (int i = 0 ; i < Server.MaxPlayers ; i++)
        {
            HoldTimeTicks[i] -= 1;
            if (HoldTimeTicks[i] <= 0) MessageText[i] = "";
        }
    }
}