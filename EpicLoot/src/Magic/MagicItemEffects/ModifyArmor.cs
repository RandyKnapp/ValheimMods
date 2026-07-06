using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor), typeof(int),typeof(float))]
    public static class ModifyArmor_ItemData_GetArmor_Patch
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            var player = PlayerExtensions.GetPlayerWithEquippedItem(__instance);

            // Apply this item's armor for just this item
            var totalArmorMod = __instance.GetMagicItem()?.GetTotalEffectValue(MagicEffectType.ModifyArmor, 0.01f) ?? 0f;

            // apply +armor from set bonuses globally
            if (player != null)
            {
                totalArmorMod += MagicEffectsHelper.GetTotalActiveSetEffectValue(player, MagicEffectType.ModifyArmor, 0.01f);
            }

            // Apply +armor (health critical) for all items
            ModifyWithLowHealth.ApplyOnlyForLowHealth(player, MagicEffectType.ModifyArmor, effect =>
            {
                totalArmorMod += MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, __instance, effect, 0.01f);
            });

            __result *= 1.0f + totalArmorMod;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCharacterStats), typeof(Player))]
    public static class ModifyArmor_InventoryGui_UpdateCharacterStats_Patch
    {
        public static void Postfix(InventoryGui __instance, Player player)
        {
            __instance.m_armor.text = player.GetBodyArmor().ToString("0.#");
        }
    }
}