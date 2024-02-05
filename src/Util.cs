using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Server.Shared.Messages;
using Server.Shared.State;
using Server.Shared.State.Chat;
using Services;
using SML;
using UnityEngine;

namespace CommandLib.Util;

// More clear logging
public class ModLogger
{
    public static void Log(object message)
    {
        Log(message.ToString());
    }

    public static void Log(string message)
    {
        Debug.Log($"[CommandLib] {message}");
    }
}

public class FeedbackHelper
{
    public static void SendFeedbackMessage(string message)
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

public class AssemblyHelper
{
    public static string GetHarmonyIdFromAssembly(Assembly assembly)
    {
        string[] manifestResourceNames = assembly.GetManifestResourceNames();
        string modinfoResource = Array.Find(manifestResourceNames, (string resourceName) => resourceName.ToLower().EndsWith("modinfo.json"));

        if (modinfoResource == null) return "";

        using (Stream manifestResourceStream = assembly.GetManifestResourceStream(modinfoResource))
        {
            using (StreamReader streamReader = new StreamReader(manifestResourceStream))
            {
                Mod.ModInfo modInfo = JsonConvert.DeserializeObject<Mod.ModInfo>(streamReader.ReadToEnd());
                return modInfo.HarmonyId;
            }
        }
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

    // Get appropriate text color (white or black), given a background color
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