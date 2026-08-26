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

            var coal = ItsJustWood.GetCachedItem("Coal");
            if (coal == null)
                return;

            if (ItsJustWood.AllowAncientBarkForCoal.Value)
            {
                ItemDrop ancientWood = ItsJustWood.GetCachedItem("ElderBark");
                if (ancientWood != null && __instance.m_conversion.Find(x => x.m_from == ancientWood) == null)
                {
                    __instance.m_conversion.Add(new Smelter.ItemConversion()
                    {
                        m_from = ancientWood,
                        m_to = coal
                    });
                }
            }

            if (ItsJustWood.AllowYggdrasilWoodForCoal.Value)
            {
                ItemDrop yggdrasilWood = ItsJustWood.GetCachedItem("YggdrasilWood");
                if (yggdrasilWood != null && __instance.m_conversion.Find(x => x.m_from == yggdrasilWood) == null)
                {
                    __instance.m_conversion.Add(new Smelter.ItemConversion()
                    {
                        m_from = yggdrasilWood,
                        m_to = coal
                    });
                }
            }
        }
    }

    // NOTE: this patch currently never substitutes anything -- vanilla smelters/kilns burn Coal (or
    // nothing), and GetReplacementFuelItem only substitutes when the built-in fuel is Wood. Kept
    // deliberately (see also CookingStation_OnAddFuelSwitch_Patch) in case a station with wood fuel
    // appears; the gate lives in GetReplacementFuelItem.
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

        // Finalizer, not postfix: restore must run even when the original throws (see Fireplace patch).
        private static void Finalizer(Smelter __instance, ItemDrop __state)
        {
            if (__state == null)
                return;

            __instance.m_fuelItem = __state;
        }
    }
}