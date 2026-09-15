using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace ServerSync;

[HarmonyPatch]
internal class VersionCheck
{
	private static readonly HashSet<VersionCheck> versionChecks;

	private static readonly Dictionary<string, string> notProcessedNames;

	public string Name;

	private string? displayName;

	private string? currentVersion;

	private string? minimumRequiredVersion;

	public bool ModRequired = true;

	private string? ReceivedCurrentVersion;

	private string? ReceivedMinimumRequiredVersion;

	private readonly List<ZRpc> ValidatedClients = new List<ZRpc>();

	private ConfigSync? ConfigSync;

	public string DisplayName
	{
		get
		{
			return displayName ?? Name;
		}
		set
		{
			displayName = value;
		}
	}

	public string CurrentVersion
	{
		get
		{
			return currentVersion ?? "0.0.0";
		}
		set
		{
			currentVersion = value;
		}
	}

	public string MinimumRequiredVersion
	{
		get
		{
			return minimumRequiredVersion ?? (ModRequired ? CurrentVersion : "0.0.0");
		}
		set
		{
			minimumRequiredVersion = value;
		}
	}

	private static void PatchServerSync()
	{
		Patches patchInfo = PatchProcessor.GetPatchInfo(AccessTools.DeclaredMethod(typeof(ZNet), "Awake", (Type[])null, (Type[])null));
		if (patchInfo != null && Enumerable.Count(patchInfo.Postfixes, (Patch p) => p.PatchMethod.DeclaringType == typeof(ConfigSync.RegisterRPCPatch)) > 0)
		{
			return;
		}
		Harmony val = new Harmony("org.bepinex.helpers.ServerSync");
		foreach (Type item in Enumerable.Where(Enumerable.Concat(typeof(ConfigSync).GetNestedTypes(BindingFlags.NonPublic), new Type[1] { typeof(VersionCheck) }), (Type t) => t.IsClass))
		{
			val.PatchAll(item);
		}
	}

	static VersionCheck()
	{
		versionChecks = new HashSet<VersionCheck>();
		notProcessedNames = new Dictionary<string, string>();
		typeof(ThreadingHelper).GetMethod("StartSyncInvoke").Invoke(ThreadingHelper.Instance, new object[1]
		{
			new Action(PatchServerSync)
		});
	}

	public VersionCheck(string name)
	{
		Name = name;
		ModRequired = true;
		versionChecks.Add(this);
	}

	public VersionCheck(ConfigSync configSync)
	{
		ConfigSync = configSync;
		Name = ConfigSync.Name;
		versionChecks.Add(this);
	}

	public void Initialize()
	{
		ReceivedCurrentVersion = null;
		ReceivedMinimumRequiredVersion = null;
		if (ConfigSync != null)
		{
			Name = ConfigSync.Name;
			DisplayName = ConfigSync.DisplayName;
			CurrentVersion = ConfigSync.CurrentVersion;
			MinimumRequiredVersion = ConfigSync.MinimumRequiredVersion;
			ModRequired = ConfigSync.ModRequired;
		}
	}

	private bool IsVersionOk()
	{
		if (ReceivedMinimumRequiredVersion == null || ReceivedCurrentVersion == null)
		{
			return !ModRequired;
		}
		bool flag = new System.Version(CurrentVersion) >= new System.Version(ReceivedMinimumRequiredVersion);
		bool flag2 = new System.Version(ReceivedCurrentVersion) >= new System.Version(MinimumRequiredVersion);
		return flag && flag2;
	}

	private string ErrorClient()
	{
		if (ReceivedMinimumRequiredVersion == null)
		{
			return DisplayName + " is not installed on the server.";
		}
		return (new System.Version(CurrentVersion) >= new System.Version(ReceivedMinimumRequiredVersion)) ? (DisplayName + " may not be higher than version " + ReceivedCurrentVersion + ". You have version " + CurrentVersion + ".") : (DisplayName + " needs to be at least version " + ReceivedMinimumRequiredVersion + ". You have version " + CurrentVersion + ".");
	}

	private string ErrorServer(ZRpc rpc)
	{
		return "Disconnect: The client (" + rpc.GetSocket().GetHostName() + ") doesn't have the correct " + DisplayName + " version " + MinimumRequiredVersion;
	}

	private string Error(ZRpc? rpc = null)
	{
		return (rpc == null) ? ErrorClient() : ErrorServer(rpc);
	}

	private static VersionCheck[] GetFailedClient()
	{
		return Enumerable.ToArray(Enumerable.Where(versionChecks, (VersionCheck check) => !check.IsVersionOk()));
	}

	private static VersionCheck[] GetFailedServer(ZRpc rpc)
	{
		ZRpc rpc2 = rpc;
		return Enumerable.ToArray(Enumerable.Where(versionChecks, (VersionCheck check) => check.ModRequired && !check.ValidatedClients.Contains(rpc2)));
	}

	private static void Logout()
	{
		Game.instance.Logout(true, true);
		AccessTools.DeclaredField(typeof(ZNet), "m_connectionStatus").SetValue(null, (object)(ZNet.ConnectionStatus)3);
	}

	private static void DisconnectClient(ZRpc rpc)
	{
		rpc.Invoke("Error", new object[1] { 3 });
	}

	private static void CheckVersion(ZRpc rpc, ZPackage pkg)
	{
		CheckVersion(rpc, pkg, null);
	}

	private static void CheckVersion(ZRpc rpc, ZPackage pkg, Action<ZRpc, ZPackage>? original)
	{
		string text = pkg.ReadString();
		string text2 = pkg.ReadString();
		string text3 = pkg.ReadString();
		bool flag = false;
		foreach (VersionCheck versionCheck in versionChecks)
		{
			if (!(text != versionCheck.Name))
			{
				Debug.Log((object)("Received " + versionCheck.DisplayName + " version " + text3 + " and minimum version " + text2 + " from the " + (ZNet.instance.IsServer() ? "client" : "server") + "."));
				versionCheck.ReceivedMinimumRequiredVersion = text2;
				versionCheck.ReceivedCurrentVersion = text3;
				if (ZNet.instance.IsServer() && versionCheck.IsVersionOk())
				{
					versionCheck.ValidatedClients.Add(rpc);
				}
				flag = true;
			}
		}
		if (flag)
		{
			return;
		}
		pkg.SetPos(0);
		if (original != null)
		{
			original(rpc, pkg);
			if (pkg.GetPos() == 0)
			{
				notProcessedNames.Add(text, text3);
			}
		}
	}

	[HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
	[HarmonyPrefix]
	private static bool RPC_PeerInfo(ZRpc rpc, ZNet __instance)
	{
		VersionCheck[] array = (__instance.IsServer() ? GetFailedServer(rpc) : GetFailedClient());
		if (array.Length == 0)
		{
			return true;
		}
		VersionCheck[] array2 = array;
		foreach (VersionCheck versionCheck in array2)
		{
			Debug.LogWarning((object)versionCheck.Error(rpc));
		}
		if (__instance.IsServer())
		{
			DisconnectClient(rpc);
		}
		else
		{
			Logout();
		}
		return false;
	}

	[HarmonyPatch(typeof(ZNet), "OnNewConnection")]
	[HarmonyPrefix]
	private static void RegisterAndCheckVersion(ZNetPeer peer, ZNet __instance)
	{
		notProcessedNames.Clear();
		IDictionary dictionary = (IDictionary)typeof(ZRpc).GetField("m_functions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(peer.m_rpc);
		if (dictionary.Contains(StringExtensionMethods.GetStableHashCode("ServerSync VersionCheck")))
		{
			object obj = dictionary[StringExtensionMethods.GetStableHashCode("ServerSync VersionCheck")];
			Action<ZRpc, ZPackage> action = (Action<ZRpc, ZPackage>)obj.GetType().GetField("m_action", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
			peer.m_rpc.Register<ZPackage>("ServerSync VersionCheck", (Action<ZRpc, ZPackage>)delegate(ZRpc rpc, ZPackage pkg)
			{
				CheckVersion(rpc, pkg, action);
			});
		}
		else
		{
			peer.m_rpc.Register<ZPackage>("ServerSync VersionCheck", (Action<ZRpc, ZPackage>)CheckVersion);
		}
		foreach (VersionCheck versionCheck in versionChecks)
		{
			versionCheck.Initialize();
			if (versionCheck.ModRequired || __instance.IsServer())
			{
				Debug.Log((object)("Sending " + versionCheck.DisplayName + " version " + versionCheck.CurrentVersion + " and minimum version " + versionCheck.MinimumRequiredVersion + " to the " + (__instance.IsServer() ? "client" : "server") + "."));
				ZPackage val = new ZPackage();
				val.Write(versionCheck.Name);
				val.Write(versionCheck.MinimumRequiredVersion);
				val.Write(versionCheck.CurrentVersion);
				peer.m_rpc.Invoke("ServerSync VersionCheck", new object[1] { val });
			}
		}
	}

	[HarmonyPatch(typeof(ZNet), "Disconnect")]
	[HarmonyPrefix]
	private static void RemoveDisconnected(ZNetPeer peer, ZNet __instance)
	{
		if (!__instance.IsServer())
		{
			return;
		}
		foreach (VersionCheck versionCheck in versionChecks)
		{
			versionCheck.ValidatedClients.Remove(peer.m_rpc);
		}
	}

	[HarmonyPatch(typeof(FejdStartup), "ShowConnectError")]
	[HarmonyPostfix]
	private static void ShowConnectionError(FejdStartup __instance)
	{
		if (!__instance.m_connectionFailedPanel.activeSelf || (int)ZNet.GetConnectionStatus() != 3)
		{
			return;
		}
		bool flag = false;
		VersionCheck[] failedClient = GetFailedClient();
		if (failedClient.Length != 0)
		{
			string text = string.Join("\n", Enumerable.Select(failedClient, (VersionCheck check) => check.Error()));
			TMP_Text connectionFailedError = __instance.m_connectionFailedError;
			connectionFailedError.text = connectionFailedError.text + "\n" + text;
			flag = true;
		}
		foreach (KeyValuePair<string, string> item in Enumerable.OrderBy<KeyValuePair<string, string>, string>(notProcessedNames, (KeyValuePair<string, string> kv) => kv.Key))
		{
			if (!__instance.m_connectionFailedError.text.Contains(item.Key))
			{
				TMP_Text connectionFailedError2 = __instance.m_connectionFailedError;
				connectionFailedError2.text = connectionFailedError2.text + "\nServer expects you to have " + item.Key + " (Version: " + item.Value + ") installed.";
				flag = true;
			}
		}
		if (flag)
		{
			RectTransform component = ((Component)__instance.m_connectionFailedPanel.transform.Find("Image")).GetComponent<RectTransform>();
			Vector2 sizeDelta = component.sizeDelta;
			sizeDelta.x = 675f;
			component.sizeDelta = sizeDelta;
			__instance.m_connectionFailedError.ForceMeshUpdate(false, false);
			float num = __instance.m_connectionFailedError.renderedHeight + 105f;
			RectTransform component2 = ((Component)((Component)component).transform.Find("ButtonOk")).GetComponent<RectTransform>();
			component2.anchoredPosition = new Vector2(component2.anchoredPosition.x, component2.anchoredPosition.y - (num - component.sizeDelta.y) / 2f);
			sizeDelta = component.sizeDelta;
			sizeDelta.y = num;
			component.sizeDelta = sizeDelta;
		}
	}
}
