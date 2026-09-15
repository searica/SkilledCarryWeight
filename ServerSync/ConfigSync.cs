using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ServerSync;

internal class ConfigSync
{
	private class SnatchCurrentlyHandlingRPC
	{
		public static ZRpc? currentRpc;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ZRpc), "HandlePackage")]
		private static void Prefix(ZRpc __instance)
		{
			currentRpc = __instance;
		}
	}

	[HarmonyPatch(typeof(ZNet), "Awake")]
	internal static class RegisterRPCPatch
	{
		[HarmonyPostfix]
		private static void Postfix(ZNet __instance)
		{
			isServer = __instance.IsServer();
			foreach (ConfigSync configSync2 in configSyncs)
			{
				ZRoutedRpc.instance.Register<ZPackage>(configSync2.Name + " ConfigSync", (Action<long, ZPackage>)configSync2.RPC_FromOtherClientConfigSync);
				if (isServer)
				{
					configSync2.InitialSyncDone = true;
					Debug.Log($"Registered '{configSync2.Name} ConfigSync' RPC - waiting for incoming connections");
				}
			}
			if (isServer)
			{
				((MonoBehaviour)__instance).StartCoroutine(WatchAdminListChanges());
			}
			static void SendAdmin(List<ZNetPeer> peers, bool isAdmin)
			{
				ZPackage package = ConfigsToPackage(null, null, new PackageEntry[1]
				{
					new PackageEntry
					{
						section = "Internal",
						key = "lockexempt",
						type = typeof(bool),
						value = isAdmin
					}
				});
				ConfigSync configSync = configSyncs.First();
				if (configSync != null)
				{
					((MonoBehaviour)ZNet.instance).StartCoroutine(configSync.sendZPackage(peers, package));
				}
			}
			static IEnumerator WatchAdminListChanges()
			{
				MethodInfo listContainsId = AccessTools.DeclaredMethod(typeof(ZNet), "ListContainsId", (Type[])null, (Type[])null);
				SyncedList adminList = (SyncedList)AccessTools.DeclaredField(typeof(ZNet), "m_adminList").GetValue(ZNet.instance);
				List<string> CurrentList = new List<string>(adminList.GetList());
				while (true)
				{
					yield return new WaitForSeconds(30f);
					if (!adminList.GetList().SequenceEqual(CurrentList))
					{
						CurrentList = new List<string>(adminList.GetList());
						List<ZNetPeer> adminPeer = ZNet.instance.GetPeers().Where(delegate (ZNetPeer p)
						{
							string hostName = p.m_rpc.GetSocket().GetHostName();
							return listContainsId == null ? adminList.Contains(hostName) : (bool)listContainsId.Invoke(ZNet.instance, new object[2] { adminList, hostName });
						}).ToList();
						List<ZNetPeer> nonAdminPeer = ZNet.instance.GetPeers().Except(adminPeer).ToList();
						SendAdmin(nonAdminPeer, isAdmin: false);
						SendAdmin(adminPeer, isAdmin: true);
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(ZNet), "OnNewConnection")]
	private static class RegisterClientRPCPatch
	{
		[HarmonyPostfix]
		private static void Postfix(ZNet __instance, ZNetPeer peer)
		{
			if (__instance.IsServer())
			{
				return;
			}
			foreach (ConfigSync configSync in configSyncs)
			{
				peer.m_rpc.Register<ZPackage>(configSync.Name + " ConfigSync", (Action<ZRpc, ZPackage>)configSync.RPC_FromServerConfigSync);
			}
		}
	}

	private class ParsedConfigs
	{
		public Dictionary<string, OwnConfigEntryBase> configEntries = new();
		public Dictionary<string, CustomSyncedValueBase> customValues = new();
	}

	private class PackageEntry
	{
		public string section = "";
		public string key = "";
		public Type? type;
		public object? value;
	}

	[HarmonyPatch(typeof(ZNet), "Shutdown")]
	private static class ResetConfigsOnShutdown
	{
		[HarmonyPostfix]
		private static void Postfix()
		{
			ProcessingServerUpdate = true;
			foreach (ConfigSync configSync in configSyncs)
			{
				configSync.resetConfigsFromServer();
				configSync.IsSourceOfTruth = true;
				configSync.InitialSyncDone = false;
			}
			ProcessingServerUpdate = false;
		}
	}

	private static bool ProcessingServerUpdate;
	private static readonly HashSet<ConfigSync> configSyncs;
	private static bool isServer;
	private static bool lockExempt;
	private static long packageCounter;

	public string Name;
	public bool IsSourceOfTruth = true;
	public bool InitialSyncDone;
	public string DisplayName = "";
	public string CurrentVersion = "";
	public string MinimumRequiredVersion = "";
	public bool ModRequired;
	private OwnConfigEntryBase? lockedConfig;
	private Action lockedConfigChanged = delegate { };

	internal event Action<bool> SourceOfTruthChanged = delegate { };

	static ConfigSync()
	{
		ProcessingServerUpdate = false;
		configSyncs = new HashSet<ConfigSync>();
		lockExempt = false;
		packageCounter = 0L;
		RuntimeHelpers.RunClassConstructor(typeof(VersionCheck).TypeHandle);
	}

	public ConfigSync(string name)
	{
		Name = name;
		configSyncs.Add(this);
		new VersionCheck(this);
	}

	private bool isLockExempt(ZRpc rpc)
	{
		if (lockExempt)
		{
			return true;
		}
		if (isServer)
		{
			SyncedList adminList = (SyncedList)AccessTools.DeclaredField(typeof(ZNet), "m_adminList").GetValue(ZNet.instance);
			MethodInfo listContainsId = AccessTools.DeclaredMethod(typeof(ZNet), "ListContainsId", (Type[])null, (Type[])null);
			string hostName = rpc.GetSocket().GetHostName();
			return listContainsId == null ? adminList.Contains(hostName) : (bool)listContainsId.Invoke(ZNet.instance, new object[2] { adminList, hostName });
		}
		return false;
	}

	private readonly List<OwnConfigEntryBase> allConfigs = new();

	private OwnConfigEntryBase configData(ConfigEntryBase config)
	{
		return allConfigs.First(x => x.BaseConfig == config);
	}

	public SyncedConfigEntry<T> AddConfigEntry<T>(ConfigEntry<T> configEntry)
	{
		OwnConfigEntryBase? existing = allConfigs.FirstOrDefault(x => x.BaseConfig == configEntry);
		SyncedConfigEntry<T>? syncedEntry = existing as SyncedConfigEntry<T>;
		if (syncedEntry == null)
		{
			syncedEntry = new SyncedConfigEntry<T>(configEntry);
			configEntry.SettingChanged += delegate
			{
				if (!ProcessingServerUpdate && syncedEntry.SynchronizedConfig)
				{
					Broadcast(ZRoutedRpc.Everybody, configEntry);
				}
			};
			allConfigs.Add(syncedEntry);
		}
		return syncedEntry;
	}

	public SyncedConfigEntry<T> AddLockingConfigEntry<T>(ConfigEntry<T> lockingConfig) where T : IConvertible
	{
		if (lockedConfig != null)
		{
			throw new Exception("Cannot initialize locking ConfigEntry twice");
		}
		lockedConfig = AddConfigEntry<T>(lockingConfig);
		lockingConfig.SettingChanged += delegate
		{
			lockedConfigChanged?.Invoke();
		};
		return (SyncedConfigEntry<T>)lockedConfig;
	}

	private void RPC_FromOtherClientConfigSync(long sender, ZPackage package)
	{
		if (isServer)
		{
			HandleConfigSyncRPC(sender, package, clientUpdate: true);
		}
	}

	private void RPC_FromServerConfigSync(ZRpc rpc, ZPackage package)
	{
		lockedConfigChanged += serverLockedSettingChanged;
		IsSourceOfTruth = false;
		if (HandleConfigSyncRPC(0L, package, clientUpdate: false))
		{
			InitialSyncDone = true;
		}
	}

	private void serverLockedSettingChanged()
	{
		if (lockedConfig != null)
		{
			((SyncedConfigEntry<IConvertible>)lockedConfig).AssignLocalValue(((SyncedConfigEntry<IConvertible>)lockedConfig).Value);
		}
	}

	private bool HandleConfigSyncRPC(long sender, ZPackage package, bool clientUpdate)
	{
		ParsedConfigs parsedConfigs = ParseConfigs(package);

		foreach (var kv in parsedConfigs.configEntries.Where(x => x.Value.SynchronizedConfig))
		{
			OwnConfigEntryBase config = kv.Value;
			if (config is SyncedConfigEntry<IConvertible> syncedConfig)
			{
				if (config == lockedConfig && !isLockExempt(SnatchCurrentlyHandlingRPC.currentRpc!))
				{
					continue;
				}
				syncedConfig.AssignLocalValue(syncedConfig.Value);
			}
		}

		if (clientUpdate && isServer)
		{
			Broadcast(ZRoutedRpc.Everybody, package);
		}

		return true;
	}

	private ParsedConfigs ParseConfigs(ZPackage package)
	{
		ParsedConfigs parsedConfigs = new();
		int count = package.ReadInt();
		for (int i = 0; i < count; i++)
		{
			string section = package.ReadString();
			string key = package.ReadString();
			string typeName = package.ReadString();
			Type? valueType = Type.GetType(typeName);
			object? value = null;
			if (valueType != null)
			{
				MethodInfo? readMethod = typeof(ZPackage).GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.FirstOrDefault(m => m.Name == "Read" && m.ReturnType == valueType && m.GetParameters().Length == 0);
				if (readMethod != null)
				{
					value = readMethod.Invoke(package, null);
				}
			}

			string configKey = section + "." + key;
			OwnConfigEntryBase? config = allConfigs.FirstOrDefault(x =>
				x.BaseConfig.Definition.Section == section && x.BaseConfig.Definition.Key == key);
			if (config != null)
			{
				parsedConfigs.configEntries[configKey] = config;
			}
		}
		return parsedConfigs;
	}

	private static ZPackage ConfigsToPackage(IEnumerable<ConfigEntryBase>? configs = null, IEnumerable<CustomSyncedValueBase>? customValues = null, IEnumerable<PackageEntry>? packageEntries = null, bool partial = true)
	{
		ZPackage package = new ZPackage();
		package.Write(partial ? 1 : 0);
		List<ConfigEntryBase> configList = configs?.ToList() ?? new List<ConfigEntryBase>();
		List<CustomSyncedValueBase> customList = customValues?.ToList() ?? new List<CustomSyncedValueBase>();
		package.Write(configList.Count + customList.Count + (packageEntries?.Count() ?? 0));
		foreach (ConfigEntryBase config in configList)
		{
			package.Write(config.Definition.Section);
			package.Write(config.Definition.Key);
			package.Write(config.SettingType.FullName ?? "");
			AccessTools.DeclaredMethod(typeof(ZPackage), "Write", new Type[] { config.SettingType })?.Invoke(package, new object[] { config.BoxedValue });
		}
		foreach (CustomSyncedValueBase custom in customList)
		{
			package.Write("CustomSyncedValue");
			package.Write(custom.Identifier);
			package.Write(custom.Type?.FullName ?? "");
			custom.WriteToPackage(package);
		}
		if (packageEntries != null)
		{
			foreach (PackageEntry entry in packageEntries)
			{
				package.Write(entry.section);
				package.Write(entry.key);
				package.Write(entry.type?.FullName ?? "");
				AccessTools.DeclaredMethod(typeof(ZPackage), "Write", new Type[] { entry.type! })?.Invoke(package, new object[] { entry.value! });
			}
		}
		return package;
	}

	private void Broadcast(long target, ConfigEntryBase config)
	{
		Broadcast(target, ConfigsToPackage(new ConfigEntryBase[1] { config }));
	}

	private void Broadcast(long target, ZPackage package)
	{
		if (ZRoutedRpc.instance == null)
		{
			return;
		}
		if (isServer)
		{
			ZRoutedRpc.instance.InvokeRoutedRPC(target, Name + " ConfigSync", package);
		}
		else
		{
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Name + " ConfigSync", package);
		}
	}

	private void BroadcastToPeers(List<ZNetPeer> peers, ZPackage package)
	{
		foreach (ZNetPeer peer in peers)
		{
			peer.m_rpc.Invoke(Name + " ConfigSync", package);
		}
	}

	private IEnumerator sendZPackage(List<ZNetPeer> peers, ZPackage package)
	{
		yield return null;
		BroadcastToPeers(peers, package);
	}

	private void resetConfigsFromServer()
	{
		foreach (OwnConfigEntryBase config in allConfigs)
		{
			if (config.SynchronizedConfig && config.LocalBaseValue != null)
			{
				if (config is SyncedConfigEntry<IConvertible> syncedConfig)
				{
					syncedConfig.AssignLocalValue(syncedConfig.Value);
				}
				config.LocalBaseValue = null;
			}
		}
	}
}
