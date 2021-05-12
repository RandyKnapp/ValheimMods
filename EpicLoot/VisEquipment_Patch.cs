using HarmonyLib;
using UnityEngine;

namespace EpicLoot
{
    public enum FxAttachMode
    {
        None,
        Player,
        ItemRoot,
        EquipRoot
    }

    [HarmonyPatch]
    public static class VisEquipment_Patch
    {
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.AttachItem))]
        [HarmonyPostfix]
        public static void AttachItem_Postfix(VisEquipment __instance, GameObject __result, int itemHash)
        {
            if (!CanCreateEffect(__instance, itemHash, out var player, out var equippedItem))
            {
                return;
            }

            var equipFx = GetEquipFxName(equippedItem, out var mode);
            if (!string.IsNullOrEmpty(equipFx))
            {
                var asset = EpicLoot.LoadAsset<GameObject>(equipFx);
                if (asset != null)
                {
                    var attachObject = mode == FxAttachMode.Player ? player.transform : __result.transform;
                    var equipEffects = attachObject.Find("equiped");
                    if (equipEffects != null && mode == FxAttachMode.EquipRoot)
                    {
                        attachObject = equipEffects;
                    }

                    AttachFx(attachObject, equipFx, asset);
                }
            }
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.AttachArmor))]
        [HarmonyPostfix]
        public static void AttachArmor_Postfix(VisEquipment __instance, int itemHash)
        {
            if (!CanCreateEffect(__instance, itemHash, out var player, out var equippedItem))
            {
                return;
            }

            var equipFx = GetEquipFxName(equippedItem, out var mode);
            if (!string.IsNullOrEmpty(equipFx) && mode == FxAttachMode.Player)
            {
                var asset = EpicLoot.LoadAsset<GameObject>(equipFx);
                if (asset != null)
                {
                    var attachObject = player.transform;
                    AttachFx(attachObject, equipFx, asset);
                }
            }
        }

        private static void AttachFx(Transform attachObject, string equipFx, GameObject asset)
        {
            if (attachObject.Find(equipFx) != null)
            {
                return;
            }

            var newEffect = Object.Instantiate(asset, attachObject, false);
            newEffect.name = equipFx;
            var audioSources = newEffect.GetComponentsInChildren<AudioSource>();
            foreach (var audioSource in audioSources)
            {
                audioSource.outputAudioMixerGroup = AudioMan.instance.m_ambientMixer;
            }
        }


        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        [HarmonyPrefix]
        public static void Humanoid_UnequipItem_Prefix(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (item == null || !item.m_equiped)
            {
                return;
            }

            var equipFx = GetEquipFxName(item, out var mode);
            if (OtherItemsUseThisEffect(__instance, equipFx, item, mode))
            {
                return;
            }

            if (!string.IsNullOrEmpty(equipFx) && mode == FxAttachMode.Player)
            {
                var effect = __instance.transform.Find(equipFx);
                if (effect == null)
                {
                    EpicLoot.LogError($"Unequipped item ({item.m_shared.m_name}) from player that had fx, but could not find fx ({equipFx})!");
                    return;
                }

                Object.Destroy(effect.gameObject);
            }
        }

        private static bool OtherItemsUseThisEffect(Humanoid humanoid, string equipFx, ItemDrop.ItemData item, FxAttachMode mode)
        {
            if (humanoid == null || !humanoid.IsPlayer())
            {
                return false;
            }

            var player = (Player)humanoid;
            foreach (var equipmentItemData in player.GetEquipment())
            {
                if (equipmentItemData == item)
                {
                    continue;
                }

                if (GetEquipFxName(equipmentItemData, out var equippedItemMode) == equipFx && equippedItemMode == mode)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CanCreateEffect(VisEquipment __instance, int itemHash, out Player player, out ItemDrop.ItemData equippedItem)
        {
            equippedItem = null;
            player = __instance.GetComponent<Player>();
            if (player == null)
            {
                return false;
            }

            var itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
            if (itemPrefab == null)
            {
                return false;
            }

            var itemDrop = itemPrefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                return false;
            }

            var itemData = itemDrop.m_itemData;
            equippedItem = player.GetEquipmentOfType(itemData.m_shared.m_itemType);
            return equippedItem != null;
        }

        public static string GetEquipFxName(ItemDrop.ItemData equippedItem, out FxAttachMode mode)
        {
            if (equippedItem.IsMagic())
            {
                var magicItem = equippedItem.GetMagicItem();
                if (magicItem.IsUniqueLegendary())
                {
                    if (!string.IsNullOrEmpty(magicItem.GetLegendaryInfo()?.EquipFx))
                    {
                        mode = FxAttachMode.EquipRoot;
                        return magicItem.GetLegendaryInfo().EquipFx;
                    }
                }
                else
                {
                    var equipEffect = magicItem.GetFirstEquipEffect(out mode);
                    if (!string.IsNullOrEmpty(equipEffect))
                    {
                        return equipEffect;
                    }
                }
            }

            mode = FxAttachMode.None;
            return null;
        }
    }
}
