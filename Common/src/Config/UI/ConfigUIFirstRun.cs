using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using System;
using System.IO;
using UnityEngine;

namespace Common {

    // A mod's own, per-profile say over its first-run popup, bound as a client setting. The member names
    // are the words written into every player's cfg file, so they can never be renamed; the values are
    // explicit so a number typed into the file keeps meaning the same thing.
    internal enum FirstRunMode {
        Auto = 0,              // show it once per user: the shared record decides
        ShowNextLaunch = 1,    // show it on the next launch in this profile, then back to Auto
        Never = 2,             // never open it by itself in this profile
    }

    // Which first-run popups this user has already seen, recorded once per user rather than per profile.
    //
    // Every mod manager profile (Gale, r2modman) has a BepInEx/config of its own, so a flag kept there
    // greets the same user again in every new profile. This record lives next to Valheim's own per-user
    // files instead (Application.persistentDataPath -- LocalLow/IronGate/Valheim on Windows), where every
    // profile sees it. Deleting it brings every mod's first-run popup back.
    //
    // FROZEN: the folder, file and section names below, and the meaning of each value -- the highest
    // revision of that mod's popup the user has seen, 0 or absent for none. Every copy of this folder, in
    // every mod, reads and writes the same file.
    internal static class ConfigUIFirstRun {
        // --- FROZEN ---
        internal const string FolderName = "ModQuickConfig";
        internal const string FileName = "FirstRun.cfg";
        internal const string Section = "FirstRun";
        // --- END FROZEN ---

        // Suffix for a first-run popup's key in the startup popup queue.
        internal const string QueueKeySuffix = ".FirstRun";

        // Stands in for "no such line" when a retired key is bound to read it. No cfg value is ever this.
        private const string LegacyPlaceholder = "\u0001";

        private static string recordPath;

        // Resolved on first use, on the main thread: persistentDataPath is a Unity call, and this folder is
        // compiled into mods that never touch the record at all.
        internal static string RecordPath {
            get {
                if (recordPath == null) {
                    recordPath = Path.Combine(Path.Combine(Application.persistentDataPath, FolderName), FileName);
                }
                return recordPath;
            }
        }

        // Whether modKey's popup should open this launch. Never and ShowNextLaunch answer for themselves;
        // Auto asks the record. A mod without a setting of its own passes null, which counts as Auto.
        internal static bool ShouldShow(string modKey, int revision, ConfigEntry<FirstRunMode> mode) {
            if (GUIManager.IsHeadless()) { return false; }
            switch (ModeOf(mode)) {
                case FirstRunMode.Never: return false;
                case FirstRunMode.ShowNextLaunch: return true;
                default: return SeenRevision(modKey) < revision;
            }
        }

        // The highest revision of modKey's popup this user has seen, 0 when none. A record that exists but
        // cannot be read reports int.MaxValue -- seen -- so a folder that cannot be read stays quiet rather
        // than opening the popup on every launch.
        internal static int SeenRevision(string modKey) {
            if (GUIManager.IsHeadless() || IsValidKey(modKey) == false) { return int.MaxValue; }
            string path = null;
            try {
                path = RecordPath;
                if (File.Exists(path) == false) { return 0; }
                return Math.Max(0, BindSeen(OpenRecord(), modKey).Value);
            } catch (Exception e) {
                ModLogger.LogWarning($"Could not read the first-run record at {path}, so the '{modKey}' first-run popup " +
                    $"is treated as seen: {e.Message}");
                return int.MaxValue;
            }
        }

        // Records that this user has seen revision `revision` of modKey's popup. Never lowers a revision
        // already there. Call it when the popup closes, by whatever route.
        internal static void MarkSeen(string modKey, int revision) {
            if (GUIManager.IsHeadless() || IsValidKey(modKey) == false || revision <= 0) { return; }
            string path = null;
            try {
                path = RecordPath;
                ConfigFile record = OpenRecord();
                ConfigEntry<int> seen = BindSeen(record, modKey);
                if (seen.Value >= revision) { return; }
                seen.Value = revision;
                record.Save();   // creates the folder when it is missing
            } catch (Exception e) {
                ModLogger.LogWarning($"Could not write the first-run record at {path}: {e.Message}");
            }
        }

        // Queues modKey's first-run popup on the shared startup popup queue, if it should show at all. open
        // and isOpen follow StartupPopupQueue.Enqueue's contract; the popup must call MarkSeen when it closes.
        //
        // ShowNextLaunch is spent the moment the popup opens, not when it closes: the popup may itself offer
        // "show this again next launch", and a reset at close would throw that choice away.
        internal static bool QueueFirstRunPopup(string modKey, int revision, ConfigEntry<FirstRunMode> mode,
            int order, Func<bool> open, Func<bool> isOpen) {
            if (open == null || isOpen == null || ShouldShow(modKey, revision, mode) == false) { return false; }
            return ConfigUIStartupPopups.Enqueue(modKey + QueueKeySuffix, order,
                () => OpenFirstRun(modKey, revision, mode, open), isOpen);
        }

        private static bool OpenFirstRun(string modKey, int revision, ConfigEntry<FirstRunMode> mode, Func<bool> open) {
            // Asked again: the setting may have been changed on the main menu while this waited its turn.
            if (ShouldShow(modKey, revision, mode) == false) { return false; }
            bool spent = false;
            if (mode != null && mode.Value == FirstRunMode.ShowNextLaunch) {
                mode.Value = FirstRunMode.Auto;
                spent = true;
            }
            bool opened = false;
            try {
                opened = open();
            } finally {
                if (opened == false && spent) { mode.Value = FirstRunMode.ShowNextLaunch; }
            }
            return opened;
        }

        // Reads a retired key's value out of cfg and drops the key, so the next Save no longer writes it.
        // BepInEx writes an unbound line back out forever, and binding it is the only way to read it, so the
        // key is bound as a string for a moment and removed again. Returns false when the file never had it.
        internal static bool TakeLegacyEntry(ConfigFile cfg, string section, string key, out string raw) {
            raw = null;
            if (cfg == null || string.IsNullOrEmpty(section) || string.IsNullOrEmpty(key)) { return false; }
            bool saveOnSet = cfg.SaveOnConfigSet;
            // Off while probing, or the bind would write the placeholder straight into the file.
            cfg.SaveOnConfigSet = false;
            try {
                ConfigDefinition definition = new ConfigDefinition(section, key);
                if (cfg.ContainsKey(definition)) {
                    raw = cfg[definition].GetSerializedValue();
                } else {
                    string value = cfg.Bind(definition, LegacyPlaceholder, new ConfigDescription("Retired.")).Value;
                    raw = value == LegacyPlaceholder ? null : value;
                }
                cfg.Remove(definition);
                if (saveOnSet) { cfg.Save(); }
            } catch (Exception e) {
                ModLogger.LogWarning($"Could not read the retired setting [{section}] {key}: {e.Message}");
                raw = null;
            } finally {
                cfg.SaveOnConfigSet = saveOnSet;
            }
            return raw != null;
        }

        // A fresh instance for every read and write, never one kept for the session. Every mod's copy works
        // on the same file, and the user may delete it while the game runs: a kept instance would still
        // hold the values it read at startup, and its next Save would write every other mod's line back
        // into the file the user just cleared. A fresh one reads what is there now -- nothing, if the file
        // is gone -- and writes back only that, plus its own line.
        private static ConfigFile OpenRecord() {
            BepInPlugin owner = new BepInPlugin("ModQuickConfig.FirstRun",
                "ModQuickConfig first-run record - delete this file to see every mod's first-run popup again", "1.0.0");
            return new ConfigFile(RecordPath, false, owner) { SaveOnConfigSet = false };
        }

        private static ConfigEntry<int> BindSeen(ConfigFile record, string modKey) {
            return record.Bind(Section, modKey, 0, new ConfigDescription(
                "The newest revision of this mod's first-run popup you have seen. 0 has not seen it; delete the line to see it again."));
        }

        private static FirstRunMode ModeOf(ConfigEntry<FirstRunMode> mode) {
            if (mode == null) { return FirstRunMode.Auto; }
            FirstRunMode value = mode.Value;
            // BepInEx accepts any number for an enum, so "7" in the file arrives as an undefined value.
            return Enum.IsDefined(typeof(FirstRunMode), value) ? value : FirstRunMode.Auto;
        }

        // BepInEx refuses these in a key, and throws from Bind rather than saying so.
        private static bool IsValidKey(string modKey) {
            if (string.IsNullOrEmpty(modKey) || modKey.Trim() != modKey || modKey.IndexOfAny(InvalidKeyChars) >= 0) {
                ModLogger.LogError($"'{modKey}' cannot be a first-run record key; use a plain identifier.");
                return false;
            }
            return true;
        }

        private static readonly char[] InvalidKeyChars = { '=', '\n', '\t', '\\', '"', '\'', '[', ']' };
    }
}
