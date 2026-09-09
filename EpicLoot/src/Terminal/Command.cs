using System.Collections.Generic;
using System.Linq;

namespace EpicLoot;

public class Command : Terminal.ConsoleCommand
{
    private readonly CommandOptions options;
    public readonly bool hideFromHelp;
    public Command(
        string command, 
        string description, 
        Terminal.ConsoleEvent action, 
        CommandOptions options = null, 
        bool isNetwork = false, 
        bool onlyServer = false, 
        bool isSecret = false, 
        bool allowInDevBuild = false, 
        Terminal.ConsoleOptionsFetcher optionsFetcher = null, 
        bool alwaysRefreshTabOptions = false, 
        bool remoteCommand = false, 
        bool onlyAdmin = false, 
        bool hideFromHelp = false, 
        params string[] alternates)
        // Epic Loot commands are never cheats and never sit behind devcommands, so they run without
        // `devcommands` enabled, do not trip the cheat-confirmation prompt or flag the profile as
        // cheated, and stay usable from the chat window (Chat.isAllowedCommand rejects both flags).
        // The gate is admin instead - see RequireAdmin.
        : base(command, description, RequireAdmin(command, action), isCheat: false, isNetwork, onlyServer, isSecret, allowInDevBuild, hideBehindDevCommands: false, optionsFetcher, alwaysRefreshTabOptions || options != null, remoteCommand, onlyAdmin)
    {
        this.options = options;
        this.hideFromHelp = hideFromHelp;
        // Fallback for any vanilla path that asks for options without going through TerminalManager's
        // patches: hand back the first argument's options. Everything that actually drives completion
        // resolves the argument under the caret via GetTabOptions(tokens, argIndex) instead.
        if (options != null) m_tabOptionsFetcher = () => GetTabOptions([command, string.Empty], 1);
        foreach (var alt in alternates)
        {
            _ = new Command(alt, description, action, options, isNetwork, onlyServer, isSecret,
                allowInDevBuild, optionsFetcher, alwaysRefreshTabOptions, remoteCommand, onlyAdmin, hideFromHelp: true);
        }

        TerminalManager._commands[command] = this;
    }

    /// <summary>
    /// Wraps a command body in the admin check. Every Epic Loot command spawns items, rewrites
    /// adventure state or dumps diagnostics, so the gate is the world's admin list rather than
    /// <c>devcommands</c>: a solo player or host always passes, a client only when its user id is on
    /// the server's adminlist.txt (which the server syncs to every client, so the check reads the same
    /// list on both sides).
    /// </summary>
    /// <remarks>
    /// Vanilla's own <c>onlyAdmin</c> constructor flag cannot do this. Nothing in the game ever reads
    /// <c>ConsoleCommand.OnlyAdmin</c>, and only the <c>ConsoleEventFailable</c> overload folds it into
    /// <c>OnlyServer</c> - the <c>ConsoleEvent</c> overload used here drops it, so passing it is a
    /// no-op. Folding it into <c>OnlyServer</c> would be wrong anyway: that rejects the command outright
    /// on any client of a dedicated server, admin or not. The check therefore lives in the action, which
    /// also means it re-evaluates per invocation rather than being frozen at registration time, when
    /// there is no ZNet yet.
    /// </remarks>
    private static Terminal.ConsoleEvent RequireAdmin(string command, Terminal.ConsoleEvent action) => args =>
    {
        if (ZNet.instance == null || !ZNet.instance.LocalPlayerIsAdminOrHost())
        {
            args.Context?.AddString($"'{command}' requires admin.");
            return;
        }

        action(args);
    };

    /// <summary>
    /// Options for the argument at <paramref name="argIndex"/> of <paramref name="tokens"/>, where
    /// index 0 is the command name itself.
    /// </summary>
    /// <remarks>
    /// Option providers select on the token count (<c>args.Length switch { 2 => ..., 3 => ... }</c>),
    /// so the input is truncated at the argument being completed. That keeps
    /// <c>args.Length == argIndex + 1</c> true even when the caret sits partway along a longer line,
    /// and leaves earlier arguments readable for providers whose options depend on them.
    /// </remarks>
    public List<string> GetTabOptions(string[] tokens, int argIndex)
    {
        if (options == null || argIndex < 1 || argIndex >= tokens.Length) return [];
        var argsUpToCaret = argIndex == tokens.Length - 1 ? tokens : tokens.Take(argIndex + 1).ToArray();
        return options(argsUpToCaret) ?? [];
    }

    public delegate List<string> CommandOptions(string[] strArray);
}
