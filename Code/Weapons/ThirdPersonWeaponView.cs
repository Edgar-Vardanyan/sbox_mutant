using Sandbox;

public sealed class ThirdPersonWeaponView : Component
{
	[Property, Group( "References" )]
	public ModelRenderer WeaponRenderer { get; set; }

	[Property, Group( "References" )]
	public PlayerRole PlayerRole { get; set; }

	[Property, Group( "References" )]
	public SkinnedModelRenderer BodyRenderer { get; set; }

	[Property, Group( "Attach" )]
	public string AttachBoneName { get; set; } = "hold_R";

	[Property, Group( "Attach" )]
	public Vector3 LocalPositionOffset { get; set; } = Vector3.Zero;

	[Property, Group( "Attach" )]
	public Angles LocalRotationOffset { get; set; } = Angles.Zero;

	[Property, Group( "Visibility" )]
	public bool HideForLocalPlayer { get; set; } = true;

	[Property, Group( "Debug" )]
	public bool DebugLogs { get; set; } = false;

	private GameObject _attachObject;

	protected override void OnStart()
	{
		WeaponRenderer ??= Components.GetInDescendantsOrSelf<ModelRenderer>();
		PlayerRole ??= Components.GetInAncestorsOrSelf<PlayerRole>();
		BodyRenderer ??= Components.GetInAncestorsOrSelf<SkinnedModelRenderer>();

		FindAttachObject();
	}

	protected override void OnUpdate()
	{
		if ( WeaponRenderer is null || PlayerRole is null )
			return;

		var isHuman = PlayerRole.Team == PlayerTeam.Human;
		var shouldShow = isHuman;

		if ( HideForLocalPlayer && !IsProxy )
			shouldShow = false;

		WeaponRenderer.Enabled = shouldShow;

		if ( !shouldShow )
			return;

		if ( _attachObject is null || !_attachObject.IsValid() )
			FindAttachObject();

		if ( _attachObject is null || !_attachObject.IsValid() )
			return;

		GameObject.WorldPosition =
			_attachObject.WorldPosition + _attachObject.WorldRotation * LocalPositionOffset;

		GameObject.WorldRotation =
			_attachObject.WorldRotation * LocalRotationOffset.ToRotation();
	}

	private void FindAttachObject()
	{
		if ( BodyRenderer is null || !BodyRenderer.IsValid() )
			return;

		_attachObject = BodyRenderer.GetBoneObject( AttachBoneName );

		if ( _attachObject is not null && _attachObject.IsValid() )
		{
			if ( DebugLogs )
				Log.Info( $"World weapon attached to bone: {AttachBoneName}" );

			return;
		}

		_attachObject = FindChildByName( BodyRenderer.GameObject, AttachBoneName );

		if ( _attachObject is not null && _attachObject.IsValid() )
		{
			if ( DebugLogs )
				Log.Info( $"World weapon attached to child: {AttachBoneName}" );

			return;
		}

		if ( DebugLogs )
			Log.Warning( $"Could not find attach object: {AttachBoneName}" );
	}

	private GameObject FindChildByName( GameObject root, string name )
	{
		if ( root is null )
			return null;

		foreach ( var child in root.Children )
		{
			if ( child.Name == name )
				return child;

			var found = FindChildByName( child, name );

			if ( found is not null )
				return found;
		}

		return null;
	}
}