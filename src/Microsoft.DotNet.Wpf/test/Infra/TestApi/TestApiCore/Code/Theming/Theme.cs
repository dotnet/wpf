// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using Microsoft.Test.Input;
using Microsoft.Win32;
using System.Collections.Generic;

namespace Microsoft.Test.Theming 
{
    /// <summary>
    /// Enables changing of the Theme configuration of the system.
    /// </summary>
    ///
    /// <example>
    /// The following example demonstrates changing the OS theme to each available
    /// system theme to verify a control's appearance. 
    ///
    /// <code>
    /// Theme originalTheme = Theme.GetCurrent();
    /// 
    /// try
    /// {
    ///     Theme[] availableThemes = Theme.GetAvailableSystemThemes();
    ///     foreach (Theme theme in availableThemes)
    ///     {
    ///         Theme.SetCurrent(theme);
    ///         VerifyMyControlAppearance(theme);
    ///     }
    /// }
    /// finally
    /// {
    ///     Theme.SetCurrent(originalTheme);
    /// }
    /// </code>
    /// </example>
    public class Theme
    {
        #region Private Data

        private readonly static string ThemeProcessName;
        private readonly static string ThemeDir = Environment.ExpandEnvironmentVariables(@"%WINDIR%\Resources\Themes");
        private readonly static string AccessibleThemesDir = Environment.ExpandEnvironmentVariables(@"%WINDIR%\Resources\Ease of Access Themes");

        #endregion Private Data

        #region Enums

        /// <summary>
        /// Enum for High Contrast theme names. These 
        /// do not work on Vista - ok to use on Win7+.
        /// </summary>
        public enum HighContrastTheme
        {
            /// <summary>
            /// High Contrast #1
            /// Background: Black
            /// Foreground: Yellow
            /// </summary>
            hc1, 
            /// <summary>
            /// High Contrast #2
            /// Background: Black
            /// Foreground: Green
            /// </summary>
            hc2, 
            /// <summary>
            /// High Contrast Black
            /// Background: Black
            /// Foreground: White
            /// </summary>
            hcblack, 
            /// <summary>
            /// High Contrast White
            /// Background: White
            /// Foreground: Black
            /// </summary>
            hcwhite
        }

        #endregion 

        #region Constructors

        static Theme()
        {
            // 6.1 (Vista) and below have a different process for Theme automation
            if (Environment.OSVersion.Version < new Version("6.1"))
            {
                ThemeProcessName = "rundll32";
            }
            else
            {
                ThemeProcessName = "explorer";
            }
        }

        /// <summary>
        /// No default constructor for Theme class
        /// </summary>
        private Theme()
        {
        }

        /// <summary>
        /// Constructor that takes a file and retrieves the rest
        /// of the info directly from the theme file.
        /// </summary>        
        private Theme(FileInfo path)
        {
            Path = path;
            IsEnabled = true;
            SetThemeProperties(this);
        }

        private Theme(FileInfo path, string name, string style, bool isEnabled)
        {
            Path = path;
            Name = name;
            Style = style;
            IsEnabled = isEnabled;
        }

        #endregion Constructors

        #region Public Static Members

        /// <summary>
        /// Returns the current OS theme.
        /// </summary>
        /// <returns>Returns the current OS theme.</returns>
        public static Theme GetCurrent()
        {
            string themeFilename = GetCurrentThemePath();
            string style = GetCurrentThemeStyle();
            bool isEnabled = GetCurrentThemeIsEnabled();
            string themeName = string.Empty;
            if (!string.IsNullOrEmpty(themeFilename))
            {
                themeName = System.IO.Path.GetFileNameWithoutExtension(themeFilename);
            }

            return new Theme(new FileInfo(themeFilename), themeName, style, isEnabled);
        }

        /// <summary>
        /// Sets the current OS theme with the given theme parameter.
        /// </summary>
        /// <param name="theme">The theme to set the OS theme to.</param>
        public static void SetCurrent(Theme theme)
        {
            lock (typeof(Theme))
            {
                EnsureTheme(theme);

                // only change the theme if we are in a different theme
                if (GetCurrent().Path.FullName != theme.Path.FullName)
                {
                    // Copy the file to another location before setting the theme.
                    // The reason for this is that if the custom theme is left after the test
                    // executes and a test harness possibly deletes the current theme, the system 
                    // issues an error that prevents the system from restoring the default theme
                    string destFilename = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.InternetCache),
                        System.IO.Path.GetFileName(theme.Path.FullName));
                    File.Copy(theme.Path.FullName, destFilename, true);

                    // programmatically setting theme behavior is different for 6.1 and up
                    if (Environment.OSVersion.Version < new Version("6.1"))
                    {
                        MonitorProcess(ThemeProcessName, theme.Path.FullName);
                    }
                    else
                    {
                        SetThemeThroughExplorer(theme.Path.FullName);
                    }
                }

                WaitForThemeSet(theme.Path.FullName);
            }
        }

        /// <summary>
        /// Sets the current OS theme with the given fileInfo parameter.
        /// </summary>
        /// <param name="fileInfo">The fileInfo to set the theme to.</param>
        public static void SetCurrent(FileInfo fileInfo)
        {
            SetCurrent(new Theme(fileInfo));
        }

        /// <summary>
        /// Sets the current OS theme using the given HighContrastTheme parameter
        /// </summary>
        /// <param name="theme"></param>
        public static void SetCurrent(HighContrastTheme theme)
        {
            var themes = new List<Theme>(Theme.GetAccessibleThemes());

            var hcTheme = themes.Find((Theme t) =>
            {
                return (string.Equals(t.Name, theme.ToString(), StringComparison.InvariantCultureIgnoreCase));

            });

            if (hcTheme == null)
            {
                throw new NotSupportedException($"Theme {theme.ToString()} is not supported");
            }

            Theme.SetCurrent(hcTheme);
        }

        /// <summary>
        /// Sets the current OS theme with the given theme parameter
        /// </summary>
        /// <param name="theme"></param>
        /// <returns></returns>
        public static ThemeSwitcher SetTheme(Theme theme)
        {
            return new ThemeSwitcher(theme);
        }

        /// <summary>
        /// Sets the current OS theme using the given HighContrastTheme parameter.
        /// </summary>
        /// <param name="theme"></param>
        /// <returns></returns>
        public static ThemeSwitcher SetTheme(HighContrastTheme theme)
        {
            return new ThemeSwitcher(theme);
        }

        /// <summary>
        /// Returns all the available system themes on the OS.
        /// </summary>
        /// <returns>Returns all the available system themes</returns>
        public static Theme[] GetAvailableSystemThemes()
        {
            return GetThemesFromDirectory(ThemeDir);
        }

        /// <summary>
        /// Returns all the available accessible themes on the OS
        /// </summary>
        /// <returns></returns>
        public static Theme[] GetAccessibleThemes()
        {
            try
            {
                return GetThemesFromDirectory(AccessibleThemesDir);
            }
            catch (Exception e)
            {
                throw new PlatformNotSupportedException("This method is not supported on Vista", e);
            }
        }


        #endregion Public Static Members

        #region Public Properties

        /// <summary>
        /// Gets the theme name.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the path of the theme.  
        /// </summary>
        /// <remarks>
        /// Should be of type *.theme.
        /// </remarks>
        public FileInfo Path
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the color style of the theme.
        /// </summary>
        public string Style
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the IsEnabled state of the theme.
        /// </summary>
        public bool IsEnabled
        {
            get;
            private set;
        }

        #endregion Public Properties

        #region Private Members

        private static string GetCurrentThemePath()
        {
            // 6.1 (Vista) and below have a different process for Theme automation
            if (Environment.OSVersion.Version < new Version("6.1"))
            {
                using (RegistryKey currentTheme = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Plus!\Themes\Current"))
                {
                    if (currentTheme == null)
                    {
                        return null;
                    }

                    return currentTheme.GetValue(null) as string;
                }
            }
            else
            {
                string themeFilename = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes", "CurrentTheme", "") as string;
                if (!String.IsNullOrEmpty(themeFilename))
                {
                    return themeFilename.ToLowerInvariant();
                }

                return null;
            }
        }

        private static string GetCurrentThemeStyle()
        {
            string themeStyle = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ThemeManager", "ColorName", "") as string;
            if (!string.IsNullOrEmpty(themeStyle))
            {
                return themeStyle.ToLowerInvariant();
            }

            return null;
        }

        private static bool GetCurrentThemeIsEnabled()
        {
            string themeActive = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ThemeManager", "ThemeActive", string.Empty);
            return string.Compare(themeActive, "1") == 0;
        }
        
        private static void SetThemeProperties(Theme theme)
        {
            EnsureTheme(theme);

            // find the [visualstyles]colorstyle property in the the theme file
            string themeStyle = string.Empty;
            using (var fs = new FileStream(theme.Path.FullName, FileMode.Open, FileAccess.Read))
            {
                using (var sr = new StreamReader(fs))
                {
                    var line = sr.ReadLine();
                    while (!sr.EndOfStream && line != null)
                    {
                        if (line.ToLowerInvariant() == "[visualstyles]")
                        {
                            line = sr.ReadLine();
                            while (!sr.EndOfStream && !string.IsNullOrEmpty(line))
                            {
                                if (line.ToLowerInvariant().Contains("colorstyle"))
                                {
                                    var keyValuePair = line.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                                    if (keyValuePair != null && keyValuePair.Length == 2)
                                    {
                                        themeStyle = keyValuePair[1];
                                        break;
                                    }
                                }

                                line = sr.ReadLine();
                            }

                            break;
                        }

                        line = sr.ReadLine();
                    }
                }
            }

            if (!string.IsNullOrEmpty(themeStyle))
            {
                theme.Style = themeStyle;
            }

            theme.Name = System.IO.Path.GetFileNameWithoutExtension(theme.Path.FullName).ToLowerInvariant();
        }

        private static void EnsureTheme(Theme theme)
        {
            if (theme == null)
            {
                throw new ArgumentException("theme cannot be null.");
            }

            if (theme.Path != null && string.IsNullOrEmpty(theme.Path.FullName))
            {
                throw new ArgumentException("theme path cannot be null or empty.");
            }

            if (!File.Exists(theme.Path.FullName))
            {
                throw new ArgumentException("The theme '" + theme.Path.FullName + "' does not exist.", theme.Path.FullName);
            }

            if (System.IO.Path.GetExtension(theme.Path.FullName).ToLowerInvariant() != ".theme")
            {
                throw new ArgumentException("The theme file must have a .theme extention.", "filename");
            }
        }

        private static void WaitForThemeSet(string themeToBeSet)
        {
            int counter = 0;
            int max = 20;

            do
            {
                Thread.Sleep(1500);
                counter++;
            } while (counter < max && Theme.GetCurrent().Path.FullName.ToLower() != themeToBeSet.ToLower());
        }

        /// <remarks>
        /// For the case of OS versons below 6.1 the process for setting
        /// the theme is as follows:
        /// 
        /// 1. launch the .theme file
        /// 2. wait for the new rundll32 to start.
        /// 3. press enter to select the theme and close the dialog
        /// </remarks>
        private static void MonitorProcess(string processName, string themeFilename)
        {
            // Kill all processes with name = processName. 
            // This will help ensure that FindProcessName will always return the 
            // one unique process that we should wait on. 
            KillProcesses(processName);

            // Get the active window since the window activation will be lost             
            IntPtr activehWnd = IntPtr.Zero;
            ManualResetEvent waitEvent = new ManualResetEvent(false);
            System.Timers.Timer timer = null;
            bool enterFlag = true;

            try
            {
                // initialize the wait event                
                waitEvent.Reset();
                activehWnd = NativeMethods.GetForegroundWindow();

                // set the actual theme which launches the rundll32.exe window
                Process themeChangeProcess = new Process();
                themeChangeProcess.StartInfo.FileName = themeFilename;
                themeChangeProcess.Start();

                bool themeSet = false;

                // wait for the window to activate                
                timer = new System.Timers.Timer(1500);
                timer.Elapsed += (s, e) =>
                {
                    if (enterFlag)
                    {
                        enterFlag = false;

                        // find the process to monitor
                        Process process = FindProcessFromName(processName);
                        if (process != null)
                        {
                            process.Refresh();

                            HwndInfo[] topLevelWindows = WindowEnumerator.GetTopLevelVisibleWindows(process);
                            if (topLevelWindows.Length > 0)
                            {
                                // In case the dialog was opened previously
                                NativeMethods.SetForegroundWindow(topLevelWindows[0].hWnd);
                                Thread.Sleep(1000);

                                // The process monitor calls back more than once, only do this action once.
                                if (!themeSet)
                                {
                                    themeSet = true;

                                    // press enter to confirm and set the theme
                                    Keyboard.Type(Key.Return);
                                }
                            }
                        }
                        else
                        {
                            // For good measure                     
                            Thread.Sleep(1000);
                            waitEvent.Set();
                        }

                        enterFlag = true;
                    }
                };
                timer.Start();

                waitEvent.WaitOne(60000, false);
            }
            finally
            {
                enterFlag = false;

                if (timer != null)
                {
                    timer.Stop();
                    timer.Dispose();
                    timer = null;
                }

                // Restore the active window
                if (activehWnd != IntPtr.Zero)
                {
                    NativeMethods.SetForegroundWindow(activehWnd);
                }
            }
        }

        private static List<Process> FindProcessesFromName(string processName)
        {
            Process[] currentProcesses = new Process[0];
            List<Process> result = new List<Process>();

            try
            {  // Workaround whidbey bug that randomly throws Win32Exception
                currentProcesses = Process.GetProcesses();
            }
            catch (Win32Exception)
            {
                //Unable to enumerate Process Moduals (this is probobly a whidbey bug)
            }

            foreach (Process process in currentProcesses)
            {
                // Get the name and ensure that the process is still running
                string procName;
                try
                {
                    procName = process.ProcessName;
                    // Query to process to ensure that we have access to it and it has not exited
                    if (process.HasExited)
                    {
                        continue;
                    }
                }
                catch (InvalidOperationException)
                {
                    // the process has exited
                    continue;
                }
                catch (Win32Exception)
                {
                    // strange whidbey bug.. the process has most likely already exited
                    // Running as restricted user or under LUA on Longhorn and the process is not a user process
                    continue;
                }

                if (string.Equals(procName, processName, StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Add(process);
                }
            }

            return result;
        }

        private static Process FindProcessFromName(string processName)
        {
            List<Process> processes = FindProcessesFromName(processName);

            if ((processes.Count != 1) || (processes[0].HasExited))
            {
                throw new Exception(string.Format("Unique process with name: {0} not found", processName));
            }

            return processes[0];
        }

        private static void KillProcesses(string processName)
        {
            List<Process> processes = FindProcessesFromName(processName);

            foreach (Process process in processes)
            {
                if (process.HasExited)
                {
                    continue;
                }

                process.Kill();
            }
        }



        /// <remarks>
        /// For the case of OS versons 6.1 and above the process for setting
        /// the theme is as follows:
        /// 
        /// 1. launch the .theme file (theme is automatically selected)
        /// 2. wait for explorer to launch the 'personalization' child window 
        /// 3. close the 'personalization' window
        /// 
        /// Unlike MonitorProcess, explorer.exe is already running and a subwindow is launched for
        /// this process.  
        /// </remarks>
        private static void SetThemeThroughExplorer(string themeFilename)
        {
            // Get the active window since the window activation will be lost 
            // by lauching the ControlPanel.Personalization window.
            IntPtr activehWnd = IntPtr.Zero;
            ManualResetEvent waitEvent = new ManualResetEvent(false);
            System.Timers.Timer timer = null;
            bool enterFlag = true;

            try
            {
                // initialize the wait event                
                waitEvent.Reset();
                activehWnd = NativeMethods.GetForegroundWindow();

                // set the actual theme which launches the personalization window
                Process themeChangeProcess = new Process();
                themeChangeProcess.StartInfo.FileName = themeFilename;
                themeChangeProcess.Start();

                // wait for the window to activate                
                timer = new System.Timers.Timer(1500);
                timer.Elapsed += (s, e) =>
                {
                    if (enterFlag)
                    {
                        enterFlag = false;

                        foreach (var process in Process.GetProcesses())
                        {
                            if (process.ProcessName.ToLower() == ThemeProcessName)
                            {
                                IntPtr hWnd = WindowEnumerator.FindFirstWindowWithCaption(process, "personalization");
                                if (hWnd != IntPtr.Zero)
                                {
                                    // first make sure it's active 
                                    int maxCounter = 20;
                                    int counter = 0;
                                    IntPtr foregroundHWnd = NativeMethods.GetForegroundWindow();
                                    Console.WriteLine("Thread: " + Thread.CurrentThread.ManagedThreadId + ", foregourndHWnd: " + foregroundHWnd + ", hWnd: " + hWnd);
                                    while ((foregroundHWnd != hWnd || !NativeMethods.IsWindowVisible(hWnd)) &&
                                           counter < maxCounter &&
                                           timer != null)
                                    {
                                        Console.WriteLine("Thread: " + Thread.CurrentThread.ManagedThreadId + ", restore the window. hWnd: " + hWnd + ", foregroundHWnd: " + foregroundHWnd);

                                        NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
                                        Thread.Sleep(500);

                                        NativeMethods.BringWindowToTop(hWnd);
                                        Thread.Sleep(500);

                                        foregroundHWnd = NativeMethods.GetForegroundWindow();
                                        counter++;
                                    }

                                    if (foregroundHWnd == hWnd)
                                    {
                                        timer.Stop();

                                        CloseAndWaitForWindow();
                                        waitEvent.Set();
                                    }

                                    break;
                                }
                            }
                        }

                        enterFlag = true;
                    }
                };
                timer.Start();

                waitEvent.WaitOne(60000, false);
            }
            finally
            {
                enterFlag = false;

                if (timer != null)
                {
                    timer.Stop();
                    timer.Dispose();
                    timer = null;
                }

                // Restore the active window
                if (activehWnd != IntPtr.Zero)
                {
                    NativeMethods.SetForegroundWindow(activehWnd);
                }
            }
        }

        private static void CloseAndWaitForWindow()
        {
            // close the window
            Keyboard.Press(Key.Alt);
            Keyboard.Press(Key.F4);
            Keyboard.Release(Key.F4);
            Keyboard.Release(Key.Alt);

            int counter = 0;
            int max = 20;
            bool found = true;
            while (counter < max && found)
            {
                Thread.Sleep(1000);
                counter++;

                found = false;
                foreach (var process in Process.GetProcesses())
                {
                    if (process.ProcessName.ToLower() == ThemeProcessName)
                    {
                        IntPtr hWnd = WindowEnumerator.FindFirstWindowWithCaption(process, "personalization");
                        if (hWnd != IntPtr.Zero)
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }
        }

        private static Theme[] GetThemesFromDirectory(string directory)
        {
            string[] files = Directory.GetFiles(directory, "*.theme");
            Theme[] themes = new Theme[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                themes[i] = new Theme(new FileInfo(files[i]));
            }

            return themes;
        }

        #endregion

        #region Public Types

        /// <summary>
        /// Helper class capable of reverting to original theme 
        /// using the IDispose pattern
        /// </summary>
        public class ThemeSwitcher: IDisposable
        {

            #region IDisposable Support

            private bool _disposed = false; // To detect redundant calls

            /// <summary>
            /// 
            /// </summary>
            /// <param name="disposing"></param>
            protected virtual void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    Theme.SetCurrent(_originalTheme);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            ~ThemeSwitcher()
            {
                Dispose(false);
            }

            /// <summary>
            /// 
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            #endregion

            /// <summary>
            /// 
            /// </summary>
            /// <param name="theme"></param>
            public ThemeSwitcher(Theme theme):this()
            {
                Theme.SetCurrent(theme);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="theme"></param>
            public ThemeSwitcher(HighContrastTheme theme): this()
            {
                Theme.SetCurrent(theme);
            }

            private ThemeSwitcher()
            {
                _originalTheme = Theme.GetCurrent();
            }

            Theme _originalTheme;
        }

        #endregion
    }
}
