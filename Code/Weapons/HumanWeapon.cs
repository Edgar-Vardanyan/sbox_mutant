using Sandbox;
using System.Threading.Tasks;

public sealed class HumanWeapon : Component
{

    [Property]
    public FirstPersonViewModel ViewModel { get; set; }

    [Property]
    public int Damage { get; set; } = 25;

    [Property]
    public float FireRange { get; set; } = 5000f;

    [Property]
    public float FireCooldown { get; set; } = 0.15f;

    [Property]
    public int MagazineSize { get; set; } = 30;

    [Sync(SyncFlags.FromHost), Property]
    public int CurrentAmmo { get; private set; } = 30;

    [Sync(SyncFlags.FromHost), Property]
    public int ReserveAmmo { get; private set; } = 120;

    [Sync(SyncFlags.FromHost), Property]
    public bool IsReloading { get; private set; }

    [Property]
    public float ReloadDuration { get; set; } = 1.8f;

    [Property]
    public bool InfiniteReserveAmmo { get; set; } = true;

    [Property]
    public bool DebugLogs { get; set; } = true;

    private TimeUntil _nextFireTime;
    private TimeUntil _reloadFinishedTime;

    protected override void OnStart()
    {
        ViewModel ??= Components.Get<FirstPersonViewModel>();
        _nextFireTime = 0f;
        CurrentAmmo = MagazineSize;

        if (DebugLogs)
        {
            Log.Info($"HumanWeapon started on {GameObject.Name}. Ammo: {CurrentAmmo}/{ReserveAmmo}");
        }
    }

    protected override void OnUpdate()
    {
        if (IsProxy)
            return;

        var role = Components.Get<PlayerRole>();

        if (role is null)
            return;

        if (role.Team != PlayerTeam.Human)
            return;

        if (IsReloading)
            return;

        if (Input.Pressed("reload"))
        {
            RequestStartReload();
            return;
        }

        if (!_nextFireTime)
            return;

        if (Input.Down("attack1"))
        {
            var start = GetFireStartPosition();
            var direction = GetFireDirection();

            RequestFire(start, direction);

            _nextFireTime = FireCooldown;
        }
    }

    [Rpc.Host]
    private void RequestFire(Vector3 start, Vector3 direction)
    {
        var shooterRole = Components.Get<PlayerRole>();

        if (shooterRole is null)
            return;

        if (shooterRole.Team != PlayerTeam.Human)
            return;

        if (IsReloading)
            return;

        if (CurrentAmmo <= 0)
        {

            ViewModel?.TriggerDryFire();
            if (DebugLogs)
            {
                Log.Info($"{GameObject.Name} tried to shoot, but magazine is empty.");
            }

            return;
        }

        CurrentAmmo--;
        ViewModel?.TriggerFire();

        var end = start + direction.Normal * FireRange;

        var result = Scene.Trace
            .Sphere(6f, start, end)
            .IgnoreGameObjectHierarchy(GameObject)
            .UseHitPosition()
            .Run();

        if (DebugLogs)
        {
            Log.Info($"HOST human shot from {GameObject.Name}. Ammo: {CurrentAmmo}/{ReserveAmmo}. Hit: {result.Hit}, Object: {result.GameObject?.Name}");
        }

        if (!result.Hit)
            return;

        var targetRole = FindPlayerRoleFromHitObject(result.GameObject);

        if (targetRole is null)
            return;

        if (targetRole == shooterRole)
            return;

        if (targetRole.Team != PlayerTeam.Mutant)
            return;

        var targetHealth = targetRole.Components.Get<PlayerHealth>();

        if (targetHealth is null)
            return;

        targetHealth.TakeDamage(Damage);

        var feedback = Components.Get<PlayerHitFeedback>();

        if (feedback is not null)
        {
            feedback.TriggerHitMarker();
        }

        if (DebugLogs)
        {
            Log.Info($"{GameObject.Name} damaged {targetRole.GameObject.Name} for {Damage}. Health: {targetHealth.CurrentHealth}/{targetHealth.MaxHealth}");
        }
    }

    [Rpc.Host]
    private void RequestStartReload()
    {
        var shooterRole = Components.Get<PlayerRole>();

        if (shooterRole is null)
            return;

        if (shooterRole.Team != PlayerTeam.Human)
            return;

        if (IsReloading)
            return;

        if (CurrentAmmo >= MagazineSize)
            return;

        if (!InfiniteReserveAmmo && ReserveAmmo <= 0)
            return;

        IsReloading = true;
        ViewModel?.TriggerReload();

        if (DebugLogs)
        {
            Log.Info($"{GameObject.Name} started reload.");
        }

        _ = FinishReloadAfterDelay();
    }

    private async Task FinishReloadAfterDelay()
    {
        await Task.DelaySeconds(ReloadDuration);

        if (!IsReloading)
            return;

        var shooterRole = Components.Get<PlayerRole>();

        if (shooterRole is null || shooterRole.Team != PlayerTeam.Human)
        {
            IsReloading = false;
            return;
        }

        FinishReloadOnHost();
    }

    private void FinishReloadOnHost()
    {
        var neededAmmo = MagazineSize - CurrentAmmo;

        if (InfiniteReserveAmmo)
        {
            CurrentAmmo = MagazineSize;
        }
        else
        {
            var ammoToLoad = ReserveAmmo.Clamp(0, neededAmmo);
            CurrentAmmo += ammoToLoad;
            ReserveAmmo -= ammoToLoad;
        }

        IsReloading = false;

        if (DebugLogs)
        {
            Log.Info($"{GameObject.Name} finished reload. Ammo: {CurrentAmmo}/{ReserveAmmo}");
        }
    }

    private PlayerRole FindPlayerRoleFromHitObject(GameObject hitObject)
    {
        if (hitObject is null)
            return null;

        var current = hitObject;

        while (current is not null)
        {
            var role = current.Components.Get<PlayerRole>();

            if (role is not null)
                return role;

            current = current.Parent;
        }

        return null;
    }

    private Vector3 GetFireStartPosition()
    {
        if (Scene.Camera is not null)
        {
            return Scene.Camera.WorldPosition;
        }

        return GameObject.WorldPosition + Vector3.Up * 64f;
    }

    private Vector3 GetFireDirection()
    {
        if (Scene.Camera is not null)
        {
            return Scene.Camera.WorldRotation.Forward;
        }

        return GameObject.WorldRotation.Forward;
    }
}