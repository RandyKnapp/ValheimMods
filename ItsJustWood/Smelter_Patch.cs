using HarmonyLib;

namespace ItsJustWood
{
    [HarmonyPatch(typeof(Smelter), nameof(Smelter.Awake))]
    public static class Smelter_Awake_Patch
    {
        public static void Postfix(Smelter __instance)
        {
            if (!ItsJustWood.modEnabled.Value) 
                return;
        
            if (__instance.m_conversion.Find(x => x.m_from == ItsJustWood.wood) == null)
                return;

            if (ItsJustWood.AllowAncientBarkForCoal.Value)
            {
                ItemDrop ancientWood = ObjectDB.instance.GetItemPrefab("ElderBark").GetComponent<ItemDrop>();
                Smelter.ItemConversion itemConversion = __instance.m_conversion.Find(x => x.m_from == ancientWood);
                if (itemConversion == null)
                {
                    var coal = ObjectDB.instance.GetItemPrefab("Coal").GetComponent<ItemDrop>();
                    itemConversion = new Smelter.ItemConversion()
                    {
                        m_from = ancientWood,
                        m_to = coal
                    };
                    __instance.m_conversion.Add(itemConversion);
                }
            }

            if (ItsJustWood.AllowYggdrasilWoodForCoal.Value)
            {
                ItemDrop yggdrasilWood = ObjectDB.instance.GetItemPrefab("YggdrasilWood").GetComponent<ItemDrop>();
                Smelter.ItemConversion itemConversion = __instance.m_conversion.Find(x => x.m_from == yggdrasilWood);
                if (itemConversion == null)
                {
                    var coal = ObjectDB.instance.GetItemPrefab("Coal").GetComponent<ItemDrop>();
                    itemConversion = new Smelter.ItemConversion()
                    {
                        m_from = yggdrasilWood,
                        m_to = coal
                    };
                    __instance.m_conversion.Add(itemConversion);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnAddFuel))]
    public static class Smelter_OnAddFuel_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Smelter __instance, Humanoid user, ItemDrop.ItemData item, ref ItemDrop __state)
        {
            if (!ItsJustWood.modEnabled.Value) 
                return;

            if (item != null && item.m_shared.m_name == __instance.m_fuelItem.m_itemData.m_shared.m_name)
                return;

            ItemDrop itemFuelReplacement = ItsJustWood.GetReplacementFuelItem(user.GetInventory(), __instance.m_fuelItem);
            if (itemFuelReplacement == null)
                return;

            __state = __instance.m_fuelItem;

            __instance.m_fuelItem = itemFuelReplacement;
        }

        [HarmonyPriority(Priority.First)]
        private static void Postfix(Smelter __instance, ItemDrop __state)
        {
            if (!ItsJustWood.modEnabled.Value) 
                return;

            if (__state == null) 
                return;

            __instance.m_fuelItem = __state;
        }
    }
}
