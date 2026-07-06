using System.Collections.Generic;
using System.Linq;
using static ItemDrop;

namespace EpicLoot.General
{
    internal static class Extensions
    {
        /// <summary>
        /// Take any list of Objects and return it with Fischer-Yates shuffle
        /// </summary>
        /// <returns></returns>
        public static List<T> shuffleList<T>(this List<T> inputList)
        {
            T p = default;
            List<T> tempList = new List<T>();
            tempList.AddRange(inputList);
            int count = inputList.Count;
            for (int i = 0; i < count; i++)
            {
                int r = UnityEngine.Random.Range(i, count);
                p = tempList[i];
                tempList[i] = tempList[r];
                tempList[r] = p;
            }
            return tempList;
        }

        public static bool EpicLootHasElementalDamage(this ItemDrop.ItemData item)
        {
            return item.m_shared.m_damages.m_fire +
                item.m_shared.m_damages.m_frost +
                item.m_shared.m_damages.m_lightning +
                item.m_shared.m_damages.m_poison +
                item.m_shared.m_damages.m_spirit > 0;
        }

        public static float EpicLootGetTotalDamage(this HitData.DamageTypes damage)
        {
            return damage.GetTotalDamage() - damage.m_chop - damage.m_pickaxe;
        }

        public static float EpicLootGetTotalDamageAgainstPlayer(this HitData.DamageTypes damage)
        {
            return damage.GetTotalDamage() - damage.m_chop - damage.m_pickaxe - damage.m_spirit;
        }
    }
}