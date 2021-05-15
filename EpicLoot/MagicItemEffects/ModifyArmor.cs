using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public float GetArmor(int quality) => this.m_shared.m_armor + (float) Mathf.Max(0, quality - 1) * this.m_shared.m_armorPerLevel;
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor), typeof(int))]
    public static class ModifyArmor_ItemData_GetArmor_Patch
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            var player = PlayerExtensions.GetPlayerWithEquippedItem(__instance);
	        var totalArmorMod = 0f;
	        ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyArmor, effect =>
	        {
			    totalArmorMod += MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, __instance, effect, 0.01f);
	        });
            __result *= 1.0f + totalArmorMod;
        }
    }

    //public void UpdateCharacterStats(Player player)
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCharacterStats), typeof(Player))]
    public static class ModifyArmor_InventoryGui_UpdateCharacterStats_Patch
    {
        public static void Postfix(InventoryGui __instance, Player player)
        {
            __instance.m_armor.text = player.GetBodyArmor().ToString("0.#");
        }
    }
}