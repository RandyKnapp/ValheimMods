using System;
using HarmonyLib;

namespace EquipmentAndQuickSlots
{
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    public static class Terminal_Patch
    {
        public static void Postfix()
        {
            if (Terminal.m_terminalInitialized)
                return;

            Terminal.ConsoleCommand resetinventory = new Terminal.ConsoleCommand("resetinventory", "Remove everything from every inventory", (args =>
            {
                Player.m_localPlayer.GetAllInventories().ForEach(x =>x.RemoveAll());
            }), true);

            Terminal.ConsoleCommand breakequipment = new Terminal.ConsoleCommand("breakequipment", "Break all the equipment in your inventory", (args =>
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
            }), true);
        }
    }
}
