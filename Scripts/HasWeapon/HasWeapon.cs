using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace ArcadeScripts.Scripts;

public class HasWeapon : ScriptBase
{
    public HasWeapon(CLogicScript owner) : base(owner)
    {
        Functions.Add("CheckForPlayersWithWeapons", new ScriptFunction(CheckForPlayersWithWeapons));
        Functions.Add("EnterArea", new ScriptFunction<CEntityInstance>(EnterArea));
        Functions.Add("SetEnabled", new ScriptFunction<bool>(SetEnabled));
        Functions.Add("ExitArea", new ScriptFunction<CEntityInstance>(ExitArea));

        ArcadeScripts.Instance.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
    }

    private Dictionary<string, bool> Guns = new()
    {
        { "weapon_ak47", true },
        { "weapon_aug", true },
        { "weapon_awp", true },
        { "weapon_bizon", true },
        { "weapon_cz75a", true },
        { "weapon_deagle", true },
        { "weapon_elite", true },
        { "weapon_famas", true },
        { "weapon_fiveseven", true },
        { "weapon_g3sg1", true },
        { "weapon_galilar", true },
        { "weapon_glock", true },
        { "weapon_hkp2000", true },
        { "weapon_m249", true },
        { "weapon_m4a1", true },
        { "weapon_m4a1_silencer", true },
        { "weapon_mac10", true },
        { "weapon_mag7", true },
        { "weapon_mp7", true },
        { "weapon_mp9", true },
        { "weapon_negev", true },
        { "weapon_nova", true },
        { "weapon_p250", true },
        { "weapon_p90", true },
        { "weapon_sawedoff", true },
        { "weapon_scar20", true },
        { "weapon_sg556", true },
        { "weapon_ssg08", true },
        { "weapon_tec9", true },
        { "weapon_ump45", true },
        { "weapon_usp_silencer", true },
        { "weapon_xm1014", true },
        { "weapon_revolver", true }
    };

    private bool Enabled = false;
    private Dictionary<CCSPlayerPawn, bool> PlayersInArea = [];
    private Dictionary<CCSPlayerPawn, bool> PlayersWithGuns = [];
    private Dictionary<CCSPlayerPawn, bool> PlayersWithoutGuns = [];

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerPawn? pawn = @event.Userid?.PlayerPawn.Value;
        ExitArea(pawn);

        return HookResult.Continue;
    }

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        if (!enabled)
        {
            foreach (KeyValuePair<CCSPlayerPawn, bool> player in PlayersWithoutGuns)
            {
                SetVisibleDelayed(player.Key);
            }
            PlayersInArea.Clear();
            PlayersWithGuns.Clear();
            PlayersWithoutGuns.Clear();
        }
    }

    public void EnterArea(CEntityInstance? activator)
    {
        if (activator == null || !activator.IsValid || activator.DesignerName != "player") return;

        CCSPlayerPawn pawn = activator.As<CCSPlayerPawn>();
        PlayersInArea[pawn] = true;
    }

    public void ExitArea(CEntityInstance? activator)
    {
        if (activator == null || !activator.IsValid || activator.DesignerName != "player") return;

        CCSPlayerPawn pawn = activator.As<CCSPlayerPawn>();
        if (!PlayersInArea.ContainsKey(pawn)) return;

        SetVisibleDelayed(pawn);
        PlayersInArea.Remove(pawn);
        PlayersWithGuns.Remove(pawn);
        PlayersWithoutGuns.Remove(pawn);
    }

    public void CheckForPlayersWithWeapons()
    {
        if (!Enabled) return;

        Dictionary<CCSPlayerPawn, bool> NewPlayersWithGuns = [];
        Dictionary<CCSPlayerPawn, bool> NewPlayersWithoutGuns = PlayersInArea.ToDictionary();

        foreach (CCSPlayerPawn pawn in PlayersInArea.Keys)
        {
            CPlayer_WeaponServices? weaponServices = pawn.WeaponServices;
            if (weaponServices == null) continue;

            foreach (CHandle<CBasePlayerWeapon> handle in weaponServices.MyWeapons)
            {
                CBasePlayerWeapon? weapon = handle.Value;
                if (weapon == null || !weapon.IsValid || !Guns.ContainsKey(weapon.DesignerName)) continue;

                // filter out weapons whose owners are already named
                NewPlayersWithoutGuns.Remove(pawn);
                NewPlayersWithGuns[pawn] = true;
                break;
            }
        }

        // Set newly armed players to visible
        foreach (KeyValuePair<CCSPlayerPawn, bool> player in NewPlayersWithGuns)
        {
            if (!PlayersWithGuns.ContainsKey(player.Key))
            {
                SetVisibleDelayed(player.Key);
            }
        }
        PlayersWithGuns = NewPlayersWithGuns;

        // Set newly unarmed players to invisible
        foreach (KeyValuePair<CCSPlayerPawn, bool> player in NewPlayersWithoutGuns)
        {
            if (!PlayersWithoutGuns.ContainsKey(player.Key))
            {
                SetInvisibleDelayed(player.Key);
            }
        }
        PlayersWithoutGuns = NewPlayersWithoutGuns;
    }

    private void SetVisibleDelayed(CCSPlayerPawn pawn)
    {
        pawn.AddEntityIOEvent(inputName: "Alpha", value: "254", delay: 0.25f);
    }

    private void SetInvisibleDelayed(CCSPlayerPawn pawn)
    {
        pawn.AddEntityIOEvent(inputName: "Alpha", value: "100", delay: 0.25f);
    }

    public override void Remove()
    {
        ArcadeScripts.Instance.DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
    }
}