using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace ExtendedItemDataFramework
{
    [BepInPlugin("randyknapp.mods.extendeditemdataframework", "Extended Item Data Framework", "1.0.0")]
    public class ExtendedItemDataFramework
    {
        private Harmony _harmony;

        private void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchAll();
        }

        public static void TryConvertItemPrefabs(ObjectDB objectDb)
        {
            foreach (var itemPrefab in objectDb.m_items)
            {
                var itemDrop = itemPrefab.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    if (!(itemDrop.m_itemData is ExtendedItemData))
                    {
                        itemDrop.m_itemData = new ExtendedItemData(itemDrop.m_itemData);
                    }
                }
            }
        }
    }
}
