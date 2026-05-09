using Sandbox;
using Sandbox.Citizen;

public sealed class CitizenWeaponPoseController : Component
{
	[Property] public PlayerRole PlayerRole { get; set; }
	[Property] public CitizenAnimationHelper AnimationHelper { get; set; }
	[Property] public SkinnedModelRenderer BodyRenderer { get; set; }
	[Property] public GameObject EyeSource { get; set; }

	[Property]
	public CitizenAnimationHelper.HoldTypes HumanHoldType { get; set; } = CitizenAnimationHelper.HoldTypes.Rifle;

	[Property] public bool DebugLogs { get; set; } = false;

	protected override void OnStart()
	{
		PlayerRole ??= Components.Get<PlayerRole>();
		AnimationHelper ??= Components.GetInDescendantsOrSelf<CitizenAnimationHelper>();
		BodyRenderer ??= Components.GetInDescendantsOrSelf<SkinnedModelRenderer>();
	}

	protected override void OnUpdate()
	{
		if ( PlayerRole is null || !PlayerRole.IsValid() )
			return;

		if ( AnimationHelper is null || !AnimationHelper.IsValid() )
			return;

		if ( BodyRenderer is null || !BodyRenderer.IsValid() )
			return;

		AnimationHelper.Target = BodyRenderer;

		if ( EyeSource is not null && EyeSource.IsValid() )
			AnimationHelper.EyeSource = EyeSource;

		var isHuman = PlayerRole.Team == PlayerTeam.Human;

		if ( !isHuman )
		{
			AnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.None;
			AnimationHelper.IsWeaponLowered = true;
			return;
		}

		AnimationHelper.HoldType = HumanHoldType;
		AnimationHelper.IsWeaponLowered = false;

		// Make the upper body aim in the player's facing direction.
		AnimationHelper.AimAngle = GameObject.WorldRotation.Angles();

		// Important: do NOT assign IkRightHand / IkLeftHand here.
		// We avoid IK feedback loop for now.
	}
}