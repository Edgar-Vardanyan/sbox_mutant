using Sandbox;

public enum PlayerTeam
{
	Human,
	Mutant,
	Spectator
}

public sealed class PlayerRole : Component
{
	[Sync( SyncFlags.FromHost ), Property]
	public PlayerTeam Team { get; set; } = PlayerTeam.Human;

	[Property]
	public bool DebugLogs { get; set; } = true;

	[Property]
	public float HumanWalkSpeed { get; set; } = 190f;

	[Property]
	public float HumanRunSpeed { get; set; } = 320f;

	[Property]
	public float MutantWalkSpeed { get; set; } = 260f;

	[Property]
	public float MutantRunSpeed { get; set; } = 420f;

	[Property]
	public float SpectatorWalkSpeed { get; set; } = 0f;

	[Property]
	public float SpectatorRunSpeed { get; set; } = 0f;

	protected override void OnStart()
	{
		if ( DebugLogs )
		{
			Log.Info( $"PlayerRole started on {GameObject.Name}. Current team: {Team}" );
		}

		ApplyRole();
	}

	[Button( "Set Human" )]
	public void SetHuman()
	{
		SetTeam( PlayerTeam.Human );
	}

	[Button( "Set Mutant" )]
	public void SetMutant()
	{
		SetTeam( PlayerTeam.Mutant );
	}

	[Button( "Set Spectator" )]
	public void SetSpectator()
	{
		SetTeam( PlayerTeam.Spectator );
	}

	public void SetTeam( PlayerTeam newTeam )
	{
		if ( Team == newTeam )
			return;

		Team = newTeam;
		ApplyRole();

		if ( DebugLogs )
		{
			Log.Info( $"{GameObject.Name} changed team to {Team}" );
		}
	}

	private void ApplyRole()
	{
		switch ( Team )
		{
			case PlayerTeam.Human:
				ApplyHumanRole();
				break;

			case PlayerTeam.Mutant:
				ApplyMutantRole();
				break;

			case PlayerTeam.Spectator:
				ApplySpectatorRole();
				break;
		}
	}

	private void ApplyHumanRole()
	{
		var health = Components.Get<PlayerHealth>();

		if ( health is not null )
		{
			health.SetMaxHealth( 100 );
		}

		ApplyMovementSpeed( HumanWalkSpeed, HumanRunSpeed );
		ApplyMovementSpeedOwner( HumanWalkSpeed, HumanRunSpeed );

		if ( DebugLogs )
		{
			Log.Info( $"{GameObject.Name} applied Human role." );
		}
	}

	private void ApplyMutantRole()
	{
		var health = Components.Get<PlayerHealth>();

		if ( health is not null )
		{
			health.SetMaxHealth( 3000 );
		}

		ApplyMovementSpeed( MutantWalkSpeed, MutantRunSpeed );
		ApplyMovementSpeedOwner( MutantWalkSpeed, MutantRunSpeed );

		if ( DebugLogs )
		{
			Log.Info( $"{GameObject.Name} applied Mutant role." );
		}
	}

	private void ApplySpectatorRole()
	{
		var health = Components.Get<PlayerHealth>();

		if ( health is not null )
		{
			health.SetMaxHealth( 1 );
		}

		ApplyMovementSpeed( SpectatorWalkSpeed, SpectatorRunSpeed );
		ApplyMovementSpeedOwner( SpectatorWalkSpeed, SpectatorRunSpeed );

		if ( DebugLogs )
		{
			Log.Info( $"{GameObject.Name} applied Spectator role." );
		}
	}

	private void ApplyMovementSpeed( float walkSpeed, float runSpeed )
	{
		var controller = Components.Get<PlayerController>();

		if ( controller is null )
		{
			if ( DebugLogs )
			{
				Log.Warning( $"{GameObject.Name} has no PlayerController. Cannot apply movement speed." );
			}

			return;
		}

		controller.WalkSpeed = walkSpeed;
		controller.RunSpeed = runSpeed;
	}

	[Rpc.Owner]
	private void ApplyMovementSpeedOwner( float walkSpeed, float runSpeed )
	{
		ApplyMovementSpeed( walkSpeed, runSpeed );

		if ( DebugLogs )
		{
			Log.Info( $"{GameObject.Name} owner applied movement speed. Walk: {walkSpeed}, Run: {runSpeed}" );
		}
	}
}