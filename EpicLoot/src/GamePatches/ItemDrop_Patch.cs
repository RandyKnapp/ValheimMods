using EpicLoot.Crafting;
using EpicLoot.LootBeams;
using EpicLoot.ShardStones;
using HarmonyLib;
using System.Runtime.CompilerServices;

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

    // Taking an item off an item stand re-instantiates it from the prefab and then loads the stand's saved
    // data over it. The magic follows by itself (MagicItemData reads a replaced entry on the next read); a
    // shard's data, which LoadFromZDO clears before repopulating, is rebuilt from m_shared.
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.LoadFromExternalZDO))]
    public static class ItemDrop_LoadFromExternalZDO_Patch
    {
        public static void Postfix(ItemDrop __instance)
        {
            Shards.EnsureShardMetadata(__instance.m_itemData);
        }
    }

    // ArmorStand cannot use LoadFromExternalZDO: it stores one item per slot, so it needs the indexed
    // overload and calls the static LoadFromZDO directly, which means the patch above never fires for it.
    // Only the indexed form needs this; index < 0 is ItemDrop.Load and LoadFromExternalZDO, both covered.
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.LoadFromZDO))]
    public static class ItemDrop_LoadFromZDO_Patch
    {
        public static void Postfix(ItemDrop.ItemData itemData, int index)
        {
            if (index < 0 || itemData?.m_shared == null)
            {
                return;
            }

            Shards.EnsureShardMetadata(itemData);
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Load), typeof(ZPackage))]
    public static class Inventory_Load_Patch
    {
        public static void Postfix(Inventory __instance)
        {
            foreach (ItemDrop.ItemData itemData in __instance.m_inventory)
            {
                itemData.InitializeCustomData();
            }
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Load))]
    public static class Container_Load_Patch
    {
        // m_lastRevision as of each container's last walk. Container.CheckForChanges calls Load once a
        // second on every loaded container and Load returns straight away unless the ZDO revision moved,
        // but a postfix runs either way, so this used to walk every item of every chest in range each
        // second. m_lastRevision moves on a reload and on every owner-side Save, so comparing it still
        // reaches items put straight into the inventory without a reload (the DropTable clones vanilla
        // AddDefaultItems adds) as soon as it used to.
        private static readonly ConditionalWeakTable<Container, StrongBox<uint>> WalkedRevision =
            new ConditionalWeakTable<Container, StrongBox<uint>>();

        public static void Postfix(Container __instance)
        {
            var walked = WalkedRevision.GetValue(__instance, _ => new StrongBox<uint>(uint.MaxValue));
            if (walked.Value == __instance.m_lastRevision)
            {
                return;
            }

            walked.Value = __instance.m_lastRevision;

            foreach (ItemDrop.ItemData itemData in __instance.m_inventory.m_inventory)
            {
                itemData.InitializeCustomData();
            }
        }
    }
}