using EpicLoot.LegendarySystem;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        //SetLeftHandEquiped
        //SetRightHandEquiped
        public enum ItemSettingSlot { None, Helmet, LeftHand, RightHand, Armor }

        public static ItemSettingSlot AttachingItemSlot = ItemSettingSlot.None;

        // Player-mode fx are reconciled against GetMagicEquipment() rather than attached from the
        // VisEquipment model path, so items in slots added by other mods get them too. Valued by
        // instance, not just name: destruction then targets the exact object we created, so a
        // same-named child belonging to something else can never be destroyed.
        private static readonly ConditionalWeakTable<Player, Dictionary<string, GameObject>> AttachedPlayerFx = new();

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetLeftHandEquipped))]
        [HarmonyPrefix]
        public static void SetLeftHandEquiped_Prefix()
        {
            AttachingItemSlot = ItemSettingSlot.LeftHand;
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetLeftHandEquipped))]
        [HarmonyPostfix]
        public static void SetLeftHandEquiped_Postfix()
        {
            AttachingItemSlot = ItemSettingSlot.None;
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetRightHandEquipped))]
        [HarmonyPrefix]
        public static void SetRightHandEquiped_Prefix()
        {
            AttachingItemSlot = ItemSettingSlot.RightHand;
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetRightHandEquipped))]
        [HarmonyPostfix]
        public static void SetRightHandEquiped_Postfix()
        {
            AttachingItemSlot = ItemSettingSlot.None;
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetHelmetEquipped))]
        [HarmonyPrefix]
        public static void SetHelmetEquiped_Prefix()
        {
            AttachingItemSlot = ItemSettingSlot.Helmet;
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetHelmetEquipped))]
        [HarmonyPostfix]
        public static void SetHelmetEquiped_Postfix()
        {
            AttachingItemSlot = ItemSettingSlot.None;
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.AttachItem))]
        [HarmonyPostfix]
        public static void AttachItem_Postfix(VisEquipment __instance, GameObject __result, int itemHash)
        {
            if (!CanCreateEffect(__instance, itemHash, AttachingItemSlot, out Player player,
                out ItemDrop.ItemData equippedItem, out string itemID))
            {
                return;
            }

            SetTextureOverrides(__instance, new List<GameObject> { __result }, itemID, equippedItem);
            SetItemFx(__result, equippedItem);
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.AttachArmor))]
        [HarmonyPostfix]
        public static void AttachArmor_Postfix(VisEquipment __instance, List<GameObject> __result, int itemHash)
        {
            if (!CanCreateEffect(__instance, itemHash, ItemSettingSlot.Armor, out Player player,
                out ItemDrop.ItemData equippedItem, out string itemID))
            {
                return;
            }

            SetTextureOverrides(__instance, __result, itemID, equippedItem);
        }

        private static void SetItemFx(GameObject __result, ItemDrop.ItemData equippedItem)
        {
            string equipFx = GetEquipFxName(equippedItem, out FxAttachMode mode);
            if (mode == FxAttachMode.Player || string.IsNullOrEmpty(equipFx))
            {
                // Player-mode fx are owned by RefreshPlayerFx, which sees items in modded slots too.
                return;
            }

            GameObject asset = EpicLoot.LoadAsset<GameObject>(equipFx);
            if (asset == null || __result == null)
            {
                return;
            }

            Transform attachObject = __result.transform;
            Transform equipEffects = attachObject.Find("equiped");
            if (equipEffects != null && mode == FxAttachMode.EquipRoot)
            {
                attachObject = equipEffects;
            }

            AttachFx(attachObject, equipFx, asset);
        }

        private static GameObject AttachFx(Transform attachObject, string equipFx, GameObject asset)
        {
            Transform existing = attachObject.Find(equipFx);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject newEffect;
            ZNetView.m_forceDisableInit = true;
            try
            {
                // m_forceDisableInit is global; leaking it true would break every later ZNetView.
                newEffect = Object.Instantiate(asset, attachObject, false);
            }
            finally
            {
                ZNetView.m_forceDisableInit = false;
            }

            newEffect.name = equipFx;
            if (AudioMan.instance != null)
            {
                AudioSource[] audioSources = newEffect.GetComponentsInChildren<AudioSource>();
                foreach (AudioSource audioSource in audioSources)
                {
                    audioSource.outputAudioMixerGroup = AudioMan.instance.m_ambientMixer;
                }
            }

            return newEffect;
        }

        private static void SetTextureOverrides(VisEquipment __instance, List<GameObject> __result,
            string itemID, ItemDrop.ItemData equippedItem)
        {
            GetTexOverrides(itemID, equippedItem, out string mainTexture, out string chestTex, out string legsTex);
            if (!string.IsNullOrEmpty(mainTexture))
            {
                foreach (GameObject go in __result)
                {
                    SkinnedMeshRenderer[] skinnedMeshRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    SetMainTextureOnRenderers(skinnedMeshRenderers, mainTexture);

                    MeshRenderer[] meshRenderers = go.GetComponentsInChildren<MeshRenderer>(true);
                    SetMainTextureOnRenderers(meshRenderers, mainTexture);
                }
            }

            if (!string.IsNullOrEmpty(chestTex))
            {
                Texture chestTexAsset = EpicLoot.LoadAsset<Texture>(chestTex);
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
                Texture legsTexAsset = EpicLoot.LoadAsset<Texture>(legsTex);
                if (legsTexAsset != null)
                {
                    __instance.m_bodyModel.material.SetTexture("_LegsTex", legsTexAsset);
                }
                else
                {
                    EpicLoot.LogError($"Missing Texture Override Asset: LegsTex={legsTex}");
                }
            }
        }

        private static void SetMainTextureOnRenderers(IEnumerable<Renderer> renderers, string mainTexture)
        {
            Texture mainTextureAsset = EpicLoot.LoadAsset<Texture>(mainTexture);
            if (mainTextureAsset != null)
            {
                foreach (Renderer renderer in renderers)
                {
                    renderer.material.mainTexture = mainTextureAsset;
                }
            }
            else
            {
                EpicLoot.LogError($"Missing Texture Override Asset: MainTexture={mainTexture}");
            }
        }

        private static void GetTexOverrides(string itemID, ItemDrop.ItemData equippedItem,
            out string mainTexture, out string chestTex, out string legsTex)
        {
            mainTexture = null;
            chestTex = null;
            legsTex = null;

            if (equippedItem.IsMagic(out MagicItem magicItem) && magicItem.IsUniqueLegendary())
            {
                TextureReplacement textureOverride =
                    magicItem.GetLegendaryInfo()?.TextureReplacements?.Find(x => x.ItemID == itemID);
                if (textureOverride != null)
                {
                    mainTexture = textureOverride.MainTexture;
                    chestTex = textureOverride.ChestTex;
                    legsTex = textureOverride.LegsTex;
                }
            }
        }

        /// <summary>
        /// Brings the player's <see cref="FxAttachMode.Player"/> effects in line with what
        /// <see cref="PlayerExtensions.GetMagicEquipment"/> reports, adding what is missing and removing
        /// what is no longer worn. Because that list already merges in registered equipment providers,
        /// items equipped into slots added by other mods are covered without any per-mod handling.
        /// </summary>
        public static void RefreshPlayerFx(Player player)
        {
            if (player == null)
            {
                return;
            }

            HashSet<string> desired = new HashSet<string>(StringComparer.Ordinal);
            foreach (ItemDrop.ItemData item in player.GetMagicEquipment())
            {
                string fx = GetEquipFxName(item, out FxAttachMode mode);
                if (mode == FxAttachMode.Player && !string.IsNullOrEmpty(fx))
                {
                    // Two items granting the same effect share one instance -- the set does what
                    // the old OtherItemsUseThisEffect check did by hand.
                    desired.Add(fx);
                }
            }

            Dictionary<string, GameObject> attached = AttachedPlayerFx.GetOrCreateValue(player);

            List<string> stale = null;
            foreach (KeyValuePair<string, GameObject> entry in attached)
            {
                // entry.Value == null covers a destroyed instance; Unity's operator== is the only
                // thing that reports that, so do not switch this to a pattern match.
                if (entry.Value != null && desired.Contains(entry.Key))
                {
                    continue;
                }

                if (entry.Value != null)
                {
                    DestroyFx(entry.Value);
                }

                (stale ??= new List<string>()).Add(entry.Key);
            }

            if (stale != null)
            {
                foreach (string fx in stale)
                {
                    attached.Remove(fx);
                }
            }

            foreach (string fx in desired)
            {
                if (attached.ContainsKey(fx))
                {
                    continue;
                }

                GameObject asset = EpicLoot.LoadAsset<GameObject>(fx);
                if (asset == null)
                {
                    EpicLoot.LogError($"Missing equip fx asset: {fx}");
                    continue;
                }

                GameObject instance = AttachFx(player.transform, fx, asset);
                if (instance != null)
                {
                    attached[fx] = instance;
                }
            }
        }

        private static void DestroyFx(GameObject effect)
        {
            if (ZNetScene.instance != null)
            {
                ZNetScene.instance.Destroy(effect);
            }
            else
            {
                Object.Destroy(effect);
            }
        }

        public static bool CanCreateEffect(VisEquipment __instance, int itemHash, ItemSettingSlot slot,
            out Player player, out ItemDrop.ItemData equippedItem, out string itemID)
        {
            equippedItem = null;
            itemID = null;
            player = __instance.GetComponent<Player>();
            if (player == null)
            {
                return false;
            }

            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
            if (itemPrefab == null)
            {
                return false;
            }

            itemID = itemPrefab.name;
            ItemDrop itemDrop = itemPrefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                return false;
            }

            switch (slot)
            {
                case ItemSettingSlot.None:
                    equippedItem = null;
                    break;
                case ItemSettingSlot.Helmet:
                    equippedItem = player.m_helmetItem;
                    break;
                case ItemSettingSlot.LeftHand:
                    equippedItem = player.m_leftItem;
                    break;
                case ItemSettingSlot.RightHand:
                    equippedItem = player.m_rightItem;
                    break;
                case ItemSettingSlot.Armor:
                    equippedItem = FindWornItemByPrefab(player, itemID);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }

            return equippedItem != null;
        }

        /// <summary>
        /// The worn item this armor model was actually built from. AttachArmor is only ever driven by
        /// Humanoid.SetupVisEquipment writing the vanilla worn fields, so the answer is one of them.
        /// Matching by item type instead would pick the wrong one whenever two worn items share a type
        /// -- a utility belt plus a Utility-typed ring held in a third-party slot, say -- and apply the
        /// wrong legendary texture override.
        /// </summary>
        private static ItemDrop.ItemData FindWornItemByPrefab(Player player, string itemID)
        {
            // Humanoid_Patch substitutes the dummy prefab for a null m_dropPrefab, so two items can
            // carry it at once; it is never a usable identity.
            if (string.IsNullOrEmpty(itemID) || itemID == EpicAssets.DummyName)
            {
                return null;
            }

            if (MatchesPrefab(player.m_chestItem, itemID)) return player.m_chestItem;
            if (MatchesPrefab(player.m_legItem, itemID)) return player.m_legItem;
            if (MatchesPrefab(player.m_helmetItem, itemID)) return player.m_helmetItem;
            if (MatchesPrefab(player.m_shoulderItem, itemID)) return player.m_shoulderItem;
            if (MatchesPrefab(player.m_utilityItem, itemID)) return player.m_utilityItem;
            if (MatchesPrefab(player.m_trinketItem, itemID)) return player.m_trinketItem;
            return null;
        }

        private static bool MatchesPrefab(ItemDrop.ItemData item, string itemID)
        {
            return item != null && item.m_dropPrefab != null && item.m_dropPrefab.name == itemID;
        }

        public static string GetEquipFxName(ItemDrop.ItemData equippedItem, out FxAttachMode mode)
        {
            if (equippedItem.IsMagic(out MagicItem magicItem))
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
                    string equipEffect = magicItem.GetFirstEquipEffect(out mode);
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
                if (__instance.m_rightItem != null &&
                    __instance.m_rightItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch &&
                    __instance.m_leftItem == null)
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
            else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trinket)
            {
                __instance.m_visEquipment.m_currentTrinketItemHash = -1;
            }
        }
    }
}
