using BepInEx.Configuration;

namespace ServerSync;

internal class SyncedConfigEntry<T> : OwnConfigEntryBase
{
	public readonly ConfigEntry<T> SourceConfig;

	public override ConfigEntryBase BaseConfig => SourceConfig;

	public T Value
	{
		get => SourceConfig.Value;
		set => SourceConfig.Value = value;
	}

	public SyncedConfigEntry(ConfigEntry<T> sourceConfig)
	{
		SourceConfig = sourceConfig;
	}

	public void AssignLocalValue(T value)
	{
		if (LocalBaseValue == null)
		{
			Value = value;
		}
		else
		{
			LocalBaseValue = value;
		}
	}
}
