using BepInEx.Configuration;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Common {
    // Shared IMGUI widgets used by the item/piece config drawers.
    //
    // Every entity (item or piece) collapses its many ConfigEntries into a single ConfigurationManager
    // row backed by a CustomDrawer. The underlying ConfigEntries are untouched (same keys, same sync) -
    // only their display is changed: all but one "anchor" entry get Browsable = false so the manager no
    // longer lays each of them out every frame, which is what caused the lag.
    internal static class ConfigDrawHelpers {
        // Edit buffers so partial typing in text/number fields doesn't immediately overwrite the live value.
        private static readonly Dictionary<object, string> TextBuffer = new Dictionary<object, string>();
        // In-progress slider values; committed to the ConfigEntry only on mouse release to avoid a disk
        // write (SaveOnConfigSet is true) on every frame of a drag.
        private static readonly Dictionary<object, float> SliderPending = new Dictionary<object, float>();

        private static GUIStyle _headerButton;
        private static GUIStyle _groupLabel;
        private static GUIStyle _dim;

        internal static GUIStyle HeaderButton => _headerButton;
        internal static GUIStyle Dim => _dim;

        internal static void EnsureStyles() {
            if (_headerButton != null) { return; }
            _headerButton = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            _groupLabel = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            _dim = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic, fontSize = 11 };
        }

        // Fetches the Jotunn ConfigurationManagerAttributes already attached by ConfigBinder.
        internal static ConfigurationManagerAttributes GetAttributes(ConfigEntryBase entry) =>
            entry?.Description?.Tags?.OfType<ConfigurationManagerAttributes>().FirstOrDefault();

        // Hides an entry from the Configuration Manager window without affecting saving or server sync.
        internal static void Hide(ConfigEntryBase entry) {
            ConfigurationManagerAttributes attr = GetAttributes(entry);
            if (attr != null) { attr.Browsable = false; }
        }

        internal static void GroupHeader(string text) {
            GUILayout.Space(4f);
            GUILayout.Label(text, _groupLabel);
        }

        private static bool EnterPressed() =>
            Event.current.type == EventType.KeyDown &&
            (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);

        internal static void DrawBool(string label, ConfigEntry<bool> cfg) {
            bool v = GUILayout.Toggle(cfg.Value, " " + label);
            if (v != cfg.Value) { cfg.Value = v; }
        }

        // Free-text string field that commits on Enter or when focus leaves the field (never per keystroke,
        // so handlers like "crafting station changed" don't fire/warn on every character).
        internal static void DrawString(string label, ConfigEntry<string> cfg) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(230f));
            string ctrl = "str_" + cfg.GetHashCode();
            GUI.SetNextControlName(ctrl);
            bool focused = GUI.GetNameOfFocusedControl() == ctrl;
            string shown = focused && TextBuffer.TryGetValue(cfg, out string buf) ? buf : cfg.Value;
            string typed = GUILayout.TextField(shown, GUILayout.Width(230f));
            if (focused) {
                TextBuffer[cfg] = typed;
                if (EnterPressed() && cfg.Value != typed) { cfg.Value = typed; }
            } else if (TextBuffer.TryGetValue(cfg, out string pending)) {
                if (cfg.Value != pending) { cfg.Value = pending; }
                TextBuffer.Remove(cfg);
            }
            GUILayout.EndHorizontal();
        }

        // Cycler for string entries restricted to an AcceptableValueList (damage modifiers, piece category).
        internal static void DrawChoice(string label, ConfigEntry<string> cfg) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(230f));
            if (cfg.Description.AcceptableValues is AcceptableValueList<string> list && list.AcceptableValues.Length > 0) {
                string[] vals = list.AcceptableValues;
                int idx = Array.IndexOf(vals, cfg.Value);
                if (idx < 0) { idx = 0; }
                if (GUILayout.Button("◄", GUILayout.Width(26f))) { cfg.Value = vals[(idx - 1 + vals.Length) % vals.Length]; }
                GUILayout.Label(cfg.Value, GUILayout.Width(160f));
                if (GUILayout.Button("►", GUILayout.Width(26f))) { cfg.Value = vals[(idx + 1) % vals.Length]; }
            } else {
                GUILayout.Label(cfg.Value);
            }
            GUILayout.EndHorizontal();
        }

        internal static void DrawFloat(string label, ConfigEntry<float> cfg) {
            float min = 0f, max = Mathf.Max(100f, cfg.Value);
            if (cfg.Description.AcceptableValues is AcceptableValueRange<float> r) { min = r.MinValue; max = r.MaxValue; }
            float v = DrawSliderRow(label, cfg, cfg.Value, min, max, false);
            if (v != cfg.Value) { cfg.Value = Mathf.Clamp(v, min, max); }
        }

        internal static void DrawInt(string label, ConfigEntry<int> cfg) {
            float min = 0f, max = Mathf.Max(100f, cfg.Value);
            if (cfg.Description.AcceptableValues is AcceptableValueRange<int> r) { min = r.MinValue; max = r.MaxValue; }
            float v = DrawSliderRow(label, cfg, cfg.Value, min, max, true);
            int iv = Mathf.Clamp(Mathf.RoundToInt(v), (int)min, (int)max);
            if (iv != cfg.Value) { cfg.Value = iv; }
        }

        // Slider (deferred-commit on mouse release) + a buffered numeric text box (commit on Enter/blur).
        // Returns the value the caller should write; never writes the ConfigEntry itself.
        private static float DrawSliderRow(string label, object key, float current, float min, float max, bool isInt) {
            float result = current;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(230f));

            float shown = SliderPending.TryGetValue(key, out float pend) ? pend : current;
            float slid = GUILayout.HorizontalSlider(shown, min, max, GUILayout.Width(150f));
            if (Mathf.Abs(slid - shown) > Mathf.Epsilon) { SliderPending[key] = slid; }
            if (SliderPending.TryGetValue(key, out float dragging)) {
                // Deferred commit: while the mouse is held, only the pending display value changes.
                // Returning the dragged value every frame wrote cfg.Value (and with SaveOnConfigSet, the
                // .cfg file) once per frame of the drag -- commit exactly once, on release.
                if (!Input.GetMouseButton(0)) {
                    SliderPending.Remove(key);
                    result = dragging;
                }
            }

            // Numeric text box for exact entry. Shows the in-flight drag value while one exists.
            float uiValue = SliderPending.TryGetValue(key, out float pendingDrag) ? pendingDrag : result;
            string ctrl = "num_" + key.GetHashCode();
            GUI.SetNextControlName(ctrl);
            bool focused = GUI.GetNameOfFocusedControl() == ctrl;
            string live = isInt ? Mathf.RoundToInt(uiValue).ToString() : uiValue.ToString("0.###");
            string shownText = focused && TextBuffer.TryGetValue(key, out string buf) ? buf : live;
            string typed = GUILayout.TextField(shownText, GUILayout.Width(70f));
            if (focused) {
                TextBuffer[key] = typed;
                if (EnterPressed() && float.TryParse(typed, out float parsed)) { result = parsed; }
            } else if (TextBuffer.TryGetValue(key, out string pendingText)) {
                if (float.TryParse(pendingText, out float p)) { result = p; }
                TextBuffer.Remove(key);
            }

            GUILayout.Label($"[{(isInt ? min.ToString() : min.ToString("0.#"))} - {(isInt ? max.ToString() : max.ToString("0.#"))}]", _dim, GUILayout.Width(90f));
            GUILayout.EndHorizontal();
            return result;
        }
    }

    // Collapses an ItemDefinition's ~10-20 ConfigEntries into a single expandable, grouped drawer.
    internal static class ItemConfigDrawer {
        private static readonly Dictionary<string, bool> Expanded = new Dictionary<string, bool>();
        // CSV recipe rows being edited in-memory; only written back to the ConfigEntry on "Apply".
        private static readonly Dictionary<object, List<string[]>> RecipeRows = new Dictionary<object, List<string[]>>();
        private static int _order = 0;

        private static readonly KeyValuePair<string, ItemStat[]>[] StatGroups = new KeyValuePair<string, ItemStat[]>[] {
            new KeyValuePair<string, ItemStat[]>("Damage", new[] {
                ItemStat.slash, ItemStat.slash_per_level, ItemStat.blunt, ItemStat.blunt_per_level,
                ItemStat.pierce, ItemStat.pierce_per_level, ItemStat.fire, ItemStat.fire_per_level,
                ItemStat.lightning, ItemStat.lightning_per_level, ItemStat.frost, ItemStat.frost_per_level,
                ItemStat.poison, ItemStat.poison_per_level, ItemStat.spirit, ItemStat.spirit_per_level,
                ItemStat.pickaxe, ItemStat.pickaxe_per_level, ItemStat.chop, ItemStat.chop_per_level
            }),
            new KeyValuePair<string, ItemStat[]>("Combat", new[] {
                ItemStat.attack_force, ItemStat.primary_attack_force_multiply, ItemStat.secondary_attack_force_multiply,
                ItemStat.primary_attack_stamina, ItemStat.primary_attack_eitr, ItemStat.primary_attack_flat_health_cost,
                ItemStat.primary_attack_percent_health_cost, ItemStat.primary_attack_health_returned,
                ItemStat.primary_attack_damage_bonus_per_missing_hp, ItemStat.primary_attack_projectile_count,
                ItemStat.secondary_attack_stamina, ItemStat.secondary_attack_eitr, ItemStat.secondary_attack_flat_health_cost,
                ItemStat.secondary_attack_percent_health_cost, ItemStat.projectile_velocity, ItemStat.projectile_accuracy_max,
                ItemStat.bow_draw_speed, ItemStat.crossbow_reload_speed, ItemStat.crossbow_reload_stamina_drain,
                ItemStat.draw_stamina_drain
            }),
            new KeyValuePair<string, ItemStat[]>("Defense", new[] {
                ItemStat.block_armor, ItemStat.block_armor_per_level, ItemStat.parry,
                ItemStat.block_force, ItemStat.block_force_per_level
            }),
            new KeyValuePair<string, ItemStat[]>("Item", new[] {
                ItemStat.durability, ItemStat.durability_per_level, ItemStat.max_item_level,
                ItemStat.tool_level, ItemStat.movement_speed, ItemStat.amount
            }),
        };

        // Hides the item's sub-entries and turns CraftableCfg into the single visible drawer row.
        internal static void Attach(ItemDefinition itemdef) {
            ConfigDrawHelpers.Hide(itemdef.StationLVLCfg);
            ConfigDrawHelpers.Hide(itemdef.CraftAmountCfg);
            ConfigDrawHelpers.Hide(itemdef.CraftedAtCfg);
            ConfigDrawHelpers.Hide(itemdef.Recipe.RecipeConfig);
            if (itemdef.ModifableStats != null) {
                foreach (ItemStatConfig stat in itemdef.ModifableStats.Values) {
                    if (stat.Cfg != null) { ConfigDrawHelpers.Hide(stat.Cfg); }
                    if (stat.CfgInt != null) { ConfigDrawHelpers.Hide(stat.CfgInt); }
                }
            }
            if (itemdef.DamageMods != null) {
                foreach (HitCustomDamageMod mod in itemdef.DamageMods.Values) {
                    if (mod.DmgModCfg != null) { ConfigDrawHelpers.Hide(mod.DmgModCfg); }
                }
            }

            ConfigurationManagerAttributes anchor = ConfigDrawHelpers.GetAttributes(itemdef.CraftableCfg);
            if (anchor != null) {
                anchor.CustomDrawer = _ => Draw(itemdef);
                anchor.HideSettingName = true;
                anchor.HideDefaultButton = true;
                anchor.Category = itemdef.Category.ToString();
                anchor.Order = _order--;
            }
        }

        private static void Draw(ItemDefinition itemdef) {
            ConfigDrawHelpers.EnsureStyles();
            GUILayout.BeginVertical(GUI.skin.box);

            bool expanded = Expanded.TryGetValue(itemdef.DisplayName, out bool e) && e;
            if (GUILayout.Button((expanded ? "▾ " : "▸ ") + itemdef.Name, ConfigDrawHelpers.HeaderButton)) {
                expanded = !expanded;
                Expanded[itemdef.DisplayName] = expanded;
            }

            if (expanded) {
                bool admin = SynchronizationManager.Instance == null || SynchronizationManager.Instance.PlayerIsAdmin;
                if (!admin) { GUILayout.Label("Server-controlled – admin only.", ConfigDrawHelpers.Dim); }
                bool prevEnabled = GUI.enabled;
                GUI.enabled = admin;

                ConfigDrawHelpers.GroupHeader("Crafting");
                ConfigDrawHelpers.DrawBool("Craftable", itemdef.CraftableCfg);
                ConfigDrawHelpers.DrawString("Crafted at", itemdef.CraftedAtCfg);
                ConfigDrawHelpers.DrawInt("Station level", itemdef.StationLVLCfg);
                ConfigDrawHelpers.DrawInt("Craft amount", itemdef.CraftAmountCfg);

                DrawRecipeEditor(itemdef);

                HashSet<ItemStat> drawn = new HashSet<ItemStat>();
                foreach (KeyValuePair<string, ItemStat[]> group in StatGroups) {
                    List<ItemStat> present = group.Value.Where(s => IsConfigurable(itemdef, s)).ToList();
                    bool damageMods = group.Key == "Damage" && itemdef.DamageMods != null && itemdef.DamageMods.Count > 0;
                    if (present.Count == 0 && !damageMods) { continue; }
                    ConfigDrawHelpers.GroupHeader(group.Key);
                    foreach (ItemStat s in present) { DrawStat(itemdef, s); drawn.Add(s); }
                    if (damageMods) {
                        foreach (KeyValuePair<HitData.DamageType, HitCustomDamageMod> mod in itemdef.DamageMods) {
                            if (mod.Value.Configurable == false || mod.Value.DmgModCfg == null) { continue; }
                            ConfigDrawHelpers.DrawChoice(mod.Key + " resistance", mod.Value.DmgModCfg);
                        }
                    }
                }

                // Catch any configurable stat not covered by a group above.
                if (itemdef.ModifableStats != null) {
                    List<ItemStat> other = itemdef.ModifableStats.Keys.Where(s => !drawn.Contains(s) && IsConfigurable(itemdef, s)).ToList();
                    if (other.Count > 0) {
                        ConfigDrawHelpers.GroupHeader("Other");
                        foreach (ItemStat s in other) { DrawStat(itemdef, s); }
                    }
                }

                GUI.enabled = prevEnabled;
            }

            GUILayout.EndVertical();
        }

        private static bool IsConfigurable(ItemDefinition itemdef, ItemStat s) =>
            itemdef.ModifableStats != null && itemdef.ModifableStats.TryGetValue(s, out ItemStatConfig sc) && sc.Configurable;

        private static void DrawStat(ItemDefinition itemdef, ItemStat s) {
            ItemStatConfig sc = itemdef.ModifableStats[s];
            string label = s.ToString().Replace('_', ' ');
            if (sc.IsInt) { ConfigDrawHelpers.DrawInt(label, sc.CfgInt); } else { ConfigDrawHelpers.DrawFloat(label, sc.Cfg); }
        }

        // Recipe shown as one row per resource: prefab (string) + amount (int) + per-level (int).
        // Edits stay in-memory until "Apply", which re-serializes to the existing CSV format and writes it
        // to the ConfigEntry, firing the existing SettingChanged -> ValidateRecipeConfig pipeline.
        private static void DrawRecipeEditor(ItemDefinition itemdef) {
            ConfigEntry<string> cfg = itemdef.Recipe.RecipeConfig;
            ConfigDrawHelpers.GroupHeader("Recipe");

            if (!RecipeRows.TryGetValue(cfg, out List<string[]> rows)) {
                rows = ParseCsv(cfg.Value, 3);
                RecipeRows[cfg] = rows;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Resource (prefab)", GUILayout.Width(200f));
            GUILayout.Label("Amount", GUILayout.Width(60f));
            GUILayout.Label("Per level", GUILayout.Width(70f));
            GUILayout.EndHorizontal();

            for (int i = 0; i < rows.Count; i++) {
                string[] row = rows[i];
                GUILayout.BeginHorizontal();
                row[0] = GUILayout.TextField(row[0] ?? "", GUILayout.Width(200f));
                row[1] = DigitsOnly(GUILayout.TextField(row[1] ?? "", GUILayout.Width(60f)));
                row[2] = DigitsOnly(GUILayout.TextField(row[2] ?? "", GUILayout.Width(70f)));
                bool remove = GUILayout.Button("✕", GUILayout.Width(26f));
                GUILayout.EndHorizontal();
                if (remove) { rows.RemoveAt(i); i--; }
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add resource", GUILayout.Width(120f))) { rows.Add(new[] { "", "1", "0" }); }
            if (GUILayout.Button("Apply", GUILayout.Width(80f))) { cfg.Value = SerializeCsv(rows); }
            if (GUILayout.Button("Reload", GUILayout.Width(80f))) { RecipeRows[cfg] = ParseCsv(cfg.Value, 3); }
            GUILayout.EndHorizontal();
            GUILayout.Label("Applied: " + cfg.Value, ConfigDrawHelpers.Dim);
        }

        internal static List<string[]> ParseCsv(string csv, int cols) {
            List<string[]> rows = new List<string[]>();
            foreach (string part in (csv ?? "").Split('|')) {
                if (string.IsNullOrEmpty(part) || part.Trim().Length == 0) { continue; }
                string[] seg = part.Split(',');
                string[] row = new string[cols];
                for (int i = 0; i < cols; i++) { row[i] = i < seg.Length ? seg[i] : ""; }
                rows.Add(row);
            }
            return rows;
        }

        internal static string SerializeCsv(List<string[]> rows) =>
            string.Join("|", rows.Where(r => !string.IsNullOrEmpty(r[0]) && r[0].Trim().Length > 0).Select(r => string.Join(",", r)).ToArray());

        internal static string DigitsOnly(string s) =>
            new string((s ?? "").Where(ch => char.IsDigit(ch) || ch == '-').ToArray());
    }

    // Collapses a building piece's ConfigEntries into a single expandable, grouped drawer.
    internal static class PieceConfigDrawer {
        private static readonly Dictionary<string, bool> Expanded = new Dictionary<string, bool>();
        private static readonly Dictionary<object, List<string[]>> CostRows = new Dictionary<object, List<string[]>>();
        private static int _order = 0;

        // Hides the piece's sub-entries and turns Enabled into the single visible drawer row.
        internal static void Attach(PieceLoader.BuildPiece jp) {
            ConfigDrawHelpers.Hide(jp.Cfgs.RequiresWorkbench);
            ConfigDrawHelpers.Hide(jp.Cfgs.Workbench);
            ConfigDrawHelpers.Hide(jp.Cfgs.PieceCategory);
            ConfigDrawHelpers.Hide(jp.Cfgs.PieceCost);

            ConfigurationManagerAttributes anchor = ConfigDrawHelpers.GetAttributes(jp.Cfgs.Enabled);
            if (anchor != null) {
                anchor.CustomDrawer = _ => Draw(jp);
                anchor.HideSettingName = true;
                anchor.HideDefaultButton = true;
                anchor.Category = "Building Pieces";
                anchor.Order = _order--;
            }
        }

        private static void Draw(PieceLoader.BuildPiece jp) {
            ConfigDrawHelpers.EnsureStyles();
            GUILayout.BeginVertical(GUI.skin.box);

            bool expanded = Expanded.TryGetValue(jp.Name, out bool e) && e;
            if (GUILayout.Button((expanded ? "▾ " : "▸ ") + jp.Name, ConfigDrawHelpers.HeaderButton)) {
                expanded = !expanded;
                Expanded[jp.Name] = expanded;
            }

            if (expanded) {
                bool admin = SynchronizationManager.Instance == null || SynchronizationManager.Instance.PlayerIsAdmin;
                if (!admin) { GUILayout.Label("Server-controlled – admin only.", ConfigDrawHelpers.Dim); }
                bool prevEnabled = GUI.enabled;
                GUI.enabled = admin;

                ConfigDrawHelpers.GroupHeader("Settings");
                ConfigDrawHelpers.DrawBool("Enabled", jp.Cfgs.Enabled);
                ConfigDrawHelpers.DrawBool("Requires workbench", jp.Cfgs.RequiresWorkbench);
                ConfigDrawHelpers.DrawString("Workbench", jp.Cfgs.Workbench);
                ConfigDrawHelpers.DrawChoice("Piece category", jp.Cfgs.PieceCategory);

                DrawCostEditor(jp);

                GUI.enabled = prevEnabled;
            }

            GUILayout.EndVertical();
        }

        // Building cost shown as one row per resource: prefab (string) + amount (int) + refundable (bool).
        private static void DrawCostEditor(PieceLoader.BuildPiece jp) {
            ConfigEntry<string> cfg = jp.Cfgs.PieceCost;
            ConfigDrawHelpers.GroupHeader("Building Cost");

            if (!CostRows.TryGetValue(cfg, out List<string[]> rows)) {
                rows = ItemConfigDrawer.ParseCsv(cfg.Value, 3);
                CostRows[cfg] = rows;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Resource (prefab)", GUILayout.Width(200f));
            GUILayout.Label("Amount", GUILayout.Width(60f));
            GUILayout.Label("Refund", GUILayout.Width(90f));
            GUILayout.EndHorizontal();

            for (int i = 0; i < rows.Count; i++) {
                string[] row = rows[i];
                GUILayout.BeginHorizontal();
                row[0] = GUILayout.TextField(row[0] ?? "", GUILayout.Width(200f));
                row[1] = ItemConfigDrawer.DigitsOnly(GUILayout.TextField(row[1] ?? "", GUILayout.Width(60f)));
                bool refund = (row[2] ?? "").Trim().ToLower() != "false";
                bool nr = GUILayout.Toggle(refund, refund ? " refundable" : " no refund", GUILayout.Width(110f));
                row[2] = nr ? "true" : "false";
                bool remove = GUILayout.Button("✕", GUILayout.Width(26f));
                GUILayout.EndHorizontal();
                if (remove) { rows.RemoveAt(i); i--; }
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add resource", GUILayout.Width(120f))) { rows.Add(new[] { "", "1", "true" }); }
            if (GUILayout.Button("Apply", GUILayout.Width(80f))) { cfg.Value = ItemConfigDrawer.SerializeCsv(rows); }
            if (GUILayout.Button("Reload", GUILayout.Width(80f))) { CostRows[cfg] = ItemConfigDrawer.ParseCsv(cfg.Value, 3); }
            GUILayout.EndHorizontal();
            GUILayout.Label("Applied: " + cfg.Value, ConfigDrawHelpers.Dim);
        }
    }
}
