using EpicLoot.Crafting;
using EpicLoot.Data;
using EpicLoot.LootBeams;
using EpicLoot.ShardStones;
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
        }
    }

    // Only magic items get a LootBeam so its per-frame Update doesn't run on every non-magic drop.
    // This is gated in Start rather than Awake because most drop paths (fresh kills, player/container
    // drops via ItemDrop.DropItem, item stands via LoadFromExternalZDO) assign the magic data to
    // m_itemData after Awake has already run. By Start (the following frame) every path has finished
    // assigning magic, so IsMagic() is reliable here for all of them.
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Start))]
    public static class ItemDrop_Start_Patch
    {
        public static void Postfix(ItemDrop __instance)
        {
            if (__instance.m_itemData == null || !__instance.m_itemData.IsMagic())
            {
                return;
            }

            if (__instance.gameObject.GetComponent<LootBeam>() == null)
            {
                __instance.gameObject.AddComponent<LootBeam>();
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.LoadFromExternalZDO))]
    public static class ItemDrop_LoadFromExternalZDO_Patch
    {
        // Items taken from an item stand are re-instantiated from the prefab, so Awake/
        // InitializeCustomData caches an empty MagicItemComponent before LoadFromExternalZDO
        // reloads m_customData. Re-load the component so its MagicItem is deserialized from the
        // freshly loaded custom data instead of staying null (item would look un-enchanted).
        public static void Postfix(ItemDrop __instance)
        {
            __instance.m_itemData?.Data().Get<MagicItemComponent>()?.Load();

            // LoadFromZDO clears m_customData before repopulating it, so a shard taken off an item stand
            // can land here with its magic data gone. Its identity is in m_shared, so rebuild from that.
            Shards.EnsureShardMetadata(__instance.m_itemData);
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