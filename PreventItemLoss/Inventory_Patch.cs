using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace PreventItemLoss
{
    [HarmonyPatch(typeof(Inventory), "Load", new Type[] { typeof(ZPackage) })]
    public static class Inventory_Load_Patch
    {
        private static readonly List<ItemDrop.ItemData> OutsideItems = new List<ItemDrop.ItemData>();
        private static readonly List<ItemDrop.ItemData> GraveItems = new List<ItemDrop.ItemData>();
        private static readonly List<ItemDrop.ItemData> DroppedItems = new List<ItemDrop.ItemData>();

        public static bool Prefix(Inventory __instance, ZPackage pkg)
        {
            var version = pkg.ReadInt();
            var itemCount = pkg.ReadInt();
            __instance.m_inventory.Clear();

            OutsideItems.Clear();
            GraveItems.Clear();
            DroppedItems.Clear();

            for (var index = 0; index < itemCount; ++index)
            {
                var name = pkg.ReadString();
                var stack = pkg.ReadInt();
                var durability = pkg.ReadSingle();
                var pos = pkg.ReadVector2i();
                var equiped = pkg.ReadBool();
                var quality = 1;
                if (version >= 101)
                    quality = pkg.ReadInt();
                var variant = 0;
                if (version >= 102)
                    variant = pkg.ReadInt();
                long crafterID = 0;
                var crafterName = "";
                if (version >= 103)
                {
                    crafterID = pkg.ReadLong();
                    crafterName = pkg.ReadString();
                }

                if (name != "")
                {
                    __instance.AddItem(name, stack, durability, pos, equiped, quality, variant, crafterID, crafterName);
                    if (IsOutsideInventory(__instance, pos))
                    {
                        Debug.LogWarning($"Item ({name}) was outside inventory ({pos}), finding new position.");
                        var item = __instance.GetItemAt(pos.x, pos.y);
                        OutsideItems.Add(item);
                    }
                }
            }

            foreach (var item in OutsideItems)
            {
                var addedItem = __instance.AddItem(item);
                if (!addedItem)
                {
                    Debug.LogWarning($"Could not add item ({item.m_shared.m_name}) to regular inventory, adding item to grave");
                    GraveItems.Add(item);
                }
            }

            if (GraveItems.Count > 0)
            {
                if (Player.m_localPlayer.GetInventory() == __instance)
                {
                    var graveInventory = CreateTempGrave(Player.m_localPlayer);
                    foreach (var item in GraveItems)
                    {
                        bool addedItem = graveInventory.AddItem(item);
                        if (!addedItem)
                        {
                            Debug.LogWarning($"Could not add item ({item.m_shared.m_name}) to temp grave, dropping on ground");
                            DroppedItems.Add(item);
                        }
                    }
                }
                else
                {
                    DroppedItems.AddRange(GraveItems);
                }
            }

            foreach (var item in DroppedItems)
            {
                if (Player.m_localPlayer.GetInventory() == __instance)
                {
                    Player.m_localPlayer.DropItem(__instance, item, item.m_stack);
                }
                else
                {
                    Debug.LogError($"Could not recover item from non-Player inventory ({__instance.GetName()}), dropping at origin?!?!");
                    ItemDrop.DropItem(item, item.m_stack, Vector3.zero + Vector3.up * 10, Quaternion.identity);
                }
            }

            __instance.Changed();
            return false;
        }

        private static bool IsOutsideInventory(Inventory instance, Vector2i pos)
        {
            if (pos.x < 0 || pos.x >= instance.GetWidth())
            {
                return true;
            }
            else if (pos.y < 0 || pos.y >= instance.GetHeight())
            {
                return true;
            }

            return false;
        }

        private static Inventory CreateTempGrave(Player player)
        {
            GameObject graveGO = UnityEngine.Object.Instantiate<GameObject>(player.m_tombstone, player.GetCenterPoint(), player.transform.rotation);
            TombStone grave = graveGO.GetComponent<TombStone>();
            PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
            string name = playerProfile.GetName();
            long playerId = playerProfile.GetPlayerID();
            grave.Setup(name, playerId);

            return graveGO.GetComponent<Container>().GetInventory();
        }
    }
}
