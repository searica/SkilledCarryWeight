// Ignore Spelling: SkilledCarryWeight Jotunn

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace SkilledCarryWeight
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, Jotunn.Main.Version)]
    internal class SkilledCarryWeight : BaseUnityPlugin
    {
        internal const string Author = "Searica";
        public const string PluginName = "SkilledCarryWeight";
        public const string PluginGUID = $"{Author}.Valheim.{PluginName}";
        public const string PluginVersion = "1.0.0";

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        //public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private Harmony _harmony;

        public void Awake()
        {
            Log.Init(Logger);

            Configs.Config.Init(Config);
            Configs.Config.SetUpConfig();

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);

            Game.isModded = true;

            Configs.Config.SetupWatcher();
            Configs.Config.CheckForConfigManager();
            Configs.Config.OnConfigWindowClosed += () => { Configs.Config.Save(); };
        }

        public void OnDestroy()
        {
            Configs.Config.Save();
            _harmony?.UnpatchSelf();
        }
    }

    /// <summary>
    /// Helper class for properly logging from static contexts.
    /// </summary>
    internal static class Log
    {
        internal static ManualLogSource _logSource;

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        internal static void LogDebug(object data) => _logSource.LogDebug(data);

        internal static void LogError(object data) => _logSource.LogError(data);

        internal static void LogFatal(object data) => _logSource.LogFatal(data);

        internal static void LogInfo(object data) => _logSource.LogInfo(data);

        internal static void LogMessage(object data) => _logSource.LogMessage(data);

        internal static void LogWarning(object data) => _logSource.LogWarning(data);
    }
}