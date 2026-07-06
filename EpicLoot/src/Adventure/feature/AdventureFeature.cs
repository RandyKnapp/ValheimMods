using System;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace EpicLoot.Adventure.Feature
{
    public enum AdventureFeatureType
    {
        None,
        SecretStash,
        Gamble,
        TreasureMaps,
        Bounties
    }

    public abstract class AdventureFeature
    {
        public abstract AdventureFeatureType Type { get; }
        public abstract int RefreshInterval { get; }

        public int GetSecondsUntilRefresh()
        {
            return GetSecondsUntilIntervalRefresh(RefreshInterval);
        }

        public int GetCurrentInterval()
        {
            return GetCurrentInterval(RefreshInterval);
        }

        public Random GetRandom()
        {
            return GetRandomForInterval(GetCurrentInterval(), RefreshInterval);
        }

        public virtual void OnZNetStart()
        {
        }

        public virtual void OnZNetDestroyed()
        {
        }

        public virtual void OnWorldSave()
        {
        }

        protected static int GetSecondsUntilIntervalRefresh(int intervalDays)
        {
            if (ZNet.m_world == null || EnvMan.instance == null)
            {
                return -1;
            }

            var currentDay = EnvMan.instance.GetCurrentDay();
            var startOfNextInterval = GetNextMultiple(currentDay, intervalDays);
            var daysRemaining = (startOfNextInterval - currentDay) - EnvMan.instance.m_smoothDayFraction;
            return (int)(daysRemaining * EnvMan.instance.m_dayLengthSec);
        }

        protected static int GetNextMultiple(int n, int multiple)
        {
            // TODO: Disable features when interval is set to or less than 0.
            if (multiple == 0)
            {
                return 0;
            }

            return ((n / multiple) + 1) * multiple;
        }

        protected static int GetCurrentInterval(int intervalDays)
        {
            // TODO: Disable features when interval is set to or less than 0.
            if (intervalDays == 0)
            {
                return 0;
            }

            var currentDay = EnvMan.instance.GetCurrentDay();
            return currentDay / intervalDays;
        }

        private static int GetSeedForInterval(int currentInterval, int intervalDays)
        {
            var worldSeed = ZNet.m_world?.m_seed ?? 0;

            if (Player.m_localPlayer == null)
            {
                return 0;
            }

            var playerId = (int)(Player.m_localPlayer.GetPlayerID());
            return unchecked(worldSeed + playerId + currentInterval * 1000 + intervalDays * 100);
        }

        protected static Random GetRandomForInterval(int currentInterval, int intervalDays)
        {
            return new Random(GetSeedForInterval(currentInterval, intervalDays));
        }

        public static ItemDrop CreateItemDrop(string prefabName)
        {
            var itemPrefab = ObjectDB.instance.GetItemPrefab(prefabName);
            if (itemPrefab == null)
            {
                return null;
            }

            var itemDropPrefab = itemPrefab.GetComponent<ItemDrop>();
            if (itemDropPrefab == null)
            {
                return null;
            }

            ZNetView.m_forceDisableInit = true;
            var item = Object.Instantiate(itemDropPrefab);
            ZNetView.m_forceDisableInit = false;

            return item;
        }

        public static List<SecretStashItemInfo> CollectItems(List<SecretStashItemConfig> itemList)
        {
            return CollectItems(itemList, (x) => x.Item, (x) => true);
        }

        protected static List<SecretStashItemInfo> CollectItems(
            List<SecretStashItemConfig> itemList,
            Func<SecretStashItemConfig, string> itemIdPredicate,
            Func<ItemDrop.ItemData, bool> itemOkayToAddPredicate)
        {
            var results = new List<SecretStashItemInfo>();
            foreach (var itemConfig in itemList)
            {
                var itemId = itemIdPredicate(itemConfig);
                var itemDrop = CreateItemDrop(itemId);
                if (itemDrop == null)
                {
                    EpicLoot.LogWarning($"[AdventureData] Could not find item type (gated={itemId} orig={itemConfig}) in ObjectDB!");
                    continue;
                }

                var itemData = itemDrop.m_itemData;
                if (itemOkayToAddPredicate(itemData))
                {
                    results.Add(new SecretStashItemInfo(itemId, itemData, itemConfig.GetCost()));
                }
                ZNetScene.instance.Destroy(itemDrop.gameObject);
            }

            return results;
        }

        /// <summary>
        /// Randomly select N items from the list without duplicates.
        /// </summary>
        protected static void RollOnListNTimes<T>(Random random, List<T> list, int n, out List<T> results)
        {
            results = new List<T>();
            HashSet<int> indexes = new HashSet<int>();
            if (n > list.Count)
            {
                // Return all items
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    results.Add(item);
                    indexes.Add(i);
                }

                return;
            }

            int count = 0;

            while (count < n)
            {
                var index = random.Next(0, list.Count);
                if (!indexes.Contains(index))
                {
                    var item = list[index];
                    results.Add(item);
                    indexes.Add(index);
                    count++;
                }
            }
        }

        protected static void RollOnListNTimesUnique<T>(Random random, List<T> list, int n, List<T> results)
        {
            HashSet<int> indexes = new HashSet<int>();
            // Return all items
            if (n > list.Count)
            {
                for (int i = 0; i < list.Count; i++) {
                    var item = list[i];
                    results.Add(item);
                    indexes.Add(i);
                }
                return;
            }

            int count = 0;
            while (count < n) {
                var index = random.Next(0, list.Count);
                if (!indexes.Contains(index)) {
                    var item = list[index];
                    results.Add(item);
                    indexes.Add(index);
                    count++;
                }
            }
        }

        protected static T RollOnList<T>(Random random, List<T> list)
        {
            var index = random.Next(0, list.Count);
            return list[index];
        }

        protected static List<SecretStashItemInfo> SortListByRarity(List<SecretStashItemInfo> list)
        {
            return list.OrderBy(x=> x.Rarity).ToList();
        }
    }
}
