// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.CommandLineParsing
{
    /// <summary>
    /// Provides a base class for the functionality that all commands must implement.
    /// </summary>
    /// 
    /// <example>
    /// The following example shows parsing of a command-line such as "Test.exe RUN /verbose /runId=10"
    /// into a strongly-typed Command, that can then be excuted.
    /// <code>
    /// using System;
    /// using System.Linq;
    /// using Microsoft.Test.CommandLineParsing;
    /// 
    /// public class RunCommand : Command
    /// {
    ///     public bool? Verbose { get; set; }
    ///     public int? RunId { get; set; }
    ///
    ///     public override void Execute()
    ///     {
    ///         Console.WriteLine("RunCommand: Verbose={0} RunId={1}", Verbose, RunId);  
    ///     }
    /// }
    ///
    /// public class Program
    /// {
    ///     public static void Main(string[] args)
    ///     {
    ///         if (String.Compare(args[0], "run", StringComparison.InvariantCultureIgnoreCase) == 0)
    ///         {
    ///             Command c = new RunCommand();
    ///             c.ParseArguments(args.Skip(1)); // or CommandLineParser.ParseArguments(c, args.Skip(1))
    ///             c.Execute();
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class Command
    {
        /// <summary>
        /// The name of the command. The base implementation is to strip off the last
        /// instance of "Command" from the end of the type name. So "DiscoverCommand"
        /// would become "Discover". If the type name does not have the string "Command" in it, 
        /// then the name of the command is the same as the type name. This behavior can be 
        /// overridden, but most derived classes are going to be of the form [Command Name] + Command.
        /// </summary>
        public virtual string Name
        {
            get
            {
                string typeName = this.GetType().Name;
                if (typeName.Contains("Command"))
                {
                    return typeName.Remove(typeName.LastIndexOf("Command", StringComparison.Ordinal));
                }
                else
                {
                    return typeName;
                }
            }
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        public abstract void Execute();
    }
}