using Sandbox;

public sealed class MutantAttack : Component
{
	[Property]
	public float AttackRange { get; set; } = 100f;

	[Property]
	public float AttackCooldown { get; set; } = 1f;

	[Property]
	public FirstPersonViewModel ViewModel { get; set; }

	[Property]
	public bool DebugLogs { get; set; } = true;

	private TimeUntil _nextAttackTime;

	protected override void OnStart()
	{
		_nextAttackTime = 0f;
		ViewModel ??= Components.Get<FirstPersonViewModel>();

		if ( DebugLogs )
		{
			Log.Info( $"MutantAttack started on {GameObject.Name}" );
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		var role = Components.Get<PlayerRole>();

		if ( role is null )
			return;

		if ( role.Team != PlayerTeam.Mutant )
			return;

		if ( !_nextAttackTime )
			return;

		if ( Input.Pressed( "attack1" ) )
		{
			ViewModel?.TriggerMeleeAttack();

			var start = GetAttackStartPosition();
			var direction = GetAttackDirection();

			RequestMutantAttack( start, direction );

			_nextAttackTime = AttackCooldown;
		}
	}

	[Rpc.Host]
	private void RequestMutantAttack( Vector3 start, Vector3 direction )
	{
		var role = Components.Get<PlayerRole>();

		if ( role is null )
			return;

		// Host validates that the attacker is really Mutant.
		if ( role.Team != PlayerTeam.Mutant )
			return;

		var end = start + direction.Normal * AttackRange;

		var result = Scene.Trace
			.Ray( start, end )
			.IgnoreGameObjectHierarchy( GameObject )
			.UseHitPosition()
			.Run();

		if ( DebugLogs )
		{
			Log.Info( $"HOST mutant attack from {GameObject.Name}. Hit: {result.Hit}, Object: {result.GameObject?.Name}" );
		}

		if ( !result.Hit )
			return;

		var targetRole = FindPlayerRoleFromHitObject( result.GameObject );

		if ( targetRole is null )
			return;

		if ( targetRole == role )
			return;

		if ( targetRole.Team != PlayerTeam.Human )
			return;

		targetRole.SetTeam( PlayerTeam.Mutant );

		ViewModel?.TriggerMeleeHit();

		if ( DebugLogs )
		{
			Log.Info( $"{GameObject.Name} infected {targetRole.GameObject.Name}" );
		}
	}

	private PlayerRole FindPlayerRoleFromHitObject( GameObject hitObject )
	{
		if ( hitObject is null )
			return null;

		var current = hitObject;

		while ( current is not null )
		{
			var role = current.Components.Get<PlayerRole>();

			if ( role is not null )
				return role;

			current = current.Parent;
		}

		return null;
	}

	private Vector3 GetAttackStartPosition()
	{
		if ( Scene.Camera is not null )
		{
			return Scene.Camera.WorldPosition;
		}

		return GameObject.WorldPosition + Vector3.Up * 64f;
	}

	private Vector3 GetAttackDirection()
	{
		if ( Scene.Camera is not null )
		{
			return Scene.Camera.WorldRotation.Forward;
		}

		return GameObject.WorldRotation.Forward;
	}
}