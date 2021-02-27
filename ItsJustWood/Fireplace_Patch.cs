using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace ItsJustWood
{
    //public bool Interact(Humanoid user, bool hold)
    [HarmonyPatch(typeof(Fireplace), "Interact")]
    public static class Fireplace_Interact_Patch
    {
        public static bool Prefix(Fireplace __instance, ref bool __result, Humanoid user, bool hold)
        {
            if (hold)
            {
                __result = false;
                return false;
            }

            if (!__instance.m_nview.HasOwner())
            {
                __instance.m_nview.ClaimOwnership();
            }

            Inventory inventory = user.GetInventory();
            if (inventory == null)
            {
                __result = true;
                return false;
            }

            var fuelItem = GetAvailableFuelItem(inventory, __instance.m_fuelItem.m_itemData.m_shared.m_name);
            Debug.Log($"Try adding: {fuelItem}");
            if (!string.IsNullOrEmpty(fuelItem))
            {
                if ((double)Mathf.CeilToInt(__instance.m_nview.GetZDO().GetFloat("fuel")) >= (double)__instance.m_maxFuel)
                {
                    user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_cantaddmore", __instance.m_fuelItem.m_itemData.m_shared.m_name));
                    __result = false;
                    return false;
                }
                user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_fireadding", fuelItem));
                inventory.RemoveItem(fuelItem, 1);
                __instance.m_nview.InvokeRPC("AddFuel", (object[])Array.Empty<object>());
                __result = true;
                return false;
            }
            user.Message(MessageHud.MessageType.Center, "$msg_outof " + __instance.m_fuelItem.m_itemData.m_shared.m_name);

            __result = false;
            return false;
        }

        public static string GetAvailableFuelItem(Inventory inventory, string builtIn)
        {
            if (inventory.HaveItem(builtIn))
            {
                return builtIn;
            }

            var fineWood = ObjectDB.instance.GetItemPrefab("FineWood").GetComponent<ItemDrop>();
            var coreWood = ObjectDB.instance.GetItemPrefab("RoundLog").GetComponent<ItemDrop>();
            var ancientBark = ObjectDB.instance.GetItemPrefab("ElderBark").GetComponent<ItemDrop>();
            if (ItsJustWood.AllowFineWoodForFuel.Value && inventory.HaveItem(fineWood.m_itemData.m_shared.m_name))
            {
                return fineWood.m_itemData.m_shared.m_name;
            }
            else if (ItsJustWood.AllowCoreWoodForFuel.Value && inventory.HaveItem(coreWood.m_itemData.m_shared.m_name))
            {
                return coreWood.m_itemData.m_shared.m_name;
            }
            else if (ItsJustWood.AllowAncientBarkForFuel.Value && inventory.HaveItem(ancientBark.m_itemData.m_shared.m_name))
            {
                return ancientBark.m_itemData.m_shared.m_name;
            }

            return string.Empty;
        }
    }
}
