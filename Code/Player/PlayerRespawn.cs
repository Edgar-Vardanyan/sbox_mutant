using Sandbox;

public sealed class PlayerRespawn : Component
{
	[Property]
	public bool DebugLogs { get; set; } = true;

	public void RespawnAt( Vector3 position, Rotation rotation )
	{
		// Set on host too.
		GameObject.WorldPosition = position;
		GameObject.WorldRotation = rotation;

		// Tell the owning client to also teleport.
		TeleportOwner( position, rotation );
	}

	[Rpc.Owner]
	private void TeleportOwner( Vector3 position, Rotation rotation )
	{
		GameObject.WorldPosition = position;
		GameObject.WorldRotation = rotation;

		if ( DebugLogs )
		{
			Log.Info( $"{GameObject.Name} teleported to spawn point." );
		}
	}
}