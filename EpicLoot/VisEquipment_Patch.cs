using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

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
            if (!CanCreateEffect(__instance, itemHash, out var player, out var equippedItem, out var itemID))
            {
                return;
            }

            SetTextureOverrides(__instance, new List<GameObject> { __result }, itemID, equippedItem);
            SetItemFx(__result, equippedItem, player);
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.AttachArmor))]
        [HarmonyPostfix]
        public static void AttachArmor_Postfix(VisEquipment __instance, List<GameObject> __result, int itemHash)
        {
            if (!CanCreateEffect(__instance, itemHash, out var player, out var equippedItem, out var itemID))
            {
                return;
            }

            SetTextureOverrides(__instance, __result, itemID, equippedItem);
            SetItemFx(null, equippedItem, player);
        }

        private static void SetItemFx(GameObject __result, ItemDrop.ItemData equippedItem, Player player)
        {
            var equipFx = GetEquipFxName(equippedItem, out var mode);
            if (!string.IsNullOrEmpty(equipFx))
            {
                var asset = EpicLoot.LoadAsset<GameObject>(equipFx);
                if (asset != null)
                {
                    var attachObject = mode == FxAttachMode.Player ? player.transform : __result?.transform;
                    if (attachObject == null)
                    {
                        EpicLoot.LogError($"Tried to attach FX to item that did not exist. item={equippedItem.m_shared.m_name}, equipFx={equipFx}, mode={mode}");
                        return;
                    }

                    var equipEffects = attachObject.Find("equiped");
                    if (equipEffects != null && mode == FxAttachMode.EquipRoot)
                    {
                        attachObject = equipEffects;
                    }

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

            ZNetView.m_forceDisableInit = true;
            var newEffect = Object.Instantiate(asset, attachObject, false);
            ZNetView.m_forceDisableInit = false;

            newEffect.name = equipFx;
            var audioSources = newEffect.GetComponentsInChildren<AudioSource>();
            foreach (var audioSource in audioSources)
            {
                audioSource.outputAudioMixerGroup = AudioMan.instance.m_ambientMixer;
            }
        }

        private static void SetTextureOverrides(VisEquipment __instance, List<GameObject> __result, string itemID, ItemDrop.ItemData equippedItem)
        {
            GetTexOverrides(itemID, equippedItem, out var mainTexture, out var chestTex, out var legsTex);
            if (!string.IsNullOrEmpty(mainTexture))
            {
                foreach (var go in __result)
                {
                    var skinnedMeshRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    SetMainTextureOnRenderers(skinnedMeshRenderers, mainTexture);

                    var meshRenderers = go.GetComponentsInChildren<MeshRenderer>(true);
                    SetMainTextureOnRenderers(meshRenderers, mainTexture);
                }
            }

            if (!string.IsNullOrEmpty(chestTex))
            {
                var chestTexAsset = EpicLoot.LoadAsset<Texture>(chestTex);
                if (chestTexAsset != null)
                {
                    __instance.m_bodyModel.material.SetTexture("_ChestTex", chestTexAsset);
                }
                else
                {
                    EpicLoot.LogError($"Missing Texture Override Asset: ChestTex={chestTex}");
                }
            }

            if (!string.IsNullOrEmpty(legsTex))
            {
                var legsTexAsset = EpicLoot.LoadAsset<Texture>(legsTex);
                if (legsTexAsset != null)
                {
                    __instance.m_bodyModel.material.SetTexture("_ChestTex", legsTexAsset);
                }
                else
                {
                    EpicLoot.LogError($"Missing Texture Override Asset: LegsTex={legsTex}");
                }
            }
        }

        private static void SetMainTextureOnRenderers(IEnumerable<Renderer> renderers, string mainTexture)
        {
            var mainTextureAsset = EpicLoot.LoadAsset<Texture>(mainTexture);
            if (mainTextureAsset != null)
            {
                foreach (var renderer in renderers)
                {
                    renderer.material.mainTexture = mainTextureAsset;
                }
            }
            else
            {
                EpicLoot.LogError($"Missing Texture Override Asset: MainTexture={mainTexture}");
            }
        }

        private static void GetTexOverrides(string itemID, ItemDrop.ItemData equippedItem, out string mainTexture, out string chestTex, out string legsTex)
        {
            mainTexture = null;
            chestTex = null;
            legsTex = null;

            if (equippedItem.IsMagic(out var magicItem) && magicItem.IsUniqueLegendary())
            {
                var textureOverride = magicItem.GetLegendaryInfo()?.TextureReplacements?.Find(x => x.ItemID == itemID);
                if (textureOverride != null)
                {
                    mainTexture = textureOverride.MainTexture;
                    chestTex = textureOverride.ChestTex;
                    legsTex = textureOverride.LegsTex;
                }
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

                ZNetScene.instance.Destroy(effect.gameObject);
            }
        }

        private static bool OtherItemsUseThisEffect(Humanoid humanoid, string equipFx, ItemDrop.ItemData item, FxAttachMode mode)
        {
            if (humanoid == null || !humanoid.IsPlayer())
            {
                return false;
            }

            var player = (Player) humanoid;
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

        public static bool CanCreateEffect(VisEquipment __instance, int itemHash, out Player player, out ItemDrop.ItemData equippedItem, out string itemID)
        {
            equippedItem = null;
            itemID = null;
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

            itemID = itemPrefab.name;
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
            if (equippedItem.IsMagic(out var magicItem))
            {
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

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        [HarmonyPostfix]
        public static void EquipItem_Postfix(Humanoid __instance, bool __result, ItemDrop.ItemData item)
        {
            if (!__result || __instance == null || __instance.m_visEquipment == null || item == null)
            {
                return;
            }

            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool)
            {
                __instance.m_visEquipment.m_currentRightItemHash = -1;
            }
            else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon)
            {
                if (__instance.m_rightItem != null && __instance.m_rightItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch && __instance.m_leftItem == null)
                {
                    __instance.m_visEquipment.m_currentLeftItemHash = -1;
                }
                __instance.m_visEquipment.m_currentRightItemHash = -1;
            }
            else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
            {
                __instance.m_visEquipment.m_currentLeftItemHash = -1;
            }
            else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow)
            {
                __instance.m_visEquipment.m_currentLeftItemHash = -1;
            }
            else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon)
            {
                __instance.m_visEquipment.m_currentRightItemHash = -1;
            }
            else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft)
            {
                __instance.m_visEquipment.m_currentLeftItemHash = -1;
                __instance.m_visEquipment.m_currentRightItemHash = -1;
            }
            else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest)
            {
                __instance.m_visEquipment.m_currentChestItemHash = -1;
            }
            else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs)
            {
                __instance.m_visEquipment.m_currentLegItemHash = -1;
            }
            else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet)
            {
                __instance.m_visEquipment.m_currentHelmetItemHash = -1;
            }
            else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder)
            {
                __instance.m_visEquipment.m_currentShoulderItemHash = -1;
            }
            else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility)
            {
                __instance.m_visEquipment.m_currentUtilityItemHash = -1;
            }
        }
    }
}
