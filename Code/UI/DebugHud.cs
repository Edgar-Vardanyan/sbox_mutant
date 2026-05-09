using Sandbox;
using System.Linq;

public sealed class DebugHud : Component
{
    [Property]
    public bool ShowHud { get; set; } = true;

    protected override void OnUpdate()
    {
        if (!ShowHud)
            return;

        if (Scene.Camera is null)
            return;

        var hud = Scene.Camera.Hud;

        var localRole = FindLocalPlayerRole();
        var localHealth = localRole?.Components.Get<PlayerHealth>();
        var localWeapon = localRole?.Components.Get<HumanWeapon>();
        var hitFeedback = localRole?.Components.Get<PlayerHitFeedback>();
        var gameManager = Scene.GetAllComponents<GameManager>().FirstOrDefault();

        var roleText = localRole is null ? "None" : localRole.Team.ToString();
        var healthText = localHealth is null ? "None" : $"{localHealth.CurrentHealth}/{localHealth.MaxHealth}";
        var ammoText = localWeapon is null ? "None" : $"{localWeapon.CurrentAmmo}/{localWeapon.ReserveAmmo}";
        var reloadText = localWeapon is null ? "No" : localWeapon.IsReloading ? "Yes" : "No";
        var stateText = gameManager is null ? "None" : gameManager.State.ToString();
        var timerText = gameManager is null ? "0" : $"{gameManager.StateTimeLeft:0}";
        var teamCountText = gameManager is null ? "Humans: 0 | Mutants: 0" : $"Humans: {gameManager.HumanCount} | Mutants: {gameManager.MutantCount}";
        var winnerText = gameManager is null ? "" : gameManager.LastWinnerText;

        hud.DrawText($"Role: {roleText}", 24, Color.White, new Vector2(32, 32), TextFlag.LeftTop);
        hud.DrawText($"Health: {healthText}", 24, Color.White, new Vector2(32, 64), TextFlag.LeftTop);
        hud.DrawText($"Ammo: {ammoText}", 24, Color.White, new Vector2(32, 96), TextFlag.LeftTop);
        hud.DrawText($"Reloading: {reloadText}", 24, Color.White, new Vector2(32, 128), TextFlag.LeftTop);
        hud.DrawText($"Round: {stateText}", 24, Color.White, new Vector2(32, 160), TextFlag.LeftTop);
        hud.DrawText($"Time: {timerText}", 24, Color.White, new Vector2(32, 192), TextFlag.LeftTop);
        hud.DrawText(teamCountText, 24, Color.White, new Vector2(32, 224), TextFlag.LeftTop);

        var center = new Vector2(Screen.Width / 2f, Screen.Height / 2f);

        if (!string.IsNullOrWhiteSpace(winnerText))
        {
            hud.DrawText(winnerText, 48, Color.White, new Vector2(Screen.Width / 2f, 120), TextFlag.Center);
        }

        hud.DrawText("+", 32, Color.White, center, TextFlag.Center);

        if (hitFeedback is not null && hitFeedback.ShouldShowHitMarker)
        {
            hud.DrawText("X", 42, Color.White, center + new Vector2(0, 36), TextFlag.Center);
        }
    }

    private PlayerRole FindLocalPlayerRole()
    {
        foreach (var role in Scene.GetAllComponents<PlayerRole>())
        {
            if (role.IsProxy)
                continue;

            return role;
        }

        return null;
    }
}