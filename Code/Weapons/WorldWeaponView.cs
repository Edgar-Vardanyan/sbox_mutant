using Sandbox;

public sealed class WorldWeaponView : Component
{
	[Property]
	public ModelRenderer WeaponRenderer { get; set; }

	[Property]
	public PlayerRole PlayerRole { get; set; }

	[Property]
	public Vector3 LocalPositionOffset { get; set; } = new Vector3( 18f, -6f, 48f );

	[Property]
	public Angles LocalRotationOffset { get; set; } = new Angles( 0f, 90f, 0f );

	[Property]
	public bool DebugLogs { get; set; } = false;

	protected override void OnStart()
	{
		WeaponRenderer ??= Components.GetInDescendantsOrSelf<ModelRenderer>();
		PlayerRole ??= Components.GetInAncestorsOrSelf<PlayerRole>();
	}

	protected override void OnUpdate()
	{
		if ( WeaponRenderer is null || PlayerRole is null )
			return;

		var isHuman = PlayerRole.Team == PlayerTeam.Human;

		// First version:
		// Only show this weapon for proxy players.
		// Your own weapon is the first-person viewmodel.
		WeaponRenderer.Enabled = IsProxy && isHuman;

		GameObject.LocalPosition = LocalPositionOffset;
		GameObject.LocalRotation = LocalRotationOffset.ToRotation();
	}
}