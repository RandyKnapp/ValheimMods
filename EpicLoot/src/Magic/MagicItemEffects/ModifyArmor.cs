using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor), typeof(int),typeof(float))]
    public static class ModifyArmor_ItemData_GetArmor_Patch
    {
        // The one GetArmor postfix: it is called for every worn piece on every damage calculation and HUD refresh,
        // and finding the wearer scans the players, so the shard armor effects run from here on that one lookup.
        // Every step multiplies, so their order does not matter.
        public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            var player = PlayerExtensions.GetPlayerWithEquippedItem(__instance);

            // This item's own armor bonus applies to this item only.
            var totalArmorMod = __instance.GetMagicItem()?.GetTotalEffectValue(MagicEffectType.ModifyArmor, 0.01f) ?? 0f;

            if (player != null)
            {
                // Set bonuses and the health critical variant apply to every piece.
                totalArmorMod += MagicEffectsHelper.GetTotalActiveSetEffectValue(player, MagicEffectType.ModifyArmor, 0.01f);
                if (ModifyWithLowHealth.PlayerHasLowHealth(player))
                {
                    totalArmorMod += MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, __instance,
                        ModifyWithLowHealth.EffectNameWithLowHealth(MagicEffectType.ModifyArmor), 0.01f);
                }
            }

            __result *= 1.0f + totalArmorMod;

            if (player != null)
            {
                Shards.DayArmor.ModifyArmor(player, ref __result);
                Shards.ArmorFromMovementPenalty.ModifyArmor(player, ref __result);
                Shards.ReduceArmorIncreaseDamage.ModifyArmor(player, ref __result);
            }
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