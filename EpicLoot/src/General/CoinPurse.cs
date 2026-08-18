using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace EpicLoot.General
{
    /// <summary>
    /// Shared read/spend helpers for the player's coin stash. Coins are located by drop-prefab name
    /// rather than display name because the display name is localized and can be gated at the world
    /// level; removal, however, goes by the item's own display name, which is what
    /// <see cref="Inventory.RemoveItem(string, int, int, bool)"/> matches on.
    /// </summary>
    internal static class CoinPurse
    {
        private const string CoinsPrefab = "Coins";

        /// <summary>Every coin stack in the player's inventory. Never null; empty when the player has none.</summary>
        public static List<ItemDrop.ItemData> GetCoinStacks(Player player)
        {
            if (player == null)
            {
                return new List<ItemDrop.ItemData>();
            }

            return player.GetInventory().GetAllItems()
                .Where(item => item.m_dropPrefab != null && item.m_dropPrefab.name == CoinsPrefab)
                .ToList();
        }

        /// <summary>Total coins across every stack.</summary>
        public static int GetTotalCoins(List<ItemDrop.ItemData> coinStacks)
        {
            return coinStacks == null ? 0 : coinStacks.Sum(stack => stack.m_stack);
        }

        /// <summary>Total coins across every stack in the player's inventory.</summary>
        public static int GetTotalCoins(Player player)
        {
            return GetTotalCoins(GetCoinStacks(player));
        }

        /// <summary>
        /// Removes <paramref name="amount"/> coins, using the stacks previously fetched by
        /// <see cref="GetCoinStacks"/> to resolve the localized item name. No-op when the amount is
        /// non-positive or the player holds no coins.
        /// </summary>
        public static void Spend(Player player, List<ItemDrop.ItemData> coinStacks, int amount)
        {
            if (player == null || amount <= 0 || coinStacks == null || coinStacks.Count == 0)
            {
                return;
            }

            player.GetInventory().RemoveItem(coinStacks[0].m_shared.m_name, amount, -1, false);
        }

        /// <summary>
        /// Gives <paramref name="amount"/> coins back to the player, dropping them at their feet if the
        /// inventory cannot take them so a refund is never silently lost. Mirrors
        /// InventoryManagement.GiveItem without its per-call debug logging, which would spam on a
        /// per-hit/per-kill path.
        /// </summary>
        public static void Refund(Player player, int amount)
        {
            if (player == null || amount <= 0)
            {
                return;
            }

            if (player.GetInventory().AddItem(CoinsPrefab, amount, 1, 0, 0, string.Empty) != null)
            {
                return;
            }

            var prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(CoinsPrefab) : null;
            if (prefab == null)
            {
                return;
            }

            var dropped = Object.Instantiate(prefab,
                player.transform.position + player.transform.forward + player.transform.up,
                player.transform.rotation);
            dropped.GetComponent<ItemDrop>()?.SetStack(amount);
        }
    }
}
