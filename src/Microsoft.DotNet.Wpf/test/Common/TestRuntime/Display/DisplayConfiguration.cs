// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using Microsoft.Test.Diagnostics;
using Microsoft.Test.Input;
using Microsoft.Test.Win32;
using Microsoft.Win32;

namespace Microsoft.Test.Display
{
    /// <summary>
    /// Enables you to change the Display Configuration of the system
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public static class DisplayConfiguration
    {
        #region Private Data

        private readonly static string ThemeProcessName = "rundll32";
        private readonly static string themeDirectory = Environment.ExpandEnvironmentVariables(@"%WINDIR%\Resources\Themes");

        private const string AeroString = "aero";
        private const string LunaString = "luna";
        private const string RoyaleString = "royale";
        private const string WindowsClassicString = "windows classic";
        private const string LunaNormalColorString = "normalcolor";
        private const string LunaMetallicString = "metallic";
        private const string LunaHomesteadString = "homestead";

        private delegate void ChangeAppearance();

        #endregion

        #region Public Members

        /// <summary>
        /// Sets the theme on the machine
        /// </summary>
        /// <param name="name"></param>
        public static void SetTheme(string name)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            name = name.ToLowerInvariant();

            string filename = Path.Combine(themeDirectory, name + ".theme");
            if (!File.Exists(filename))
                throw new ArgumentException("The theme '" + name + "' does not exist.", name);
            
            //change the theme if we are in a different theme
            if (name != GetTheme())
                SetCustomTheme(filename); 
        }        

        /// <summary>
        /// Sets a theme based on a filename
        /// </summary>
        /// <param name="filename"></param>
        public static void SetCustomTheme(string filename)
        {
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
            string destFilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache), Path.GetFileName(filename));
            File.Copy(filename, destFilename, true);
            bool themeSet = false;

            MonitorProcess(filename, new VisibleWindowHandler(delegate(IntPtr topLevelhWnd, IntPtr hWnd, Process process, string title)
            {
                if (!themeSet)
                {
                    themeSet = true;    //The process monitor calls back more than once
                 
                    //TODO: Use UI Automation if possible
                    InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.Enter, true);
                    InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.Enter, false);
                }
            }), null);
        }

        /// <summary>
        /// Sets a theme based on a filename
        /// </summary>
        /// <param name="filename"></param>
        public static void SaveCurrentTheme(string filename)
        {
            //Need to ensure the sleeps timeouts work and check if we can
            //disable user input with Win32 call

            if (filename == null)
                throw new ArgumentNullException("filename");
            if (Path.GetExtension(filename).ToLowerInvariant() != ".theme")
                throw new ArgumentException("The theme file must have a .theme extention.", "filename");

            bool themeSaved = false;

            //Open the Display Properties window
            MonitorProcess("desk.cpl", new VisibleWindowHandler(delegate(IntPtr topLevelhWnd, IntPtr hWnd, Process process, string title)
            {
                if (!themeSaved)
                {
                    themeSaved = true;    //The process monitor calls back more than once

                    Thread.Sleep(1000);

                    InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.Tab, true);
                    InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.Tab, false);
                    System.Diagnostics.Debug.WriteLine("Moving to Save Button");
                    Thread.Sleep(1000);                    

                    InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.Enter, true);
                    InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.Enter, false);
                    System.Diagnostics.Debug.WriteLine("Opening Save Dialog");
                    Thread.Sleep(1000);
                                        
                    InputHelper.SendKeyboardString(filename);
                    System.Diagnostics.Debug.WriteLine("Entering filename");
                    Thread.Sleep(1000);                

                    if (File.Exists(filename))
                    {
                        //Expect a confirmation dialog and then close the dialog
                        InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.Enter, true);
                        InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.Enter, false);
                        System.Diagnostics.Debug.WriteLine("Saving file");                        
                        InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.Tab, true);
                        InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.Tab, false);                        
                        System.Diagnostics.Debug.WriteLine("Switching to Yes dialog option");
                        Thread.Sleep(1000);
                    }
                    //Close the save dialog (and the confirmation dialog if file exists)
                    InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.Enter, true);
                    InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.Enter, false);
                    System.Diagnostics.Debug.WriteLine("Closing save dialog");
                    Thread.Sleep(1000);
                    //Close the Display Properties dialog
                    InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.LeftShift, true);
                    InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.Tab, true);
                    InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.Tab, false);
                    InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.LeftShift, false);
                    Thread.Sleep(200);
                    InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.Enter, true);
                    InputHelper.SendKeyboardInput(Microsoft.Test.Input.InputHelper.Key.Enter, false);

                    System.Diagnostics.Debug.WriteLine("Closing Display Properties dialog");
                    Thread.Sleep(1000);                   
                    
                }
            }), null);
        }

        /// <summary>
        /// Gets a list of available themes installed on the system
        /// </summary>
        /// <returns>a list containing the names of the avalible themes</returns>
        public static string[] GetAvailableThemes()
        {
            string[] files = Directory.GetFiles(themeDirectory, "*.theme");
            string[] themeNames = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
                themeNames[i] = Path.GetFileNameWithoutExtension(files[i]).ToLowerInvariant();
            return themeNames;
        }

        /// <summary>
        /// Gets the name of the current theme
        /// </summary>
        /// <returns>the name of the current theme</returns>
        public static string GetTheme()
        {
            StringBuilder themeFileName = new StringBuilder(512);
            GetCurrentThemeName(themeFileName, 512, 0, 0, 0, 0);
            string themeName = Path.GetFileNameWithoutExtension(themeFileName.ToString());
            
            // Classic "theme" doesn't have a .msstyles file, so we get "".  Return "Windows Classic" in this case.
            if (themeName == "")
            {
                themeName = WindowsClassicString;
            }
            return themeName;
        }

        [DllImport("uxtheme.dll", CharSet = CharSet.Auto, BestFitMapping = false)]
        static extern int GetCurrentThemeName(StringBuilder pszThemeFileName, int dwMaxNameChars, int pszColorBuff, int dwMaxColorChars, int pszSizeBuff, int cchMaxSizeChars);

        /// <summary>
        /// Returns the filename for the theme, or null if no theme file is being used
        /// </summary>
        /// <returns></returns>
        public static string GetThemeFilename()
        {
            using (RegistryKey currentTheme = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Plus!\Themes\Current"))
            {
                //HACK: Workaround issue on Vista Server where no themes are installed we report we are in the Classic theme
                if (currentTheme == null)
                    return null;
                return currentTheme.GetValue(null) as string;
            }
        }

        /// <summary>
        /// Gets the name of the current style
        /// </summary>
        /// <returns>the name of the current style</returns>
        public static string GetThemeStyle()
        {
            string themeStyle = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ThemeManager", "ColorName", "") as string;
            if (!String.IsNullOrEmpty(themeStyle))
                return themeStyle.ToLowerInvariant();
            
            return null;
        }

        /// <summary>
        /// Get the current defined appearance
        /// </summary>
        /// <returns></returns>
        public static DesktopAppearance GetAppearance()
        {
            string theme = GetTheme();

            if (theme == AeroString)
            {
                if (IsDwmEnabled)
                    return DesktopAppearance.AeroWithComposition;
                else
                    return DesktopAppearance.AeroWithoutComposition;
            }
            else if (theme == LunaString)
            {
                string colorScheme = GetThemeStyle();

                if (colorScheme == LunaMetallicString)
                    return DesktopAppearance.LunaMetallic;
                else if (colorScheme == LunaHomesteadString)
                    return DesktopAppearance.LunaHomestead;
                else
                    return DesktopAppearance.LunaNormalColor;
            }
            else if (theme == WindowsClassicString)
                return DesktopAppearance.WindowsClassic;
            else
                return DesktopAppearance.Unrecognized;
        }

        /// <summary>
        /// Gets the collection of supported appearances on this machine
        /// </summary>
        /// <returns></returns>
        public static DesktopAppearance[] GetAvailableAppearances()
        {
            List<DesktopAppearance> appearanceList = new List<DesktopAppearance>();
            string[] availableThemes = GetAvailableThemes();

            foreach (string theme in availableThemes)
            {
                switch (theme)
                {
                    case AeroString:
                        appearanceList.Add(DesktopAppearance.AeroWithoutComposition);
                        if (IsDwmCompositionSupported)
                            appearanceList.Add(DesktopAppearance.AeroWithComposition);
                        break;
                    case LunaString:
                        appearanceList.Add(DesktopAppearance.LunaNormalColor);
                        appearanceList.Add(DesktopAppearance.LunaMetallic);
                        appearanceList.Add(DesktopAppearance.LunaHomestead);
                        break;
                    case RoyaleString:
                        appearanceList.Add(DesktopAppearance.Royale);
                        break;
                    case WindowsClassicString:
                        appearanceList.Add(DesktopAppearance.WindowsClassic);
                        break;
                }
            }

            return appearanceList.ToArray();
        }

        /// <summary>
        /// Determines if Hardware acceleration is currently enabled
        /// </summary>
        public static bool IsHardwareAccelerated
        {
            get
            {
                // This API will is to be implemented
                // return true for now (test run in Hardware mode).
                return true;
            }
        }

        /// <summary>
        /// Determines if Theming is enabled on the machine
        /// </summary>
        public static bool IsThemeEnabled
        {
            get
            {
                string themeActive = (string) Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ThemeManager", "ThemeActive", string.Empty);
                return String.Equals(themeActive, "1");
            }
        }

        /// <summary>
        /// Returns true if DWM is Enabled. False if not
        /// </summary>
        public static bool IsDwmEnabled
        {
            get
            {
                bool isDwmEnabled = false;
                try
                {
                    //the DwmIsCompositionEnabled implementation always return S_OK
                    DwmApi._DwmIsCompositionEnabled(out isDwmEnabled);
                }
                catch (DllNotFoundException)
                {
                }
                return isDwmEnabled;
            }
        }

        /// <summary>
        /// Enables DWM
        /// </summary>
        public static void EnableDwm()
        {
            SwitchDwm(true);
        }

        /// <summary>
        /// Disable DWM
        /// </summary>
        public static void DisableDwm()
        {
            SwitchDwm(false);
        }        

  
        /// <summary>
        /// This API checks if the driver is capable of running DWM
        /// </summary>
        /// <returns>True if the Api is called on a machine with LDDM driver</returns>
        public static bool IsDwmCapable
        {
            get
            {
                bool ret = false;
                User32.DISPLAY_DEVICE dd = new User32.DISPLAY_DEVICE();
                dd.cb = (int)Marshal.SizeOf(dd);
                for (uint i = 0; User32.EnumDisplayDevices(null, i, ref dd, 0); i++)
                {
                    if ((dd.StateFlags & User32.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) == User32.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP)
                    {

                        //Verify that DisplayDevice is not a mirroring driver
                        ret = ((dd.StateFlags & User32.DISPLAY_DEVICE_MIRRORING_DRIVER) == 0);

                        if (!ret)
                        {
                            break;
                        }

                        //Verify that the DisplayDevice is using LDDM
                        User32.DEVMODE dm = new User32.DEVMODE();
                        dm.dmSize = (short)Marshal.SizeOf(dm);
                        User32.EnumDisplaySettingsEx(dd.DeviceName, User32.ENUM_CURRENT_SETTINGS, ref dm, 0);
                        ret = dm.dmDeviceName.ToLower().Equals("cdd", StringComparison.InvariantCulture);
                        if (!ret)   
                        {
                            break;
                        }
                    }
                    dd.cb = (int)Marshal.SizeOf(dd);
                }
                return ret;
            }
        }

        /// <summary>
        /// Returns whether DWM composition is supported by this machine
        /// </summary>
        public static bool IsDwmCompositionSupported
        {
            get
            {
                bool isDwmCompositionSupported = false;
                try
                {
                    //the DwmIsCompositionSupported implementation always return S_OK
                    DwmApi._DwmIsCompositionSupported(false, out isDwmCompositionSupported);
                }
                catch (DllNotFoundException)
                {
                }
                return isDwmCompositionSupported;
            }
        }

        #endregion    

        #region Private Members

        private static void MonitorProcess(string filename, VisibleWindowHandler visibleWindowHandler, ProcessExitedHandler processExitHandler)
        {
            //Get the active window since the window activation will be lost by lauching the display properties
            IntPtr activehWnd = IntPtr.Zero;
            ManualResetEvent waitEvent = new ManualResetEvent(false);

            ProcessMonitor processMonitor = new ProcessMonitor();
            Process themeChangeProcess = new Process();
            themeChangeProcess.StartInfo.FileName = filename;

            try
            {
                //Initialize the wait event                
                waitEvent.Reset();
                activehWnd = User32.GetForegroundWindow();

                //This field is set when the correct process is started and checked when any process exits
                int processId = 0;
                processMonitor.VisibleWindowFound += new VisibleWindowHandler(delegate(IntPtr topLevelhWnd, IntPtr hWnd, Process process, string title)
                {
                    if (process.ProcessName.ToLowerInvariant().Equals(ThemeProcessName))
                    {
                        processId = process.Id;
                        User32.SetForegroundWindow(topLevelhWnd);  //In case the dialog was opened previously
                        Thread.Sleep(2000);

                        if (visibleWindowHandler != null)
                            visibleWindowHandler(topLevelhWnd, hWnd, process, title);
                    }
                });

                processMonitor.ProcessExited += new ProcessExitedHandler(delegate(Process process)
                {
                    if (process.Id == processId)
                    {
                        if (processExitHandler != null)
                            processExitHandler(process);

                        Thread.Sleep(1000);     //For good measure                     
                        waitEvent.Set();
                    }
                });

                themeChangeProcess.Start();

                //Start monitoring processes
                processMonitor.AddProcess(ThemeProcessName);
                processMonitor.Start();

                waitEvent.WaitOne(60000, false);
            }
            finally
            {
                if (processMonitor != null)
                    processMonitor.Stop();

                //Restore the active window
                if (activehWnd != IntPtr.Zero)
                    User32.SetForegroundWindow(activehWnd);
            }
        }

        private static void SwitchDwm(bool enablecomposition)
        {
            try
            {
                int res = DwmApi._DwmEnableComposition(enablecomposition);
                if (res != 0) throw Marshal.GetExceptionForHR(res);
            }
            catch (DllNotFoundException e)
            {
                throw new InvalidOperationException("This method can only be used in a Vista machine", e);
            }
        }

        #endregion
    }

    /// <summary>
    /// A defined combination of theme, style, and color scheme
    /// </summary>
    public enum DesktopAppearance
    {
        /// <summary>
        /// Unrecognized combination of Theme and Color Scheme
        /// </summary>
        Unrecognized,

        /// <summary>
        /// Theme = "Windows Vista"
        /// Color Scheme = "Windows Vista Aero" (glass) or "Windows Vista Standard" (no glass) depending on SKU
        /// DWM Composition will be enabled. Glass may or may not be enabled depending on the SKU.
        /// </summary>
        AeroWithComposition,

        /// <summary>
        /// Theme = "Windows Vista"
        /// Color Scheme = "Windows Vista Basic"
        /// DWM Composition and Transparency will both be disabled
        /// </summary>
        AeroWithoutComposition,

        /// <summary>
        /// Theme = "Windows XP"
        /// Color Scheme = "Blue"
        /// </summary>
        LunaNormalColor,

        /// <summary>
        /// Theme = "Windows XP"
        /// Color Scheme = "Silver"
        /// </summary>
        LunaMetallic,

        /// <summary>
        /// Theme = "Windows XP"
        /// Color Scheme = "Olive"
        /// </summary>
        LunaHomestead,

        /// <summary>
        /// Theme = "Royale"
        /// </summary>
        Royale,

        /// <summary>
        /// Theme = "Windows Classic"
        /// </summary>
        WindowsClassic
    }
}
