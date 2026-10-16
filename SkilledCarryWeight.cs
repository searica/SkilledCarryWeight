// Ignore Spelling: SkilledCarryWeight Jotunn

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Extensions;
using Jotunn.Managers;
using Jotunn.Utils;
using Logging;
using System;
using System.Collections.Generic;
using System.Reflection;


namespace SkilledCarryWeight {

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, Jotunn.Main.Version)]
    [NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Patch)]
    [SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
    internal sealed class SkilledCarryWeight : BaseUnityPlugin {
        internal const string Author = "Searica";
        public const string PluginName = "SkilledCarryWeight";
        public const string PluginGUID = $"{Author}.Valheim.{PluginName}";
        public const string PluginVersion = "1.6.0";

        internal static SkilledCarryWeight Instance;
        internal static ConfigFileWatcher ConfigWatcher;

        internal static readonly Dictionary<Skills.SkillType, SkillConfig> SkillConfigsMap = new();

        internal class SkillConfig {
            public ConfigEntry<bool> enabledConfig;
            public ConfigEntry<float> coeffConfig;
            public ConfigEntry<float> powConfig;
            public bool IsEnabled => enabledConfig.Value;
            public float Coeff => coeffConfig.Value;
            public float Pow => powConfig.Value;
        }

        internal static ConfigEntry<bool> EnableCartPatch;
        internal static ConfigEntry<float> CartPower;
        internal static ConfigEntry<float> MaxMassReduction;
        internal static ConfigEntry<float> MinCarryWeight;
        internal static ConfigEntry<float> BaseCarryWeight;


        private const string MainSection = "Global";
        private const  string CartSection = "Cart Mass";

        private static bool SettingsUpdated = false;

        public void Awake() {
            Instance = this;
            Log.Init(Logger);

            Config.SaveOnConfigSet = false;
            Initialize();
            Config.Save();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);

            Game.isModded = true;

            ConfigWatcher = new ConfigFileWatcher(Config);

            // Update tools if config file reloaded
            ConfigWatcher.OnConfigFileReloaded += () =>
            {
                if (SettingsUpdated)
                {
                    Config.Save();
                    SettingsUpdated = false;
                }
            };

            // Update tools if in-game config manager window is closed
            SynchronizationManager.OnConfigurationWindowClosed += () =>
            {
                if (SettingsUpdated)
                {
                    Config.Save();
                    SettingsUpdated = false;
                }
            };

            // Update tools if in-game config manager window is closed
            SynchronizationManager.OnConfigurationSynchronized += (obj, args) =>
            {
                if (SettingsUpdated)
                {
                    Config.Save();
                    SettingsUpdated = false;
                }
            };
        }

        public void OnDestroy() {
            Config.Save();
        }

        private static void OnSettingChanged(object sender, EventArgs e) {
            if (!SettingsUpdated) { SettingsUpdated = true; }
        }

        /// <summary>
        ///     Set up configuration entries
        /// </summary>
        internal static void Initialize() {
            Log.Verbosity = Instance.Config.BindConfigInOrder(
                MainSection,
                "Verbosity",
                Log.InfoLevel.Low,
                "Low will log basic information about the mod. Medium will log information that " +
                "is useful for troubleshooting. High will log a lot of information, do not set " +
                "it to this without good reason as it will slow down your game.",
                synced: false
            );
            Log.Verbosity.SettingChanged += OnSettingChanged;

            BaseCarryWeight = Instance.Config.BindConfigInOrder(
                MainSection,
                "BaseCarryWeight",
                300f,
                "Base maximum carry weight for the player. Vanilla value is 300.",
                acceptableValues: new AcceptableValueRange<float>(1f, 1000f),
                synced: true
            );

            BaseCarryWeight.SettingChanged += OnSettingChanged;

            EnableCartPatch = Instance.Config.BindConfigInOrder(
                CartSection,
                "CarryWeightAffectsCart",
                true,
                "Set to true/enabled to allow your max carry weight affect how easy carts are to pull by reducing the mass of carts you pull.",
                synced: true
            );
            EnableCartPatch.SettingChanged += OnSettingChanged;

            CartPower = Instance.Config.BindConfigInOrder(
                CartSection,
                "Power",
                1f,
                "Affects how much your maximum carry weight making pulling carts easier. " +
                "Higher powers make your maximum carry weight reduce the mass of carts more.",
                acceptableValues: new AcceptableValueRange<float>(0, 3),
                synced: true
            );
            CartPower.SettingChanged += OnSettingChanged;

            MaxMassReduction = Instance.Config.BindConfigInOrder(
                CartSection,
                "MaxMassReduction",
                0.7f,
                "Maximum reduction in cart mass due to increased max carry weight. Limits effective " +
                "cart mass to always be equal to or greater than Mass * (1 - MaxMassReduction)",
                acceptableValues: new AcceptableValueRange<float>(0, 1),
                synced: true
            );
            MaxMassReduction.SettingChanged += OnSettingChanged;

            MinCarryWeight = Instance.Config.BindConfigInOrder(
                CartSection,
                "MinCarryWeightForCartReduction",
                300f,
                "Minimum value your maximum carry weight must be before it starts making carts easier to pull.",
                acceptableValues: new AcceptableValueRange<float>(300, 1000),
                synced: true
            );
            MinCarryWeight.SettingChanged += OnSettingChanged;


            foreach (var skillType in Skills.s_allSkills) {
                if (skillType == Skills.SkillType.All) { continue; }

                var skillName = skillType.ToString();
                var skillConfig = new SkillConfig();

                skillConfig.enabledConfig = Instance.Config.BindConfigInOrder(
                    skillName,
                    "Enabled",
                    GetDefaultEnabledValue(skillType),
                    "Set to true/enabled to allow this skill to increase your max carry weight.",
                    synced: true
                );
                skillConfig.enabledConfig.SettingChanged += OnSettingChanged;

                skillConfig.coeffConfig = Instance.Config.BindConfigInOrder(
                    skillName,
                    "Coefficient",
                    0.25f,
                    "Value to multiply the skill level by to determine how much extra carry weight it grants.",
                    acceptableValues: new AcceptableValueRange<float>(0, 10),
                    synced: true
                );
                skillConfig.coeffConfig.SettingChanged += OnSettingChanged;

                skillConfig.powConfig = Instance.Config.BindConfigInOrder(
                    skillName,
                    "Power",
                    1f,
                    "Power the skill level is raised to before multiplying by Coefficient to determine extra carry weight.",
                    acceptableValues: new AcceptableValueRange<float>(0, 10),
                    synced: true
                );
                skillConfig.powConfig.SettingChanged += OnSettingChanged;

                SkillConfigsMap[skillType] = skillConfig;
            }
        }

        private static bool GetDefaultEnabledValue(Skills.SkillType skillType) {
            switch (skillType) {
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

                case Skills.SkillType.Dodge:
                    return true;

                case Skills.SkillType.Farming:
                    return true;

                default:
                    return false;
            }
        }
    }
}
