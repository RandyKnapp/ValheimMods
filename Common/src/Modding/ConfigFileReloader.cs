using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Common {
    /// <summary>
    /// Reloads files the owning mod reads from disk when they change: its BepInEx .cfg (see
    /// <see cref="Begin"/>) and any other file a caller registers with <see cref="Watch"/>, such as
    /// EpicLoot's baseconfig json files.
    ///
    /// Driven by each file's timestamp+size rather than directly by FileSystemWatcher events, for two
    /// reasons that both only bite on Linux servers:
    ///
    /// Trailing edge, not leading. One save is a burst of events, and on Linux the first of them
    /// arrives as soon as the writer truncates the file - before any of the new content is there.
    /// Reloading on that first event and then ignoring the rest of the burst (a leading-edge debounce,
    /// which is what both mods used to do) reads an empty or half written file and never looks again,
    /// so the edit appears to be ignored until the next restart. It reads clean on Windows only by
    /// accident: the writer holds a share lock there, the partial read throws, and a later event in
    /// the same burst retries. Worse, applying a half read file with SaveOnConfigSet on writes the
    /// merged result straight back over the file the admin is still uploading. So: wait for the file
    /// to stop changing, then read it once.
    ///
    /// Poll, not events alone. inotify only reports writes made through the same mount, so a config
    /// folder the host exposes over NFS/SFTP or a container bind mount can be edited without the game
    /// ever seeing an event. The stat below costs nothing and makes the reload work regardless; the
    /// watcher events just make it react in half a second instead of two.
    ///
    /// Files are applied in registration order, and a burst that touches several files is applied in
    /// one pass once the last of them holds still, so a folder upload lands in the dependency order
    /// the registrations encode rather than the order the writes happened to finish in.
    ///
    /// Common is a shared project, so the statics here belong to one mod assembly, not to all of them.
    /// </summary>
    public class ConfigFileReloader : MonoBehaviour {
        /// <summary>One registered file and what is known about its state on disk.</summary>
        public sealed class WatchedFile {
            /// <summary>Absolute path of the file.</summary>
            public string Path { get; }

            /// <summary>
            /// Reads the file into the live config. Exceptions are caught and logged by the scheduler.
            /// </summary>
            public Func<bool> Reload { get; }

            /// <summary>
            /// Whether an edit is applied while this machine is a client on someone else's server.
            /// False means the file obeys the owner rule (see <see cref="OwnsConfigOnDisk"/>): the edit
            /// is skipped while connected and applied after disconnecting.
            /// </summary>
            public bool ReloadOnConnectedClient { get; }

            // The state of the file as last read into the live config. A length of -1 never matches a
            // real stat, so the next check treats the file as changed (see MarkDirty).
            internal DateTime AppliedWriteTimeUtc;
            internal long AppliedLength = -1;

            // The state the previous check observed, whether or not it was applied. Distinguishes a
            // file that actually changed on disk from one whose applied stamp was merely forgotten.
            internal DateTime SeenWriteTimeUtc;
            internal long SeenLength = -1;

            // The state seen by the previous check, while waiting for it to settle, and the time at
            // which it counts as settled if it is still the same then.
            internal bool ChangePending;
            internal DateTime PendingWriteTimeUtc;
            internal long PendingLength;
            internal float PendingSettledAt;

            // So a client connected to a server hears once that its edit is waiting, not once per poll.
            internal bool SkipLogged;

            internal WatchedFile(string path, Func<bool> reload, bool reloadOnConnectedClient) {
                Path = path;
                Reload = reload;
                ReloadOnConnectedClient = reloadOnConnectedClient;
            }
        }

        /// <summary>How long a file must hold still before it is read.</summary>
        private const float QuietPeriodSeconds = 0.5f;

        /// <summary>How often the files are checked with no watcher event to prompt it.</summary>
        private const float PollIntervalSeconds = 2f;

        private static ConfigFileReloader instance;

        /// <summary>
        /// Set from a FileSystemWatcher callback, which may run on an arbitrary thread; consumed in
        /// Update. A plain flag rather than a scheduled call so it stays correct no matter which
        /// thread the event lands on.
        /// </summary>
        private static volatile bool checkRequested;

        // Registration order. The scheduler applies changed files in this order.
        private readonly List<WatchedFile> files = new List<WatchedFile>();

        private float nextCheck;

        /// <summary>
        /// Starts reloading the mod's BepInEx <paramref name="config"/> from
        /// <paramref name="configFilePath"/> when it changes. Safe to call more than once.
        /// </summary>
        public static void Begin(ConfigFile config, string configFilePath) {
            Watch(configFilePath, () => ReloadConfigFile(config));
        }

        /// <summary>
        /// Registers <paramref name="path"/> and stats it now: the file as it stands is what is already
        /// in memory, so call this right after the initial read and only later edits reload. Registering
        /// a path again replaces its entry in place, keeping its position in the apply order.
        /// </summary>
        /// <param name="reload">Reads the file into the live config.</param>
        /// <param name="reloadOnConnectedClient">
        /// True to apply edits even while connected to someone else's server; false (the default) to
        /// leave the server's copy in effect until disconnecting.
        /// </param>
        public static WatchedFile Watch(string path, Func<bool> reload, bool reloadOnConnectedClient = false) {
            ConfigFileReloader host = EnsureInstance();
            WatchedFile file = new WatchedFile(path, reload, reloadOnConnectedClient);
            (file.AppliedWriteTimeUtc, file.AppliedLength) = Stat(path);
            file.SeenWriteTimeUtc = file.AppliedWriteTimeUtc;
            file.SeenLength = file.AppliedLength;

            int existing = host.IndexOf(path);
            if (existing >= 0) {
                host.files[existing] = file;
            } else {
                host.files.Add(file);
            }

            return file;
        }

        /// <summary>
        /// Records the file as it stands now as what is in memory. For code that wrote the file itself
        /// and already read it back, so the scheduler does not apply it a second time. No-op for a path
        /// that is not registered.
        /// </summary>
        public static void MarkApplied(string path) {
            WatchedFile file = Find(path);
            if (file == null) { return; }

            (file.AppliedWriteTimeUtc, file.AppliedLength) = Stat(path);
            file.SeenWriteTimeUtc = file.AppliedWriteTimeUtc;
            file.SeenLength = file.AppliedLength;
            file.ChangePending = false;
            file.SkipLogged = false;
        }

        /// <summary>
        /// Forgets what was applied: memory no longer reflects the file (a server pushed its own copy).
        /// The file is read again on the next check that is allowed to apply it - on a connected client
        /// that is the first check after disconnecting, which is what restores the local configs then.
        /// The file has not changed on disk, so this is not reported as an edit. No-op for a path that
        /// is not registered.
        /// </summary>
        public static void MarkDirty(string path) {
            WatchedFile file = Find(path);
            if (file == null) { return; }

            file.AppliedWriteTimeUtc = default;
            file.AppliedLength = -1;
            file.ChangePending = false;
            file.SkipLogged = false;
        }

        /// <summary>Brings the next check forward to the following frame. Callable from any thread.</summary>
        public static void CheckSoon() {
            checkRequested = true;
        }

        /// <summary>
        /// Whether this machine owns what is in its config files.
        ///
        /// Mirrors Jotunn's own ReadWriteConfigFromDisk: no ZNet means the main menu, IsServer covers a
        /// dedicated server, a host and singleplayer. A connected client is left out because its
        /// server-synced entries hold the server's values, not the ones on its disk.
        ///
        /// For the .cfg this is belt and braces rather than the only guard: Jotunn already prefixes
        /// ConfigEntryBase.SetSerializedValue to drop disk writes to admin-only (IsAdminOnly, i.e.
        /// ConfigBinder.BindServerConfig) entries on a connected client, so those cannot be clobbered
        /// by a reload in any case. What this adds is that a client does not reload at all, which also
        /// keeps every file save from waking Jotunn's SynchronizeChangedConfig. The cost is that
        /// client-local (BindClientConfig) entries stop hot-reloading while connected to someone
        /// else's server; the pending edit is applied on disconnect, since the stamp is deliberately
        /// left unrecorded while this returns false. Files registered with
        /// <c>reloadOnConnectedClient: true</c> opt out of the rule.
        /// </summary>
        private static bool OwnsConfigOnDisk() {
            return ZNet.instance == null || ZNet.instance.IsServer();
        }

        private static ConfigFileReloader EnsureInstance() {
            // Unity's null check, not C#'s: the host object is destroyed if the game ever tears down
            // DontDestroyOnLoad, and the stale reference then compares equal to null and is replaced.
            if (instance != null) { return instance; }

            // Named per mod: every Common consumer gets its own copy of this type, so a scene dump
            // would otherwise show several identically named objects.
            GameObject host = new GameObject($"ConfigFileReloader ({ModContext.PluginGuid})");
            DontDestroyOnLoad(host);

            instance = host.AddComponent<ConfigFileReloader>();
            instance.nextCheck = Time.realtimeSinceStartup + PollIntervalSeconds;
            return instance;
        }

        private static WatchedFile Find(string path) {
            if (instance == null) { return null; }

            int index = instance.IndexOf(path);
            return index >= 0 ? instance.files[index] : null;
        }

        private int IndexOf(string path) {
            for (int i = 0; i < files.Count; i++) {
                if (string.Equals(files[i].Path, path, StringComparison.OrdinalIgnoreCase)) {
                    return i;
                }
            }

            return -1;
        }

        private void Update() {
            float now = Time.realtimeSinceStartup;
            if (checkRequested) {
                checkRequested = false;
                nextCheck = now;
            }

            if (now < nextCheck) { return; }

            bool owner = OwnsConfigOnDisk();
            bool unsettled = false;
            List<WatchedFile> due = null;

            foreach (WatchedFile file in files) {
                (DateTime writeTimeUtc, long length) = Stat(file.Path);
                bool changedOnDisk = writeTimeUtc != file.SeenWriteTimeUtc || length != file.SeenLength;
                file.SeenWriteTimeUtc = writeTimeUtc;
                file.SeenLength = length;

                if (length < 0) {
                    // Gone for the moment - mid rename, or deleted. Nothing to read; when it comes back
                    // its stamp will differ from the applied one and this picks it up then.
                    file.ChangePending = false;
                    continue;
                }

                if (writeTimeUtc == file.AppliedWriteTimeUtc && length == file.AppliedLength) {
                    file.ChangePending = false;
                    continue;
                }

                if (!owner && !file.ReloadOnConnectedClient) {
                    // The server's copy is in effect. The applied stamp is deliberately left alone so
                    // the edit is picked up by the first check after disconnecting. Reported only for a
                    // real edit, not for a file whose stamp MarkDirty forgot after a server push.
                    file.ChangePending = false;
                    if (changedOnDisk && !file.SkipLogged) {
                        ModLogger.LogInfo($"Config file {file.Path} changed on disk, but this client is " +
                            "connected to a server whose copy is in effect; the edit is applied after disconnecting.");
                        file.SkipLogged = true;
                    }
                    continue;
                }

                if (file.ChangePending && writeTimeUtc == file.PendingWriteTimeUtc && length == file.PendingLength) {
                    // Same expression nextCheck is set from below, so a check that is due never finds
                    // a file a rounding error short of settled.
                    if (now >= file.PendingSettledAt) {
                        (due ??= new List<WatchedFile>()).Add(file);
                    } else {
                        unsettled = true;
                    }
                    continue;
                }

                // Changed since the last read, or still being written. Only act once it has held the
                // same stamp for a whole quiet period, so a file mid-write is left alone until the
                // writer is done with it.
                file.ChangePending = true;
                file.PendingWriteTimeUtc = writeTimeUtc;
                file.PendingLength = length;
                file.PendingSettledAt = now + QuietPeriodSeconds;
                unsettled = true;
            }

            if (unsettled) {
                // Wait for the whole burst: a folder upload is applied in one ordered pass once the
                // last file holds still, not file by file as the writes happen to finish.
                nextCheck = now + QuietPeriodSeconds;
                return;
            }

            nextCheck = now + PollIntervalSeconds;
            if (due == null) { return; }

            foreach (WatchedFile file in due) {
                Reload(file);
            }
        }

        private static void Reload(WatchedFile file) {
            (DateTime beforeWriteTimeUtc, long beforeLength) = Stat(file.Path);

            // Info, not Debug: on a server this line is the only evidence an admin's edit was picked up.
            // An unknown applied stamp means the file did not change; memory did (a server's copy was
            // in effect until now, or the file only just appeared), so say that instead.
            ModLogger.LogInfo(file.AppliedLength < 0
                ? $"Reading config file {file.Path} from disk."
                : $"Config file {file.Path} changed on disk, reloading it.");
            try {
                file.Reload();
            } catch (Exception e) {
                ModLogger.LogWarning($"There was an issue loading {System.IO.Path.GetFileName(file.Path)}.\n{e.Message}");
            }

            (DateTime afterWriteTimeUtc, long afterLength) = Stat(file.Path);
            file.ChangePending = false;
            file.SkipLogged = false;
            file.SeenWriteTimeUtc = afterWriteTimeUtc;
            file.SeenLength = afterLength;

            if (afterWriteTimeUtc == beforeWriteTimeUtc && afterLength == beforeLength) {
                // Recorded even when the read failed, so a file that cannot be parsed is attempted
                // once per edit instead of on every poll.
                file.AppliedWriteTimeUtc = afterWriteTimeUtc;
                file.AppliedLength = afterLength;
                return;
            }

            // The file changed underneath the reload: a save that landed mid-read, or a reload handler
            // that wrote back. The applied stamp is left alone, so the next check sees a new pending
            // change and reads again after a quiet period. A write-back converges: the second read
            // yields the values just applied, so nothing changes and nothing is written again.
        }

        /// <summary>The .cfg reload registered by <see cref="Begin"/>.</summary>
        private static bool ReloadConfigFile(ConfigFile config) {
            bool saveOnConfigSet = config.SaveOnConfigSet;
            try {
                // Disk is the source of truth for this pass, so suppress the write-back each changed
                // entry would otherwise trigger: it would rewrite the file we are reading and re-trip
                // every watcher for no change in content.
                config.SaveOnConfigSet = false;
                config.Reload();
                return true;
            } finally {
                config.SaveOnConfigSet = saveOnConfigSet;
            }
        }

        /// <summary>Timestamp and size of the file, or (default, -1) when it does not exist.</summary>
        private static (DateTime WriteTimeUtc, long Length) Stat(string path) {
            try {
                FileInfo info = new FileInfo(path);
                return info.Exists ? (info.LastWriteTimeUtc, info.Length) : (default, -1L);
            } catch (Exception) {
                // A stat can fail on its own (permissions, a path that stopped being a file). Treat it
                // as absent; the next check tries again.
                return (default, -1L);
            }
        }
    }
}
