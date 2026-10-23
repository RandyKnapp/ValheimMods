using System;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using EpicLoot.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using Iff = ItemFavoriteFrameworkAPI.ItemFavorites;

namespace EpicLoot.Compatibility;

/// <summary>
/// Item Favorite Framework integration. The player marks items (and inventory slots) as favorites in
/// that mod; here they are left out of every enchanting-table list that destroys or consumes the item,
/// and get a star wherever the tables list items.
///
/// Soft in every direction: no assembly reference, only a soft BepInDependency for load order. Every call
/// goes through the framework's own shim (ItemFavoriteFrameworkAPI/ItemFavorites.cs, copied unchanged from
/// the framework's repository; replace it whole to update it), which binds each endpoint once and answers
/// "not favorited" when the mod is absent or too old for that endpoint. This class adds Epic Loot's own
/// rules on top: the config switches, failing closed, and the star.
/// </summary>
public static class ItemFavorites
{
    private const string StarName = "EL_FavoriteStar";

    private static bool _reportedFailure;

    /// <summary>True when Item Favorite Framework is installed and its API was found.</summary>
    public static bool IsLoaded => Iff.IsLoaded();

    /// <summary>
    /// The player favorited this item, or it sits in a favorited slot of their inventory, and they
    /// want Epic Loot to respect that. Use it to keep an item out of anything that destroys or
    /// consumes it, both when building a list and again when the action runs.
    /// </summary>
    public static bool IsProtected(ItemDrop.ItemData item)
    {
        if (item == null || !ELConfig.RespectItemFavorites.Value)
        {
            return false;
        }

        try
        {
            return Iff.IsProtected(item);
        }
        catch (Exception e)
        {
            // Fail closed: when favorites cannot be checked, hide the item rather than risk
            // destroying one the player marked to keep.
            ReportFailure(e);
            return true;
        }
    }

    /// <summary>
    /// Shows or hides the favorite star on an item icon: gold for a favorited item, the slot colour
    /// for an item only protected by its slot. Call on every refresh, including for empty rows (null
    /// item), because list rows are pooled and would otherwise keep a star from their previous item.
    /// </summary>
    public static void UpdateStar(Image icon, ItemDrop.ItemData item)
    {
        if (icon == null)
        {
            return;
        }

        Transform existing = icon.transform.Find(StarName);
        int marks = item != null && ELConfig.ShowFavoriteStars.Value ? GetMarks(item) : 0;
        if ((marks & (Iff.MarkItemFavorite | Iff.MarkSlotFavorite)) == 0)
        {
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }

            return;
        }

        Image star = existing != null ? existing.GetComponent<Image>() : CreateStar(icon);
        if (star == null)
        {
            return;
        }

        star.color = (marks & Iff.MarkItemFavorite) != 0 ? Iff.GetFavoriteColor() : Iff.GetFavoritedSlotColor();
        star.gameObject.SetActive(true);
    }

    private static int GetMarks(ItemDrop.ItemData item)
    {
        try
        {
            return Iff.GetMarks(item);
        }
        catch (Exception e)
        {
            ReportFailure(e);
            return 0;
        }
    }

    // In the icon's top-left corner, sized by anchors so it follows whatever size the row gives the
    // icon. A child of the icon, so it draws above it and hides with it.
    private static Image CreateStar(Image icon)
    {
        Sprite sprite = Iff.GetFavoriteIcon();
        if (sprite == null)
        {
            return null;
        }

        GameObject go = new GameObject(StarName, typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(icon.transform, false);
        rect.anchorMin = new Vector2(0f, 0.55f);
        rect.anchorMax = new Vector2(0.45f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image star = go.GetComponent<Image>();
        star.sprite = sprite;
        star.preserveAspect = true;
        star.raycastTarget = false;
        return star;
    }

    // ---------------------------------------------------------------------------------------------------
    // Wishlists and loot rolls (the framework's API 3). An older framework lacks these endpoints, and the
    // shim then simply does nothing for them, so favorites still work.
    // ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// Registers Epic Loot's wishlist conditions (rarity, unique, set, effects) and its magic item names with
    /// the framework. Call once from Awake: the framework loads first, since Epic Loot soft-depends on it.
    /// </summary>
    public static void RegisterLootIntegration()
    {
        if (!Iff.IsLoaded())
        {
            if (Chainloader.PluginInfos.ContainsKey(Iff.PluginGuid))
            {
                EpicLoot.LogWarningForce("Item Favorite Framework is installed but its API was not found; favorites are ignored.");
            }

            return;
        }

        EpicLoot.Log($"Item Favorite Framework {Iff.GetPluginVersion()} found (API {Iff.GetApiVersion()}); " +
            "favorited items are kept out of destructive enchanting lists.");

        try
        {
            if (Iff.RegisterWishlistProvider(ItemFavoritesWishlist.ProviderId, ItemFavoritesWishlist.GetSchema,
                    ItemFavoritesWishlist.Matches, ItemFavoritesWishlist.Describe, ItemFavoritesWishlist.Suggest))
            {
                EpicLoot.Log("Registered Epic Loot's wishlist conditions with Item Favorite Framework.");
            }

            Iff.AddItemNameProvider(ItemFavoritesWishlist.DisplayName);
        }
        catch (Exception e)
        {
            EpicLoot.LogWarningForce($"Could not register Epic Loot's wishlist conditions with Item Favorite Framework: {e.Message}");
        }
    }

    /// <summary>
    /// Hands the magic items a creature just dropped to the framework's need/greed/pass roll, among the
    /// players near the drop, when the server asks for that (Request Loot Rolls) and runs rolls. Call on the
    /// machine that spawned the drops, right after spawning them.
    /// </summary>
    public static void RequestLootRolls(List<GameObject> loot, string characterName, Vector3 dropPoint)
    {
        if (loot == null || loot.Count == 0 || !ELConfig.RequestLootRolls.Value)
        {
            return;
        }

        try
        {
            if (!Iff.IsRollingAvailable())
            {
                return;
            }

            GameObject creature = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(characterName) : null;
            Character character = creature != null ? creature.GetComponent<Character>() : null;
            if (ELConfig.LootRollBossesOnly.Value && (character == null || !character.IsBoss()))
            {
                return;
            }

            string members = Iff.GetPlayerIdsNearJson(dropPoint, ELConfig.LootRollPlayerRange.Value);
            if (JArray.Parse(members).Count < ELConfig.LootRollMinimumPlayers.Value)
            {
                return;
            }

            string label = character != null ? Localization.instance.Localize(character.m_name) : characterName;
            var options = new JObject
            {
                ["label"] = label,
                ["source"] = "epicloot",
                ["groupId"] = $"epicloot-{characterName}-{DateTime.UtcNow.Ticks}"
            }.ToString(Formatting.None);

            foreach (GameObject go in loot)
            {
                ItemDrop drop = go != null ? go.GetComponent<ItemDrop>() : null;
                if (drop == null || !drop.m_itemData.IsMagic(out MagicItem magicItem) || magicItem.Rarity < ELConfig.LootRollMinimumRarity.Value)
                {
                    continue;
                }

                if (!Iff.StartRoll(drop, members, options, out _, out string error))
                {
                    EpicLoot.Log($"No loot roll for {drop.m_itemData.m_shared.m_name}: {error}.");
                }
            }
        }
        catch (Exception e)
        {
            EpicLoot.LogWarningForce($"Starting loot rolls failed: {e.Message}");
        }
    }

    private static void ReportFailure(Exception e)
    {
        if (_reportedFailure)
        {
            return;
        }

        _reportedFailure = true;
        EpicLoot.LogErrorForce($"Item Favorite Framework threw while checking an item; items it cannot check are treated as favorited: {e}");
    }
}
