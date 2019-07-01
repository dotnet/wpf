// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Test.Input;

namespace Microsoft.Test.Execution.StateManagement.ModernShell
{
    class ModernShellUtilities
    {
        public const int ZBID_DEFAULT = 0;
        public const int ZBID_DESKTOP = 1;

        [DllImport("user32.dll")]
        private static extern bool GetWindowBand(IntPtr hwnd, ref int idBand);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        //Checks to see if any Immersive window is open
        internal static bool IsImmersiveWindowOpen()
        {
            bool fIsImmersive = true;
            int idBand = ZBID_DEFAULT;
            try
            {
                GetWindowBand(GetForegroundWindow(), ref idBand);
                fIsImmersive = (idBand != ZBID_DESKTOP);
            }
            catch (Exception) 
            {
                fIsImmersive = true;
            }

            return fIsImmersive;
        }

        // Switches to desktop waits and restores the windows
        internal static void EnsureDesktop()
        {
            string showDesktopPath = GetShowDesktopPath();
            ShowDesktop(showDesktopPath);

            // Check if any Immersive window is open
            if (!WaitForDesktop())
            {
                try
                {
                    //We'll try to move the mouse out of the way in case it is triggering some Mosh UI interactions.			
                    Mouse.MoveTo(new System.Drawing.Point(100, 100));
                }
                catch (Exception)
                {
                    //Can't move mouse for some reason. Oh well, it was worth a try.
                }
                ShowDesktopLegacy();
            }

        }

        //Waits for the Immersive  window to be dismissed.
        //Returns false if the immersive window fails to dismiss.
        internal static bool WaitForDesktop()
        {
            int maxRetries = 5;
            
            for (int i = 0; i < maxRetries && IsImmersiveWindowOpen(); i++)
            {
                Thread.Sleep(1000);
            }

            //return true if immersive window is no longer open.
            return !IsImmersiveWindowOpen();
        }

        internal static void ShowDesktopLegacy()
        {
            Keyboard.Press(Key.LWin);
           // Keyboard.Type(Key.D);
            Keyboard.Release(Key.LWin);
        }
        
        internal static string GetShowDesktopPath()
        {
            string winDir = "";
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("windir")))
            {
                winDir = Environment.GetEnvironmentVariable("windir");
            }

            return (Path.Combine(winDir, @"ShowDesktop.exe"));
        }

        internal static void ShowDesktop(string showDesktopPath)
        {
            if (File.Exists(showDesktopPath))
            {
                string logFile = string.Format("showdesktop-{0:yyyy-MM-dd_hh-mm-ss-tt}.log", DateTime.Now);

                ProcessStartInfo startInfo = new ProcessStartInfo(showDesktopPath, "-o " + Path.GetTempPath() + logFile);
                startInfo.UseShellExecute = true;
                try
                {
                    Process showDesktopProcess = Process.Start(startInfo);
                    showDesktopProcess.WaitForExit(10000);
                }
                catch (Exception)
                {
                    ShowDesktopLegacy();
                }
            }
            else
            {
                ShowDesktopLegacy();
            }
        }  
    }
}
