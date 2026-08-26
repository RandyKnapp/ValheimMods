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
        bool isCheat = false, 
        bool isNetwork = false, 
        bool onlyServer = false, 
        bool isSecret = false, 
        bool allowInDevBuild = false, 
        Terminal.ConsoleOptionsFetcher optionsFetcher = null, 
        bool alwaysRefreshTabOptions = false, 
        bool remoteCommand = false, 
        bool onlyAdmin = false, 
        bool hideFromHelp = false, 
        params string[] alternates) : base(command, description, action, isCheat, isNetwork, onlyServer, isSecret, allowInDevBuild, optionsFetcher, alwaysRefreshTabOptions || options != null, remoteCommand, onlyAdmin)
    {
        this.options = options;
        this.hideFromHelp = hideFromHelp;
        // Fallback for any vanilla path that asks for options without going through TerminalManager's
        // patches: hand back the first argument's options. Everything that actually drives completion
        // resolves the argument under the caret via GetTabOptions(tokens, argIndex) instead.
        if (options != null) m_tabOptionsFetcher = () => GetTabOptions([command, string.Empty], 1);
        foreach (var alt in alternates)
        {
            _ = new Command(alt, description, action, options, isCheat, isNetwork, onlyServer, isSecret,
                allowInDevBuild, optionsFetcher, alwaysRefreshTabOptions, remoteCommand, onlyAdmin, hideFromHelp: true);
        }

        TerminalManager._commands[command] = this;
    }

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
