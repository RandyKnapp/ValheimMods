using System;
using System.Collections.Generic;
using EpicLoot.MagicItemEffects;
using Newtonsoft.Json;
using UnityEngine;

namespace EpicLoot;

/// <summary>
/// Epic Loot's item tooltip, in three steps: <see cref="TooltipGatherer"/> works out the data, registered
/// editors (API.AddTooltipEditor) may change it, and <see cref="TooltipRenderer"/> draws it. Every reader
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

    // One line per item and failure, not one per frame: a tooltip that fails is asked for again every frame.
    private static readonly HashSet<string> LoggedFailures = new HashSet<string>();

    #region Data

    private sealed class CacheEntry
    {
        public int Quality;
        public bool IncludeDetails;
        public ItemTooltipData Data;
        // The data as JSON, for the API; serialized on first use within the frame.
        public string Json;
        public string LocalizedJson;
    }

    // One frame's worth: the hovered tooltip is rebuilt every frame (vanilla InventoryGrid.UpdateGui calls
    // CreateItemTooltip each frame), the text, title and hints read the data separately, Auga asks once per
    // stat line, and a mod drawing its own tooltip asks for the JSON more than once.
    private static readonly Dictionary<ItemDrop.ItemData, CacheEntry> Cache = new Dictionary<ItemDrop.ItemData, CacheEntry>();
    private static int _cacheFrame = -1;

    /// <summary>
    /// The tooltip data of <paramref name="item"/> after the registered editors have run. Shared within the
    /// frame, so treat it as read-only.
    /// </summary>
    /// <returns>null without an item or a local player, or when working it out failed (logged once)</returns>
    public static ItemTooltipData GetData(ItemDrop.ItemData item, int qualityLevel, bool includeDetails)
    {
        return GetEntry(item, qualityLevel, includeDetails)?.Data;
    }

    /// <summary>The same data as JSON, localized or not, serialized once per frame.</summary>
    /// <returns>"" where <see cref="GetData"/> returns null</returns>
    internal static string GetJson(ItemDrop.ItemData item, int qualityLevel, bool includeDetails, bool localize)
    {
        CacheEntry entry = GetEntry(item, qualityLevel, includeDetails);
        if (entry?.Data == null)
        {
            return "";
        }

        if (entry.Json == null)
        {
            entry.Json = JsonConvert.SerializeObject(entry.Data, JsonSettings);
        }

        if (!localize)
        {
            return entry.Json;
        }

        if (entry.LocalizedJson == null)
        {
            // A copy: the cached data is what Epic Loot draws, unlocalized.
            ItemTooltipData copy = JsonConvert.DeserializeObject<ItemTooltipData>(entry.Json, JsonSettings);
            copy.Localize(text => Localization.instance.Localize(text));
            entry.LocalizedJson = JsonConvert.SerializeObject(copy, JsonSettings);
        }

        return entry.LocalizedJson;
    }

    private static CacheEntry GetEntry(ItemDrop.ItemData item, int qualityLevel, bool includeDetails)
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
            return entry;
        }

        entry = new CacheEntry { Quality = qualityLevel, IncludeDetails = includeDetails };
        try
        {
            // Bows read the Elemental Magic skill while the tooltip is worked out, as they do in ItemData.GetTooltip
            // (see ElementalArchery). Opened here too, for the API, which does not go through GetTooltip.
            bool remapped = ElementalArchery.EnterTooltipScope(item);
            try
            {
                entry.Data = TooltipGatherer.Gather(item, qualityLevel, includeDetails);
            }
            finally
            {
                ElementalArchery.ExitTooltipScope(remapped);
            }

            if (entry.Data != null && Editors.Count > 0)
            {
                entry.Data = ApplyEditors(item, entry.Data, out string json);
                entry.Json = json;
            }
        }
        catch (Exception e)
        {
            // Vanilla's text rather than an exception out of InventoryGui.Update every frame. Cached as a miss,
            // so the frame's other readers (the title and hints, Auga, the API) do not throw either.
            LogFailure(item, e);
            entry.Data = null;
            entry.Json = null;
        }

        Cache[item] = entry;
        return entry;
    }

    /// <summary>Drops the cached data, after an item's magic data or the editor chain changed.</summary>
    internal static void InvalidateCache()
    {
        Cache.Clear();
        EditorRuns.Clear();
    }

    internal static void LogFailure(ItemDrop.ItemData item, Exception e)
    {
        if (LoggedFailures.Add($"{item.m_shared?.m_name}|{e.Message}"))
        {
            EpicLoot.LogErrorForce($"Tooltip for {item.m_shared?.m_name} failed, showing vanilla's (logged once): {e}");
        }
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
        // The same key ItemData.GetTooltip gathered under, so this is the cached data (or the cached miss).
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

    private sealed class TooltipEditor
    {
        public string Id;
        public Func<ItemDrop.ItemData, string, string> Edit;
    }

    private static readonly List<TooltipEditor> Editors = new List<TooltipEditor>();

    // The editor chain's last run per item: the gathered data it was given, as JSON, and what it produced. A
    // hovered item gathers the same data from one frame to the next almost always, so the chain (a JSON round
    // trip per editor, and the editors' own work) reruns only when that JSON changes. An entry not read in the
    // previous frame is dropped, so an item hovered again starts with a fresh run.
    private sealed class EditorRun
    {
        public string Input;
        public ItemTooltipData Output;
        // Output as JSON when it is known to equal Input (no editor changed anything), else null.
        public string OutputJson;
        public int Frame;
    }

    private static readonly Dictionary<ItemDrop.ItemData, EditorRun> EditorRuns = new Dictionary<ItemDrop.ItemData, EditorRun>();
    private static readonly List<ItemDrop.ItemData> StaleRuns = new List<ItemDrop.ItemData>();
    private static int _editorRunsFrame = -1;

    internal static bool HasEditor(string id)
    {
        return Editors.Exists(x => x.Id == id);
    }

    internal static void AddEditor(string id, Func<ItemDrop.ItemData, string, string> editor)
    {
        Editors.Add(new TooltipEditor { Id = id, Edit = editor });
        InvalidateCache();
    }

    internal static bool RemoveEditor(string id)
    {
        bool removed = Editors.RemoveAll(x => x.Id == id) > 0;
        InvalidateCache();
        return removed;
    }

    internal static List<string> GetEditorIds()
    {
        return Editors.ConvertAll(x => x.Id);
    }

    /// <param name="outputJson">The result as JSON when it is already known, else null.</param>
    private static ItemTooltipData ApplyEditors(ItemDrop.ItemData item, ItemTooltipData data, out string outputJson)
    {
        PruneEditorRuns();
        string json = JsonConvert.SerializeObject(data, JsonSettings);
        if (EditorRuns.TryGetValue(item, out EditorRun run) && run.Input == json)
        {
            run.Frame = Time.frameCount;
            outputJson = run.OutputJson;
            return run.Output;
        }

        ItemTooltipData output = RunEditors(item, data, json, out bool edited);
        outputJson = edited ? null : json;
        EditorRuns[item] = new EditorRun { Input = json, Output = output, OutputJson = outputJson, Frame = Time.frameCount };
        return output;
    }

    private static void PruneEditorRuns()
    {
        int frame = Time.frameCount;
        if (frame == _editorRunsFrame)
        {
            return;
        }

        _editorRunsFrame = frame;
        foreach (KeyValuePair<ItemDrop.ItemData, EditorRun> pair in EditorRuns)
        {
            if (pair.Value.Frame < frame - 1)
            {
                StaleRuns.Add(pair.Key);
            }
        }

        foreach (ItemDrop.ItemData key in StaleRuns)
        {
            EditorRuns.Remove(key);
        }

        StaleRuns.Clear();
    }

    // Each editor gets the previous one's JSON and returns its own. One that throws, or returns something that
    // is not tooltip data, is removed from the chain: it would fail the same way on the next run, and a Mono
    // exception per tooltip is not cheap. Registering it again puts it back.
    private static ItemTooltipData RunEditors(ItemDrop.ItemData item, ItemTooltipData data, string json, out bool edited)
    {
        edited = false;
        ItemTooltipData result = data;
        foreach (TooltipEditor editor in Editors.ToArray())
        {
            try
            {
                string output = editor.Edit(item, json);
                if (string.IsNullOrEmpty(output) || output == json)
                {
                    continue;
                }

                ItemTooltipData parsed = JsonConvert.DeserializeObject<ItemTooltipData>(output, JsonSettings);
                if (parsed == null)
                {
                    throw new JsonException("the editor returned no tooltip data");
                }

                parsed.Normalize();
                result = parsed;
                json = output;
                edited = true;
            }
            catch (Exception ex)
            {
                Editors.Remove(editor);
                EpicLoot.LogErrorForce($"[EpicLoot.API] Tooltip editor '{editor.Id}' threw and was removed from the " +
                    $"chain; register it again to retry: {ex}");
            }
        }

        return result;
    }

    #endregion
}
