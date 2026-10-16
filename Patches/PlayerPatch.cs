using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SkilledCarryWeight.Patches
{
    [HarmonyPatch(typeof(Player))]
    internal static class PlayerPatch
    {
        private static Skills skills;
        private static Dictionary<Skills.SkillType, Skills.Skill> skillData;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Player.GetMaxCarryWeight))]
        private static void GetMaxCarryWeight(Player __instance, ref float __result)
        {
            // Replace vanilla base carry weight with the configured value.
            __result = SkilledCarryWeight.BaseCarryWeight.Value;

            skills = __instance.GetSkills();
            skillData = skills.m_skillData;

            if (skillData == null)
            {
                return;
            }

            foreach (var skill in skills.m_skills)
            {
                if (SkilledCarryWeight.SkillConfigsMap.TryGetValue(skill.m_skill, out SkilledCarryWeight.SkillConfig skillConfig) && skillConfig.IsEnabled)
                {
                    if (!skillData.TryGetValue(skill.m_skill, out Skills.Skill actualSkill))
                    {
                        continue;
                    }

                    __result += skillConfig.Coeff * Mathf.Pow(actualSkill.m_level, skillConfig.Pow);

                    if (float.IsInfinity(__result))
                    {
                        __result = float.MaxValue;
                        return;
                    }
                }
            }
        }
    }
}