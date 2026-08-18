using System.Collections.Generic;

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
        params string[] alternates) : base(command, description, action, isCheat, isNetwork, onlyServer, isSecret, allowInDevBuild, optionsFetcher, alwaysRefreshTabOptions, remoteCommand, onlyAdmin)
    {
        this.options = options;
        this.hideFromHelp = hideFromHelp;
        if (options != null) m_tabOptionsFetcher = () => options(Console.instance.m_input.text.Split(' '));
        foreach (var alt in alternates)
        {
            _ = new Command(alt, description, action, options, isCheat, isNetwork, onlyServer, isSecret,
                allowInDevBuild, optionsFetcher, alwaysRefreshTabOptions, remoteCommand, onlyAdmin, hideFromHelp: true);
        }

        TerminalManager._commands[command] = this;
    }

    public List<string> GetTabOptions(string[] strArray)
    {
        return options == null ? [] : options(strArray);
    }

    public delegate List<string> CommandOptions(string[] strArray);
}