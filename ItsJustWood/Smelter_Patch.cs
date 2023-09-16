using HarmonyLib;

namespace ItsJustWood
{
    [HarmonyPatch(typeof(Smelter), "Awake")]
    public static class Smelter_Awake_Patch
    {
        public static void Postfix(Smelter __instance)
        {
            if (ItsJustWood.AllowAncientBarkForCoal.Value)
            {
                var ancientWood = ObjectDB.instance.GetItemPrefab("ElderBark").GetComponent<ItemDrop>();
                var itemConversion = __instance.m_conversion.Find(x => x.m_from == ancientWood);
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
        }
    }
}
