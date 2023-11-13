using HarmonyLib;
using UnityEngine;

namespace SkilledCarryWeight.Patches
{
    [HarmonyPatch(typeof(Vagon))]
    internal class VagonPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Vagon.SetMass))]
        private static void SetMassPrefix(Vagon __instance, ref float mass)
        {
            if (!SkilledCarryWeight.EnableCartPatch.Value) { return; }

            if (__instance?.m_nview == null || !__instance.m_nview.IsOwner()) { return; }

            if (__instance.IsAttached(Player.m_localPlayer))
            {
                var player = Player.m_localPlayer;

                var tempMass = mass * Mathf.Pow(
                    SkilledCarryWeight.MinCarryWeight.Value / player.GetMaxCarryWeight(),
                    SkilledCarryWeight.CartPower.Value
                );

                mass *= Mathf.Max(tempMass, mass * (1 - SkilledCarryWeight.MaxMassReduction.Value));
            }
        }
    }
}