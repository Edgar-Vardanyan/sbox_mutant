using Sandbox;
using Sandbox.Rendering;

public sealed class FirstPersonViewModel : Component, ICameraSetup
{
    [Property, Group("Models")]
    public Model ViewModel { get; set; }

    [Property, Group("Models")]
    public Model ArmsModel { get; set; }

    [Property, Group("References")]
    public SkinnedModelRenderer BodyRenderer { get; set; }

    [Property, Group("References")]
    public HumanWeapon HumanWeapon { get; set; }

    [Property, Group("References")]
    public PlayerRole PlayerRole { get; set; }

    [Property, Group("ViewModel Offset")]
    public Vector3 ViewModelLocalOffset { get; set; }

    [Property, Group("Aim Offset")]
    public Vector3 AimViewModelLocalOffset { get; set; } = new Vector3(18f, 0f, -4f);

    [Property, Group("Aim Offset")]
    public Angles AimViewModelLocalRotation { get; set; } = new Angles(0f, 0f, 0f);

    [Property, Group("Aim Offset")]
    public float AimOffsetLerpSpeed { get; set; } = 14f;

    private float _aimOffsetAmount;

    [Property, Group("ViewModel Offset")]
    public Angles ViewModelLocalRotation { get; set; }

    [Property, Group("Animation")]
    public bool UseCitizenArms { get; set; } = true;

    [Property, Group("Animation")]
    public float IronsightsFireScale { get; set; } = 0.5f;

    [Property, Group("Debug")]
    public bool DebugLogs { get; set; } = false;

    private GameObject _viewModelObject;
    private SkinnedModelRenderer _weaponRenderer;
    private SkinnedModelRenderer _armsRenderer;

    private float _attackHold;
    private Rotation _lastCameraRotation;

    protected override void OnStart()
    {
        HumanWeapon ??= Components.Get<HumanWeapon>();
        PlayerRole ??= Components.Get<PlayerRole>();
        BodyRenderer ??= Components.GetInDescendantsOrSelf<SkinnedModelRenderer>();

        if (ViewModel is null)
        {
            Log.Warning($"{GameObject.Name}: FirstPersonViewModel has no ViewModel assigned.");
        }
    }

    protected override void OnEnabled()
    {
        base.OnEnabled();

        // Do not create here.
        // Wait until OnUpdate confirms this is the local camera player.
    }

    protected override void OnDisabled()
    {
        base.OnDisabled();

        DestroyViewModel();
    }

    protected override void OnUpdate()
    {
        if (IsProxy)
        {
            DestroyViewModel();
            return;
        }

        if (HumanWeapon is null)
            HumanWeapon = Components.Get<HumanWeapon>();

        if (PlayerRole is null)
            PlayerRole = Components.Get<PlayerRole>();

        if (HumanWeapon is null || PlayerRole is null)
            return;

        if (_viewModelObject is null || !_viewModelObject.IsValid() || _weaponRenderer is null)
        {
            CreateViewModel();
        }

        if (_viewModelObject is null || _weaponRenderer is null)
            return;

        var isHuman = PlayerRole.Team == PlayerTeam.Human;

        SetViewModelVisible(isHuman);

        if (!isHuman)
            return;

        UpdateAnimationParameters();
    }

    private void SetViewModelVisible(bool visible)
    {
        if (_viewModelObject is not null && _viewModelObject.IsValid())
        {
            _viewModelObject.Tags.Set("viewer", visible);
            _viewModelObject.Tags.Set("viewmodel", visible);
        }

        if (_weaponRenderer is not null)
        {
            _weaponRenderer.Enabled = visible;
        }

        if (_armsRenderer is not null)
        {
            _armsRenderer.Enabled = visible;
        }
    }

    private void CreateViewModel()
    {
        DestroyViewModel();

        if (ViewModel is null)
            return;

        _viewModelObject = new GameObject(true, "first_person_viewmodel");
        _viewModelObject.NetworkMode = NetworkMode.Never;
        _viewModelObject.SetParent(null);
        _viewModelObject.Tags.Set("viewer", true);
        _viewModelObject.Tags.Set("viewmodel", true);

        _weaponRenderer = _viewModelObject.Components.Create<SkinnedModelRenderer>();
        _weaponRenderer.Model = ViewModel;

        _weaponRenderer.RenderOptions.Overlay = true;
        _weaponRenderer.RenderOptions.Game = false;
        _weaponRenderer.CreateBoneObjects = true;
        _weaponRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;

        _weaponRenderer.Set("skeleton", UseCitizenArms ? 1 : 0);
        _weaponRenderer.Set("firing_mode", 3);
        _weaponRenderer.Set("deploy_type", 1);
        _weaponRenderer.Set("speed_reload", 1.0f);
        _weaponRenderer.Set("ironsights_fire_scale", IronsightsFireScale);

        CreateArms();

        _lastCameraRotation = Scene.Camera?.WorldRotation ?? Rotation.Identity;

        if (DebugLogs)
        {
            Log.Info($"{GameObject.Name}: Created first-person viewmodel.");
        }
    }

    private void CreateArms()
    {
        if (_viewModelObject is null || _weaponRenderer is null)
            return;

        var armsObject = new GameObject(true, "first_person_arms");
        armsObject.NetworkMode = NetworkMode.Never;
        armsObject.Parent = _viewModelObject;
        armsObject.Tags.Set("viewer", true);
        armsObject.Tags.Set("viewmodel", true);

        _armsRenderer = armsObject.Components.Create<SkinnedModelRenderer>();

        if (ArmsModel is not null)
        {
            _armsRenderer.Model = ArmsModel;
        }
        else
        {
            _armsRenderer.Model = Model.Load("models/first_person/first_person_arms.vmdl");
        }

        _armsRenderer.BoneMergeTarget = _weaponRenderer;
        _armsRenderer.RenderOptions.Overlay = true;
        _armsRenderer.RenderOptions.Game = false;
        _armsRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;
    }

    private void DestroyViewModel()
    {
        if (_viewModelObject is not null && _viewModelObject.IsValid())
        {
            _viewModelObject.Destroy();
        }

        _viewModelObject = null;
        _weaponRenderer = null;
        _armsRenderer = null;
    }

    private bool IsLocalCameraPlayer()
    {
        if (Scene.Camera is null)
            return false;

        var current = Scene.Camera.GameObject;

        while (current is not null)
        {
            if (current == GameObject)
                return true;

            current = current.Parent;
        }

        return false;
    }

    private void UpdateAnimationParameters()
    {
        var isEmpty = HumanWeapon.CurrentAmmo <= 0;
        var isReloading = HumanWeapon.IsReloading;
        var isAiming = Input.Down("attack2");
        var isFiring = Input.Down("attack1") && !isReloading && !isEmpty;

        _weaponRenderer.Set("b_empty", isEmpty);
        _weaponRenderer.Set("ironsights", isAiming ? 1 : 0);
        _weaponRenderer.Set("ironsights_fire_scale", IronsightsFireScale);

        _attackHold = _attackHold.LerpTo(isFiring ? 1f : 0f, Time.Delta * 12f);
        _weaponRenderer.Set("attack_hold", _attackHold);

        var controller = Components.Get<PlayerController>();

        if (controller is not null)
        {
            var move = controller.WishVelocity;

            _weaponRenderer.Set("move_bob", move.Length.Remap(0f, 150f, 0f, 1f));
            _weaponRenderer.Set("move_groundspeed", move.WithZ(0f).Length);
            _weaponRenderer.Set("move_x", move.x);
            _weaponRenderer.Set("move_y", move.y);
            _weaponRenderer.Set("move_z", move.z);
            _weaponRenderer.Set("b_grounded", controller.IsOnGround);

            var running = Input.Down(controller.AltMoveButton) && move.Length > 0f;
            _weaponRenderer.Set("b_sprint", running);

            if (BodyRenderer.IsValid())
            {
                _weaponRenderer.Set("b_jump", BodyRenderer.GetBool("b_jump"));
                _weaponRenderer.Set("move_groundspeed", BodyRenderer.GetFloat("move_groundspeed"));
                _weaponRenderer.Set("move_x", BodyRenderer.GetFloat("move_x"));
                _weaponRenderer.Set("move_y", BodyRenderer.GetFloat("move_y"));
                _weaponRenderer.Set("move_z", BodyRenderer.GetFloat("move_z"));
            }
        }

        if (Scene.Camera is not null)
        {
            var rotationDelta = Rotation.Difference(_lastCameraRotation, Scene.Camera.WorldRotation);
            _lastCameraRotation = Scene.Camera.WorldRotation;

            var angles = rotationDelta.Angles();

            _weaponRenderer.Set("aim_pitch", angles.pitch);
            _weaponRenderer.Set("aim_yaw", angles.yaw);
            _weaponRenderer.Set("aim_pitch_inertia", angles.pitch);
            _weaponRenderer.Set("aim_yaw_inertia", angles.yaw);
        }
    }

    public void TriggerFire()
    {
        PlayFireOwner();
    }

    public void TriggerDryFire()
    {
        PlayDryFireOwner();
    }

    public void TriggerReload()
    {
        PlayReloadOwner();
    }

    [Rpc.Owner]
    private void PlayFireOwner()
    {
        if (_weaponRenderer is null)
            return;

        _weaponRenderer.Set("b_attack", true);
    }

    [Rpc.Owner]
    private void PlayDryFireOwner()
    {
        if (_weaponRenderer is null)
            return;

        _weaponRenderer.Set("b_attack_dry", true);
    }

    [Rpc.Owner]
    private void PlayReloadOwner()
    {
        if (_weaponRenderer is null)
            return;

        _weaponRenderer.Set("b_reload", true);
    }

    void ICameraSetup.Setup(CameraComponent camera)
    {
        if (IsProxy)
        {
            DestroyViewModel();
            return;
        }

        if (_viewModelObject is null || _weaponRenderer is null)
            return;

        var isHuman = PlayerRole is not null && PlayerRole.Team == PlayerTeam.Human;

        _weaponRenderer.Enabled = isHuman;

        if (_armsRenderer is not null)
            _armsRenderer.Enabled = isHuman;

        if (!isHuman)
            return;

        if (BodyRenderer.IsValid())
        {
            _viewModelObject.Tags.Set("viewer", !BodyRenderer.Tags.Has("viewer"));
        }
        else
        {
            _viewModelObject.Tags.Set("viewer", true);
        }
        var aiming = Input.Down("attack2");

        _aimOffsetAmount = _aimOffsetAmount.LerpTo(aiming ? 1f : 0f, Time.Delta * AimOffsetLerpSpeed);

        var finalOffset = ViewModelLocalOffset.LerpTo(AimViewModelLocalOffset, _aimOffsetAmount);
        var finalRotation = ViewModelLocalRotation.LerpTo(AimViewModelLocalRotation, _aimOffsetAmount);

        _viewModelObject.WorldPosition = camera.WorldPosition;
        _viewModelObject.WorldRotation = camera.WorldRotation;

        _viewModelObject.LocalRotation *= finalRotation.ToRotation();
        _viewModelObject.LocalPosition += _viewModelObject.WorldRotation * finalOffset;

        var cameraBone = _weaponRenderer.GetBoneObject("camera");

        if (cameraBone is not null)
        {
            camera.LocalPosition += cameraBone.LocalPosition;
            camera.LocalRotation *= cameraBone.LocalRotation;
        }
    }
}