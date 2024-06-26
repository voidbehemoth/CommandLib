using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CommandLib.API;
using CommandLib.Util;
using Game.Chat;
using Game.Interface;
using HarmonyLib;
using Home.Common.Tooltips;
using Mentions;
using Mentions.UI;
using Server.Shared.Extensions;
using Server.Shared.State.Chat;
using Server.Shared.Utils;
using SML;
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

    public static TooltipTrigger _tooltip { get; private set; }

    private static Sprite CommandPaper;

    private static Dictionary<string, Tuple<Color, Color, Color>> ColorDict = new Dictionary<string, Tuple<Color, Color, Color>>();
    private static Dictionary<string, Tuple<string, Sprite>> ModDataDict = new Dictionary<string, Tuple<string, Sprite>>();

    [HarmonyPatch(nameof(ChatInputController.Start))]
    [HarmonyPostfix]
    public static void AddPlus(ChatInputController __instance)
    {
        _chatInputController = __instance;
        CommandPaper = CommandLib.sprites.GetValue("Command_Paper");
        CommandModeGO = GameObject.Instantiate(__instance.ElderTellModePanelGO, __instance.ElderTellModePanelGO.transform.parent.transform, false);
        CommandModeGO.transform.SetSiblingIndex(0);
        CommandModeGO.GetComponentInChildren<TooltipTrigger>().NonLocalizedString = "Click to exit command mode";
        _commandText = CommandModeGO.GetComponentInChildren<TextMeshProUGUI>();
        _tooltip = CommandModeGO.transform.Find("TellPanelText").gameObject.AddComponent<TooltipTrigger>();

        ExitCommandMode();
    }

    [HarmonyPatch(nameof(ChatInputController.Update))]
    [HarmonyPostfix]
    public static void HandleUpdate(ChatInputController __instance)
    {
        if (__instance.chatInput.isFocused)
        {
            // Exit command mode if the user presses backspace
            if (_isCommandMode && Input.GetKeyDown(KeyCode.Backspace) && __instance.chatInput.caretPosition == 0 && __instance.chatInput.text.Length == 0 && __instance.chatInput.TextBeforeLatestKeypress.Length == 0)
            {
                ExitCommandMode();
            }

            // If the user presses the up arrow attempt to get the previous command input
            if (!_isCommandMode && __instance.chatInput.text == "" && ChatInputControllerPlus.PreviousCommandInput != "" && Input.GetKeyDown(KeyCode.UpArrow))
            {
                __instance.chatInput.text = ChatInputControllerPlus.PreviousCommandInput;
                EnterCommandMode(_chatInputController.chatInput.text.Trim().Substring(1));
            }
        }
        else if (_isCommandMode && Input.GetKeyDown(KeyCode.Escape)) ExitCommandMode(); // Exit command mode on escape key pressed

        // Check to see if the UI should enter command mode
        if (_chatInputController.chatInput.text.StartsWith("/") && ((__instance.chatInput.isFocused && Input.GetKeyDown(KeyCode.Tab)) || _chatInputController.chatInput.text.Contains(' ')))
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
    public static void SetChatStatePlus()
    {
        if (!_isCommandMode) return;

        // Update the BG image
        _chatInputController.parchmentBackgroundImage.sprite = CommandPaper;

        // This is in case something very wrong happened
        if (_currentCommand == null) return;

        // Get 3 colors: background, placeholder text/chat input text color, and the complementary color of the background
        Tuple<Color, Color, Color> colors = GetColor(_currentCommand.HarmonyId);

        _chatInputController.parchmentBackgroundImage.color = colors.Item1;
        _chatInputController.chatInputText.color = colors.Item2;
        _chatInputController.placeholderText.color = colors.Item2;
        _commandText.color = colors.Item3;
    }

    private static void EnterCommandMode(string input)
    {
        string[] tokens = input.Split(" ");
        
        // Attempt to find current command
        _currentCommand = CommandRegistry.Commands.Find((Command command) => command.Name == tokens[0].ToLowerInvariant() || command.Aliases.Contains(tokens[0].ToLowerInvariant()));

        // Return if unable to find current command
        if (_currentCommand == null) return;

        _chatInputController.ExitWhisperMode();
        _chatInputController.ExitElderTellMode();

        CommandModeGO.SetActive(true);

        // Get the mod name and the mod icon
        Tuple<string, Sprite> dataValue = GetModData(_currentCommand.HarmonyId);

        CommandModeGO.GetComponentInChildren<Image>().sprite = dataValue.Item2;
        _tooltip.NonLocalizedString = $"Added by {dataValue.Item1}";
        _isCommandMode = true;
        _commandText.SetText($"/{_currentCommand.Name}");
        _chatInputController.chatInput.text = tokens.Length > 1 ? tokens.Skip(1).Join(null, " ") : "";
        _chatInputController.SetChatState();
    }

    private static Tuple<string, Sprite> GetModData(string currentCommand)
    {
        if (ModDataDict.ContainsKey(currentCommand)) return ModDataDict.GetValue(currentCommand);

        Mod.ModInfo modInfo = ModStates.EnabledMods.Find((Mod.ModInfo m) => m.HarmonyId == currentCommand);

        Sprite modIcon = modInfo == null ? CommandLib.sprites.GetValue("RoleCard_Trapper_Ability_2") : modInfo.Thumbnail;
        string modName = modInfo == null ? "Unknown" : modInfo.DisplayName;

        Tuple<string, Sprite> value = new Tuple<string, Sprite>(modName, modIcon);

        ModDataDict.Add(currentCommand, value);

        return value;
    }

    public static void ExitCommandMode()
    {
        // Reset everything
        _isCommandMode = false;
        _commandText.SetText(string.Empty);
        _currentCommand = null;
        CommandModeGO.SetActive(false);
        _chatInputController.parchmentBackgroundImage.color = Color.white;
        _chatInputController.SetChatState();
    }

    public static Tuple<Color, Color, Color> GetColor(string harmonyId)
    {
        if (ColorDict.ContainsKey(harmonyId)) return ColorDict.GetValue(harmonyId);

        // Use the mod's harmonyId as a seed for a random color
        System.Random random = new System.Random(harmonyId.GetHashCode());
        Color color = new Color(
            (float)random.NextDouble(),
            (float)random.NextDouble(),
            (float)random.NextDouble()
        );

        Tuple<Color, Color, Color> colors = new Tuple<Color, Color, Color>(color, ColorHelper.GetTextColor(color), ColorHelper.GetOppositeColor(color));

        ColorDict.Add(harmonyId, colors);

        return colors;
    }

    public static void SetColor(string harmonyID, Color color)
    {
        if (ColorDict.ContainsKey(harmonyID)) return;

        Tuple<Color, Color, Color> colors = new Tuple<Color, Color, Color>(color, ColorHelper.GetTextColor(color), ColorHelper.GetOppositeColor(color));

        ColorDict.Add(harmonyID, colors);
    }
}
/*
[HarmonyPatch(typeof(MentionMenuItem), nameof(MentionMenuItem.Initialize))]
public class MentionMenuItemPlus {
    [HarmonyPrefix]
    public static bool Prefix(MentionMenuItem __instance, MentionInfo mentionInfo) {
        if (mentionInfo.mentionInfoType != MentionInfoTypePlus.COMMAND) return true;
        return false;
    }
}

[HarmonyPatch(typeof(MentionsProvider))]
public class Autocomplete {
    public static readonly HashSet<char> AdditionalExpansionTokens = new HashSet<char> { '/' };

    [HarmonyPatch(nameof(MentionsProvider.Start))]
    [HarmonyPrefix]
    public static void UpdateExpansionTokens() {

    }

    [HarmonyPatch(nameof(MentionsProvider.UpdateMatchInfo))]
    [HarmonyPrefix]
    public static bool UpdateMatchInfo(MentionsProvider __instance, string fullText, int stringPosition) {
        if (!fullText.StartsWith('/')) return false;

        __instance._matchInfo.fullText = fullText;
        __instance._matchInfo.stringPosition = stringPosition;
        __instance._matchInfo.matchIndex = 0;
        __instance._matchInfo.matchToken = '/';
        
        int length = __instance._matchInfo.fullText.Length;

        __instance._matchInfo.endsWithPunctuationCharacter = false;
        __instance._matchInfo.followedByPunctuationCharacter = false;
        char c2 = __instance._matchInfo.fullText[__instance._matchInfo.stringPosition - 1];
        __instance._matchInfo.endsWithSubmissionCharacter = c2 == ' ';
        __instance._matchInfo.endsWithPunctuationCharacter = length > 1 && SharedUtils.IsNonMentionPunctuation(c2);
        if (__instance._matchInfo.stringPosition + 1 <= length)
        {
            char c3 = __instance._matchInfo.fullText[__instance._matchInfo.stringPosition];
            __instance._matchInfo.followedBySubmissionCharacter = c3 == ' ';
            __instance._matchInfo.followedByPunctuationCharacter = length > 1 && SharedUtils.IsNonMentionPunctuation(c3);
        }

        if (__instance._matchInfo.matchIndex >= 0)
        {
            int num = __instance._matchInfo.stringPosition - __instance._matchInfo.matchIndex;
            if (__instance._matchInfo.endsWithSubmissionCharacter || __instance._matchInfo.endsWithPunctuationCharacter)
            {
                num = ((!__instance._matchInfo.endsWithSubmissionCharacter || num != 2) ? (num - 1) : 0);
            }

            __instance._matchInfo.matchString = ((num > 0 && __instance._matchInfo.fullText.Length >= __instance._matchInfo.matchIndex + num) ? __instance._matchInfo.fullText.Substring(__instance._matchInfo.matchIndex, num) : string.Empty);
        }
        else
        {
            __instance._matchInfo.matchString = string.Empty;
        }
    }
}*/