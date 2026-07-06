using EpicLoot.Crafting;
using EpicLoot.LootBeams;
using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Awake))]
    public static class ItemDrop_Awake_Patch
    {
        public static void Postfix(ItemDrop __instance)
        {
            if (__instance.m_itemData == null)
            {
                return;
            }

            __instance.m_itemData.InitializeCustomData();

            if (__instance.gameObject.GetComponent<LootBeam>() == null)
            {
                __instance.gameObject.AddComponent<LootBeam>();
            }
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Load))]
    public static class Inventory_Load_Patch
    {
        public static void Postfix(Inventory __instance)
        {
            foreach (ItemDrop.ItemData itemData in __instance.m_inventory)
            {
                if (itemData.IsMagicCraftingMaterial())
                {
                    itemData.CreateMagicItem();
                }

                itemData.InitializeCustomData();
            }
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Load))]
    public static class Container_Load_Patch
    {
        public static void Postfix(Container __instance)
        {
            foreach (ItemDrop.ItemData itemData in __instance.m_inventory.m_inventory)
            {
                if (itemData.IsMagicCraftingMaterial())
                {
                    itemData.CreateMagicItem();
                }

                itemData.InitializeCustomData();
            }
        }
    }
}