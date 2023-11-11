using HarmonyLib;
using UnityEngine;
using SkilledCarryWeight.Configs;

namespace SkilledCarryWeight.Patches
{
    [HarmonyPatch(typeof(Vagon))]
    internal class VagonPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Vagon.SetMass))]
        private static void SetMassPrefix(Vagon __instance, ref float mass)
        {
            if (!ConfigManager.EnableCartPatch.Value) { return; }

            if (__instance?.m_nview == null || !__instance.m_nview.IsOwner()) { return; }

            if (__instance.IsAttached(Player.m_localPlayer))
            {
                var player = Player.m_localPlayer;

                mass *= Mathf.Pow(player.m_maxCarryWeight / player.GetMaxCarryWeight(), ConfigManager.CartPower.Value);
            }
        }
    }
}