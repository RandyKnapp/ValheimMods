using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EquipmentAndQuickSlots
{
    public static class PlayerInventoryManager
    {
        public static Stack<string> FunctionStack = new Stack<string>();

        public static readonly List<string> UseCombinedInventory = new List<string>()
        {
            "UpdateInventoryWeight"
        };

        [HarmonyPatch(typeof(Humanoid), "GetInventory")]
        public static class Humanoid_GetInventory_Patch
        {
            public static bool Prefix(Humanoid __instance, ref Inventory __result)
            {
                if (IsPlayer(__instance, out var player) && FunctionStack.Count > 0)
                {
                    var currentFunction = FunctionStack.Peek();
                    var currentStack = string.Join(" > ", FunctionStack);
                    //Debug.LogWarning($"Getting inventory during: {currentStack}");
                    if (UseCombinedInventory.Contains(currentFunction))
                    {
                        __result = player.GetCombinedInventory();
                        //Debug.Log($"result: inventory.count={__result.m_inventory.Count} weight={__result.GetTotalWeight()}");
                    }
                    else
                    {
                        __result = player.m_inventory;
                    }

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateInventoryWeight")]
        public static class InventoryGui_UpdateInventoryWeight_Patch
        {
            public static bool Prefix() { return Push("UpdateInventoryWeight"); } 
            public static void Postfix() { Pop(); }
        }

        public static bool IsPlayer(Humanoid h, out Player player)
        {
            if (h.IsPlayer())
            {
                player = h as Player;
                return true;
            }

            player = null;
            return false;
        }

        public static bool Push(string functionName)
        {
            //Debug.LogWarning($"Pushed: {functionName}");
            FunctionStack.Push(functionName);
            return true;
        }

        public static void Pop()
        {
            FunctionStack.Pop();
        }
    }
}
