using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using ShaedyHudManager;

namespace shaedyInstadefuse;

public class ShaedyInstaDefuse : BasePlugin
{
    private const float SafetyRadius = 175.0f;
    private const float BombProximityRadius = 400.0f;

    private string ChatPrefix => string.Concat(ChatColors.White, "[", ChatColors.Green, "shaedy-InstaDefuse", ChatColors.White, "]");

    private CounterStrikeSharp.API.Modules.Timers.Timer? _bombTimer;
    private HashSet<int> _defusingPlayers = new();
    private Dictionary<int, float> _defuseStatusCooldown = new();

    public override string ModuleName => "shaedy InstaDefuse";
    public override string ModuleVersion => "1.4.0";
    public override string ModuleAuthor => "shaedy";

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventBombBegindefuse>(OnBombBeginDefuse);
        RegisterEventHandler<EventBombPlanted>(OnBombPlanted);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);

        Console.WriteLine("[shaedyInstadefuse] v1.4.0 loaded.");
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        StopBombTimer();
        _defusingPlayers.Clear();
        _defuseStatusCooldown.Clear();
        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        StopBombTimer();
        _defusingPlayers.Clear();
        _defuseStatusCooldown.Clear();
        return HookResult.Continue;
    }

    private HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
    {
        StartBombTimer();
        return HookResult.Continue;
    }

    private void StartBombTimer()
    {
        StopBombTimer();
        _bombTimer = AddTimer(1.0f, OnBombTimerTick, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
    }

    private void StopBombTimer()
    {
        _bombTimer?.Kill();
        _bombTimer = null;
    }

    private void OnBombTimerTick()
    {
        var bombEntities = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4");
        var bomb = bombEntities.FirstOrDefault();

        if (bomb == null || !bomb.IsValid)
        {
            StopBombTimer();
            return;
        }

        float timeRemaining = bomb.C4Blow - Server.CurrentTime;
        if (timeRemaining <= 0)
        {
            StopBombTimer();
            return;
        }

        var players = Utilities.GetPlayers();
        foreach (var player in players)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive || player.IsBot)
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || pawn.AbsOrigin == null)
                continue;

            float dist = CalculateDistance(pawn.AbsOrigin, bomb.AbsOrigin);
            if (dist <= BombProximityRadius)
            {
                bool hasDefuseCooldown = _defuseStatusCooldown.ContainsKey(player.Slot) &&
                    (Server.CurrentTime - _defuseStatusCooldown[player.Slot]) < 3.0f;

                if (!hasDefuseCooldown)
                {
                    bool isDefusing = _defusingPlayers.Contains(player.Slot);
                    ShowBombTimerOverlay(player, timeRemaining, isDefusing, dist);
                }
            }
        }
    }

    private void ShowBombTimerOverlay(CCSPlayerController player, float timeRemaining, bool isDefusing, float distance)
    {
        int minutes = (int)timeRemaining / 60;
        int seconds = (int)timeRemaining % 60;
        string timeStr = minutes + ":" + seconds.ToString("D2");

        string timerColor = timeRemaining <= 10 ? "#ff4444" : timeRemaining <= 20 ? "#ffaa00" : "#4ade80";

        bool hasKit = false;
        var pawn = player.PlayerPawn.Value;
        if (pawn != null && pawn.ItemServices != null)
        {
            var csItemServices = pawn.ItemServices.As<CCSPlayer_ItemServices>();
            if (csItemServices != null)
                hasKit = csItemServices.HasDefuser;
        }

        float defuseTimeNeeded = hasKit ? 5.0f : 10.0f;
        bool canDefuse = timeRemaining >= defuseTimeNeeded;

        string defuseLine = "";
        if (isDefusing)
        {
            defuseLine = "<div style='font-size:14px;color:#4ade80;margin-top:4px;'>defusing...</div>";
        }
        else if (player.TeamNum == (byte)CsTeam.CounterTerrorist)
        {
            string defuseColor = canDefuse ? "#4ade80" : "#f87171";
            string kitLabel = hasKit ? "KIT" : "NO KIT";
            defuseLine = "<div style='font-size:12px;color:" + defuseColor + ";margin-top:4px;'>can defuse: " + kitLabel + "</div>";
        }

        string html = "<html><body style='margin:0;padding:0;'><div style='text-align:center;font-family:Arial;'>";
        html += "<div style='font-size:12px;color:#888;letter-spacing:2px;'>BOMB</div>";
        html += "<div style='font-size:32px;font-weight:bold;color:" + timerColor + ";text-shadow:0 0 15px " + timerColor + ";'>" + timeStr + "</div>";
        html += defuseLine;
        html += "</div></body></html>";

        HudManager.Show(player.SteamID, html, HudPriority.Critical, 1);
    }

    private HookResult OnBombBeginDefuse(EventBombBegindefuse @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null || !player.IsValid || !player.PawnIsAlive)
            return HookResult.Continue;

        _defusingPlayers.Add(player.Slot);

        var bombEntities = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4");
        var bomb = bombEntities.FirstOrDefault();

        if (bomb == null || !bomb.IsValid)
            return HookResult.Continue;

        if (AreAnyTerroristsAlive())
        {
            ShowDefuseStatus(player, "Enemies alive -- normal defuse", "#ffaa00");
            _defuseStatusCooldown[player.Slot] = Server.CurrentTime;
            return HookResult.Continue;
        }

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

        if (timeRemaining < defuseTimeNeeded)
        {
            bomb.C4Blow = Server.CurrentTime;
            ShowDefuseStatus(player, "TOO LATE! BOOM! (" + timeRemaining.ToString("F1") + "s)", "#ff4444");
            _defuseStatusCooldown[player.Slot] = Server.CurrentTime;
            _defusingPlayers.Remove(player.Slot);
            return HookResult.Continue;
        }

        if (IsBombInFire(bomb.AbsOrigin))
        {
            ShowDefuseStatus(player, "Bomb in fire -- insta defuse blocked", "#ff4444");
            _defuseStatusCooldown[player.Slot] = Server.CurrentTime;
            _defusingPlayers.Remove(player.Slot);
            return HookResult.Continue;
        }

        if (AreGrenadesNearby(bomb.AbsOrigin))
        {
            ShowDefuseStatus(player, "Projectiles nearby -- insta defuse blocked", "#ff4444");
            _defuseStatusCooldown[player.Slot] = Server.CurrentTime;
            _defusingPlayers.Remove(player.Slot);
            return HookResult.Continue;
        }

        // Directly set defuse countdown instead of using a delayed timer (race condition fix)
        if (bomb.IsValid)
            bomb.DefuseCountDown = Server.CurrentTime;

        ShowDefuseStatus(player, "INSTA DEFUSED! (" + timeRemaining.ToString("F1") + "s remaining)", "#4ade80");
        _defuseStatusCooldown[player.Slot] = Server.CurrentTime;

        AddTimer(0.5f, () =>
        {
            _defusingPlayers.Remove(player.Slot);
        });

        return HookResult.Continue;
    }

    private void ShowDefuseStatus(CCSPlayerController player, string message, string color)
    {
        string html = "<html><body style='margin:0;padding:0;'><div style='text-align:center;font-family:Arial;'><div style='font-size:20px;font-weight:bold;color:" + color + ";text-shadow:0 0 10px " + color + ";'>" + message + "</div></div></body></html>";

        HudManager.Show(player.SteamID, html, HudPriority.Critical, 3);
        player.PrintToChat(ChatPrefix + " " + ChatColors.White + message);
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
