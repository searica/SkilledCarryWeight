// Ignore Spelling: Jotunn

using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using SkilledCarryWeight.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkilledCarryWeight.Configs
{
    internal class ConfigManager
    {
        private static readonly string ConfigFileName = SkilledCarryWeight.PluginGUID + ".cfg";

        private static readonly string ConfigFileFullPath = string.Concat(
            Paths.ConfigPath,
            Path.DirectorySeparatorChar,
            ConfigFileName
        );

        private static ConfigFile configFile;
        private static BaseUnityPlugin ConfigurationManager;
        private const string ConfigManagerGUID = "com.bepis.bepinex.configurationmanager";

        #region Events

        /// <summary>
        ///     Event triggered after a the in-game configuration manager is closed.
        /// </summary>
        internal static event Action OnConfigWindowClosed;

        /// <summary>
        ///     Safely invoke the <see cref="OnConfigWindowClosed"/> event
        /// </summary>
        private static void InvokeOnConfigWindowClosed()
        {
            OnConfigWindowClosed?.SafeInvoke();
        }

        /// <summary>
        ///     Event triggered after the file watcher reloads the configuration file.
        /// </summary>
        internal static event Action OnConfigFileReloaded;

        /// <summary>
        ///     Safely invoke the <see cref="OnConfigFileReloaded"/> event
        /// </summary>
        private static void InvokeOnConfigFileReloaded()
        {
            OnConfigFileReloaded?.SafeInvoke();
        }

        #endregion Events

        #region LoggerLevel

        internal enum LoggerLevel
        {
            Low = 0,
            Medium = 1,
            High = 2,
        }

        internal static ConfigEntry<LoggerLevel> Verbosity { get; private set; }
        internal static LoggerLevel VerbosityLevel => Verbosity.Value;
        internal static bool IsVerbosityLow => Verbosity.Value >= LoggerLevel.Low;
        internal static bool IsVerbosityMedium => Verbosity.Value >= LoggerLevel.Medium;
        internal static bool IsVerbosityHigh => Verbosity.Value >= LoggerLevel.High;

        #endregion LoggerLevel

        #region BindConfig

        internal static ConfigEntry<T> BindConfig<T>(
            string section,
            string name,
            T value,
            string description,
            AcceptableValueBase acceptVals = null,
            bool synced = true
        )
        {
            string extendedDescription = GetExtendedDescription(description, synced);
            ConfigEntry<T> configEntry = configFile.Bind(
                section,
                name,
                value,
                new ConfigDescription(
                    extendedDescription,
                    acceptVals,
                    synced ? AdminConfig : ClientConfig
                )
            );
            return configEntry;
        }

        private static readonly ConfigurationManagerAttributes AdminConfig = new() { IsAdminOnly = true };
        private static readonly ConfigurationManagerAttributes ClientConfig = new() { IsAdminOnly = false };
        private const char ZWS = '\u200B';

        /// <summary>
        ///     Prepends Zero-Width-Space to set ordering of configuration sections
        /// </summary>
        /// <param name="sectionName">Section name</param>
        /// <param name="priority">Number of ZWS chars to prepend</param>
        /// <returns></returns>
        private static string SetStringPriority(string sectionName, int priority)
        {
            if (priority == 0) { return sectionName; }
            return new string(ZWS, priority) + sectionName;
        }

        internal static string GetExtendedDescription(string description, bool synchronizedSetting)
        {
            return description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]");
        }

        #endregion BindConfig

        internal static readonly Dictionary<Skills.SkillType, SkillConfig> SkillConfigsMap = new();

        internal class SkillConfig
        {
            public ConfigEntry<bool> enabledConfig;
            public ConfigEntry<float> coeffConfig;
            public ConfigEntry<float> powConfig;

            public bool IsEnabled => enabledConfig != null && enabledConfig.Value;

            public float Coeff => coeffConfig.Value;

            public float Pow => powConfig.Value;
        }

        internal static ConfigEntry<bool> EnableCartPatch;
        internal static ConfigEntry<float> CartPower;

        private static readonly string MainSection = SetStringPriority("Global", 2);
        private static readonly string CartSection = SetStringPriority("Cart", 1);

        internal static void Init(ConfigFile config)
        {
            configFile = config;
            configFile.SaveOnConfigSet = false;
        }

        internal static void SetUpConfig()
        {
            Verbosity = BindConfig(
                MainSection,
                "Verbosity",
                LoggerLevel.Low,
                "Low will log basic information about the mod. Medium will log information that " +
                "is useful for troubleshooting. High will log a lot of information, do not set " +
                "it to this without good reason as it will slow down your game.",
                synced: false
            );

            EnableCartPatch = BindConfig(
                CartSection,
                "CarryWeightAffectsCart",
                true,
                "Set to true/enabled to allow your max carry weight affect how easy carts are to pull."
            );

            CartPower = BindConfig(
                CartSection,
                "CartPower",
                1f,
                "Cart mass is calculated as \"new mass = (mass * 300 / mass carry weight) ^ CartPower\"."
            );

            foreach (var skillType in Skills.s_allSkills)
            {
                if (skillType == Skills.SkillType.All) { continue; }

                var skillName = skillType.ToString();
                var skillConfig = new SkillConfig();

                skillConfig.enabledConfig = BindConfig(
                    skillName,
                    SetStringPriority("Enabled", 1),
                    GetDefaultEnabledValue(skillType),
                    "Set to true/enabled to allow this skill to increase your max carry weight."
                );

                skillConfig.coeffConfig = BindConfig(
                    skillName,
                    "Coefficient",
                    0.25f,
                    "Value to multiply the skill level by to determine how much extra carry weight it grants.",
                    new AcceptableValueRange<float>(0, 10)
                );

                skillConfig.powConfig = BindConfig(
                    skillName,
                    "Power",
                    1f,
                    "Power the skill level is raised to before multiplying by Coefficient to determine extra carry weight.",
                    new AcceptableValueRange<float>(0, 10)
                );

                SkillConfigsMap[skillType] = skillConfig;
            }

            Save();
        }

        private static bool GetDefaultEnabledValue(Skills.SkillType skillType)
        {
            switch (skillType)
            {
                case Skills.SkillType.Run:
                    return true;

                case Skills.SkillType.Jump:
                    return true;

                case Skills.SkillType.Swim:
                    return true;

                case Skills.SkillType.WoodCutting:
                    return true;

                case Skills.SkillType.Pickaxes:
                    return true;

                case Skills.SkillType.Ride:
                    return true;

                case Skills.SkillType.Sneak:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        ///     Sets SaveOnConfigSet to false and returns
        ///     the value prior to calling this method.
        /// </summary>
        /// <returns></returns>
        private static bool DisableSaveOnConfigSet()
        {
            var val = configFile.SaveOnConfigSet;
            configFile.SaveOnConfigSet = false;
            return val;
        }

        /// <summary>
        ///     Set the value for the SaveOnConfigSet field.
        /// </summary>
        /// <param name="value"></param>
        internal static void SaveOnConfigSet(bool value)
        {
            configFile.SaveOnConfigSet = value;
        }

        /// <summary>
        ///     Save config file to disk.
        /// </summary>
        internal static void Save()
        {
            configFile.Save();
        }

        #region FileWatcher

        internal static void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReloadConfigFile;
            watcher.Created += ReloadConfigFile;
            watcher.Renamed += ReloadConfigFile;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private static void ReloadConfigFile(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) { return; }
            try
            {
                Log.LogInfo("Reloading config file");

                // turn off saving on config entry set
                var saveOnConfigSet = DisableSaveOnConfigSet();
                configFile.Reload();
                SaveOnConfigSet(saveOnConfigSet); // reset config saving state
                InvokeOnConfigFileReloaded(); // fire event
            }
            catch
            {
                Log.LogError($"There was an issue loading your {ConfigFileName}");
                Log.LogError("Please check your config entries for spelling and format!");
            }
        }

        #endregion FileWatcher

        #region ConfigManagerWindow

        /// <summary>
        ///     Checks for in-game configuration manager and
        ///     sets up OnConfigWindowClosed event if it is present
        /// </summary>
        internal static void CheckForConfigManager()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return;
            }

            if (Chainloader.PluginInfos.TryGetValue(ConfigManagerGUID, out PluginInfo configManagerInfo) && configManagerInfo.Instance)
            {
                ConfigurationManager = configManagerInfo.Instance;
                Log.LogDebug("Configuration manager found, hooking DisplayingWindowChanged");

                EventInfo eventinfo = ConfigurationManager.GetType().GetEvent("DisplayingWindowChanged");

                if (eventinfo != null)
                {
                    Action<object, object> local = new(OnConfigManagerDisplayingWindowChanged);
                    Delegate converted = Delegate.CreateDelegate(
                        eventinfo.EventHandlerType,
                        local.Target,
                        local.Method
                    );
                    eventinfo.AddEventHandler(ConfigurationManager, converted);
                }
            }
        }

        private static void OnConfigManagerDisplayingWindowChanged(object sender, object e)
        {
            PropertyInfo pi = ConfigurationManager.GetType().GetProperty("DisplayingWindow");
            bool ConfigurationManagerWindowShown = (bool)pi.GetValue(ConfigurationManager, null);

            if (!ConfigurationManagerWindowShown)
            {
                InvokeOnConfigWindowClosed();
            }
        }

        #endregion ConfigManagerWindow
    }
}