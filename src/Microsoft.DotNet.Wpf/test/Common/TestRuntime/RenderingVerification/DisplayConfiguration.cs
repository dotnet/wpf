// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using Microsoft.Test.Logging;
using Microsoft.Test.Loaders;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Automation;
using System.Windows.Input;

namespace Microsoft.Test.RenderingVerification {
    

    /// <summary>
    /// Enables you to change the Display Configuration of the system
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public static partial class DisplayConfiguration
    {

        #region Public Members

        /// <summary>
        /// Sets the system theme to the specified installed theme on the system
        /// </summary>
        /// <param name="name">Name of the theme to be set (this is the name of the .theme file without the extention)</param>
        public static void SetTheme(string name) {
            if (name == null)
                throw new ArgumentNullException("name");
            string filename = Path.Combine(themeDirectory, name + ".theme");
            if (!File.Exists(filename))
                throw new ArgumentException("The theme '" + name + "' does not exist.", name);
            
            //change the theme if we are in a different theme
            if (name.ToLowerInvariant() != GetTheme().ToLowerInvariant())
                SetCustomTheme(filename);
        }

        /// <summary>
        /// Sets the system theme to a custom .theme file
        /// </summary>
        /// <param name="filename">the path the the .theme file that should be set as the system theme</param>
        public static void SetCustomTheme(string filename) {
            //validate input
            if (filename == null)
                throw new ArgumentNullException("filename");
            if (!File.Exists(filename))
                throw new FileNotFoundException("The specified theme file could not be found", filename);
            if (Path.GetExtension(filename).ToLowerInvariant() != ".theme")
                throw new ArgumentException("The theme file must have a .theme extention.", "filename");

            //Copy the file to another location before setting the theme
            //The reason for this is that if the custom theme is left after the test
            //executes the Automation Harness may delete the current theme which
            //gives and error that prevents the system from restoring the default theme
            string destFilename = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.InternetCache), Path.GetFileName(filename));
            File.Copy(filename, destFilename, true);

            //Get the active window since the window activation will be lost by lauching the display properties
            IntPtr activehWnd = GetForegroundWindow();
            ApplicationMonitor appMonitor = new ApplicationMonitor();

            try {
                //invoke the theme and handle the dispay config UI
                appMonitor.RegisterUIHandler(new ChangeThemesUIHandler(), "rundll32", null, UIHandlerNotification.Visible);
                appMonitor.StartProcess(destFilename);
                if (!appMonitor.WaitForUIHandlerAbort(60000))
                    throw new TimeoutException("The 60 second timeout occured while waiting for themes to change.");
            }
            finally {
                //Stop the AppMonitor (this will kill rundll32 if it has not exited... hopefuly this wont leave the machine in a bad state if it a timeout occurs
                appMonitor.Close();

                //Restore the active window
                if (activehWnd != IntPtr.Zero)
                    SetForegroundWindow(activehWnd);
            }
        }

        /// <summary>
        /// Change the theme, style, and color scheme to match a defined appearance
        /// </summary>
        /// <param name="appearance">the desired defined appearance</param>
        public static void SetAppearance(DesktopAppearance appearance)
        {
            if (appearance == GetAppearance())
                return;
            else if ((!new ArrayList(GetAvailableAppearances()).Contains(appearance)))
                throw new ArgumentException("The appearance '" + appearance + "' is not currently available.", appearance.ToString());

            string themeName = GetTheme();
            string styleFilename = null;

            // This is only needed if a change in color scheme is required (for Luna and Aero)
            UIHandler appearanceUIHandler = null;

            if (appearance == DesktopAppearance.LunaNormalColor
                || appearance == DesktopAppearance.LunaMetallic
                || appearance == DesktopAppearance.LunaHomestead)
            {
                // First ensure the theme is set to Luna
                if (themeName.ToLowerInvariant() != LunaString.ToLowerInvariant())
                {
                    SetTheme(LunaString);
                    themeName = GetTheme();
                    if (themeName.ToLowerInvariant() != LunaString.ToLowerInvariant())
                        throw new ArgumentException("Failed to change to the theme '" + LunaString + "'.", appearance.ToString());
                }

                // Build the path for the Luna.msstyles file
                styleFilename = Path.Combine(themeDirectory, themeName + @"\" + themeName + ".msstyles");
                if (!File.Exists(styleFilename))
                    throw new ArgumentException("The msstyles file '" + styleFilename + "' does not exist.", appearance.ToString());

                string lunaResourceString = "";

                // Get the localized string to select in the color scheme menu
                if (appearance == DesktopAppearance.LunaMetallic)
                    lunaResourceString = Microsoft.Test.Loaders.ResourceHelper.GetUnmanagedResourceString(styleFilename, 1001);
                else if (appearance == DesktopAppearance.LunaHomestead)
                    lunaResourceString = Microsoft.Test.Loaders.ResourceHelper.GetUnmanagedResourceString(styleFilename, 1002);
                else
                    lunaResourceString = Microsoft.Test.Loaders.ResourceHelper.GetUnmanagedResourceString(styleFilename, 1000);

                // Use the UIHandler for the XP Appearance dialog
                appearanceUIHandler = new ChangeXPAppearanceUIHandler(lunaResourceString);
            }
            else if (appearance == DesktopAppearance.AeroWithoutComposition
                || appearance == DesktopAppearance.AeroWithComposition)
            {
                // First ensure the theme is set to Aero
                if (themeName.ToLowerInvariant() != AeroString.ToLowerInvariant())
                {
                    SetTheme(AeroString);
                    themeName = GetTheme();
                    if (themeName.ToLowerInvariant() != AeroString.ToLowerInvariant())
                        throw new ArgumentException("Failed to change to the theme '" + AeroString + "'.", appearance.ToString());
                }

                // Build the path to Aero.msstyles
                styleFilename = Path.Combine(themeDirectory, themeName + @"\" + themeName + ".msstyles");
                if (!File.Exists(styleFilename))
                    throw new ArgumentException("The msstyles file '" + styleFilename + "' does not exist.", appearance.ToString());

                // Use the UIHandler for the Vista Appearance dialog
                appearanceUIHandler = new ChangeVistaAppearanceUIHandler(appearance);
            }
            else if (appearance == DesktopAppearance.Royale)
            {
                // Change the theme to Royale
                if (themeName.ToLowerInvariant() != RoyaleString.ToLowerInvariant())
                {
                    SetTheme(RoyaleString);
                    themeName = GetTheme();
                    if (themeName.ToLowerInvariant() != RoyaleString.ToLowerInvariant())
                        throw new ArgumentException("Failed to change to the theme '" + RoyaleString + "'.", appearance.ToString());
                }
            }
            else if (appearance == DesktopAppearance.WindowsClassic)
            {
                // Change the theme to Classic
                if (themeName.ToLowerInvariant() != WindowsClassicString.ToLowerInvariant())
                {
                    SetTheme(WindowsClassicString);
                    themeName = GetTheme();
                    if (themeName.ToLowerInvariant() != WindowsClassicString.ToLowerInvariant())
                        throw new ArgumentException("Failed to change to the theme '" + WindowsClassicString + "'.", appearance.ToString());
                }
            }

            if (appearanceUIHandler != null)
            {
                //Get the active window since the window activation will be lost by lauching the display properties
                IntPtr activehWnd = GetForegroundWindow();
                ApplicationMonitor appMonitor = new ApplicationMonitor();
                try
                {
                    //invoke the msstyles file and handle the dispay config UI
                    appMonitor.RegisterUIHandler(appearanceUIHandler, "rundll32", null, UIHandlerNotification.Visible);
                    appMonitor.StartProcess(styleFilename);
                    if (!appMonitor.WaitForUIHandlerAbort(60000))
                        throw new TimeoutException("The 60 second timeout occured while waiting for themes to change.");
                }
                finally
                {
                    //Stop the AppMonitor (this will kill rundll32 if it has not exited... hopefuly this wont leave the machine in a bad state if it a timeout occurs
                    appMonitor.Close();

                    //Restore the active window
                    if (activehWnd != IntPtr.Zero)
                        SetForegroundWindow(activehWnd);
                }
            }

            if (GetAppearance() != appearance)
            {
                throw new Exception("Unable to change appearance.");
            }
        }

        #endregion


        #region Private Implementation

        [System.Runtime.InteropServices.DllImport("user32")]
        static extern IntPtr GetForegroundWindow();
        
        [System.Runtime.InteropServices.DllImport("user32")]
        static extern void SetForegroundWindow(IntPtr hWnd);

        //handles the Dispay Configuration UI
        class ChangeThemesUIHandler : UIHandler {
            public override UIHandlerAction HandleWindow(IntPtr topLevelhWnd, IntPtr hwnd, Process process, string title, UIHandlerNotification notification) {
                GlobalLog.LogDebug("Display Configuration window found.");
                AutomationElement window = AutomationElement.FromHandle(topLevelhWnd);

                // get a reference the address bar
                GlobalLog.LogDebug("Finding and clicking the OK Button");
                Condition cond = new PropertyCondition(AutomationElement.AutomationIdProperty, "1");
                AutomationElement okBtn = window.FindFirst(TreeScope.Descendants, cond);
                object patternObject;
                okBtn.TryGetCurrentPattern(InvokePattern.Pattern, out patternObject);
                InvokePattern invokePattern = patternObject as InvokePattern;
                invokePattern.Invoke();

                GlobalLog.LogDebug("Waiting for theme to be applied...");
                process.WaitForExit();

                return UIHandlerAction.Abort;
            }
        }

        class ChangeXPAppearanceUIHandler : UIHandler
        {
            private string resourceString;

            public ChangeXPAppearanceUIHandler(string resourceString) : base()
            {
                this.resourceString = resourceString;
            }

            public override UIHandlerAction HandleWindow(IntPtr topLevelhWnd, IntPtr hwnd, Process process, string title, UIHandlerNotification notification)
            {
                GlobalLog.LogDebug("Display Properties (Appearance tab) window found.");
                AutomationElement window = AutomationElement.FromHandle(topLevelhWnd);

                GlobalLog.LogDebug("Finding Color Scheme combo box and selecting '" + resourceString + "'.");
                Condition cond = new PropertyCondition(AutomationElement.AutomationIdProperty, "1114");
                AutomationElement colorSchemeCombo = window.FindFirst(TreeScope.Descendants, cond);

                cond = new PropertyCondition(AutomationElement.NameProperty, resourceString);
                AutomationElement item = colorSchemeCombo.FindFirst(TreeScope.Descendants, cond);

                // Tell the correct combobox item to be selected
                object patternObject;
                item.TryGetCurrentPattern(SelectionItemPattern.Pattern, out patternObject);
                SelectionItemPattern selectionItemPattern = patternObject as SelectionItemPattern;
                selectionItemPattern.Select();

                GlobalLog.LogDebug("Finding and clicking the OK Button");
                cond = new PropertyCondition(AutomationElement.AutomationIdProperty, "1");
                AutomationElement okBtn = window.FindFirst(TreeScope.Descendants, cond);
                
                okBtn.TryGetCurrentPattern(InvokePattern.Pattern, out patternObject);
                InvokePattern invokePattern = patternObject as InvokePattern;
                invokePattern.Invoke();

                GlobalLog.LogDebug("Waiting for appearance to be applied...");
                process.WaitForExit();

                return UIHandlerAction.Abort;
            }
        }

        class ChangeVistaAppearanceUIHandler : UIHandler
        {
            private DesktopAppearance newAppearance;

            public ChangeVistaAppearanceUIHandler(DesktopAppearance newAppearance) : base()
            {
                this.newAppearance = newAppearance;
            }

            public override UIHandlerAction HandleWindow(IntPtr topLevelhWnd, IntPtr hwnd, Process process, string title, UIHandlerNotification notification)
            {
                GlobalLog.LogDebug("Appearance settings window found.");
                AutomationElement window = AutomationElement.FromHandle(topLevelhWnd);

                GlobalLog.LogDebug("Finding Color Scheme combo box and selecting correct value.");
                Condition cond = new PropertyCondition(AutomationElement.AutomationIdProperty, "1114");
                AutomationElement colorSchemeComboBox = window.FindFirst(TreeScope.Descendants, cond);

                // This gets us to the vertical scroll bar
                AutomationElement item = TreeWalker.ControlViewWalker.GetFirstChild(colorSchemeComboBox);
                
                // This gets us to the first item in the list
                item = TreeWalker.ControlViewWalker.GetNextSibling(item);
                
                // On systems which support DWM composition, the first item will be either
                // "Windows Vista Aero" or "Windows Vista Standard" (depending on the SKU).
                // The second item will be "Windows Vista Basic".
                // "Windows Vista Aero" features both DWM composition and glass.
                // "Windows Vista Standard" features DWM composition but no glass.
                // "Windows Vista Basic" does not feature DWM composition or glass.
                // No machine will ever support both Aero and Standard. All machines support Basic.

                // If this machine supports composition, but we want to turn it off,
                // we need to move to the second item in the list, "Windows Vista Basic".
                // Otherwise, we select the first item, either because composition is supported
                // and we want it enabled, or it is not supported.
                if (IsDwmCompositionSupported && newAppearance == DesktopAppearance.AeroWithoutComposition)
                {
                    item = TreeWalker.ControlViewWalker.GetNextSibling(item);
                }

                object patternObject;
                item.TryGetCurrentPattern(SelectionItemPattern.Pattern, out patternObject);
                SelectionItemPattern selectionItemPattern = patternObject as SelectionItemPattern;
                selectionItemPattern.Select();

                // HACK: On recent builds of Vista, UI Automation does not seem to properly
                // notify the dialog that the selection has changed. This message is normally
                // sent by the combo box to the dialog.
                IntPtr wParam = MakeWParam(1114, CBN_SELCHANGE);

                SendMessage(hwnd, WM_COMMAND, wParam, new IntPtr((int)colorSchemeComboBox.GetCurrentPropertyValue(AutomationElement.NativeWindowHandleProperty)));


                GlobalLog.LogDebug("Finding and clicking the OK Button");
                cond = new PropertyCondition(AutomationElement.AutomationIdProperty, "1");
                AutomationElement okBtn = window.FindFirst(TreeScope.Descendants, cond);

                okBtn.TryGetCurrentPattern(InvokePattern.Pattern, out patternObject);
                InvokePattern invokePattern = patternObject as InvokePattern;
                invokePattern.Invoke();

                GlobalLog.LogDebug("Waiting for appearance to be applied...");
                process.WaitForExit();

                return UIHandlerAction.Abort;
            }
        }

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static IntPtr MakeWParam(int LoWord, int HiWord)
        {
            return new IntPtr((HiWord << 16) | (LoWord & 0xffff));
        }

        private const uint WM_COMMAND = 0x0111;

        private const int CBN_SELCHANGE = 1;


        #endregion        
    }
}
