using System;
using System.Collections.Generic;
using System.Reflection;
using CommandLib.UI;
using CommandLib.Util;
using SalemModLoader;
using Server.Shared.Extensions;
using SML;
using UnityEngine;

namespace CommandLib.API;

public abstract class Command
{

    public string name { get; private set; }
    public string[] aliases { get; private set; }
    public string harmonyId { get; private set; }

    public Command(string name, string harmonyId = null, string[] aliases = null)
    {
        this.name = name;
        this.harmonyId = harmonyId ?? AssemblyHelper.GetHarmonyIdFromAssembly(Assembly.GetCallingAssembly());
        this.aliases = aliases ?? [];
    }

    public Command(string name, string[] aliases = null, string harmonyId = null)
    {
        this.name = name;
        this.harmonyId = harmonyId ?? AssemblyHelper.GetHarmonyIdFromAssembly(Assembly.GetCallingAssembly());
        this.aliases = aliases ?? [];
    }

    /// <summary>
    /// Method <c>Execute<c> executes the command.
    /// </summary>
    /// <param name="args">the arguments given by the user</param>
    /// <returns>
    /// A Tuple containing a boolean and a string.
    /// The boolean represents whether the command executed properly.
    /// The string is the error message given to the user if the command fails.
    /// </returns>
    public abstract Tuple<bool, string> Execute(string[] args);

    /// <summary>
    /// Static method <c>RegisterCustomColor<c> registers a custom color to all commands from a given harmonyID.
    /// </summary>
    /// <param name="harmonyID">the harmonyID</param>
    /// <param name="color">the custom color</param>
    public static void RegisterCustomColor(string harmonyID, Color color)
    {
        CommandUI.SetColor(harmonyID, color);
    }

    /// <summary>
    /// Static method <c>RegisterCustomColor<c> registers a custom color to all commands from a given harmonyID.
    /// </summary>
    /// <param name="color">the custom color</param>
    public static void RegisterCustomColor(Color color)
    {
        CommandUI.SetColor(AssemblyHelper.GetHarmonyIdFromAssembly(Assembly.GetCallingAssembly()), color);
    }
}

public class CommandRegistry
{
    public static List<Command> Commands { get; private set; } = new List<Command>();

    public static void AddCommand(Command command)
    {
        ModLogger.Log($"Registered {command.name} from {command.harmonyId}");
        Commands.Add(command);
    }

    public static void RemoveCommand(Command command)
    {
        ModLogger.Log($"Unregistered {command.name} from {command.harmonyId}");
        Commands.Remove(command);
    }
}

public interface IHelpMessage
{
    string GetHelpMessage();
}