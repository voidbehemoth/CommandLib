using System;
using System.Collections.Generic;
using System.Linq;
using CommandLib.API;
using CommandLib.Util;
using Game.Interface;
using HarmonyLib;
using Home.Common.Tooltips;
using Server.Shared.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CommandLib.UI;

[HarmonyPatch(typeof(ChatInputController))]
public class CommandUI
{
    public static bool _isCommandMode { get; private set; }
    public static Command _currentCommand { get; private set; }
    public static ChatInputController _chatInputController { get; private set; }

    public static GameObject CommandModeGO { get; private set; }
    public static TextMeshProUGUI _commandText { get; private set; }

    private static Sprite CommandPaper;

    private static Dictionary<string, Tuple<Color, Color, Color>> ColorDict = new Dictionary<string, Tuple<Color, Color, Color>>();

    [HarmonyPatch(nameof(ChatInputController.Start))]
    [HarmonyPostfix]
    public static void AddPlus(ChatInputController __instance)
    {
        _chatInputController = __instance;
        CommandPaper = CommandLib.sprites.GetValue("Command_Paper");
        Console.WriteLine($"Name of sprite is '{CommandLib.sprites.First().Key}'");
        CommandModeGO = GameObject.Instantiate(__instance.ElderTellModePanelGO, __instance.ElderTellModePanelGO.transform.parent.transform, false);
        CommandModeGO.transform.SetSiblingIndex(0);
        _commandText = CommandModeGO.GetComponentInChildren<TextMeshProUGUI>();

        CommandModeGO.GetComponentInChildren<TooltipTrigger>().NonLocalizedString = "Click to exit command mode";
        CommandModeGO.GetComponentInChildren<Image>().sprite = CommandLib.sprites.GetValue("RoleCard_Trapper_Ability_2");

        ExitCommandMode();
    }

    [HarmonyPatch(nameof(ChatInputController.Update))]
    [HarmonyPostfix]
    public static void HandleUpdate(ChatInputController __instance)
    {
        if (__instance.chatInput.isFocused)
        {
            if (_isCommandMode && Input.GetKeyDown(KeyCode.Backspace) && __instance.chatInput.caretPosition == 0 && __instance.chatInput.text.Length == 0 && __instance.chatInput.TextBeforeLatestKeypress.Length == 0)
            {
                ExitCommandMode();
            }
        }
        else if (_isCommandMode && Input.GetKeyDown(KeyCode.Escape)) ExitCommandMode();

        if (_chatInputController.chatInput.text.StartsWith("/") && _chatInputController.chatInput.text.EndsWith(" "))
        {
            EnterCommandMode(_chatInputController.chatInput.text.Trim().Substring(1));
        }
    }

    [HarmonyPatch(nameof(ChatInputController.HandleOnClickExitElderTellMode))]
    [HarmonyPostfix]
    public static void ExitElderWhisper()
    {
        ExitCommandMode();
    }

    [HarmonyPatch(nameof(ChatInputController.HandleOnClickExitWhisperMode))]
    [HarmonyPostfix]
    public static void ExitWhisperMode()
    {
        ExitCommandMode();
    }

    [HarmonyPatch(nameof(ChatInputController.SetChatState))]
    [HarmonyPostfix]
    public static void FixBackgroundSprite()
    {
        if (!_isCommandMode) return;

        _chatInputController.parchmentBackgroundImage.sprite = CommandPaper;

        if (_currentCommand == null) return;

        Tuple<Color, Color, Color> colors = GetColor(_currentCommand.harmonyID);

        _chatInputController.parchmentBackgroundImage.color = colors.Item1;
        _chatInputController.chatInputText.color = colors.Item2;
        _chatInputController.placeholderText.color = colors.Item2;
        _commandText.color = colors.Item3;
    }

    private static void EnterCommandMode(string commandName)
    {
        _currentCommand = CommandRegistry.Commands.Find((Command command) => command.name == commandName || command.aliases.Contains(commandName));

        if (_currentCommand == null) return;

        _chatInputController.ExitWhisperMode();
        _chatInputController.ExitElderTellMode();

        CommandModeGO.SetActive(true);
        _isCommandMode = true;
        _commandText.SetText($"/{_currentCommand.name}");
        _chatInputController.chatInput.text = string.Empty;
        _chatInputController.SetChatState();
    }

    public static void ExitCommandMode()
    {
        _isCommandMode = false;
        _commandText.SetText(string.Empty);
        _currentCommand = null;
        CommandModeGO.SetActive(false);
        _chatInputController.parchmentBackgroundImage.color = Color.white;
        _chatInputController.SetChatState();
    }

    public static Tuple<Color, Color, Color> GetColor(string harmonyID)
    {
        if (ColorDict.ContainsKey(harmonyID)) return ColorDict.GetValue(harmonyID);

        System.Random random = new System.Random(harmonyID.GetHashCode());
        Color color = new Color(
            (float)random.NextDouble(),
            (float)random.NextDouble(),
            (float)random.NextDouble()
        );

        Tuple<Color, Color, Color> colors = new Tuple<Color, Color, Color>(color, ColorHelper.GetTextColor(color), ColorHelper.GetOppositeColor(color));

        ColorDict.Add(harmonyID, colors);

        return colors;
    }

    public static void SetColor(string harmonyID, Color color)
    {
        if (ColorDict.ContainsKey(harmonyID)) return;

        Tuple<Color, Color, Color> colors = new Tuple<Color, Color, Color>(color, ColorHelper.GetTextColor(color), ColorHelper.GetOppositeColor(color));

        ColorDict.Add(harmonyID, colors);
    }
}