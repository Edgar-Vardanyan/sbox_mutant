using Sandbox;

public enum SpawnPointType
{
	Any,
	Human,
	Mutant
}

public sealed class SpawnPoint : Component
{
	[Property]
	public SpawnPointType Type { get; set; } = SpawnPointType.Any;
}