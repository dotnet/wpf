// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Diagnostics;

namespace Microsoft.Test.Execution
{      
    /// <summary>
    /// Handles policy for Installation and use/interactions with CDB debugger   
    /// </summary>
    internal static class CdbUtilities
    {
        private static readonly string cdbPath = @"c:\debuggers\cdb.exe";

        internal static void InstallCdb()
        {
            //Install cdb if it is absent. If this doesn't fail something hokey with the installation, that should be pretty obvious
            if (!File.Exists(cdbPath))
            {                
                //Note: Use cmd to run install script indirectly to avoid it getting a return code before it finishes
                //Note: Script isn't really ideal for automation. We may want to implement ourselves IF it's unreliable...
                ProcessUtilities.Run("cmd", @" /c \\dbg\privates\latest\dbginstall.cmd", ProcessWindowStyle.Normal, true);
            }
        }

        /// <summary>
        /// Debug the specified process.
        /// </summary>
        internal static bool DebugProcess(string pid, FileInfo debugLogFilePath, FileInfo debugDumpFilePath)
        {
            string debuggerPath = cdbPath;


            //Not including the descriptions so we can feed command directly into cdb as argument.
            //We may want to scale this up if the logs turn out to be helpful/popular.
            //Do not use !Analyse as this command is far too slow to execute.
            string debugCommandSequence =   ".logopen \\\"" + debugLogFilePath.FullName + "\\\"; " +
                                            ".symfix;.reload; " +               //Getting Symbols set up
                                            ".loadby sos " + DetermineClrName() + "; " + //Loading SOS for Managed Debugging;
                                            "!pe; " +                           //Print the Managed exception if there is one to justify loading SOS
                                            ".lines; " +                        //Providing source lines for stack trace;
                                            "KP;|;~; " +                        //Stack Trace;
                                            "version; " +                       //Collecting Common Version/System Information;
                                            ".dump /m /o \\\"" + debugDumpFilePath.FullName + "\\\"; " +   //Produce mini dump file, with overwrite
                                            ".logclose; " +                     //Debug session is finished;
                                            ".kill;qd ";

            string debuggerCommand = " -pv -p " + pid + " -c \"" + debugCommandSequence + " \"";

            int exitCode = ProcessUtilities.Run(debuggerPath, debuggerCommand, ProcessWindowStyle.Normal, true);
            return (exitCode != 0);
        }

        private static string DetermineClrName()
        {
            string clrDllName = "clr";
            try
            {
                int clrVersion = new Version(Environment.Version.ToString()).Major;
                // Note that there are no plans to rename clr.dll in v.Next but if it happens,
                // this needs to change to a case statement.
                if (clrVersion < 4)
                {
                    clrDllName = "mscorwks";
                }
                else
                {
                    clrDllName = "clr";
                }
            }
            catch { /* Do nothing */ };
            return clrDllName;
        }
    }
}