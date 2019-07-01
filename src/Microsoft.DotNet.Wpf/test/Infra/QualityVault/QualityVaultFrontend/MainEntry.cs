// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Test.CommandLineParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Test.Commands;

namespace Microsoft.Test
{
    /// <summary>
    /// Entry point for RunTests.
    /// </summary>
    internal static class MainEntry
    {
        const string debugQv = "debugqv";
        /// <summary>
        /// Collection of all possible commands that RunTests supports.
        /// </summary>
        // We exclude MadDogDiscoverCommand on non-4.0 b/c it requires external DevDiv dlls that are only available as 4.0
        private static readonly Command[] RunTestsCommands = new Command[] { new CleanupCommand(),
                                                                             new RegisterForDistributionCommand(),
                                                                             new DiscoverAndDistributeCommand(),
                                                                             new ExecuteCommand(),
                                                                             new MergeResultsCommand(),
                                                                             new RunCommand(),
                                                                             new MakeReportCommand()};

        /// <summary>
        /// Entry point for test infrastructure:
        /// -Figure out what task we are being called to do, and pass the params to that component.
        /// -Provide suitable help to user if invalid input is supplied.
        /// -Deal with errors.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [LoaderOptimization(LoaderOptimization.MultiDomainHost)]
        [STAThread]
        private static int Main(string[] args)
        {
            try
            {   
                // If no arguments were specified or the first argument does
                // not match the name of any known command, print general usage.
                IEnumerable<string> commandNames = RunTestsCommands.Select(command => command.Name);
                if (args.Length == 0 || !commandNames.Contains(args[0], StringComparer.OrdinalIgnoreCase))
                {
                    CommandLineParser.PrintCommands(RunTestsCommands);
                    return -1;
                }

                // Identify the command whose name matches the first
                // command line argument. We already verified that
                // such a command an exists.
                Command commandToExecute = RunTestsCommands.First(command => command.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase));

                if (args.Any(arg => arg.EndsWith(debugQv, StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("Waiting for debugger to attach to QualityVaultFrontEnd.exe...");
                    while (!System.Diagnostics.Debugger.IsAttached)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                    System.Diagnostics.Debugger.Break();
                }

                // The rest of the command line should be key/value pairs
                // of the form /PropertyName=Value, so we set the
                // properties on the command from those arguments, and
                // if SetProperties throws an ArgumentException, print
                // that exception message along with specific usage
                // for the particular command.
                IEnumerable<string> kvpArguments = args.Skip(1).Where(arg => !arg.EndsWith(debugQv, StringComparison.OrdinalIgnoreCase));
                try
                {
                    commandToExecute.ParseArguments(kvpArguments);
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine(e.Message);
                    CommandLineParser.PrintUsage(commandToExecute);
                    return -1;
                }

                commandToExecute.Execute();
                Profiler.GenerateReport();
                Console.WriteLine("Done.");
            }
            catch (Exception e)
            {
                //Log only the simple error summary to console. 
                Console.WriteLine("An infrastructure error occurred. Please take a look:");
                Console.WriteLine(e);                

                return -1;
            }

            return 0;
        }
    }
}
