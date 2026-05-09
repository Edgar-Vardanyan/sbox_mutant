using Sandbox;

public sealed class MutantKnifeViewModel : Component, ICameraSetup
{
	[Property, Group( "Models" )]
	public Model ViewModel { get; set; }

	[Property, Group( "Models" )]
	public Model ArmsModel { get; set; }

	[Property, Group( "References" )]
	public PlayerRole PlayerRole { get; set; }

	[Property, Group( "Offset" )]
	public Vector3 ViewModelLocalOffset { get; set; } = new Vector3( 10f, 0f, -2f );

	[Property, Group( "Offset" )]
	public Angles ViewModelLocalRotation { get; set; } = Angles.Zero;

	[Property, Group( "Animation" )]
	public bool UseCitizenArms { get; set; } = true;

	[Property, Group( "Debug" )]
	public bool DebugLogs { get; set; } = false;

	private GameObject _viewModelObject;
	private SkinnedModelRenderer _weaponRenderer;
	private SkinnedModelRenderer _armsRenderer;

	protected override void OnStart()
	{
		PlayerRole ??= Components.Get<PlayerRole>();
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();
		DestroyViewModel();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
		{
			DestroyViewModel();
			return;
		}

		PlayerRole ??= Components.Get<PlayerRole>();

		if ( PlayerRole is null )
			return;

		if ( _viewModelObject is null || !_viewModelObject.IsValid() || _weaponRenderer is null )
		{
			CreateViewModel();
		}

		var isMutant = PlayerRole.Team == PlayerTeam.Mutant;

		SetVisible( isMutant );
	}

	private void CreateViewModel()
	{
		DestroyViewModel();

		if ( ViewModel is null )
		{
			if ( DebugLogs )
				Log.Warning( $"{GameObject.Name}: MutantKnifeViewModel has no ViewModel assigned." );

			return;
		}

		_viewModelObject = new GameObject( true, "mutant_knife_viewmodel" );
		_viewModelObject.NetworkMode = NetworkMode.Never;
		_viewModelObject.Tags.Set( "viewer", true );
		_viewModelObject.Tags.Set( "viewmodel", true );

		_weaponRenderer = _viewModelObject.Components.Create<SkinnedModelRenderer>();
		_weaponRenderer.Model = ViewModel;
		_weaponRenderer.RenderOptions.Overlay = true;
		_weaponRenderer.RenderOptions.Game = false;
		_weaponRenderer.CreateBoneObjects = true;
		_weaponRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;

		_weaponRenderer.Set( "skeleton", UseCitizenArms ? 1 : 0 );
		_weaponRenderer.Set( "deploy_type", 1 );
		_weaponRenderer.Set( "speed_deploy", 1.0f );

		CreateArms();
	}

	private void CreateArms()
	{
		if ( _viewModelObject is null || _weaponRenderer is null )
			return;

		var armsObject = new GameObject( true, "mutant_knife_arms" );
		armsObject.NetworkMode = NetworkMode.Never;
		armsObject.Parent = _viewModelObject;
		armsObject.Tags.Set( "viewer", true );
		armsObject.Tags.Set( "viewmodel", true );

		_armsRenderer = armsObject.Components.Create<SkinnedModelRenderer>();

		if ( ArmsModel is not null )
		{
			_armsRenderer.Model = ArmsModel;
		}
		else
		{
			_armsRenderer.Model = Model.Load( "models/first_person/first_person_arms.vmdl" );
		}

		_armsRenderer.BoneMergeTarget = _weaponRenderer;
		_armsRenderer.RenderOptions.Overlay = true;
		_armsRenderer.RenderOptions.Game = false;
		_armsRenderer.RenderType = ModelRenderer.ShadowRenderType.Off;
	}

	private void DestroyViewModel()
	{
		if ( _viewModelObject is not null && _viewModelObject.IsValid() )
		{
			_viewModelObject.Destroy();
		}

		_viewModelObject = null;
		_weaponRenderer = null;
		_armsRenderer = null;
	}

	private void SetVisible( bool visible )
	{
		if ( _viewModelObject is not null && _viewModelObject.IsValid() )
		{
			_viewModelObject.Tags.Set( "viewer", visible );
			_viewModelObject.Tags.Set( "viewmodel", visible );
		}

		if ( _weaponRenderer is not null )
			_weaponRenderer.Enabled = visible;

		if ( _armsRenderer is not null )
			_armsRenderer.Enabled = visible;
	}

	public void TriggerAttack()
	{
		PlayAttackOwner();
	}

	public void TriggerHit()
	{
		PlayHitOwner();
	}

	[Rpc.Owner]
	private void PlayAttackOwner()
	{
		if ( _weaponRenderer is null )
			return;

		_weaponRenderer.Set( "b_attack", true );
	}

	[Rpc.Owner]
	private void PlayHitOwner()
	{
		if ( _weaponRenderer is null )
			return;

		_weaponRenderer.Set( "b_attack_hit", true );
	}

	void ICameraSetup.Setup( CameraComponent camera )
	{
		if ( IsProxy )
		{
			DestroyViewModel();
			return;
		}

		if ( _viewModelObject is null || _weaponRenderer is null )
			return;

		PlayerRole ??= Components.Get<PlayerRole>();

		var isMutant = PlayerRole is not null && PlayerRole.Team == PlayerTeam.Mutant;

		SetVisible( isMutant );

		if ( !isMutant )
			return;

		_viewModelObject.WorldPosition = camera.WorldPosition;
		_viewModelObject.WorldRotation = camera.WorldRotation;

		_viewModelObject.LocalRotation *= ViewModelLocalRotation.ToRotation();
		_viewModelObject.LocalPosition += _viewModelObject.WorldRotation * ViewModelLocalOffset;

		var cameraBone = _weaponRenderer.GetBoneObject( "camera" );

		if ( cameraBone is not null )
		{
			camera.LocalPosition += cameraBone.LocalPosition;
			camera.LocalRotation *= cameraBone.LocalRotation;
		}
	}
}