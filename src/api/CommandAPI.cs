using System;
using CommandLib.UI;
using UnityEngine;

namespace CommandLib.API;

public abstract class Command
{
    public enum CommandArgType
    {
        Text,
        Integer,
        Float,
        Any
    }

    public string name { get; private set; }
    public string[] aliases { get; private set; }
    public string harmonyID { get; private set; }
    public Color customColor { get; private set; }

    public Command(string name, string harmonyID, string[] aliases = null)
    {
        this.name = name;
        this.harmonyID = harmonyID;
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
}

public interface IHelpMessage
{
    string GetHelpMessage();
}