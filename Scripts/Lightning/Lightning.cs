using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using static ArcadeScripts.TextDisplayHelper;

namespace ArcadeScripts.Scripts;

public class Lightning : ScriptBase
{
    public Lightning(CLogicScript owner) : base(owner)
    {
        Functions.Add("TryLightning", new ScriptFunction(TryLightning));
        Functions.Add("PickUpSpecialItem", new ScriptFunction<CEntityInstance, CEntityInstance>(PickUpSpecialItem));

        EntityNames = ["hurt_script", "lightning_template", "lightning_sound_script1", "lightning_sound_script2"];

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent += OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent += OnEntitySpawned;
    }

    private const float LightningLifetime = 3.0f;
    private const int LightningBranches = 5;
    private const float LightningCooldown = 10.0f;
    private float NextLightningAvailable = 0.0f;

    private Dictionary<string, bool> UsedLightningBeamStarts = [];

    private CWeaponFiveSeven Item = null!;
    private CCSPlayerPawn SpecialPlayer = null!;
    private CLogicScript HurtScript = null!;
    private CPointTemplate LightningTemplate = null!;
    private CLogicScript LightningSoundScript1 = null!;
    private CLogicScript LightningSoundScript2 = null!;
    private List<CFuncRotating> LightningStarts = [];
    private List<CFuncRotating> LightningStops = [];
    private List<CEnvBeam> LightningBeams = [];
    
    private void OnAllEntitiesSpawned()
    {
        foreach (KeyValuePair<string, List<CEntityInstance>> kv in EntityList)
        {
            List<CEntityInstance> entities = kv.Value;
            string targetname = kv.Key;

            if (targetname == "hurt_script")
            {
                HurtScript = entities[0].As<CLogicScript>();
            }
            else if (targetname == "lightning_template")
            {
                LightningTemplate = entities[0].As<CPointTemplate>();
            }
            else if (targetname == "lightning_sound_script1")
            {
                LightningSoundScript1 = entities[0].As<CLogicScript>();
            }
            else if (targetname == "lightning_sound_script2")
            {
                LightningSoundScript2 = entities[0].As<CLogicScript>();
            }
        }
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        string? targetname = entity.Entity?.Name;
        if (string.IsNullOrEmpty(targetname)) return;

        if (targetname.StartsWith("lightning_start"))
        {
            LightningStarts.Add(entity.As<CFuncRotating>());
        }
        else if (targetname.StartsWith("lightning_stop"))
        {
            LightningStops.Add(entity.As<CFuncRotating>());
        }
        else if (targetname.StartsWith("lightning_beam"))
        {
            LightningBeams.Add(entity.As<CEnvBeam>());
        }
    }

    public void PickUpSpecialItem(CEntityInstance? activator, CEntityInstance? caller)
    {
        if (activator == null || caller == null) return;

        Item = caller.As<CWeaponFiveSeven>();
        SpecialPlayer = activator.As<CCSPlayerPawn>();
        CCSPlayerController? player = activator.As<CCSPlayerPawn>().OriginalController.Value;
        if (player == null) return;

        SetMessage(player.Slot, "Left click to attack nearby enemies with lightning.", 15.0f);
    }
    
    public void TryLightning()
    {
        if (Server.CurrentTime < NextLightningAvailable) return;

        if (Item != null && (!Item.IsValid || Item.OwnerEntity.Value != SpecialPlayer))
        {
            SpecialPlayer = null!;
        }

        if (SpecialPlayer == null) return;

        List<PlayerDistance> victim = GetClosestPlayers(SpecialPlayer.AbsOrigin!, 1, 1024f, SpecialPlayer, SpecialPlayer.TeamNum);
        if (victim.Count == 0) return;

        NextLightningAvailable = Server.CurrentTime + LightningCooldown;

        StartLightning(SpecialPlayer, victim[0].Pawn);
    }

    private void StartLightning(CCSPlayerPawn start, CCSPlayerPawn end)
    {
        ArcadeScripts.Instance.RunScriptCode(new(HurtScript, "SetAttacker(!activator, HurterFour)", start, HurtScript));
        ArcadeScripts.Instance.RunScriptCode(new(HurtScript, "SetAttacker(!activator, HurterFive)", start, HurtScript));

        LightningTemplate.AddEntityIOEvent(inputName: "ForceSpawn");
        Timers.Add(new(0.03f, () => LightningBeam(start, end)));
        Timers.Add(new(0.5f, () => ArcadeScripts.Instance.RunScriptCode(new(HurtScript, "HurtActivator(!activator, HurterFour)", end, HurtScript))));
        ArcadeScripts.Instance.RunScriptCode(new(LightningSoundScript1, "PlaySound(!activator)", start, LightningSoundScript1));
        ArcadeScripts.Instance.RunScriptCode(new(LightningSoundScript2, "PlaySound(!activator)", end, LightningSoundScript2));

        int playerTeam = start.TeamNum;
        Vector endPlayerOrigin = end.AbsOrigin!;
        int playersHit = 1;
        float delay = 0.05f;
        List<PlayerDistance> closestPlayers = GetClosestPlayers(endPlayerOrigin, 4, 512.0f, end, playerTeam);
        for (int i = 0; i < closestPlayers.Count; i++)
        {
            PlayerDistance playerObject = closestPlayers[i];
            CCSPlayerPawn pawn = playerObject.Pawn;

            LightningTemplate.AddEntityIOEvent(inputName: "ForceSpawn");
            Timers.Add(new(delay, () => LightningBeam(end, pawn)));
            Timers.Add(new(delay + 0.1f, () => ArcadeScripts.Instance.RunScriptCode(new(HurtScript, "HurtActivator(!activator, HurterFive)", pawn, HurtScript))));

            delay += 0.05f;
            playersHit++;
            if (playersHit >= LightningBranches) break;
        }
        DisplayTextLightning(start.OriginalController.Value!, playersHit);
    }

    private void LightningBeam(CCSPlayerPawn start, CCSPlayerPawn end)
    {
        CFuncRotating? beamStart = LightningStarts.FirstOrDefault();
        CFuncRotating? beamEnd = null;
        if (beamStart != null && !UsedLightningBeamStarts.ContainsKey(beamStart.Entity!.Name))
        {
            UsedLightningBeamStarts[beamStart.Entity!.Name] = true;
            string suffix = beamStart.Entity!.Name.Substring(15);
            beamEnd = LightningStops.FirstOrDefault(beam => beam.Entity?.Name == $"lightning_stop{suffix}");
            LightningStarts.Remove(beamStart);
            LightningStops.Remove(beamEnd!);
            if (beamEnd != null) beamEnd.AddEntityIOEvent(inputName: "Kill", delay: LightningLifetime + 0.05f, activator: beamEnd);
            if (beamStart != null) beamStart.AddEntityIOEvent(inputName: "Kill", delay: LightningLifetime + 0.05f, activator: beamStart);

            foreach (CEnvBeam beam in LightningBeams.ToList())
            {
                LightningBeams.Remove(beam);
                beam.AddEntityIOEvent(inputName: "Kill", delay: LightningLifetime + 0.05f);
            }
        }
        if (beamStart == null || beamEnd == null) return;

        if (start == null || end == null || !start.IsValid || !end.IsValid) return;
        Vector startPlayerOrigin = start.AbsOrigin!;
        Vector endPlayerOrigin = end.AbsOrigin!;

        DisplayTextLightning(end.OriginalController.Value!, 0);

        beamStart.Teleport(new Vector(startPlayerOrigin!.X, startPlayerOrigin.Y, startPlayerOrigin.Z + 50.0f));
        beamStart.AddEntityIOEvent(inputName: "SetParent", value: "!activator", delay: 0.0f, activator: start);

        beamEnd.Teleport(new Vector(endPlayerOrigin!.X, endPlayerOrigin.Y, endPlayerOrigin.Z + 50.0f));
        beamEnd.AddEntityIOEvent(inputName: "SetParent", value: "!activator", delay: 0.0f, activator: end);
    }

    private void DisplayTextLightning(CCSPlayerController player, int victims)
    {
        string text = victims > 0 ? $"You hit {victims} players with lightning!" : "You were hit by lightning!";
        SetMessage(player.Slot, text, 2.0f);
    }

    private class PlayerDistance
    {
        public required CCSPlayerPawn Pawn;
        public required float Distance;
    }

    private List<PlayerDistance> GetClosestPlayers(Vector origin, int maxPlayers, float maxDistance, CCSPlayerPawn ignorePlayer, int ignoreTeam)
    {
        List<PlayerDistance> nearbyPlayers = [];
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;
            if (pawn == null || pawn == ignorePlayer || player.TeamNum == ignoreTeam || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;

            float distanceToOrigin = VectorExtensions.DistanceTo(pawn.AbsOrigin!, origin);
            if (distanceToOrigin > maxDistance) continue;

            PlayerDistance playerDistance = new() { Pawn = pawn, Distance = distanceToOrigin };
            nearbyPlayers.Add(playerDistance);
        }

        nearbyPlayers.Sort(ComparePlayersByDistance);
        int numPlayers = (maxPlayers < nearbyPlayers.Count) ? maxPlayers : nearbyPlayers.Count;


        return nearbyPlayers.GetRange(0, numPlayers);
    }

    private int ComparePlayersByDistance(PlayerDistance playerA, PlayerDistance playerB)
    {
        if (playerA.Distance > playerB.Distance) return 1;
        else if (playerA.Distance < playerB.Distance) return -1;
        return 0;
    }

    public override void Remove()
    {
        foreach (Timer timer in Timers) timer?.Kill();

        ArcadeScripts.Instance.OnAllEntitiesSpawnedEvent -= OnAllEntitiesSpawned;
        ArcadeScripts.Instance.OnEntitySpawnedEvent -= OnEntitySpawned;
    }
}