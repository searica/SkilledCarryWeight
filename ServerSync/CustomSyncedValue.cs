using System;

namespace ServerSync;

internal class CustomSyncedValue<T> : CustomSyncedValueBase
{
	private bool localIsOwner = true;
	private object? boxedValue;

	public override object? BoxedValue
	{
		get => boxedValue;
		set => boxedValue = value;
	}

	public T Value
	{
		get => (T)boxedValue!;
		set => boxedValue = value;
	}

	public CustomSyncedValue(ConfigSync configSync, string identifier, T value = default!, int priority = 0)
		: base(configSync, identifier, typeof(T), priority)
	{
		Value = value;
	}

	public void AssignLocalValue(T value)
	{
		if (localIsOwner)
		{
			Value = value;
		}
		else
		{
			LocalBaseValue = value;
		}
	}
}
