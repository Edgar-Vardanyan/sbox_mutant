using Sandbox;

public sealed class PlayerHitFeedback : Component
{
	[Property]
	public float HitMarkerDuration { get; set; } = 0.15f;

	[Property]
	public bool DebugLogs { get; set; } = false;

	public bool ShouldShowHitMarker => _hitMarkerTimeLeft > 0f;

	private float _hitMarkerTimeLeft;

	protected override void OnUpdate()
	{
		if ( _hitMarkerTimeLeft > 0f )
		{
			_hitMarkerTimeLeft -= Time.Delta;
		}
	}

	public void TriggerHitMarker()
	{
		ShowHitMarkerOwner();
	}

	[Rpc.Owner]
	private void ShowHitMarkerOwner()
	{
		_hitMarkerTimeLeft = HitMarkerDuration;

		if ( DebugLogs )
		{
			Log.Info( $"{GameObject.Name} received hit marker feedback." );
		}
	}
}