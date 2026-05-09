using Sandbox;

public sealed class PlayerCameraSetupInstaller : Component
{
	[Property]
	public bool DebugLogs { get; set; } = false;

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		var camera = Scene.Camera;

		if ( camera is null )
			return;

		var existing = camera.Components.Get<CameraSetup>();

		if ( existing is not null )
			return;

		camera.Components.Create<CameraSetup>();

		if ( DebugLogs )
		{
			Log.Info( $"CameraSetup added to active camera: {camera.GameObject.Name}" );
		}
	}
}