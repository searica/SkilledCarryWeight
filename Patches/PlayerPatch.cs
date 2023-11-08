﻿using HarmonyLib;
using SkilledCarryWeight.Configs;
using UnityEngine;

namespace SkilledCarryWeight.Patches
{
    [HarmonyPatch(typeof(Player))]
    internal class PlayerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Player.GetMaxCarryWeight))]
        private static void GetMaxCarryWeight(Player __instance, ref float __result)
        {
            var skills = __instance.GetSkills();
            foreach (var skill in skills.m_skills)
            {
                if (Config.SkillConfigsMap.TryGetValue(skill.m_skill, out Config.SkillConfig skillConfig) && skillConfig.IsEnabled)
                {
                    __result += skillConfig.Coeff * Mathf.Pow(skills.GetSkillLevel(skill.m_skill), skillConfig.Pow);
                }
            }
        }
    }
}