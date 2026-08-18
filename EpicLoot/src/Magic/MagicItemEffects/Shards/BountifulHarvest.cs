using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Harvest yield more items when a pickable is harvested
    public static class BountifulHarvest {
        [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
        private static class Pickable_Interact_Patch {
            [UsedImplicitly]
            private static void Prefix(Pickable __instance, out bool __state) {
                __state = __instance.GetPicked();
            }

            [UsedImplicitly]
            private static void Postfix(Pickable __instance, Humanoid character, bool repeat, bool __state) {
                // Only a fresh pick by the local player (not a held-Use repeat).
                if (repeat || __state || __instance.GetPicked() == false || character != Player.m_localPlayer
                    || __instance.m_itemPrefab == null) {
                    return;
                }

                var chance = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.BountifulHarvest, 0.01f);
                if (chance <= 0f || Random.value >= chance) {
                    return;
                }

                var offset = Random.insideUnitCircle * 0.5f;
                var position = __instance.transform.position + Vector3.up + new Vector3(offset.x, 0f, offset.y);
                var rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
                ItemDrop.OnCreateNew(Object.Instantiate(__instance.m_itemPrefab, position, rotation));
            }
        }
    }
}
