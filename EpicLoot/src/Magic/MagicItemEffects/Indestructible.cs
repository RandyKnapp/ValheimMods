using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
    public static class Indestructible
    {
        private static readonly MethodInfo memberwiseCloner = AccessTools.DeclaredMethod(typeof(object), "MemberwiseClone");
        private static ItemDrop.ItemData.SharedData Clone(this ItemDrop.ItemData.SharedData sharedData) => (ItemDrop.ItemData.SharedData)memberwiseCloner.Invoke(sharedData, new object[]{});

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int))]
        public static class Indestructible_Inventory_AddItem_Patch
        {
            [UsedImplicitly]
            public static void Prefix(Inventory __instance, ref ItemDrop.ItemData item)
            {
                MakeItemIndestructible(item);
            }
        }

        public static bool MakeItemIndestructible(ItemDrop.ItemData item)
        {
            if (item.HasMagicEffect(MagicEffectType.Indestructible))
            {
                item.m_shared = item.m_shared.Clone();
                item.m_shared.m_useDurability = false;

                return true;
            }

            return false;
        }
    }
}
