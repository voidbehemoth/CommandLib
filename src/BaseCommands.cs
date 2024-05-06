using System;
using UnityEngine;
using System.Linq;
using CommandLib.API;
using CommandLib.Util;
using Server.Shared.State;
using System.Collections.Generic;

namespace CommandLib.BaseCommands;

// Example Help command
public class HelpCommand : Command, IDescription
{
    private static readonly TokenType[][] syntaxes = [
        [],
        [ new TokenType.CommandTokenType() ]
    ];

    public HelpCommand(string name, string harmonyID, string[] aliases) : base(name, syntaxes, harmonyID, aliases) { }
    public HelpCommand(string name, string[] aliases) : base(name, syntaxes, aliases) { }

    public override Tuple<bool, string> Execute(int syntaxIndex, object[] tokens)
    {
        // List commands syntax
        if (syntaxIndex == 0) {
            string output = "The following commands are registered:\n";

            CommandRegistry.Commands.ForEach((Command command) =>
            {
                string commandText = $" <b>/{command.Name}";

                foreach (string alias in command.Aliases)
                {
                    commandText += $"|{alias}";
                }

                output += commandText + "</b>\n";
            });

            FeedbackHelper.SendFeedbackMessage(output);

            return new Tuple<bool, string>(true, "");

        } else { // Specific command syntax
            Command command = (Command)tokens[0];

            // Reject if the command does not implement IHelpMessage
            if (!typeof(IDescription).IsAssignableFrom(command.GetType())) return new Tuple<bool, string>(false, $"The command '{command.Name} does not have a registered description. Contact the developer of the developer of {command.HarmonyId} for more information.");

            string feedbackMessage = "";

            foreach (TokenType[] syntax in command.Syntaxes) {
                feedbackMessage += $"<b>{command.Name}";

                foreach (string alias in command.Aliases) {
                    feedbackMessage += $"|{alias}";
                }

                foreach (TokenType type in syntax) {
                    feedbackMessage += $" <{type.LongName}>";
                }
                feedbackMessage += "</b>\n";
            }

            feedbackMessage += $"\n{((IDescription)command).GetDescription()}\n";

            FeedbackHelper.SendFeedbackMessage(feedbackMessage);

            return new Tuple<bool, string>(true, "");
        }
    }

    public string GetDescription()
    {
        return "List help message of particular command or list all commands.";
    }
}