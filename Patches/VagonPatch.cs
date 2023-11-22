using HarmonyLib;
using UnityEngine;

namespace SkilledCarryWeight.Patches
{
    [HarmonyPatch(typeof(Vagon))]
    internal static class VagonPatch
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

                mass = Mathf.Max(tempMass, mass * (1 - SkilledCarryWeight.MaxMassReduction.Value));
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Vagon.CanAttach))]
        private static bool CanAttachPrefix(Vagon __instance, GameObject go, ref bool __result)
        {
            if (!SkilledCarryWeight.AttachOutOfPlace.Value || __instance.transform.up.y < 0.1f || go != Player.m_localPlayer.gameObject)
            {
                __instance.m_detachDistance = SkilledCarryWeight.AttachDistance.Value;
                return true;
            }

            float distance = Vector3.Distance(go.transform.position + __instance.m_attachOffset, __instance.m_attachPoint.position);
            bool withinAttachDistance = distance < SkilledCarryWeight.AttachDistance.Value;
            __result = !Player.m_localPlayer.IsTeleporting() && !Player.m_localPlayer.InDodge() && withinAttachDistance;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Vagon.GetHoverText))]
        private static void GetHoverTextPostfix(Vagon __instance, ref string __result)
        {
            KeyCode cartKey = SkilledCarryWeight.QuickCartKey.Value;
            __result = Localization.instance.Localize($"{__instance.m_name}\n[<color=yellow>{cartKey}</color>] Quick Attach/Detach");
        }
    }
}