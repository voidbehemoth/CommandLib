using CommandLib.API;
using CommandLib.BaseCommands;
using CommandLib.Util;
using Game.Interface;
using HarmonyLib;
using SML;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CommandLib.UI;
using System.Reflection;
using Server.Shared.Extensions;

namespace CommandLib;

[Mod.SalemMod]
public class CommandLib
{
    public static Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    public static void Start()
    {
        // Register example help command, passing in the command name and aliases
        CommandRegistry.AddCommand(new HelpCommand("help", ["h"]));

        LoadSprites();
    }

    private static void LoadSprites()
    {
        AssetBundle bundle = FromAssetBundle.GetAssetBundleFromResources("CommandLib.resources.assetbundles.commandlib", Assembly.GetExecutingAssembly());
        bundle.LoadAllAssets<Sprite>().ForEach((Sprite sprite) =>
        {
            sprites.Add(sprite.name, sprite);
        });
        bundle?.Unload(false);
    }


}

[HarmonyPatch(typeof(ChatInputController))]
public class ChatInputControllerPlus
{
    // Keep track of the previous command the user entered
    public static string PreviousCommandInput { get; private set; } = "";

    [HarmonyPatch(nameof(ChatInputController.SubmitChat))]
    [HarmonyPrefix]
    public static bool CommandHandler(ChatInputController __instance)
    {
        // Encode text to make keywords function correctly
        string input = __instance.chatInput.mentionPanel.mentionsProvider.EncodeText(__instance.chatInput.text.Trim());

        Command command;
        string[] args;

        // Scenario 1: the UI is in command mode, and the user hasn't entered a different command yet
        if (CommandUI._isCommandMode && !input.StartsWith('/'))
        {
            command = CommandUI._currentCommand;

            // Create an empty array if the string itself is empty
            args = (input.Length < 1) ? [] : input.Split(' ');
        }
        else // Scenario 2: the user has entered a command, but the UI isn't in command mode yet
        {
            if (input.Length < 1 || !input.StartsWith('/')) return true;

            string[] tok = input.Substring(1).Split(' ');

            string commandName = tok[0];

            // Find command
            command = CommandRegistry.Commands.Find((Command command) => command.Name == commandName.ToLowerInvariant() || command.Aliases.Contains(commandName.ToLowerInvariant()));

            if (command == null)
            {
                __instance.PlaySound("Audio/UI/Error.wav");
                FeedbackHelper.SendFeedbackMessage($"Unknown command: '{commandName}'");
                return false;
            }

            // Skip the command itself, to get only the arguments
            args = tok.Skip(1).ToArray();
        }

        // Update previous command, and empty the text input
        PreviousCommandInput = CommandUI._isCommandMode ? $"/{CommandUI._currentCommand.Name} {__instance.chatInput.text}" : __instance.chatInput.text;
        __instance.chatInput.text = string.Empty;

        if (command.Syntaxes.All((TokenType[] syntax) => args.Length > syntax.Length)) {
            FeedbackHelper.SendFeedbackMessage($"Too many arguments!");
            return false;
        }

        if (command.Syntaxes.All((TokenType[] syntax) => args.Length < syntax.Length)) {
            FeedbackHelper.SendFeedbackMessage($"Too few arguments!");
            return false;
        }

        int syntaxIndex = 0;
        bool valid = false;
        List<object> tokens = new List<object>();

        for (int i = 0; i < command.Syntaxes.Length; i++) {
            if (args.Length != command.Syntaxes[i].Length) continue;

            bool failed = false;
            for (int j = 0; j < command.Syntaxes[i].Length; j++) {
                Tuple<bool, object> validate = command.Syntaxes[i][j].Validate(args[j]);

                if (!validate.Item1) {
                    failed = true;
                    break;
                }

                tokens.Add(validate.Item2);
            }

            if (!failed) {
                syntaxIndex = i;
                valid = true;
                break;
            }

            tokens.Clear();
        }

        if (!valid) {
            FeedbackHelper.SendFeedbackMessage($"Input does not match any syntax of {command.Name}!");
            return false;
        }

        Tuple<bool, string> commandFeedback = command.Execute(syntaxIndex, [.. tokens]);

        // If the command rejects the input, let the user know why
        if (!commandFeedback.Item1)
        {
            __instance.PlaySound("Audio/UI/Error.wav");
            FeedbackHelper.SendFeedbackMessage($"Error executing command '{command.Name}': {commandFeedback.Item2}");
            return false;
        }

        CommandUI.ExitCommandMode();

        return false;
    }
}