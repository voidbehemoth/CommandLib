using CommandLib.API;
using CommandLib.BaseCommands;
using CommandLib.Util;
using Game.Interface;
using HarmonyLib;
using Server.Shared.State;
using SML;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using System.Runtime.CompilerServices;
using CommandLib.UI;
using System.IO;
using System.Reflection;
using Server.Shared.Extensions;
using System.Threading;

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

            string[] tokens = input.Substring(1).Split(' ');

            string commandName = tokens[0];

            // Find command
            command = CommandRegistry.Commands.Find((Command command) => command.name == commandName || command.aliases.Any((string alias) => alias == commandName));

            if (command == null)
            {
                __instance.PlaySound("Audio/UI/Error.wav");
                FeedbackHelper.SendFeedbackMessage($"Unknown command: '{commandName}'");
                return false;
            }

            // Skip the command itself, to get only the arguments
            args = tokens.Skip(1).ToArray();
        }

        // Update previous command, and empty the text input
        PreviousCommandInput = CommandUI._isCommandMode ? $"/{CommandUI._currentCommand.name} {__instance.chatInput.text}" : __instance.chatInput.text;
        __instance.chatInput.text = string.Empty;

        Tuple<bool, string> commandFeedback = command.Execute(args);

        // If the command rejects the input, let the user know why
        if (!commandFeedback.Item1)
        {
            __instance.PlaySound("Audio/UI/Error.wav");
            FeedbackHelper.SendFeedbackMessage($"Error executing command '{command.name}': {commandFeedback.Item2}");
            return false;
        }

        CommandUI.ExitCommandMode();

        return false;
    }
}