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

namespace CommandLib;

[Mod.SalemMod]
public class CommandLib
{
    public static Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    public static void Start()
    {

        Command.RegisterCustomColor("voidbehemoth.thatscrazy", Color.black);
        Command.RegisterCustomColor("voidbehemoth.someothermod", Color.white);
        CommandRegistry.AddCommand(new HelpCommand("help", "voidbehemoth.commandlib", ["h"]));
        CommandRegistry.AddCommand(new HelpCommand("help2", "voidbehemoth.someothermod", ["h2"]));
        CommandRegistry.AddCommand(new HelpCommand("help3", "voidbehemoth.thatscrazy", ["h3"]));

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

public class CommandRegistry
{
    public static List<Command> Commands { get; private set; } = new List<Command>();

    public static void AddCommand(Command command)
    {
        Commands.Add(command);
    }

    public static void RemoveCommand(Command command)
    {
        Commands.Remove(command);
    }
}

[HarmonyPatch(typeof(ChatInputController))]
public class ChatInputControllerPlus
{

    [HarmonyPatch(nameof(ChatInputController.SubmitChat))]
    [HarmonyPrefix]
    public static bool CommandHandler(ChatInputController __instance)
    {
        string input = __instance.chatInput.text.Trim();

        if (!CommandUI._isCommandMode && input.Length < 1) return true;

        if (!CommandUI._isCommandMode && !input.StartsWith('/')) return true;

        __instance.chatInput.text = string.Empty;

        string[] tokens = (input.Length <= 1) ? [input] : input.Substring(1).Split(' ');

        string commandName = CommandUI._isCommandMode ? CommandUI._currentCommand.name : tokens[0];
        string[] args = CommandUI._isCommandMode ? tokens : tokens.Skip(1).ToArray();

        Command foundCommand = CommandRegistry.Commands.Find((Command command) =>
        {
            Console.Write($"{commandName} == {command.name}\nEval: {command.name != commandName && !command.aliases.Contains(commandName)}");
            if (command.name != commandName && !command.aliases.Any((string alias) => alias == commandName)) return false;

            return true;
        });

        if (foundCommand == null)
        {
            __instance.PlaySound("Audio/UI/Error.wav");
            FeedbackHelper.SendFeedbackMessage($"Unknown command: '{commandName}'", ClientFeedbackType.Critical);
            CommandUI.ExitCommandMode();
            return false;
        }

        Tuple<bool, string> commandFeedback = foundCommand.Execute(args);

        if (!commandFeedback.Item1)
        {
            __instance.PlaySound("Audio/UI/Error.wav");
            FeedbackHelper.SendFeedbackMessage($"Error executing command '{foundCommand.name}': {commandFeedback.Item2}", ClientFeedbackType.Critical);
            return false;
        }

        CommandUI.ExitCommandMode();

        return false;
    }
}