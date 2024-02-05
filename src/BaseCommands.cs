using System;
using UnityEngine;
using System.Linq;
using CommandLib.API;
using CommandLib.Util;
using Server.Shared.State;

namespace CommandLib.BaseCommands;

// Example Help command
public class HelpCommand : Command, IHelpMessage
{
    public HelpCommand(string name, string harmonyID, string[] aliases) : base(name, harmonyID, aliases) { }
    public HelpCommand(string name, string[] aliases) : base(name, aliases) { }

    public override Tuple<bool, string> Execute(string[] args)
    {
        // Reject if more than 1 argument
        if (args.Length > 1) return new Tuple<bool, string>(false, "Too many arguments!");

        if (args.Length > 0)
        {
            string commandName = args[0];

            // Find specified command
            Command foundCommand = CommandRegistry.Commands.Find((Command command) => commandName.ToLowerInvariant() == command.name || command.aliases.Contains(commandName.ToLowerInvariant()));

            // Reject if command not found
            if (foundCommand == null) return new Tuple<bool, string>(false, $"Unable to find command '{commandName}'. Make sure you spelled it correctly!");

            // Reject if the command does not implement IHelpMessage
            if (!typeof(IHelpMessage).IsAssignableFrom(foundCommand.GetType())) return new Tuple<bool, string>(false, $"The command '{foundCommand.name} does not have a registered help command. Contact the developer of the developer of {foundCommand.harmonyId} for more information.");

            FeedbackHelper.SendFeedbackMessage(((IHelpMessage)foundCommand).GetHelpMessage());

            return new Tuple<bool, string>(true, "");
        }

        string output = "The following commands are registered:\n";

        CommandRegistry.Commands.ForEach((Command command) =>
        {
            string commandText = $" <b>/{command.name}";

            foreach (string alias in command.aliases)
            {
                commandText += $"|{alias}";
            }

            output += commandText + "</b>\n";
        });

        FeedbackHelper.SendFeedbackMessage(output);

        return new Tuple<bool, string>(true, "");
    }

    // Ok, this is meta
    public string GetHelpMessage()
    {
        return "<b>/help [command]</b> - list help message of particular command or list all commands.";
    }
}