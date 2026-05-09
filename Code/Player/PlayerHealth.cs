using Sandbox;

public sealed class PlayerHealth : Component
{
	[Sync( SyncFlags.FromHost ), Property]
	public int MaxHealth { get; set; } = 100;

	[Sync( SyncFlags.FromHost ), Property]
	public int CurrentHealth { get; set; } = 100;

	[Sync( SyncFlags.FromHost ), Property]
	public bool IsDead { get; set; }

	[Property]
	public bool DebugLogs { get; set; } = true;

	public System.Action<PlayerHealth> OnDied;

	protected override void OnStart()
	{
		ResetHealth();

		if ( DebugLogs )
		{
			Log.Info( $"PlayerHealth started on {GameObject.Name}. Health: {CurrentHealth}/{MaxHealth}" );
		}
	}

	public void ResetHealth()
	{
		CurrentHealth = MaxHealth;
		IsDead = false;
	}

	public void SetMaxHealth( int newMaxHealth, bool refillHealth = true )
	{
		MaxHealth = newMaxHealth;

		if ( refillHealth )
		{
			CurrentHealth = MaxHealth;
			IsDead = false;
		}
		else
		{
			CurrentHealth = CurrentHealth.Clamp( 0, MaxHealth );
			IsDead = CurrentHealth <= 0;
		}
	}

	public void TakeDamage( int damage )
	{
		if ( IsDead )
			return;

		if ( damage <= 0 )
			return;

		CurrentHealth -= damage;
		CurrentHealth = CurrentHealth.Clamp( 0, MaxHealth );

		if ( DebugLogs )
		{
			Log.Info( $"{GameObject.Name} took {damage} damage. Health: {CurrentHealth}/{MaxHealth}" );
		}

		if ( CurrentHealth <= 0 )
		{
			Die();
		}
	}

	public void Heal( int amount )
	{
		if ( IsDead )
			return;

		if ( amount <= 0 )
			return;

		CurrentHealth += amount;
		CurrentHealth = CurrentHealth.Clamp( 0, MaxHealth );

		if ( DebugLogs )
		{
			Log.Info( $"{GameObject.Name} healed {amount}. Health: {CurrentHealth}/{MaxHealth}" );
		}
	}

	private void Die()
	{
		IsDead = true;
		CurrentHealth = 0;

		if ( DebugLogs )
		{
			Log.Info( $"{GameObject.Name} died." );
		}

		OnDied?.Invoke( this );
	}

	[Button( "Test Damage 25" )]
	private void TestDamage25()
	{
		TakeDamage( 25 );
	}

	[Button( "Test Heal 25" )]
	private void TestHeal25()
	{
		Heal( 25 );
	}

	[Button( "Reset Health" )]
	private void TestResetHealth()
	{
		ResetHealth();
	}
}