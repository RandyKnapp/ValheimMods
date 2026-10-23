using EpicLoot.Compendium;
using Jotunn.Managers;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.QuickConfig;

/// <summary>
/// The runtime style pass for the Quick Configure prefabs. The placeholder TMP font authored in Unity
/// is replaced with Valheim's own per text role (the same pass TemperPanel runs), the wood panel
/// backgrounds get vanilla's lit panel material, and the scroll lists get the scroll fix the merchant
/// panel and enchanting table use.
/// </summary>
internal static class QuickConfigStyle {
    // Off by default so the authored look wins. Flip to restyle plain uGUI sliders and buttons with
    // Jotunn's Valheim-styled sprites when a prefab shipped without its own.
    private const bool UseJotunnWidgetStyle = false;

    private const string LitPanelMaterial = "litpanel";
    private const string PanelSpritePrefix = "woodpanel";
    private static Material litPanel;
    private static bool warnedNoLitPanel;

    // The enchanting table's value (vanilla's settings lists use the same): since Call to Arms a wheel
    // notch scrolls by a small fraction of what it did, and the authored 30 barely moves a list.
    private const float ScrollSensitivity = 800f;
    // The merchant panel's and craft result dialog's handle size. The ScrollRect would shrink the handle
    // to the visible fraction, which on a long list (the item picker) leaves a sliver too small to see or grab.
    private const float ScrollbarHandleSize = 0.4f;

    internal static void Apply(GameObject root) {
        if (root == null) { return; }
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true)) {
            try {
                MagicFontManager.Apply(text, RoleOf(text));
            } catch (Exception e) {
                EpicLoot.LogWarning($"Quick Configure could not restyle '{text.name}': {e.Message}");
            }
        }
        ApplyPanelLighting(root);
        ApplyScrollFix(root);
#pragma warning disable CS0162 // Unreachable code: the constant is the switch.
        if (UseJotunnWidgetStyle) { ApplyJotunnWidgets(root); }
#pragma warning restore CS0162
    }

    // NorseBoldOutline for the panel title, page titles and Row_Header labels; AveriaSansLibre for
    // text typed into a field and the status line; AveriaSansLibreOutline for everything else
    // (row labels, button captions, notes).
    private static MagicFontManager.TMP_FontOptions RoleOf(TMP_Text text) {
        if (text.name == "Title" || IsHeaderLabel(text.transform)) {
            return MagicFontManager.TMP_FontOptions.NorseBoldOutline;
        }
        if (text.name == "Status" || text.GetComponentInParent<TMP_InputField>(true) != null) {
            return MagicFontManager.TMP_FontOptions.AveriaSansLibre;
        }
        return MagicFontManager.TMP_FontOptions.AveriaSansLibreOutline;
    }

    // A header row keeps its template name (Row_Header, Row_Header (2)); its label may sit a level down.
    private static bool IsHeaderLabel(Transform text) {
        Transform current = text.parent;
        for (int depth = 0; current != null && depth < 3; depth++, current = current.parent) {
            if (current.name.StartsWith("Row_Header", StringComparison.Ordinal)) { return true; }
        }
        return false;
    }

    // Vanilla draws its panel backgrounds (inventory, store, settings, the main menu's dialogs) with the
    // litpanel material, whose shader follows the scene's light, so they dim at night. The prefabs can
    // only carry the default UI material, which stays at full brightness, since litpanel exists only in
    // the game. So every image showing a woodpanel_* sprite is switched here, the rule vanilla follows:
    // panels are lit, buttons and fields are not.
    private static void ApplyPanelLighting(GameObject root) {
        Material material = GetLitPanel();
        if (material == null) { return; }
        foreach (Image image in root.GetComponentsInChildren<Image>(true)) {
            if (image.sprite != null && image.sprite.name.StartsWith(PanelSpritePrefix, StringComparison.Ordinal)) {
                image.material = material;
            }
        }
    }

    // Apply runs again on a row after every list rebuild, so a ScrollRect is only fixed once; the marker
    // keeps the size listener from stacking.
    private static void ApplyScrollFix(GameObject root) {
        foreach (ScrollRect scrollRect in root.GetComponentsInChildren<ScrollRect>(true)) {
            if (scrollRect.GetComponent<ScrollFixed>() != null) { continue; }
            scrollRect.gameObject.AddComponent<ScrollFixed>();
            scrollRect.scrollSensitivity = ScrollSensitivity;
            Scrollbar scrollbar = scrollRect.verticalScrollbar;
            if (scrollbar == null) { continue; }
            scrollbar.size = ScrollbarHandleSize;
            scrollRect.onValueChanged.AddListener(_ => scrollbar.size = ScrollbarHandleSize);
        }
    }

    private class ScrollFixed : MonoBehaviour { }

    private static Material GetLitPanel() {
        if (litPanel != null) { return litPanel; }
        try {
            litPanel = PrefabManager.Cache.GetPrefab<Material>(LitPanelMaterial);
        } catch (Exception e) {
            EpicLoot.LogWarning($"Quick Configure could not look up the {LitPanelMaterial} material: {e.Message}");
        }
        if (litPanel == null && warnedNoLitPanel == false) {
            warnedNoLitPanel = true;
            EpicLoot.LogWarning($"Quick Configure found no {LitPanelMaterial} material; its panels keep full brightness.");
        }
        return litPanel;
    }

    private static void ApplyJotunnWidgets(GameObject root) {
        if (GUIManager.Instance == null) { return; }
        foreach (Slider slider in root.GetComponentsInChildren<Slider>(true)) {
            try { GUIManager.Instance.ApplySliderStyle(slider); } catch (Exception) { /* keep the authored look */ }
        }
        foreach (Button button in root.GetComponentsInChildren<Button>(true)) {
            try { GUIManager.Instance.ApplyButtonStyle(button); } catch (Exception) { /* keep the authored look */ }
        }
    }
}
