using System;
using HarmonyLib;

namespace EquipmentAndQuickSlots
{
    [HarmonyPatch(typeof(Console), "InputText")]
    public static class Console_Patch
    {
        private static readonly System.Random _random = new System.Random();

        public static bool Prefix(Console __instance)
        {
            var input = __instance.m_input.text;
            var args = input.Split(' ');
            if (args.Length == 0 || !__instance.IsCheatsEnabled())
            {
                return true;
            }

            var command = args[0];
            if (command.Equals("resetinventory", StringComparison.InvariantCultureIgnoreCase))
            {
                Player.m_localPlayer.GetAllInventories().ForEach(x =>x.RemoveAll());
                return false;
            }

            if (command.Equals("breakequipment", StringComparison.InvariantCulture))
            {
                foreach (var inventory in Player.m_localPlayer.GetAllInventories())
                {
                    foreach (var item in inventory.m_inventory)
                    {
                        if (item.m_equiped && item.m_shared.m_useDurability)
                        {
                            item.m_durability = 0;
                        }
                    }
                }
                return false;
            }

            return true;
        }
    }
}
