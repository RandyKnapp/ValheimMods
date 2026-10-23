using Common;
using EpicLoot.Config;
using Jotunn.Entities;
using Jotunn.Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EpicLoot.QuickConfig;

/// <summary>What the server did with a server admin's JSON changes.</summary>
internal sealed class ServerSaveResult {
    /// <summary>The files the server wrote whose push has reached this game, so their live values are the saved ones.</summary>
    internal readonly HashSet<string> Written = new HashSet<string>(StringComparer.Ordinal);
    internal readonly List<string> Notes = new List<string>();
    internal readonly List<string> Failures = new List<string>();
    /// <summary>The server rewrote magiceffects.json from its Balance Template.</summary>
    internal bool Rewritten;
}

/// <summary>
/// Saves a server admin's Quick Configure JSON changes on the server they are connected to. The
/// baseconfig files in effect are the server's, so Save fetches the server's copy of every file with a
/// staged change, applies the changes to it with the host's own code (<see cref="JsonConfigEdits.ApplyStaged"/>)
/// and sends the edited text back. The server checks that the sender is on its admin list and that the
/// file has not changed since it was fetched, then writes and reloads it. That reload pushes the file to
/// every connected player, this admin included, like any other edit of a server's baseconfig; the save
/// completes here when the push has arrived, since the push is what puts the saved values in this game.
///
/// Editing the server's own text, rather than sending the staged values, leaves everything the panel
/// does not show exactly as the server has it, and needs nothing of the panel on the server: a dedicated
/// server only checks, writes and reloads.
/// </summary>
internal static class QuickConfigServerSave {
    private const byte FetchRequest = 1;
    private const byte FetchReply = 2;
    private const byte WriteRequest = 3;
    private const byte WriteReply = 4;

    // How long the server has to answer each request, and to push back the files it wrote.
    private const float ReplyTimeout = 20f;
    private const float PushTimeout = 15f;

    private static CustomRPC rpc;

    private enum Stage { Fetching, Writing, AwaitingPush }

    private sealed class FileEdit {
        internal string File;
        internal string Hash;
        internal string Text;
    }

    private sealed class Operation {
        internal int Id;
        internal Stage Stage;
        internal float Deadline;
        // Copies taken at Save, so nothing done in the panel while the server answers changes this save.
        internal StagedConfig Staged;
        internal StagedConfig Baseline;
        internal List<string> Files;
        internal string Template;
        internal Action<ServerSaveResult> Done;
        internal readonly ServerSaveResult Result = new ServerSaveResult();
        // Files pushed to this game since the write was sent, and the written files still to arrive.
        internal readonly HashSet<string> Arrived = new HashSet<string>(StringComparer.Ordinal);
        internal readonly HashSet<string> Awaited = new HashSet<string>(StringComparer.Ordinal);
    }

    private static Operation current;
    private static int lastId;
    private static GameObject watch;

    /// <summary>True while a save is waiting on the server.</summary>
    internal static bool Busy => current != null;

    /// <summary>Registers the RPC. On every machine: the server answers it, a client sends it.</summary>
    internal static void Init() {
        if (rpc != null) { return; }
        rpc = NetworkManager.Instance.AddRPC("epicloot_quickconfig_RPC", OnServerReceive, OnClientReceive);
        ELConfig.ServerConfigApplied += OnServerConfigApplied;
    }

    // ------------------------------------------------------------------------------------------------
    //  Client
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// Sends the staged JSON changes to the server, after it rewrites magiceffects.json from
    /// <paramref name="rewriteTemplate"/> when that is not null. <paramref name="done"/> runs once, on a
    /// later frame, with what the server did. False when nothing could be sent (not connected to someone
    /// else's server, or a save is already waiting); done is then never called.
    /// </summary>
    internal static bool Begin(StagedConfig staged, StagedConfig baseline, string rewriteTemplate, Action<ServerSaveResult> done) {
        if (rpc == null || current != null || ZNet.instance == null || ZNet.instance.IsServer() || ZRoutedRpc.instance == null) {
            return false;
        }

        Operation op = new Operation {
            Id = ++lastId,
            Staged = new StagedConfig(),
            Baseline = new StagedConfig(),
            Template = rewriteTemplate,
            Done = done
        };
        op.Staged.CopyFrom(staged);
        op.Baseline.CopyFrom(baseline);
        // The rewrite replaces magiceffects.json whole, so edits staged against the old file are dropped,
        // as the host drops them.
        op.Files = staged.DirtyJsonFiles(baseline)
            .Where(file => rewriteTemplate == null || file != BalancePreset.MagicEffectsFile)
            .ToList();
        current = op;
        EnsureWatch();

        if (op.Files.Count == 0) {
            SendWrite(op, new List<FileEdit>());
        } else {
            SendFetch(op);
        }
        return true;
    }

    private static void SendFetch(Operation op) {
        ZPackage package = new ZPackage();
        package.Write(FetchRequest);
        package.Write(op.Id);
        package.Write(op.Files.Count);
        foreach (string file in op.Files) { package.Write(file); }
        Send(op, Stage.Fetching, package);
    }

    private static void SendWrite(Operation op, List<FileEdit> edits) {
        ZPackage package = new ZPackage();
        package.Write(WriteRequest);
        package.Write(op.Id);
        package.Write(op.Template ?? "");
        package.Write(edits.Count);
        foreach (FileEdit edit in edits) {
            package.Write(edit.File);
            package.Write(edit.Hash);
            package.Write(edit.Text);
        }
        // Any push from here on may be the server's answer to this write: the server pushes the files
        // before it replies, so they can arrive first.
        op.Arrived.Clear();
        Send(op, Stage.Writing, package);
    }

    private static void Send(Operation op, Stage stage, ZPackage package) {
        op.Stage = stage;
        op.Deadline = Time.unscaledTime + ReplyTimeout;
        rpc.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
    }

    private static IEnumerator OnClientReceive(long sender, ZPackage package) {
        try {
            HandleReply(package);
        } catch (Exception e) {
            EpicLoot.LogErrorForce($"Quick Configure could not read the server's reply: {e}");
            Fail(current, $"The server's reply could not be read: {e.Message}");
        }
        yield break;
    }

    private static void HandleReply(ZPackage package) {
        byte kind = package.ReadByte();
        int id = package.ReadInt();
        Operation op = current;
        if (op == null || op.Id != id) {
            // A save this game stopped waiting for. The server may still have written it; its push
            // comes like any other, but nothing here waits on it any more.
            EpicLoot.LogWarningForce("Quick Configure: the server answered a save after this game had stopped waiting for it.");
            return;
        }
        if (kind == FetchReply) {
            OnFetched(op, package);
        } else if (kind == WriteReply) {
            OnWritten(op, package);
        }
    }

    // The server's copy of each file has come: apply the staged changes to it and send it back.
    private static void OnFetched(Operation op, ZPackage package) {
        string refused = package.ReadString();
        if (refused.Length > 0) {
            Fail(op, refused);
            return;
        }

        Dictionary<string, string> texts = new Dictionary<string, string>(StringComparer.Ordinal);
        int count = package.ReadInt();
        for (int i = 0; i < count; i++) {
            string file = package.ReadString();
            texts[file] = package.ReadString();
        }

        List<FileEdit> edits = new List<FileEdit>();
        foreach (string file in op.Files) {
            if (texts.TryGetValue(file, out string text) == false) {
                op.Result.Failures.Add($"{file} was not found in the server's baseconfig folder.");
                continue;
            }
            try {
                JObject root = JObject.Parse(text);
                int changed = JsonConfigEdits.ApplyStaged(root, file, op.Staged, op.Baseline);
                edits.Add(new FileEdit {
                    File = file,
                    Hash = ConfigVersionState.HashConfigText(text),
                    Text = root.ToString(Formatting.Indented)
                });
                EpicLoot.Log($"Quick Configure: sending {changed} value(s) of {file} to the server.");
            } catch (Exception e) {
                EpicLoot.LogWarningForce($"Quick Configure could not apply the changes to the server's {file}: {e}");
                op.Result.Failures.Add($"{file} was not saved: {e.Message}");
            }
        }

        if (edits.Count == 0 && op.Template == null) {
            Finish(op);
            return;
        }
        SendWrite(op, edits);
    }

    private static void OnWritten(Operation op, ZPackage package) {
        op.Result.Rewritten = package.ReadBool();
        List<string> written = ReadList(package);
        op.Result.Notes.AddRange(ReadList(package));
        op.Result.Failures.AddRange(ReadList(package));

        foreach (string file in written) { op.Awaited.Add(file); }
        if (op.Result.Rewritten) { op.Awaited.Add(BalancePreset.MagicEffectsFile); }
        op.Stage = Stage.AwaitingPush;
        op.Deadline = Time.unscaledTime + PushTimeout;
        Settle(op);
    }

    private static void OnServerConfigApplied(string file) {
        Operation op = current;
        if (op == null || op.Stage == Stage.Fetching) { return; }
        op.Arrived.Add(file);
        if (op.Stage == Stage.AwaitingPush) { Settle(op); }
    }

    // Every awaited file that has arrived counts as written; the save finishes when none is left.
    private static void Settle(Operation op) {
        foreach (string file in op.Awaited.Where(op.Arrived.Contains).ToList()) {
            op.Awaited.Remove(file);
            op.Result.Written.Add(file);
        }
        if (op.Awaited.Count == 0) { Finish(op); }
    }

    private static void Fail(Operation op, string failure) {
        if (op == null) { return; }
        op.Result.Failures.Add(failure);
        Finish(op);
    }

    private static void Finish(Operation op) {
        if (op == null || current != op) { return; }
        current = null;
        try {
            op.Done?.Invoke(op.Result);
        } catch (Exception e) {
            EpicLoot.LogErrorForce($"Quick Configure failed to finish a save on the server: {e}");
        }
    }

    // A reply or push that never comes must not leave the panel waiting.
    private static void CheckTimeout() {
        Operation op = current;
        if (op == null) { return; }
        if (ZNet.instance == null) {
            Fail(op, "The connection to the server closed before it answered.");
            return;
        }
        if (Time.unscaledTime < op.Deadline) { return; }

        switch (op.Stage) {
            case Stage.Fetching:
                Fail(op, $"The server did not answer within {ReplyTimeout:0} seconds, so nothing was saved there.");
                break;
            case Stage.Writing:
                Fail(op, $"The server did not answer within {ReplyTimeout:0} seconds. It may still apply the changes; " +
                    "reopen the panel to see what it has.");
                break;
            default:
                Fail(op, $"The server saved {string.Join(", ", op.Awaited)} but has not sent it back to this game yet. " +
                    "Reopen the panel to see what it has.");
                break;
        }
    }

    private static void EnsureWatch() {
        if (watch != null) { return; }
        watch = new GameObject("EpicLoot_QuickConfigServerSave");
        UnityEngine.Object.DontDestroyOnLoad(watch);
        watch.AddComponent<TimeoutWatch>();
    }

    private sealed class TimeoutWatch : MonoBehaviour {
        public void Update() {
            CheckTimeout();
        }
    }

    // ------------------------------------------------------------------------------------------------
    //  Server
    // ------------------------------------------------------------------------------------------------

    private static IEnumerator OnServerReceive(long sender, ZPackage package) {
        ZPackage reply = null;
        try {
            reply = HandleRequest(sender, package);
        } catch (Exception e) {
            // The admin's game reports the timeout.
            EpicLoot.LogErrorForce($"Quick Configure could not handle a request from {Describe(sender)}: {e}");
        }
        if (reply != null) { rpc.SendPackage(sender, reply); }
        yield break;
    }

    private static ZPackage HandleRequest(long sender, ZPackage request) {
        byte kind = request.ReadByte();
        int id = request.ReadInt();
        string refused = RefusalFor(sender);
        switch (kind) {
            case FetchRequest: return Fetch(id, request, refused);
            case WriteRequest: return Write(id, sender, request, refused);
            default:
                EpicLoot.LogWarningForce($"Quick Configure: {Describe(sender)} sent an unknown request ({kind}).");
                return null;
        }
    }

    // The same check vanilla makes for its admin commands, by the sender's own connection.
    private static string RefusalFor(long sender) {
        ZNetPeer peer = ZNet.instance.GetPeer(sender);
        string hostName = peer?.m_socket?.GetHostName();
        if (string.IsNullOrEmpty(hostName) == false && ZNet.instance.IsAdmin(hostName)) { return null; }
        EpicLoot.LogWarningForce($"Quick Configure: {Describe(sender)} is not on the admin list; their request to change the config files was refused.");
        return "Only a server admin can change these settings, and this server does not list you as one.";
    }

    private static ZPackage Fetch(int id, ZPackage request, string refused) {
        ZPackage reply = new ZPackage();
        reply.Write(FetchReply);
        reply.Write(id);
        if (refused != null) {
            reply.Write(refused);
            return reply;
        }

        List<(string File, string Text)> found = new List<(string, string)>();
        int count = request.ReadInt();
        for (int i = 0; i < count; i++) {
            string file = request.ReadString();
            string text = IsEditable(file) ? ReadFile(file) : null;
            if (text != null) { found.Add((file, text)); }
        }

        reply.Write("");
        reply.Write(found.Count);
        foreach ((string file, string text) in found) {
            reply.Write(file);
            reply.Write(text);
        }
        return reply;
    }

    private static ZPackage Write(int id, long sender, ZPackage request, string refused) {
        bool rewritten = false;
        List<string> written = new List<string>();
        List<string> notes = new List<string>();
        List<string> failures = new List<string>();

        if (refused != null) {
            failures.Add(refused);
        } else {
            string template = request.ReadString();
            List<FileEdit> edits = new List<FileEdit>();
            int count = request.ReadInt();
            for (int i = 0; i < count; i++) {
                edits.Add(new FileEdit { File = request.ReadString(), Hash = request.ReadString(), Text = request.ReadString() });
            }

            if (template.Length > 0) {
                rewritten = RewriteMagicEffects(template, notes, failures);
            }

            // In the host's order, magiceffects.json last, and only the files the panel edits: the name
            // comes from the client and becomes a path here.
            Dictionary<string, string> previous = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (FileEdit edit in edits.Where(edit => IsEditable(edit.File) == false)) {
                EpicLoot.LogWarningForce($"Quick Configure: {Describe(sender)} sent '{edit.File}', which is not a file it edits; ignored.");
                failures.Add($"{edit.File} is not a file Quick Configure edits.");
            }
            foreach (string file in StagedConfig.JsonFileOrder) {
                foreach (FileEdit edit in edits.Where(edit => edit.File == file)) {
                    WriteFile(edit, previous, written, notes, failures);
                }
            }

            if (written.Count > 0) {
                RestoreUnreadable(ELConfig.ReloadBaseConfigsFromDisk(written), previous, written, notes, failures);
            }
            EpicLoot.LogForce($"Quick Configure: {Describe(sender)} saved " +
                (written.Count > 0 ? string.Join(", ", written) : "no files") + (rewritten ? " and rewrote magiceffects.json" : "") +
                (failures.Count > 0 ? $"; refused: {string.Join(" ", failures)}" : "."));
        }

        ZPackage reply = new ZPackage();
        reply.Write(WriteReply);
        reply.Write(id);
        reply.Write(rewritten);
        WriteList(reply, written);
        WriteList(reply, notes);
        WriteList(reply, failures);
        return reply;
    }

    // The template is already the server's: the admin's .cfg change is pushed ahead of this request.
    private static bool RewriteMagicEffects(string template, List<string> notes, List<string> failures) {
        string serverTemplate = BalancePreset.Normalize(ELConfig.BalanceConfigurationType.Value);
        if (serverTemplate != BalancePreset.Normalize(template)) {
            failures.Add($"The server's Balance Template is {serverTemplate}, not {template}, so magiceffects.json was not " +
                "rewritten. Save again once the setting has reached the server.");
            return false;
        }

        // The admin cannot be asked about the server's own file, so a hand-edited one is always backed up.
        bool backup = ConfigVersionManager.IsPlayerModified(BalancePreset.MagicEffectsConfigName);
        if (BalancePreset.ApplyOverhaulRewrite(backup, out string message)) {
            notes.Add(message);
            return true;
        }
        failures.Add(message);
        return false;
    }

    // Written only when the file is still the one the admin's edit was made to.
    private static void WriteFile(FileEdit edit, Dictionary<string, string> previous, List<string> written,
        List<string> notes, List<string> failures) {
        string path = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), edit.File);
        try {
            if (File.Exists(path) == false) {
                failures.Add($"{edit.File} was not found in the server's baseconfig folder.");
                return;
            }
            string current = File.ReadAllText(path);
            if (ConfigVersionState.HashConfigText(current) != edit.Hash) {
                failures.Add($"{edit.File} changed on the server while you were saving, so it was left alone. " +
                    "Save again to apply your changes to the new copy.");
                return;
            }
            // Parsed and written again here, so the file is valid JSON and has this machine's line endings
            // rather than the admin's, exactly as the host's own save would format it.
            File.WriteAllText(path, JObject.Parse(edit.Text).ToString(Formatting.Indented));
            previous[edit.File] = current;
            written.Add(edit.File);
            notes.Add($"{edit.File} saved on the server.");
            string patchedNote = JsonConfigEdits.PatchedNote(edit.File);
            if (patchedNote != null) {
                EpicLoot.LogWarningForce($"Quick Configure: {patchedNote}");
                notes.Add(patchedNote);
            }
        } catch (Exception e) {
            EpicLoot.LogWarningForce($"Quick Configure could not write {edit.File}: {e}");
            failures.Add($"{edit.File} was not saved on the server: {e.Message}");
        }
    }

    // A written file the config could not load would sit on disk until the next restart failed on it
    // too, out of the admin's reach. The previous file is still what is loaded, so it goes back.
    private static void RestoreUnreadable(List<string> unreadable, Dictionary<string, string> previous, List<string> written,
        List<string> notes, List<string> failures) {
        foreach (string file in unreadable) {
            written.Remove(file);
            notes.Remove($"{file} saved on the server.");
            string path = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), file);
            try {
                File.WriteAllText(path, previous[file]);
                ConfigFileReloader.MarkApplied(path);
                failures.Add($"{file} could not be loaded after the change, so the server put the previous file back. Its log has the details.");
            } catch (Exception e) {
                EpicLoot.LogErrorForce($"Quick Configure could not put the previous {file} back: {e}");
                failures.Add($"{file} could not be loaded after the change, and the previous file could not be put back: {e.Message}");
            }
        }
    }

    private static bool IsEditable(string file) => Array.IndexOf(StagedConfig.JsonFileOrder, file) >= 0;

    private static string ReadFile(string file) {
        string path = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), file);
        try {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        } catch (Exception e) {
            EpicLoot.LogWarningForce($"Quick Configure could not read {file}: {e.Message}");
            return null;
        }
    }

    private static string Describe(long sender) {
        ZNetPeer peer = ZNet.instance != null ? ZNet.instance.GetPeer(sender) : null;
        return peer != null ? $"{peer.m_playerName} ({peer.m_socket?.GetHostName()})" : $"peer {sender}";
    }

    private static void WriteList(ZPackage package, List<string> values) {
        package.Write(values.Count);
        foreach (string value in values) { package.Write(value); }
    }

    private static List<string> ReadList(ZPackage package) {
        int count = package.ReadInt();
        List<string> values = new List<string>(count);
        for (int i = 0; i < count; i++) { values.Add(package.ReadString()); }
        return values;
    }
}
