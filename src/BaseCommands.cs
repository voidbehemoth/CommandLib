using System;
using UnityEngine;
using System.Linq;
using CommandLib.API;
using CommandLib.Util;
using Server.Shared.State;

namespace CommandLib.BaseCommands;

public class HelpCommand : Command, IHelpMessage
{
    public HelpCommand(string name, string harmonyID, string[] aliases) : base(name, harmonyID, aliases) { }

    public override Tuple<bool, string> Execute(string[] args)
    {
        if (args.Length > 1) return new Tuple<bool, string>(false, "Too many arguments!");

        if (args.Length > 0)
        {
            string commandName = args[0];

            Command foundCommand = CommandRegistry.Commands.Find((Command command) =>
            {
                if (command.name != commandName && !command.aliases.Any((string alias) => alias == commandName)) return false;

                return true;
            });

            if (foundCommand == null) return new Tuple<bool, string>(false, $"Unable to find command '{commandName}'. Make sure you spelled it correctly!");

            if (!typeof(IHelpMessage).IsAssignableFrom(foundCommand.GetType())) return new Tuple<bool, string>(false, $"The command '{foundCommand.name} does not have a registered help command. Contact the developer of the developer of {foundCommand.harmonyID} for more information.");

            FeedbackHelper.SendFeedbackMessage(((IHelpMessage)foundCommand).GetHelpMessage(), ClientFeedbackType.Success);

            return new Tuple<bool, string>(true, "");
        }

        string output = "The following commands are registered:\n";

        CommandRegistry.Commands.ForEach((Command command) =>
        {
            string commandText = $" /{command.name}";

            foreach (string alias in aliases)
            {
                commandText += $"|{alias}";
            }

            output += commandText + "\n";
        });

        FeedbackHelper.SendFeedbackMessage(output, ClientFeedbackType.Success);

        return new Tuple<bool, string>(true, "");
    }

    public string GetHelpMessage()
    {
        return "<b>/help [command]</b> - list help message of particular command or list all commands.";
    }
}