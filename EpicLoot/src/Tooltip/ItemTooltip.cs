using System;
using System.Collections.Generic;
using EpicLoot.MagicItemEffects;
using Newtonsoft.Json;
using UnityEngine;

namespace EpicLoot;

/// <summary>
/// Epic Loot's item tooltip, in three steps: <see cref="TooltipGatherer"/> works out the data, registered
/// editors (API.AddTooltipDataEditor) may change it, and <see cref="TooltipRenderer"/> draws it. Every reader
/// goes through <see cref="GetData"/>, so Epic Loot's own tooltip, Auga's and the API all see the same edited
/// data.
/// </summary>
/// <remarks>
/// Another mod may also take over drawing the inventory tooltip (API.SetTooltipOverride). While it does,
/// Epic Loot leaves InventoryGrid.CreateItemTooltip and the inventory tooltip's box to vanilla, and the
/// GetTooltip calls made inside it get vanilla's text. Every other GetTooltip call (the crafting panel, other
/// mods' windows, Epic Loot's own panels) keeps Epic Loot's text; a mod that wants an item's text without it
/// asks for that call alone (<see cref="GetTextWithoutEpicLoot"/>).
/// </remarks>
public static class ItemTooltip
{
    internal static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        ObjectCreationHandling = ObjectCreationHandling.Replace
    };

    /// <summary>tooltipdebugvanilla: vanilla's text for items with magic data, Epic Loot's other parts kept.</summary>
    internal static bool Disabled;

    #region Data

    private struct CacheEntry
    {
        public int Quality;
        public bool IncludeDetails;
        public ItemTooltipData Data;
    }

    // One frame's worth: the hovered tooltip is rebuilt every frame (vanilla InventoryGrid.UpdateGui calls
    // CreateItemTooltip each frame), the text, title and hints read the data separately, and Auga asks once
    // per stat line.
    private static readonly Dictionary<ItemDrop.ItemData, CacheEntry> Cache = new Dictionary<ItemDrop.ItemData, CacheEntry>();
    private static int _cacheFrame = -1;

    /// <summary>
    /// The tooltip data of <paramref name="item"/> after the registered editors have run. Shared within the
    /// frame, so treat it as read-only.
    /// </summary>
    /// <returns>null without an item or a local player</returns>
    public static ItemTooltipData GetData(ItemDrop.ItemData item, int qualityLevel, bool includeDetails)
    {
        if (item == null)
        {
            return null;
        }

        if (Time.frameCount != _cacheFrame)
        {
            Cache.Clear();
            _cacheFrame = Time.frameCount;
        }

        if (Cache.TryGetValue(item, out CacheEntry entry) && entry.Quality == qualityLevel &&
            entry.IncludeDetails == includeDetails)
        {
            return entry.Data;
        }

        // Bows read the Elemental Magic skill while the tooltip is worked out, as they do in ItemData.GetTooltip
        // (see ElementalArchery). Opened here too, for the API, which does not go through GetTooltip.
        ItemTooltipData data;
        bool remapped = ElementalArchery.EnterTooltipScope(item);
        try
        {
            data = TooltipGatherer.Gather(item, qualityLevel, includeDetails);
        }
        finally
        {
            ElementalArchery.ExitTooltipScope(remapped);
        }

        if (data != null && Editors.Count > 0)
        {
            data = RunEditors(item, data);
        }

        Cache[item] = new CacheEntry { Quality = qualityLevel, IncludeDetails = includeDetails, Data = data };
        return data;
    }

    /// <summary>Drops the cached data, after an item's magic data changed within the frame.</summary>
    internal static void InvalidateCache()
    {
        Cache.Clear();
    }

    #endregion

    #region Text

    // Nested GetTooltip calls in which Epic Loot writes no text: the inventory tooltip while it is overridden,
    // and GetTextWithoutEpicLoot.
    private static int _withoutEpicLootDepth;

    /// <summary>Epic Loot writes item tooltip text in the current GetTooltip call.</summary>
    internal static bool DrawsText => _withoutEpicLootDepth == 0;

    internal static void EnterWithoutEpicLoot()
    {
        _withoutEpicLootDepth++;
    }

    internal static void ExitWithoutEpicLoot()
    {
        _withoutEpicLootDepth--;
    }

    /// <summary>
    /// ItemData.GetTooltip with Epic Loot's part switched off for this one call: vanilla's text, plus whatever
    /// other mods add to it. For a mod that draws Epic Loot's part itself, from the tooltip data.
    /// </summary>
    public static string GetTextWithoutEpicLoot(ItemDrop.ItemData item, int qualityLevel, bool crafting,
        float worldLevel, int stackOverride)
    {
        if (item == null)
        {
            return "";
        }

        EnterWithoutEpicLoot();
        try
        {
            return ItemDrop.ItemData.GetTooltip(item, qualityLevel, crafting, worldLevel, stackOverride);
        }
        finally
        {
            ExitWithoutEpicLoot();
        }
    }

    /// <summary>The inventory grid's tooltip: the text with the key hints under it, and the title.</summary>
    internal static string GetInventoryText(ItemDrop.ItemData item, out string title)
    {
        string tooltip = item.GetTooltip();
        // The same key ItemData.GetTooltip gathered under, so this is the cached data.
        ItemTooltipData data = GetData(item, item.m_quality, MagicItem.ShowEffectDetails);
        title = data?.Title ?? item.GetDecoratedName();
        return tooltip + TooltipRenderer.RenderHints(data);
    }

    #endregion

    #region Override

    private static readonly List<string> OverrideOwners = new List<string>();

    /// <summary>Another mod draws the inventory tooltip; see API.SetTooltipOverride.</summary>
    public static bool IsOverridden => OverrideOwners.Count > 0;

    internal static List<string> GetOverrideOwners()
    {
        return new List<string>(OverrideOwners);
    }

    /// <summary>Claims or releases the override for <paramref name="ownerId"/>. Logged only on a change.</summary>
    internal static void SetOverride(string ownerId, bool overriding)
    {
        if (OverrideOwners.Contains(ownerId) == overriding)
        {
            return;
        }

        if (overriding)
        {
            OverrideOwners.Add(ownerId);
            EpicLoot.LogForce($"[EpicLoot.API] '{ownerId}' is overriding Epic Loot's item tooltip; " +
                "Epic Loot will not draw inventory item tooltips while it does.");
        }
        else
        {
            OverrideOwners.Remove(ownerId);
            EpicLoot.LogForce($"[EpicLoot.API] '{ownerId}' released its override of Epic Loot's item tooltip" +
                (IsOverridden ? $"; still overridden by {string.Join(", ", OverrideOwners)}." : "."));
        }

        TooltipBoxPatches.OnOverrideChanged();
    }

    #endregion

    #region Editors

    private static readonly List<KeyValuePair<string, Func<ItemDrop.ItemData, string, string>>> Editors =
        new List<KeyValuePair<string, Func<ItemDrop.ItemData, string, string>>>();
    // An editor runs every frame a tooltip is shown, so its failure is logged once, not every frame.
    private static readonly HashSet<string> FailedEditors = new HashSet<string>();

    internal static bool HasEditor(string id)
    {
        return Editors.Exists(x => x.Key == id);
    }

    internal static void AddEditor(string id, Func<ItemDrop.ItemData, string, string> editor)
    {
        Editors.Add(new KeyValuePair<string, Func<ItemDrop.ItemData, string, string>>(id, editor));
        InvalidateCache();
    }

    internal static bool RemoveEditor(string id)
    {
        bool removed = Editors.RemoveAll(x => x.Key == id) > 0;
        FailedEditors.Remove(id);
        InvalidateCache();
        return removed;
    }

    internal static List<string> GetEditorIds()
    {
        return Editors.ConvertAll(x => x.Key);
    }

    // Each editor gets the previous one's JSON and returns its own; one that throws or returns something that is
    // not tooltip data is skipped, and the next gets what the one before it returned.
    private static ItemTooltipData RunEditors(ItemDrop.ItemData item, ItemTooltipData data)
    {
        string json = JsonConvert.SerializeObject(data, JsonSettings);
        ItemTooltipData result = data;
        foreach (KeyValuePair<string, Func<ItemDrop.ItemData, string, string>> editor in Editors.ToArray())
        {
            try
            {
                string edited = editor.Value(item, json);
                if (string.IsNullOrEmpty(edited) || edited == json)
                {
                    continue;
                }

                ItemTooltipData parsed = JsonConvert.DeserializeObject<ItemTooltipData>(edited, JsonSettings);
                if (parsed == null)
                {
                    throw new JsonException("the editor returned no tooltip data");
                }

                parsed.Normalize();
                result = parsed;
                json = edited;
            }
            catch (Exception ex)
            {
                if (FailedEditors.Add(editor.Key))
                {
                    EpicLoot.LogErrorForce($"[EpicLoot.API] Tooltip data editor '{editor.Key}' threw and was skipped " +
                        $"(logged once): {ex}");
                }
            }
        }

        return result;
    }

    #endregion
}
