using BepInEx.Configuration;

namespace ServerSync;

internal abstract class OwnConfigEntryBase
{
	public object? LocalBaseValue;

	public bool SynchronizedConfig = true;

	public abstract ConfigEntryBase BaseConfig { get; }
}
