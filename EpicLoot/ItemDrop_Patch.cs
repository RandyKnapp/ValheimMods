using EpicLoot.LootBeams;
using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch(typeof(ItemDrop), "Awake")]
    public static class ItemDrop_Awake_Patch
    {
        public static void Postfix(ItemDrop __instance)
        {
            if (__instance.m_itemData.IsMagic())
            {
                __instance.gameObject.AddComponent<LootBeam>();
            }
        }
    }
}
