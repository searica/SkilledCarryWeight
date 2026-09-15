using System;
using BepInEx.Configuration;
using HarmonyLib;

namespace ServerSync;

internal abstract class CustomSyncedValueBase
{
	public object? LocalBaseValue;

	public readonly ConfigSync Config;
	public readonly string Identifier;
	public readonly int Priority;
	public readonly Type? Type;
	public bool SynchronizedConfig = true;

	private bool localIsOwner = true;

	public abstract object? BoxedValue { get; set; }

	protected CustomSyncedValueBase(ConfigSync config, string identifier, Type type, int priority = 0)
	{
		Config = config;
		Identifier = identifier;
		Type = type;
		Priority = priority;
		config.SourceOfTruthChanged += delegate (bool truth)
		{
			localIsOwner = truth;
		};
	}

	public void WriteToPackage(ZPackage package)
	{
		AccessTools.DeclaredMethod(typeof(ZPackage), "Write", new Type[] { Type! })?.Invoke(package, new object[] { BoxedValue! });
	}
}
