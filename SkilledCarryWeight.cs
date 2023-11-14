﻿// Ignore Spelling: SkilledCarryWeight Jotunn

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using SkilledCarryWeight.Configs;
using BepInEx.Configuration;
using UnityEngine;
using System.Collections.Generic;
using SkilledCarryWeight.Extensions;
using System;

namespace SkilledCarryWeight
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, Jotunn.Main.Version)]
    internal class SkilledCarryWeight : BaseUnityPlugin
    {
        internal const string Author = "Searica";
        public const string PluginName = "SkilledCarryWeight";
        public const string PluginGUID = $"{Author}.Valheim.{PluginName}";
        public const string PluginVersion = "1.1.0";

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
        internal static ConfigEntry<float> MaxMassReduction;
        internal static ConfigEntry<float> MinCarryWeight;

        private static readonly string MainSection = ConfigManager.SetStringPriority("Global", 2);
        private static readonly string CartSection = ConfigManager.SetStringPriority("Cart", 1);
        private static bool SettingsUpdated = false;

        public void Awake()
        {
            Log.Init(Logger);

            ConfigManager.Init(PluginGUID, Config, false);
            Initialize();
            ConfigManager.Save();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);

            Game.isModded = true;

            ConfigManager.SetupWatcher();
            ConfigManager.CheckForConfigManager();
            ConfigManager.OnConfigWindowClosed += delegate
            {
                if (SettingsUpdated)
                {
                    ConfigManager.Save();
                    SettingsUpdated = false;
                }
            };
        }

        public void OnDestroy()
        {
            ConfigManager.Save();
        }

        private static void OnSettingChanged(object sender, EventArgs e)
        {
            if (!SettingsUpdated) { SettingsUpdated = true; }
        }

        /// <summary>
        ///     Set up configuration entries
        /// </summary>
        internal static void Initialize()
        {
            Log.Verbosity = ConfigManager.BindConfig(
                MainSection,
                "Verbosity",
                LogLevel.Low,
                "Low will log basic information about the mod. Medium will log information that " +
                "is useful for troubleshooting. High will log a lot of information, do not set " +
                "it to this without good reason as it will slow down your game.",
                synced: false
            );
            Log.Verbosity.SettingChanged += OnSettingChanged;

            EnableCartPatch = ConfigManager.BindConfig(
                CartSection,
                "CarryWeightAffectsCart",
                true,
                "Set to true/enabled to allow your max carry weight affect how easy carts are to pull by reducing the mass of carts you pull."
            );
            EnableCartPatch.SettingChanged += OnSettingChanged;

            CartPower = ConfigManager.BindConfig(
                CartSection,
                "Power",
                1f,
                "Affects how much your maximum carry weight making pulling carts easier. " +
                "Higher powers make your maximum carry weight reduce the mass of carts more.",
                new AcceptableValueRange<float>(0, 3)
            );
            CartPower.SettingChanged += OnSettingChanged;

            MaxMassReduction = ConfigManager.BindConfig(
                CartSection,
                "MaxMassReduction",
                0.7f,
                "Maximum reduction in cart mass due to increased max carry weight. Limits effective " +
                "cart mass to always be equal to or greater than Mass * (1 - MaxMassReduction)",
                new AcceptableValueRange<float>(0, 1)
            );
            MaxMassReduction.SettingChanged += OnSettingChanged;

            MinCarryWeight = ConfigManager.BindConfig(
                CartSection,
                "MinCarryWeight",
                300f,
                "Minimum value your maximum carry weight must be before it starts making carts easier to pull.",
                new AcceptableValueRange<float>(300, 1000)
            );
            MinCarryWeight.SettingChanged += OnSettingChanged;

            foreach (var skillType in Skills.s_allSkills)
            {
                if (skillType == Skills.SkillType.All) { continue; }

                var skillName = skillType.ToString();
                var skillConfig = new SkillConfig();

                skillConfig.enabledConfig = ConfigManager.BindConfig(
                    skillName,
                    ConfigManager.SetStringPriority("Enabled", 1),
                    GetDefaultEnabledValue(skillType),
                    "Set to true/enabled to allow this skill to increase your max carry weight."
                );
                skillConfig.enabledConfig.SettingChanged += OnSettingChanged;

                skillConfig.coeffConfig = ConfigManager.BindConfig(
                    skillName,
                    "Coefficient",
                    0.25f,
                    "Value to multiply the skill level by to determine how much extra carry weight it grants.",
                    new AcceptableValueRange<float>(0, 10)
                );
                skillConfig.coeffConfig.SettingChanged += OnSettingChanged;

                skillConfig.powConfig = ConfigManager.BindConfig(
                    skillName,
                    "Power",
                    1f,
                    "Power the skill level is raised to before multiplying by Coefficient to determine extra carry weight.",
                    new AcceptableValueRange<float>(0, 10)
                );
                skillConfig.powConfig.SettingChanged += OnSettingChanged;

                SkillConfigsMap[skillType] = skillConfig;
            }
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
    }

    /// <summary>
    ///     Log level to control output to BepInEx log
    /// </summary>
    internal enum LogLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    /// <summary>
    ///     Helper class for properly logging from static contexts.
    /// </summary>
    internal static class Log
    {
        #region Verbosity

        internal static ConfigEntry<LogLevel> Verbosity { get; set; }
        internal static LogLevel VerbosityLevel => Verbosity.Value;

        #endregion Verbosity

        internal static ManualLogSource _logSource;

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        internal static void LogDebug(object data) => _logSource.LogDebug(data);

        internal static void LogError(object data) => _logSource.LogError(data);

        internal static void LogFatal(object data) => _logSource.LogFatal(data);

        internal static void LogInfo(object data, LogLevel level = LogLevel.Low)
        {
            if (Verbosity is null || VerbosityLevel >= level)
            {
                _logSource.LogInfo(data);
            }
        }

        internal static void LogMessage(object data) => _logSource.LogMessage(data);

        internal static void LogWarning(object data) => _logSource.LogWarning(data);

        #region Logging Unity Objects

        internal static void LogGameObject(GameObject prefab, bool includeChildren = false)
        {
            LogInfo("***** " + prefab.name + " *****");
            foreach (Component compo in prefab.GetComponents<Component>())
            {
                LogComponent(compo);
            }

            if (!includeChildren) { return; }

            LogInfo("***** " + prefab.name + " (children) *****");
            foreach (Transform child in prefab.transform)
            {
                LogInfo($" - {child.gameObject.name}");
                foreach (Component compo in child.gameObject.GetComponents<Component>())
                {
                    LogComponent(compo);
                }
            }
        }

        internal static void LogComponent(Component compo)
        {
            LogInfo($"--- {compo.GetType().Name}: {compo.name} ---");

            PropertyInfo[] properties = compo.GetType().GetProperties(ReflectionUtils.AllBindings);
            foreach (var property in properties)
            {
                LogInfo($" - {property.Name} = {property.GetValue(compo)}");
            }

            FieldInfo[] fields = compo.GetType().GetFields(ReflectionUtils.AllBindings);
            foreach (var field in fields)
            {
                LogInfo($" - {field.Name} = {field.GetValue(compo)}");
            }
        }

        #endregion Logging Unity Objects
    }
}