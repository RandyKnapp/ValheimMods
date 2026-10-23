using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetMaxDurability), typeof(int))]
    public static class ModifyDurability_ItemData_GetMaxDurability_Patch
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            // Hot path: GetMaxDurability is called from UI/HUD render loops for every item, every frame.
            // Durability mods come only from this item, and a magic/extended item always has at least one
            // m_customData entry, so mundane items can bail before touching the data manager.
            if (__instance.m_customData.Count == 0)
            {
                return;
            }

            if (__instance.IsMagic(out var magicItem) && magicItem.HasEffect(MagicEffectType.ModifyDurability, includeSocketed: true))
            {
                var totalDurabilityMod = magicItem.GetTotalEffectValue(MagicEffectType.ModifyDurability, 0.01f);
                __result *= 1.0f + totalDurabilityMod;
            }
        }
    }

    public static class ModifyDurability
    {
        /// <summary>
        /// The item's durability as a fraction of its maximum, capped at full. m_durability is an absolute
        /// amount while this effect scales the maximum, so enchanting and disenchanting carry durability
        /// across as this fraction instead. The cap matters: disenchanting used to keep the boosted amount
        /// against the lower mundane maximum, the next enchant read that as a fraction above 1 and scaled it
        /// by the new bonus, and repeating the pair grew durability without bound. The cap also brings an
        /// item that was already inflated that way back to full on its next enchant or disenchant.
        /// Returns null for an item that does not use durability.
        /// </summary>
        public static float? GetDurabilityFraction(ItemDrop.ItemData item)
        {
            // An Indestructible item reports m_useDurability false; what matters is the item underneath.
            if (!Indestructible.OriginallyUsesDurability(item))
            {
                return null;
            }

            float maxDurability = item.GetMaxDurability();
            return maxDurability > 0 ? Mathf.Clamp01(item.m_durability / maxDurability) : 1f;
        }

        /// <summary>Restores a fraction from <see cref="GetDurabilityFraction"/> against the item's current maximum.</summary>
        public static void SetDurabilityFraction(ItemDrop.ItemData item, float? fraction)
        {
            if (fraction.HasValue && Indestructible.OriginallyUsesDurability(item))
            {
                item.m_durability = fraction.Value * item.GetMaxDurability();
            }
        }
    }
}
