using System;
using Server.Shared.Messages;
using Server.Shared.State;
using Server.Shared.State.Chat;
using Services;
using UnityEngine;

namespace CommandLib.Util;

public class FeedbackHelper
{
    public static void SendFeedbackMessage(string message, ClientFeedbackType clientFeedbackType)
    {
        ChatLogMessage chatLogMessage = new ChatLogMessage(new ChatLogCustomTextEntry()
        {
            customText = message,
            showInChat = true,
            showInChatLog = false
        });
        Service.Game.Sim.simulation.HandleChatLog(chatLogMessage);
    }
}

public class ColorHelper
{
    public static double GetRelativeLuminance(Color color)
    {
        double RsRGB = color.r;
        double GsRGB = color.g;
        double BsRGB = color.b;

        double R = (RsRGB <= 0.04045) ? RsRGB / 12.92 : Math.Pow((RsRGB + 0.055) / 1.055, 2.4);
        double G = (GsRGB <= 0.04045) ? GsRGB / 12.92 : Math.Pow((GsRGB + 0.055) / 1.055, 2.4);
        double B = (BsRGB <= 0.04045) ? BsRGB / 12.92 : Math.Pow((BsRGB + 0.055) / 1.055, 2.4);



        return 0.2126 * R + 0.7152 * G + 0.0722 * B;
    }

    public static double GetContrastRatio(double l1, double l2)
    {
        return (l1 + 0.05) / (l2 + 0.05);
    }

    public static Color GetTextColor(Color backgroundColor)
    {
        double backgroundLuminance = GetRelativeLuminance(backgroundColor);
        double whiteContrastRatio = GetContrastRatio(1, backgroundLuminance);
        double blackContrastRatio = GetContrastRatio(backgroundLuminance, 0);

        return (whiteContrastRatio > blackContrastRatio) ? Color.white : Color.black;
    }

    public static Color GetOppositeColor(Color color)
    {
        return new Color((color.r + 0.5f) % 1f, (color.g + 0.5f) % 1f, (color.b + 0.5f) % 1f);
    }
}