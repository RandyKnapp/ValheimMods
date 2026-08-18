using BepInEx.Logging;
using System;

namespace Common {
    /// <summary>
    /// Level-gated logging for the shared Common code, forwarding to the owning plugin's log source.
    ///
    /// Named ModLogger rather than Logger on purpose: <c>Common.Logger</c> would be ambiguous with
    /// <see cref="BepInEx.Logging.Logger"/> in the many mod files that do <c>using Common;</c>.
    /// </summary>
    public static class ModLogger {
        public static LogLevel Level = LogLevel.Info;

        internal static void OnDebugModeChanged(object sender, EventArgs e) {
            RefreshLevel();
        }

        /// <summary>Re-reads <see cref="ModContext.EnableDebugMode"/> and adjusts the gate.</summary>
        public static void RefreshLevel() {
            Level = ModContext.DebugEnabled ? LogLevel.Debug : LogLevel.Info;
        }

        public static void SetDebugLogging(bool state) {
            Level = state ? LogLevel.Debug : LogLevel.Info;
        }

        public static void LogDebug(string message) {
            if (Level >= LogLevel.Debug) { Write(LogLevel.Info, "[DEBUG]" + message); }
        }

        public static void LogInfo(string message) {
            if (Level >= LogLevel.Info) { Write(LogLevel.Info, message); }
        }

        public static void LogWarning(string message) {
            if (Level >= LogLevel.Warning) { Write(LogLevel.Warning, message); }
        }

        public static void LogError(string message) {
            if (Level >= LogLevel.Error) { Write(LogLevel.Error, message); }
        }

        // ModContext.Log is null until Initialize runs (and after the plugin is torn down). Falling back to
        // the Unity log keeps early/late diagnostics visible instead of throwing an NRE inside a config handler.
        private static void Write(LogLevel level, string message) {
            ManualLogSource log = ModContext.Log;
            if (log == null) {
                UnityEngine.Debug.Log($"[Common][{level}] {message}");
                return;
            }
            log.Log(level, message);
        }
    }
}
