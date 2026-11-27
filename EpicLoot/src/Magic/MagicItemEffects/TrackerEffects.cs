using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipmentStatusEffects))]
    public class TrackerEffects_Humanoid_UpdateEquipmentStatusEffects_Patch
    {
        [UsedImplicitly]
        public static void Postfix(Humanoid __instance)
        {
            if (__instance is not Player player)
            {
                return;
            }

            EquipmentEffectCache.Reset(player);
            Apply(player, MagicEffectType.Andvaranaut, "Andvaranaut");
            Apply(player, MagicEffectType.SkuldsThread, "SkuldsThread");
        }

        // Kept out of m_equipmentStatusEffects: vanilla removes every entry there that no worn item's own status effect
        // provides on each equipment change, so an entry of ours would be removed and added back, replaying its start
        // message, on every swap. One still listed there belongs to the worn Andvaranaut or Skuld's Thread item itself.
        // From the item prefab rather than ObjectDB.GetStatusEffect: these status effects are only added to the main
        // menu's ObjectDB, and the world's ObjectDB never gets them.
        private static void Apply(Player player, string effectType, string itemPrefabName)
        {
            StatusEffect statusEffect = ObjectDB.instance.GetItemPrefab(itemPrefabName)?.GetComponent<ItemDrop>()?
                .m_itemData.m_shared.m_equipStatusEffect;
            if (statusEffect == null)
            {
                return;
            }

            bool active = player.m_seman.HaveStatusEffect(statusEffect.NameHash());
            if (player.HasActiveMagicEffect(effectType, out float _))
            {
                if (!active)
                {
                    player.m_seman.AddStatusEffect(statusEffect);
                }
            }
            else if (active && !player.m_equipmentStatusEffects.Contains(statusEffect))
            {
                player.m_seman.RemoveStatusEffect(statusEffect.NameHash());
            }
        }
    }
}
