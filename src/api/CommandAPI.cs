using System;
using System.Collections.Generic;
using System.Linq;
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

    public string Name { get; private set; }
    public string[] Aliases { get; private set; }
    public string HarmonyId { get; private set; }
    public TokenType[][] Syntaxes { get; private set; }

    public Command(string name, TokenType[][] syntaxes, string harmonyId = null, string[] aliases = null)
    {
        this.Name = name;
        this.HarmonyId = harmonyId ?? AssemblyHelper.GetHarmonyIdFromAssembly(Assembly.GetCallingAssembly());
        this.Aliases = aliases ?? [];
        this.Syntaxes = syntaxes;
    }

    public Command(string name, TokenType[][] syntaxes, string[] aliases = null, string harmonyId = null)
    {
        this.Name = name;
        this.HarmonyId = harmonyId ?? AssemblyHelper.GetHarmonyIdFromAssembly(Assembly.GetCallingAssembly());
        this.Aliases = aliases ?? [];
        this.Syntaxes = syntaxes;
    }

    public Command(string name, TokenType[] syntax, string harmonyId = null, string[] aliases = null)
    {
        this.Name = name;
        this.HarmonyId = harmonyId ?? AssemblyHelper.GetHarmonyIdFromAssembly(Assembly.GetCallingAssembly());
        this.Aliases = aliases ?? [];
        this.Syntaxes = [ syntax ];
    }

    public Command(string name, TokenType[] syntax, string[] aliases = null, string harmonyId = null)
    {
        this.Name = name;
        this.HarmonyId = harmonyId ?? AssemblyHelper.GetHarmonyIdFromAssembly(Assembly.GetCallingAssembly());
        this.Aliases = aliases ?? [];
        this.Syntaxes = [ syntax ];
    }

    /// <summary>
    /// Method <c>Execute<c> executes the command.
    /// </summary>
    /// <param name="tokens">the arguments given by the user</param>
    /// <returns>
    /// A Tuple containing a boolean and a string.
    /// The boolean represents whether the command executed properly.
    /// The string is the error message given to the user if the command fails.
    /// </returns>
    public abstract Tuple<bool, string> Execute(int syntaxIndex, object[] tokens);

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

public class TokenType {
    public string ShortName {get; private set;}
    public string LongName {get; private set;}
    public Type ValueType {get; private set;}

    public TokenType(string shortName, string longName, Type valueType) {
        this.ShortName = shortName;
        this.LongName = longName;
        this.ValueType = valueType;
    }

    public virtual Tuple<bool, object> Validate(string input) {
        return new Tuple<bool, object>(true, input);
    }

    public virtual string Complete(string token) {
        return token;
    }

    public virtual Tuple<object, string> Tokenize(string fullInput) {
        string[] tokenized = fullInput.Split(" ");
        return new Tuple<object, string>(tokenized[0], tokenized.Skip(1).ToString());
    }

    public class TextTokenType : TokenType
    {
        public TextTokenType() : base("text", "plaintext", typeof(string))
        {
        }

        public override Tuple<bool, object> Validate(string input)
        {
            return new Tuple<bool, object>(true, input);
        }

        public override string Complete(string token) {
            return token.EndsWith('"') ? token : token + '"';
        }

        public override Tuple<object, string> Tokenize(string fullInput) {

            if (fullInput.StartsWith('"') && fullInput.Length > 1) {
                int index = fullInput.IndexOf('"', 1);
                return new Tuple<object, string>(index == -1 ? fullInput : fullInput.Substring(0, index), index == -1 ? "" : fullInput.Substring(index));
            }

            string[] tokenized = fullInput.Split(" ");
            
            return new Tuple<object, string>(tokenized[0], tokenized.Skip(1).ToString());
        }
    }

    public class NumberTokenType : TokenType
    {
        public NumberTokenType() : base("num", "number", typeof(int))
        {
        }

        public override Tuple<bool, object> Validate(string input)
        {
            int i;
            bool result = int.TryParse(input, out i);

            return new Tuple<bool, object>(result, i);
        }
    }

    public class CommandTokenType : TokenType
    {
        public CommandTokenType() : base("command", "command", typeof(Command))
        {
        }

        public override Tuple<bool, object> Validate(string input)
        {
            Command foundCommand = CommandRegistry.Commands.Find((Command command) => input.ToLowerInvariant() == command.Name || command.Aliases.Contains(input.ToLowerInvariant()));

            return new Tuple<bool, object>(foundCommand != null, foundCommand);
        }
    }
}

public class CommandRegistry
{
    public static List<Command> Commands { get; private set; } = new List<Command>();

    public static void AddCommand(Command command)
    {
        ModLogger.Log($"Registered {command.Name} from {command.HarmonyId}");
        Commands.Add(command);
    }

    public static void RemoveCommand(Command command)
    {
        ModLogger.Log($"Unregistered {command.Name} from {command.HarmonyId}");
        Commands.Remove(command);
    }
}

[Obsolete("This interface is obsolete. Use IDescription instead.", true)]
public interface IHelpMessage
{
    string GetHelpMessage();
}

public interface IDescription
{
    public string GetDescription();
}