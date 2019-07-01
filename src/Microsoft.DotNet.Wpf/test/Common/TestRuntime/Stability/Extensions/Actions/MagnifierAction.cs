// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Diagnostics;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Magnifier Actions start or stop a Magnifier
    /// </summary>
    public class MagnifierAction : SimpleDiscoverableAction
    {
        protected static readonly object MagnifierLock = new object();

        public double OccurrenceIndicator { get; set; }

        #region IAction Members

        /// <summary>
        /// Whether or not the action can be performed. 
        /// </summary>
        /// <returns>true is action can be performed, false otherwise.</returns>
        public override bool CanPerform()
        {
            // Do action only when OccurrenceIndicator is less than 0.002(1/500). 
            return OccurrenceIndicator < 0.002;
        }

        /// <summary>
        /// Perform the action. 
        /// </summary>
        public override void Perform()
        {
            lock (MagnifierLock)
            {
                Process[] magnifierProcesses = Process.GetProcessesByName("Magnify");

                if (magnifierProcesses.Length == 0)
                {
                    try 
                    {
                        Process magnifier = Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\magnify.exe");
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine("Failed to start magnifier with following exception\n. " + e.ToString());
                    }
                }
                else
                {
                    foreach (Process magnifier in magnifierProcesses)
                    {
                        try
                        {
                            magnifier.Kill();
                            magnifier.WaitForExit();
                        }
                        catch (Exception e)
                        { 
                            Trace.WriteLine("Failed to stop magnifier with following exception\n. " + e.ToString()); 
                        }
                    }
                }
            }
        }

        #endregion
    }
}
