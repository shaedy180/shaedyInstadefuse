using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Linq;

namespace shaedyInstadefuse;

public class ShaedyInstaDefuse : BasePlugin
{
    public override string ModuleName => "shaedy InstaDefuse";
    public override string ModuleVersion => "1.2.3";
    public override string ModuleAuthor => "shaedy";

    private const float SafetyRadius = 175.0f;

    // Hardcoded prefix
    private string ChatPrefix => $"{ChatColors.White}[{ChatColors.Green}shaedy-InstaDefuse{ChatColors.White}]";

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventBombBegindefuse>(OnBombBeginDefuse);
    }

    private HookResult OnBombBeginDefuse(EventBombBegindefuse @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        var bombEntities = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4");
        var bomb = bombEntities.FirstOrDefault();

        if (bomb == null || !bomb.IsValid)
            return HookResult.Continue;

        // Skip if any Ts are still alive
        if (AreAnyTerroristsAlive())
            return HookResult.Continue;

        // Check if player has a kit
        bool hasKit = false;
        var pawn = player.PlayerPawn.Value;

        if (pawn != null && pawn.ItemServices != null)
        {
            var csItemServices = pawn.ItemServices.As<CCSPlayer_ItemServices>();
            if (csItemServices != null)
                hasKit = csItemServices.HasDefuser;
        }

        float defuseTimeNeeded = hasKit ? 5.0f : 10.0f;
        float timeRemaining = bomb.C4Blow - Server.CurrentTime;

        // Not enough time left, instant explosion
        if (timeRemaining < defuseTimeNeeded)
        {
            bomb.C4Blow = Server.CurrentTime;
            player.PrintToChat($"{ChatPrefix} {ChatColors.White}Too late! {ChatColors.Red}BOOM! {ChatColors.White}Time left: {ChatColors.Red}{timeRemaining:F2}s");
            return HookResult.Continue;
        }

        // Block if bomb is in fire
        if (IsBombInFire(bomb.AbsOrigin))
        {
            player.PrintToChat($"{ChatPrefix} {ChatColors.Red}Cannot InstaDefuse! Bomb is burning.");
            return HookResult.Continue;
        }

        // Block if grenades are nearby
        if (AreGrenadesNearby(bomb.AbsOrigin))
        {
            player.PrintToChat($"{ChatPrefix} {ChatColors.Red}Cannot InstaDefuse! Projectiles detected nearby.");
            return HookResult.Continue;
        }

        // All clear, instant defuse
        AddTimer(0.01f, () =>
        {
            if (bomb != null && bomb.IsValid)
                bomb.DefuseCountDown = Server.CurrentTime;
        });

        player.PrintToChat($"{ChatPrefix} {ChatColors.White}Bomb successfully {ChatColors.Green}InstaDefused! {ChatColors.White}Time left: {ChatColors.Green}{timeRemaining:F2}s");

        return HookResult.Continue;
    }

    private bool AreAnyTerroristsAlive()
    {
        var players = Utilities.GetPlayers();
        foreach (var p in players)
        {
            if (p.IsValid && p.PawnIsAlive && p.TeamNum == (byte)CsTeam.Terrorist)
                return true;
        }
        return false;
    }

    private bool IsBombInFire(Vector? bombPosition)
    {
        if (bombPosition == null) return false;

        var infernos = Utilities.FindAllEntitiesByDesignerName<CInferno>("inferno");
        foreach (var inferno in infernos)
        {
            if (inferno == null || !inferno.IsValid) continue;

            // Molotovs burn for about 7 seconds. Anything older is just scorched ground.
            float lifeTime = Server.CurrentTime - inferno.CreateTime;
            if (lifeTime > 7.5f) continue;

            if (inferno.FireCount <= 0) continue;

            if (CalculateDistance(inferno.AbsOrigin, bombPosition) <= SafetyRadius) return true;
        }
        return false;
    }

    private bool AreGrenadesNearby(Vector? bombPosition)
    {
        if (bombPosition == null) return false;

        string[] projectileTypes = { "hegrenade_projectile", "molotov_projectile", "incendiarygrenade_projectile" };

        foreach (var type in projectileTypes)
        {
            var projectiles = Utilities.FindAllEntitiesByDesignerName<CBaseCSGrenadeProjectile>(type);
            foreach (var proj in projectiles)
            {
                if (proj == null || !proj.IsValid) continue;
                if (CalculateDistance(proj.AbsOrigin, bombPosition) <= SafetyRadius) return true;
            }
        }
        return false;
    }

    private float CalculateDistance(Vector? pos1, Vector? pos2)
    {
        if (pos1 == null || pos2 == null) return float.MaxValue;

        return (float)Math.Sqrt(
            Math.Pow(pos1.X - pos2.X, 2) +
            Math.Pow(pos1.Y - pos2.Y, 2) +
            Math.Pow(pos1.Z - pos2.Z, 2)
        );
    }
}