// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using Microsoft.Test.VisualVerification;

namespace Microsoft.Test.Execution.EngineCommands
{
    /// <summary>
    /// After each test, saves desktop snapshots differing from previous.
    /// </summary>
    internal class DesktopSnapshotCommand : ICleanableCommand
    {
        private static Snapshot previous = null; //Reference image from end of previous test

        private DirectoryInfo logDirectory;
        internal static readonly Rectangle DefaultRect = new Rectangle(0, 0, 800, 550); //Get a minimal capture - Avoid bottom bar. TODO: Get someone to identify proper bounds...        
        private static readonly Size downsampleSize = new Size(64, 64);
        
        private static int numShots = 0;
        private static int maxShots = 50; //Just accumulate the first n shots to be miserly with disk use, when it's clear we've got a lot of noise

        public static DesktopSnapshotCommand Apply(DirectoryInfo logDirectory)
        {
            return new DesktopSnapshotCommand(logDirectory);
        }

        public DesktopSnapshotCommand(DirectoryInfo logDirectory)
        {            
            this.logDirectory = logDirectory;
        }

        public void Cleanup()
        {
            try
            {
                if (numShots < maxShots) //Stop captures once we've hit 50 image
                {
                    WaitForRender();
                    Snapshot current = Capture();

                    if (previous == null)
                    {
                        LogImage(current, logDirectory, "Baseline_Post_Execution_Snapshot.png");
                    }
                    else
                    {
                        Snapshot diff = previous.CompareTo(current);
                        SnapshotVerifier verifier = new SnapshotColorVerifier();

                        if (verifier.Verify(diff) == VerificationResult.Fail) // Give test benefit of doubt, and re-capture. GDI is known to be glitchy on propagating first snap.
                        {
                            WaitForRender();
                            current = Capture();
                            diff = previous.CompareTo(current);
                            if (verifier.Verify(diff) == VerificationResult.Fail)
                            {
                                LogImage(current, logDirectory, "Current_Post_Execution_Snapshot.png");
                                LogImage(diff.Resize(downsampleSize), logDirectory, "Delta_Post_Execution_Snapshot.png");
                                Logging.LoggingMediator.LogEvent("Desktop snapshot appears to be different from state of previously capture.");
                                numShots++;
                            }
                        }
                    }
                    previous = current; //Now, the current image becomes the new previous.
                }
            }
            catch (Exception)
            {
                ExecutionEventLog.RecordStatus("Error on attempting Desktop Snapshot Command - Was the desktop locked or the context otherwise unavailable?");
            }
        }

        /// <summary>
        /// Wait for a frame of UI to render
        /// </summary>        
        private void WaitForRender()
        {
            Thread.Sleep(17); //1s/60 fps
        }

        private void LogImage(Snapshot snapshot, DirectoryInfo executionDirectory, string filename)
        {
            FileInfo imagePath = new FileInfo(Path.Combine(executionDirectory.FullName, filename));
            snapshot.ToFile(imagePath.FullName, ImageFormat.Png);            
        }

        /// <summary>
        /// Hide the infra window, take capture and return to normal...
        /// </summary>
        /// <returns></returns>
        private Snapshot Capture()
        {
            return Snapshot.FromRectangle(DefaultRect);
        }
    }
}