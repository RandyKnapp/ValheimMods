using HarmonyLib;
using UnityEngine;

namespace EpicLoot
{
    public enum EffectAttachMode
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

            var equipEffect = GetEquipEffectName(equippedItem, out var mode);
            if (!string.IsNullOrEmpty(equipEffect))
            {
                var asset = EpicLoot.LoadAsset<GameObject>(equipEffect);
                if (asset != null)
                {
                    var attachObject = mode == EffectAttachMode.Player ? player.transform : __result.transform;
                    var equipEffects = attachObject.Find("equiped");
                    if (equipEffects != null && mode == EffectAttachMode.EquipRoot)
                    {
                        attachObject = equipEffects;
                    }

                    var newEffect = Object.Instantiate(asset, attachObject, false);
                    var audioSources = newEffect.GetComponentsInChildren<AudioSource>();
                    foreach (var audioSource in audioSources)
                    {
                        audioSource.outputAudioMixerGroup = AudioMan.instance.m_ambientMixer;
                    }
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

            var equipEffect = GetEquipEffectName(equippedItem, out var mode);
            if (!string.IsNullOrEmpty(equipEffect) && mode == EffectAttachMode.Player)
            {
                var asset = EpicLoot.LoadAsset<GameObject>(equipEffect);
                if (asset != null)
                {
                    var attachObject = player.transform;
                    var newEffect = Object.Instantiate(asset, attachObject, false);
                    var audioSources = newEffect.GetComponentsInChildren<AudioSource>();
                    foreach (var audioSource in audioSources)
                    {
                        audioSource.outputAudioMixerGroup = AudioMan.instance.m_ambientMixer;
                    }
                }
            }
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

        public static string GetEquipEffectName(ItemDrop.ItemData equippedItem, out EffectAttachMode mode)
        {
            if (equippedItem.IsMagic())
            {
                var magicItem = equippedItem.GetMagicItem();
                if (magicItem.IsUniqueLegendary())
                {
                    if (!string.IsNullOrEmpty(magicItem.GetLegendaryInfo()?.EquipEffect))
                    {
                        mode = EffectAttachMode.EquipRoot;
                        return magicItem.GetLegendaryInfo().EquipEffect;
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

            mode = EffectAttachMode.None;
            return null;
        }
    }
}
