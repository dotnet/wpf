// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;
using System.Threading;
using System.Security.Principal;
using Microsoft.Test.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;

namespace Microsoft.Test.StateManagement
{
    /// <summary>
    /// Reports and allows you to modify the state of alternate browsers installed on the machine
    /// The settings include the supported browsers, whether they are installed or not, and whether they are set as the default browser.
    /// </summary>
    public class BrowserState : State <BrowserStateValue, object>
    {
        #region Private Data

        private static Dictionary<RegistryState, RegistryStateValue> changedRegistryValues = new Dictionary<RegistryState, RegistryStateValue>();

        private string currentUserSid;
        private Hashtable currentUserEnvironment;

        #endregion

        #region Constructor(s)
        
        /// <summary>
        /// Constructor for BrowserState object, used to control state of 3rd party browsers on machine
        /// </summary>
        /// <param name="userSid">Current User ID, used for registry operations</param>
        /// <param name="userEnvironment">Hashtable representing current user environment variables for starting processes as the user</param>
        public BrowserState(string userSid, Hashtable userEnvironment) : base()
        {
            currentUserSid = userSid;
            currentUserEnvironment = userEnvironment;            
        }

        #endregion

        #region Override Members

        /// <summary>
        /// Gets the current state of 3rd party browser installation on the machine
        /// </summary>
        /// <returns>BrowserStateValue with info about installed 3rd party browsers.</returns>
        public override BrowserStateValue GetValue()
        {
           return DetermineCurrentBrowserState();
        }
        
        /// <summary>
        /// Sets the current state of 3rd party browser installation on the machine.  Backs up prior state as appropriate.
        /// </summary>
        /// <param name="value">BrowserStateValue representing state to transition to.</param>
        /// <param name="action">Not used</param>
        /// <returns>True for success, false for error</returns>
        public override bool SetValue(BrowserStateValue value, object action)
        {
            BrowserStateValue currMachineState = DetermineCurrentBrowserState();
            bool shouldRestoreBackups = true;
            
            // Only take action if there is an actionable difference... 
            if ((value.DefaultBrowser != currMachineState.DefaultBrowser) ||
                (value.Installed != currMachineState.Installed) ||
                (value.Browser != currMachineState.Browser))
            {
                RestoreModifiedRegistryState();
                
                // Transitioning back to user-installed browser by copying over any backed up directories
                // Short circuits the rest since we already have all the app's info backed up.
                if ((currMachineState.Browser != BrowserIdentifier.FireFoxUnknown) &&
                                    (value.Browser == BrowserIdentifier.FireFoxUnknown))
                {
                    KillFolder(FirefoxHelper.GetMozillaAppDataFolder(BrowserIdentifier.FireFoxUnknown, this.currentUserEnvironment));
                    KillFolder(FirefoxHelper.GetMozillaProgFilesFolder(BrowserIdentifier.FireFoxUnknown));

                    RestoreAllBackedUpFolders(value.BackedUpFolders);
                    return true;
                }

                // Time to back up any folders we have earmarked as needing to be backed up 

                if (currMachineState.FoldersToPreserve.Count > 0)
                {
                    // Kill any processes that might interfere with backing up folders
                    foreach (Process proc in Process.GetProcessesByName("firefox"))
                    {
                        proc.Kill();
                    }

                    foreach (string s in currMachineState.FoldersToPreserve)
                    {
                        BackupFolderToTemp(s, value.BackedUpFolders);
                    }
                    currMachineState.FoldersToPreserve = new List<string>();

                    shouldRestoreBackups = false;
                }

                // Case 1: State change is to get rid of FireFox
                if ((value.Installed == false) && currMachineState.Installed == true)
                {
                    FirefoxHelper.UninstallFireFox(currMachineState.Browser, currentUserEnvironment);
                    CleanupAfterFireFox(currMachineState.Browser, this.currentUserEnvironment);
                    // Reset the state value to "not installed"
                    currMachineState = new BrowserStateValue(BrowserIdentifier.None, false, false);
                    SetIEAsDefaultBrowserInRegistry();
                }
                // Case 2: State change is to install a version of firefox
                else if ((value.Installed == true) && (currMachineState.Installed == false) && (currMachineState.Browser != BrowserIdentifier.FireFoxUnknown))
                {
                    FirefoxHelper.InstallFireFox(value.Browser, currentUserEnvironment);
                    currMachineState.Browser = value.Browser;
                    currMachineState.Installed = true;
                    currMachineState.DefaultBrowser = value.DefaultBrowser;

                    if (value.DefaultBrowser)
                    {
                        SetFireFoxAsDefaultBrowserInRegistry(value.Browser);
                    }
                    else
                    {
                        SetIEAsDefaultBrowserInRegistry();
                    }
                }
                // Case 3: State change is to change version of firefox
                else if (((value.Installed == true) && currMachineState.Installed == true) &&
                      (value.Browser != currMachineState.Browser))
                {
                    if (currMachineState.Browser != BrowserIdentifier.FireFoxUnknown)
                    {
                        FirefoxHelper.UninstallFireFox(currMachineState.Browser, currentUserEnvironment);
                    }
                    CleanupAfterFireFox(currMachineState.Browser, this.currentUserEnvironment);
                    FirefoxHelper.InstallFireFox(value.Browser, currentUserEnvironment);

                    currMachineState.Browser = value.Browser;
                    currMachineState.DefaultBrowser = value.DefaultBrowser;
                    currMachineState.Installed = true;

                    if (value.DefaultBrowser)
                    {
                        SetFireFoxAsDefaultBrowserInRegistry(value.Browser);
                    }
                    else
                    {
                        SetIEAsDefaultBrowserInRegistry();
                    }
                }
                // This case guarantees no change in installed state or version, but since we're doing something
                // we know it's a change in the default browser
                else
                {
                    currMachineState.DefaultBrowser = value.DefaultBrowser;
                    if (value.DefaultBrowser)
                    {
                        SetFireFoxAsDefaultBrowserInRegistry(value.Browser);
                    }
                    else
                    {
                        SetIEAsDefaultBrowserInRegistry();
                    }
                }
            }

            if (shouldRestoreBackups)
            {
                RestoreAllBackedUpFolders(value.BackedUpFolders);
            }

            return true;
        }

        /// <summary>
        /// Equivalence method for BrowserState objects
        /// </summary>
        /// <param name="obj">object to compare for equivalence</param>
        /// <returns>True if objects are equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            BrowserState other = obj as BrowserState;
            if (object.Equals(other, null))
            {
                return false;
            }

            bool equal = (this.currentUserSid == other.currentUserSid) && (this.currentUserEnvironment == other.currentUserEnvironment);
            return equal;
        }

        /// <summary>
        /// Calculates a hash code for this instance based on instance variables
        /// </summary>
        /// <returns>int representing hash of this object.</returns>
        public override int GetHashCode()
        {
            return this.currentUserSid.GetHashCode() + this.currentUserEnvironment.GetHashCode();
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Determines if FireFox is the default browser (as much as WPF Browser hosting can tell)
        /// </summary>
        /// <param name="currentUserSid">User sid</param>
        /// <returns>True if FireFox is default browser, false if other.</returns>
        public static bool IsFireFoxDefaultBrowser(string currentUserSid)
        {
            // Check both the default .htm handler and the StartMenuInternet entry.  
            return ((((string)Registry.GetValue(@"HKEY_CLASSES_ROOT\.htm", null, null)).ToLowerInvariant() == "firefoxhtml") &&
                     (((string)Registry.GetValue(RemapRegValuePathForRunAsSystem(currentUserSid, @"HKEY_CURRENT_USER\Software\Clients\StartmenuInternet"), null, null)).ToLowerInvariant() == "firefox.exe"));
        }

        #endregion    

        #region Private Methods

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern uint GetShortPathName(
        [MarshalAs(UnmanagedType.LPTStr)]
        string lpszLongPath,
        [MarshalAs(UnmanagedType.LPTStr)]
        StringBuilder lpszShortPath,
        uint cchBuffer);

        /// <summary>
        /// The ToLongPathNameToShortPathName function retrieves the short path form of a specified long input path
        /// </summary>
        /// <param name="longName">The long name path</param>
        /// <returns>A short name path string</returns>
        private static string ToShortPathName(string longName)
        {
            StringBuilder shortNameBuffer = new StringBuilder(256);
            uint bufferSize = (uint)shortNameBuffer.Capacity;
            uint result = GetShortPathName(longName, shortNameBuffer, bufferSize);
            return shortNameBuffer.ToString();
        }


        private void RestoreModifiedRegistryState()
        {
            // First, restore any values that have been set by prior changes....
            if (BrowserState.changedRegistryValues.Count > 0)
            {
                foreach (RegistryState regState in BrowserState.changedRegistryValues.Keys)
                {
                    RegistryStateValue theValue = null;
                    if (BrowserState.changedRegistryValues.TryGetValue(regState, out theValue))
                    {
                        if ((theValue != null) && (theValue.Value != null)) // Key was changed, so restore it
                        {
                            regState.SetValue(theValue, null);
                        }
                        else // This key didnt exist before, so delete it.
                        {
                            regState.SetValue(new RegistryStateValue(null), null);
                        }
                    }
                    else
                    {
                        // Error state.  
                    }
                }
                BrowserState.changedRegistryValues = new Dictionary<RegistryState, RegistryStateValue>();
            }
        }

        private BrowserStateValue DetermineCurrentBrowserState()
        {
            string fireFoxInstalledVerString = FirefoxHelper.FireFoxVersion;
            BrowserIdentifier theBrowser = BrowserIdentifier.None;
            bool isInstalled = false;
            bool isDefault = false;
            if (fireFoxInstalledVerString != null)
            {
                isInstalled = true;
                
                switch (FirefoxHelper.FireFoxVersion)
                {
                    case FirefoxHelper.firefox15VersionString:
                        {
                            theBrowser = BrowserIdentifier.FireFox15;
                            break;
                        }
                    case FirefoxHelper.firefox20VersionString:
                        {
                            theBrowser = BrowserIdentifier.FireFox20;
                            break;
                        }
                    case FirefoxHelper.firefox30VersionString:
                        {
                            theBrowser = BrowserIdentifier.FireFox30;
                            break;
                        }
                    case null:
                        theBrowser = BrowserIdentifier.None;
                        break;
                    default:
                        // Unknown version of FireFox so it needs to be backed up
                        theBrowser = BrowserIdentifier.FireFoxUnknown;
                        break;
                }
                isDefault = IsFireFoxDefaultBrowser(currentUserSid);
            }
            // else {Do stuff that figures out any other browsers we add here}
            BrowserStateValue currMachineState = new BrowserStateValue(theBrowser, isInstalled, isDefault);

            // If the current state will need to be preserved, make a note of this in the value.
            if (currMachineState.Browser == BrowserIdentifier.FireFoxUnknown)
            {
                currMachineState.FoldersToPreserve.Add(FirefoxHelper.GetMozillaAppDataFolder(BrowserIdentifier.FireFoxUnknown, this.currentUserEnvironment));
                currMachineState.FoldersToPreserve.Add(FirefoxHelper.GetMozillaProgFilesFolder(BrowserIdentifier.FireFoxUnknown));
                currMachineState.Installed = true;
            }
            else
            // If there's already a version of FireFox installed, check to see if it has the same prefs we set.
            // If not, prepare to backup the whole directory.  
            {
                bool shouldBackupPreferences = false;
                int expectedPrefFileSize = 0;
                switch (currMachineState.Browser)
                {
                    case BrowserIdentifier.FireFox15:
                        shouldBackupPreferences = true;
                        expectedPrefFileSize = FirefoxHelper.firefox15FakePrefsSize;
                        break;
                    case BrowserIdentifier.FireFox20:
                        shouldBackupPreferences = true;
                        expectedPrefFileSize = FirefoxHelper.fireFox_20_Fake_Prefs_Size;
                        break;
                    case BrowserIdentifier.FireFox30:
                        shouldBackupPreferences = true;
                        expectedPrefFileSize = FirefoxHelper.fireFox_30_Fake_Prefs_Size;
                        break;
                }
                if (shouldBackupPreferences)
                {
                    // If the prefs file size is any different than the fake one we write, back it up and make a note that this state involves restoring it.
                    if ((File.Exists(FirefoxHelper.GetMozillaAppDataFolder(currMachineState.Browser, this.currentUserEnvironment) + "\\FireFox\\Profiles\\" + Environment.UserName + "\\prefs.js")) &&
                        (File.ReadAllBytes(FirefoxHelper.GetMozillaAppDataFolder(currMachineState.Browser, this.currentUserEnvironment) + "\\FireFox\\Profiles\\" + Environment.UserName + "\\prefs.js").Length
                        != expectedPrefFileSize))
                    {
                        currMachineState.FoldersToPreserve.Add(FirefoxHelper.GetMozillaAppDataFolder(currMachineState.Browser, this.currentUserEnvironment));
                    }
                }
            }
            return currMachineState;
        }
        
        private void SetFireFoxAsDefaultBrowserInRegistry(BrowserIdentifier version)
        {
            string home = (string)currentUserEnvironment["HOMEDRIVE"];
            string progFilesX86 = (string)currentUserEnvironment["ProgramFiles"];

            // Needed because this is set by explorer, and will mess up shell execution when rolling back and forth between browsers.
            // This will get re-set as soon as any .xbap or .xaml is launched.
            RegistryState.DeleteRegistryValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.xbap", "Application");
            RegistryState.DeleteRegistryValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.xaml", "Application");

            if (version == BrowserIdentifier.FireFox30)
            {
                // Starting with 3.0 we can now just invoke helper.exe in the mozilla directory
                // This prevents brittleness when we move forward to newer versions (was no programatic way to do this before)
                // The other cool part is that this is the same for all OS'es.
                string progFiles = "";
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ProgramFiles(x86)")))
                {
                    progFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                }
                else
                {
                    progFiles = Environment.GetEnvironmentVariable("ProgramFiles");
                }
                string helperExePath = Path.Combine(progFiles, @"Mozilla Firefox\uninstall\helper.exe");
                ProcessStartInfo startInfo = new ProcessStartInfo(helperExePath, "/SetAsDefaultAppUser");

                // Write in Environment variables from the user space
                // This is needed to fool the app into thinking it's running as the current user
                startInfo.EnvironmentVariables.Clear();
                foreach (string key in currentUserEnvironment.Keys)
                {
                    startInfo.EnvironmentVariables[key] = (string)currentUserEnvironment[key];
                }
                startInfo.UseShellExecute = false;
                Process fireFoxHelperProcess = ProcessHelper.CreateProcessAsFirstActiveSessionUser(startInfo, true);
                fireFoxHelperProcess.WaitForExit(20000);
            }
            else switch (Environment.OSVersion.Version.Major)
                {
                    case 5:
                        {
                            // X64 Platforms put their x86 apps (like FireFox) into a folder that is the 2nd Program Files folder.
                            // Normally this is Progra~2, but to avoid weird configurations (like a dirty drive that has been 
                            // migrated back and forth between Vista + XP64), use Win32 API to get the "real" short path.
                            string shortProgramFilesFullPath = ToShortPathName((string)currentUserEnvironment["ProgramFiles"]);
                            string pfShortPath = shortProgramFilesFullPath.Substring(shortProgramFilesFullPath.LastIndexOf("\\") + 1);

                            if (currentUserEnvironment["ProgramFiles(x86)"] != null)
                            {
                                shortProgramFilesFullPath = ToShortPathName((string)currentUserEnvironment["ProgramFiles(x86)"]);
                                pfShortPath = shortProgramFilesFullPath.Substring(shortProgramFilesFullPath.LastIndexOf("\\") + 1);
                            }

                            switch (version)
                            {
                                case BrowserIdentifier.FireFox15:
                                    {
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\MIME\Database\Content Type\application/x-xpinstall;app=firefox", ".xpi", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\.htm", "htmlfile", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\.htm", string.Empty, "FirefoxHTML", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\.html", "htmlfile", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\.html", string.Empty, "FirefoxHTML", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\.shtml", "shtmlfile", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\.shtml", string.Empty, "FirefoxHTML", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\.xht", "xhtfile", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\.xht", string.Empty, "FirefoxHTML", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\.xhtml", "xhtmlfile", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\.xhtml", string.Empty, "FirefoxHTML", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\FirefoxHTML\DefaultIcon", home + @"\" + pfShortPath + @"\MOZILL~1\FIREFOX.EXE,1", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\FirefoxHTML\shell\open\command", home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE -url \"%1\" -requestPending", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\FirefoxHTML\shell\open\command", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE -url \"%1\"", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", "SOFTWARE\\Classes\\HTTP\\DefaultIcon", home + "\\WINDOWS\\system32\\url.dll,0", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\DefaultIcon", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE,1", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\HTTP\shell\open\command", "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\shell\open\command", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE -url \"%1\"", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\HTTPS\DefaultIcon", home + "\\WINDOWS\\system32\\url.dll,0", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\https\DefaultIcon", string.Empty, home + @"\" + pfShortPath + @"\MOZILL~1\FIREFOX.EXE,1", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\HTTPS\shell\open\command", "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\https\shell\open\command", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE -url \"%1\"", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\FTP\DefaultIcon", home + @"\WINDOWS\system32\url.dll,0", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\ftp\DefaultIcon", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE,1", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\FTP\shell\open\command", "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" %1", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\ftp\shell\open\command", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE -url \"%1\"", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\GOPHER\DefaultIcon", home + @"\WINDOWS\system32\url.dll,0", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\gopher\DefaultIcon", string.Empty, home + @"\" + pfShortPath + @"\MOZILL~1\FIREFOX.EXE,1", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\GOPHER\shell\open\command", "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\gopher\shell\open\command", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE -url \"%1\"", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\CHROME\DefaultIcon", string.Empty, home + @"\" + pfShortPath + @"\MOZILL~1\FIREFOX.EXE,1", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\CHROME\shell\open\command", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE -url \"%1\"", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\DefaultIcon", progFilesX86 + @"\Mozilla Firefox\firefox.exe,0", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\DefaultIcon", string.Empty, home + @"\" + pfShortPath + @"\MOZILL~1\FIREFOX.EXE,0", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\shell\open\command", "\"" + progFilesX86 + "\\Mozilla Firefox\\firefox.exe\"", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\shell\open\command", string.Empty, home + @"\" + pfShortPath + @"\MOZILL~1\FIREFOX.EXE", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\shell\properties\command", "\"" + progFilesX86 + "\\Mozilla Firefox\\firefox.exe\" -preferences", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\shell\properties\command", string.Empty, home + @"\" + pfShortPath + @"\MOZILL~1\FIREFOX.EXE -preferences", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Clients\StartMenuInternet", "IEXPLORE.EXE", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Clients\StartMenuInternet", string.Empty, "FIREFOX.EXE", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE", "Mozilla Firefox", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\shell\properties", "Mozilla Firefox &Options", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\shell\properties", string.Empty, "Firefox &Options", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\shell\open\ddeexec", string.Empty, "\"%1\",,0,0,,,,", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\shell\open\ddeexec\Application", string.Empty, "Firefox", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\https\shell\open\ddeexec", string.Empty, "\"%1\",,0,0,,,,", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\https\shell\open\ddeexec\Application", string.Empty, "Firefox", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\ftp\shell\open\ddeexec", string.Empty, "\"%1\",,0,0,,,,", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\ftp\shell\open\ddeexec\Application", string.Empty, "Firefox", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\gopher\shell\open\ddeexec", string.Empty, "\"%1\",,0,0,,,,", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\gopher\shell\open\ddeexec\Application", string.Empty, "Firefox", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\CHROME\shell\open\ddeexec", "NoActivateHandler", "", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\CHROME\shell\open\ddeexec\Application", string.Empty, "Firefox", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\CHROME\shell\open\ddeexec\Topic", string.Empty, "WWW_OpenURL", RegistryValueKind.String);
                                        break;
                                    }

                                case BrowserIdentifier.FireFox20:
                                    {
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\MIME\Database\Content Type\application/x-xpinstall;app=firefox", ".xpi", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\.htm", "FireFoxHTML", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\.html", "FireFoxHTML", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\.shtml", "FireFoxHTML", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\.xht", "FireFoxHTML", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\.xhtml", "FireFoxHTML", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\FirefoxHTML\DefaultIcon", "\"" + home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE,1\"", RegistryValueKind.ExpandString);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\FirefoxHTML\shell\open\command", home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE -url \"%1\" -requestPending", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\HTTP\DefaultIcon", "%SystemRoot%\\system32\\url.dll,0", RegistryValueKind.ExpandString);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\HTTP\shell\open\command", "\"%ProgramFiles%\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.ExpandString);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\HTTPS\shell\open\command", "\"%ProgramFiles%\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.ExpandString);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\HTTPS\DefaultIcon", "%SystemRoot%\\system32\\url.dll,0", RegistryValueKind.ExpandString);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\FTP\DefaultIcon", "%SystemRoot%\\system32\\url.dll,0", RegistryValueKind.ExpandString);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\FTP\shell\open\command", "\"%ProgramFiles%\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.ExpandString);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\GOPHER\DefaultIcon", "%SystemRoot%\\system32\\url.dll,0", RegistryValueKind.ExpandString);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Classes\GOPHER\shell\open\command", "\"%ProgramFiles%\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.ExpandString);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\DefaultIcon", "\"" + home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE,0\"", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\DefaultIcon", string.Empty, "\"" + home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE,0\"", RegistryValueKind.ExpandString);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\shell\open\command", "\"" + home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE\"", RegistryValueKind.ExpandString);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\shell\properties\command", "\"" + home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE\" -preferences", RegistryValueKind.ExpandString);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\shell\open\command", string.Empty, "\"" + home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE\"", RegistryValueKind.ExpandString);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\shell\properties\command", string.Empty, "\"" + home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE\" -preferences", RegistryValueKind.ExpandString);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\shell\safemode\command", "\"" + home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE\" -safe-mode", RegistryValueKind.ExpandString);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\shell\safemode\command", string.Empty, "\"" + home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE\" -safe-mode", RegistryValueKind.ExpandString);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", "SOFTWARE\\Clients\\StartMenuInternet\\FIREFOX.EXE\\", "Mozilla Firefox", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\shell\properties", "Firefox &Options", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\shell\properties", string.Empty, "Firefox &Options", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Mozilla\Desktop", @"SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\shell\safemode", "Mozilla Firefox Safe Mode", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Clients\StartMenuInternet\FIREFOX.EXE\shell\safemode", string.Empty, "FIREFOX.EXE", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CURRENT_USER\Software\Clients\StartmenuInternet", string.Empty, "FIREFOX.EXE", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\.htm", string.Empty, "FirefoxHTML", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\.html", string.Empty, "FirefoxHTML", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\.xht", string.Empty, "FirefoxHTML", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\.xhtml", string.Empty, "FirefoxHTML", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\.shtml", "PerceivedType", "text", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\.shtml", string.Empty, "FirefoxHTML", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\.shtml", "Content Type", "text/html", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\FirefoxHTML\shell\open\command", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE -url \"%1\" -requestPending", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\DefaultIcon", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE,0", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\HTTPS\DefaultIcon", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE,0", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\shell\open\command", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE -url \"%1\" -requestPending", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\HTTPS\shell\open\command", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE -url \"%1\" -requestPending", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\FTP\DefaultIcon", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE,0", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\ftp\shell\open\command", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE -url \"%1\" -requestPending", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\gopher\DefaultIcon", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE,1", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\gopher\shell\open\command", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE -url \"%1\" -requestPending", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\CHROME\DefaultIcon", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE -url \"%1\"", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\CHROME\shell\open\command", string.Empty, home + "\\" + pfShortPath + "\\MOZILL~1\\FIREFOX.EXE -url \"%1\" ", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\shell\open\ddeexec", string.Empty, "\"%1\",,0,0,,,,", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\shell\open\ddeexec\Application", string.Empty, "FireFox", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\https\shell\open\ddeexec", string.Empty, "\"%1\",,0,0,,,,", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\https\shell\open\ddeexec\Application", string.Empty, "FireFox", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\ftp\shell\open\ddeexec", string.Empty, "\"%1\",,0,0,,,,", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\ftp\shell\open\ddeexec\Application", string.Empty, "FireFox", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\gopher\shell\open\ddeexec", string.Empty, "\"%1\",,0,0,,,,", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\CHROME\shell\open\ddeexec", string.Empty, "\"%1\",,0,0,,,,", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\CHROME\shell\open\ddeexec\Application", string.Empty, "Firefox", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\CHROME\shell\open\ddeexec\Topic", string.Empty, "WWW_OpenURL", RegistryValueKind.String);
                                        break;
                                    }
                            }
                            break;
                        }
                    case 6:
                        {
                            DeleteAndSetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.htm\UserChoice", "Progid", "FirefoxHTML", RegistryValueKind.String);
                            DeleteAndSetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.html\UserChoice", "Progid", "FirefoxHTML", RegistryValueKind.String);
                            DeleteAndSetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.shtml\UserChoice", "Progid", "FirefoxHTML", RegistryValueKind.String);
                            DeleteAndSetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.xht\UserChoice", "Progid", "FirefoxHTML", RegistryValueKind.String);
                            DeleteAndSetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.xhtml\UserChoice", "Progid", "FirefoxHTML", RegistryValueKind.String);
                            SetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice", "Progid", "FirefoxURL", RegistryValueKind.String);
                            SetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice", "Progid", "FirefoxURL", RegistryValueKind.String);
                            SetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\ftp\UserChoice", "Progid", "FirefoxURL", RegistryValueKind.String);
                            SetRegValue(@"HKEY_CURRENT_USER\Software\Clients\StartmenuInternet", string.Empty, "firefox.exe", RegistryValueKind.String);
                            break;
                        }
                    default:
                        throw new System.NotImplementedException("Don't know how to make FireFox Default browser for this version of Windows.");
                }
        }
        
        private void SetIEAsDefaultBrowserInRegistry()
        {
            // Needed because this is set by explorer, and will mess up shell execution when rolling back and forth between browsers.
            // This will get re-set as soon as any .xbap or .xaml is launched.
            RegistryState.DeleteRegistryValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.xbap", "Application");
            RegistryState.DeleteRegistryValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.xaml", "Application");

            string home = (string) currentUserEnvironment["HOMEDRIVE"];
            string progFilesX86   = (string)currentUserEnvironment["ProgramFiles"];
            string progFilesWOW64 = (string)currentUserEnvironment["ProgramFiles(x86)"];
            
            // IE has different behavior
            switch (Environment.OSVersion.Version.Major)
            {
                // Windows XP
                case 5:
                    {
                    // XP can have either IE6 or 7.
                        switch (new Version(SystemInformation.Current.IEVersion).Major)
                        {
                            case 6:
                                {
                                    // Need to set these specially after others since the normal setting writes "wrong" values in WOW scenarios.
                                    // On XP, we always want to run 32 bit IE + 32 bit presentationhost.
                                    if (currentUserEnvironment["ProgramFiles(x86)"] != null)
                                    {
                                        SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\shell\open\command", string.Empty, "\"" + progFilesWOW64 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\https\shell\open\command", string.Empty, "\"" + progFilesWOW64 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\ftp\shell\open\command", string.Empty, "\"" + progFilesWOW64 + "\\Internet Explorer\\iexplore.exe\" %1", RegistryValueKind.String);
                                    }
                                    else
                                    {
                                        SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\shell\open\command",  string.Empty, "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\https\shell\open\command", string.Empty, "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\ftp\shell\open\command",   string.Empty, "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" %1", RegistryValueKind.String);
                                    }
                                    SetRegValue(@"HKEY_CLASSES_ROOT\.htm", string.Empty, "htmlfile", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\.html", string.Empty, "htmlfile", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\ftp\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\ftp\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\gopher\shell\open\command", string.Empty, "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\gopher\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\gopher\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\https\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\https\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\.htm", string.Empty, "htmlfile", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\.html'", string.Empty, "htmlfile", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ftp\shell\open\command", string.Empty, "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" %1", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ftp\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ftp\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\gopher\shell\open\command", string.Empty, "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\gopher\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\gopher\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\HTTP\shell\open\command", string.Empty, "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\HTTP\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\HTTP\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\https\shell\open\command", string.Empty, "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\https\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\https\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Clients\StartMenuInternet", string.Empty, "IEXPLORE.EXE", RegistryValueKind.String);

                                    break;
                                }
                            case 7:
                                {
                                    // Need to set these specially after others since the normal setting writes "wrong" values in WOW scenarios.
                                    // On XP, we always want to run 32 bit IE + 32 bit presentationhost.
                                    if (currentUserEnvironment["ProgramFiles(x86)"] != null)
                                    {
                                        SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\shell\open\command", string.Empty, "\"" + progFilesWOW64 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\https\shell\open\command", string.Empty, "\"" + progFilesWOW64 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\ftp\shell\open\command", string.Empty, "\"" + progFilesWOW64 + "\\Internet Explorer\\iexplore.exe\" %1", RegistryValueKind.String);
                                    }
                                    else
                                    {
                                        SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\shell\open\command", string.Empty, "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\https\shell\open\command", string.Empty, "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                                        SetRegValue(@"HKEY_CLASSES_ROOT\ftp\shell\open\command", string.Empty, "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" %1", RegistryValueKind.String);
                                    }

                                    SetRegValue(@"HKEY_CLASSES_ROOT\.htm", string.Empty, "htmlfile", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\.html", string.Empty, "htmlfile", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\ftp\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\ftp\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\gopher\shell\open\command", string.Empty, "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\gopher\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\gopher\shell\open\ddeexec", "NoActivateHandler","", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\gopher\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\gopher\shell\open\ddeexec\Topic", string.Empty, "WWW_OpenURL", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\shell\open\ddeexec", "NoActivateHandler", "", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\HTTP\shell\open\ddeexec\Topic", string.Empty, "WWW_OpenURL", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\https\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\https\shell\open\ddeexec", "NoActivateHandler", "", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\https\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_CLASSES_ROOT\https\shell\open\ddeexec\Topic", string.Empty, "WWW_OpenURL", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\.htm", string.Empty, "htmlfile", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\.html", string.Empty, "htmlfile", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ftp\shell\open\ddeexec", string.Empty,"\"%1\",,-1,0,,,,", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ftp\shell\open\ddeexec", "NoActivateHandler", "", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ftp\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ftp\shell\open\ddeexec\ifExec", string.Empty, "*", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ftp\shell\open\ddeexec\Topic", string.Empty, "WWW_OpenURL", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\gopher\shell\open\command", string.Empty, "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\gopher\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\gopher\shell\open\ddeexec", "NoActivateHandler","", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\gopher\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\gopher\shell\open\ddeexec\Topic", string.Empty, "WWW_OpenURL", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\HTTP\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\HTTP\shell\open\ddeexec", "NoActivateHandler", "", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\HTTP\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\HTTP\shell\open\ddeexec\Topic", string.Empty, "WWW_OpenURL", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\https\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\https\shell\open\ddeexec", "NoActivateHandler", "", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\https\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\https\shell\open\ddeexec\Topic", string.Empty, "WWW_OpenURL", RegistryValueKind.String);
                                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Clients\StartMenuInternet", string.Empty, "IEXPLORE.EXE", RegistryValueKind.String);

                                    break;
                                }
                            case 8:
                                {
                                    goto case 7;
                                }
                        }
                        break;
                    }

                case 6:
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM", string.Empty, "HTML Document", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM", "FriendlyTypeName", @"@%systemroot%\system32\ieframe.dll,-912", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\DefaultIcon", string.Empty, @"%ProgramFiles%\Internet Explorer\iexplore.exe,-17", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell", string.Empty, "opennew", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell\open", string.Empty, "Open in S&ame Window", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell\open", "MUIVerb", "@ieframe.dll,-5732", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell\open\command", string.Empty, "\"%ProgramFiles%\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.ExpandString); //****
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell\open\ddeexec", string.Empty, "\"file://%1\",-1,,,,,", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell\open\ddeexec", "NoActivateHandler", "", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell\open\ddeexec\Topic", string.Empty, "WWW_OpenURL", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell\opennew", string.Empty, "&Open", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell\opennew", "MUIVerb", "@ieframe.dll,-5731", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell\opennew\command", string.Empty, "\"%ProgramFiles%\\Internet Explorer\\iexplore.exe\" %1", RegistryValueKind.ExpandString); //****
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell\opennew\ddeexec", string.Empty, "\"file://%1\",-1,,,,,", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell\opennew\ddeexec", "NoActivateHandler", "", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell\opennew\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell\opennew\ddeexec\IfExec", string.Empty, "*", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell\opennew\ddeexec\Topic", string.Empty, "WWW_OpenURLNewWindow", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell\print\command", string.Empty, "rundll32.exe C:\\Windows\\system32\\mshtml.dll,PrintHTML \"%1\"", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.HTM\shell\printto\command", string.Empty, "rundll32.exe C:\\Windows\\system32\\mshtml.dll,PrintHTML \"%1\" \"%2\" \"%3\" \"%4\"", RegistryValueKind.ExpandString);
                    //*************** MHT Area ****************************
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT", string.Empty, "MHTML Document", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT", "FriendlyTypeName", @"@%systemroot%\system32\ieframe.dll,-913", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT\DefaultIcon", string.Empty, @"%ProgramFiles%\Internet Explorer\iexplore.exe,-32554", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT\shell", string.Empty, "opennew", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT\shell\open", string.Empty, "Open in S&ame Window", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT\shell\open", "MUIVerb", "@ieframe.dll,-5732", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT\shell\open\command", string.Empty, "\"%ProgramFiles%\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.ExpandString); //****
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT\shell\open\ddeexec", string.Empty, "\"file://%1\",-1,,,,,", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT\shell\open\ddeexec", "NoActivateHandler", "", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT\shell\open\ddeexec\Topic", string.Empty, "WWW_OpenURL", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT\shell\opennew", string.Empty, "&Open", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT\shell\opennew", "MUIVerb", "@ieframe.dll,-5731", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT\shell\opennew\command", string.Empty, "\"%ProgramFiles%\\Internet Explorer\\iexplore.exe\" %1", RegistryValueKind.ExpandString); //****
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT\shell\opennew\ddeexec", string.Empty, "\"file://%1\",-1,,,,,", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT\shell\opennew\ddeexec", "NoActivateHandler", "", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT\shell\opennew\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT\shell\opennew\ddeexec\IfExec", string.Empty, "*", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.MHT\shell\opennew\ddeexec\Topic", string.Empty, "WWW_OpenURLNewWindow", RegistryValueKind.String);
                    //*************** URL Area ****************************
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.URL", "EditFlags", 2, RegistryValueKind.DWord);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.URL", "IsShortcut", "", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.URL", "NeverShowExt", "", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.URL", "FriendlyTypeName", "@%systemroot%\\system32\\ieframe.dll,-10046", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.URL", "PreviewDetails", "prop:System.Link.TargetUrl;System.Rating;System.History.VisitCount;System.History.DateChanged;System.Link.DateVisited;System.Link.Description;System.Link.Comment", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.URL", "InfoTip", "prop:System.Link.TargetUrl;System.Rating;System.Link.Description;System.Link.Comment", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.URL", "FullDetails", "prop:System.Link.TargetUrl;System.Rating;System.Link.Description;System.Link.Comment", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.URL\DefaultIcon", string.Empty, "%systemroot%\\system32\\url.dll,0", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.URL\Shell", "CLSID", "{FBF23B40-E3F0-101B-8488-00AA003E56F8}", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.URL\Shell\Open", "CLSID", "{FBF23B40-E3F0-101B-8488-00AA003E56F8}", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.URL\Shell\Open", "LegacyDisable", "", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.URL\Shell\Open\Command", string.Empty, "rundll32.exe ieframe.dll,OpenURL %l", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.URL\Shell\print\command", string.Empty, "rundll32.exe C:\\Windows\\system32\\mshtml.dll,PrintHTML \"%1\"", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.URL\Shell\printto\command", string.Empty, "@%systemroot%\\system32\\mshtml.dll,PrintHTML \"%1\" \"%2\" \"%3\" \"%4\"", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.AssocFile.URL\ShellEx\IconHandler", string.Empty, "{FBF23B40-E3F0-101B-8488-00AA003E56F8}", RegistryValueKind.String);
                    // IE.FTP section
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.FTP", string.Empty, "URL:File Transfer Protocol", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.FTP", "EditFlags", 2, RegistryValueKind.DWord);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.FTP", "FriendlyTypeName", "@%systemroot%\\system32\\ieframe.dll,-905", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.FTP", "URL Protocol", "", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.FTP\DefaultIcon", string.Empty, @"%systemroot%\system32\url.dll,0", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.FTP\shell\open\command", string.Empty, "\"%ProgramFiles%\\Internet Explorer\\iexplore.exe\" %1", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.FTP\shell\open\ddeexec", "NoActivateHandler", "", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.FTP\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.FTP\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.FTP\shell\open\ddeexec\IfExec", string.Empty, "*", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.FTP\shell\open\ddeexec\Topic", string.Empty, "WWW_OpenURL", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTP", string.Empty, "URL:HyperText Transfer Protocol", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTP", "EditFlags", 2, RegistryValueKind.DWord);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTP", "URL Protocol", "", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTP", "FriendlyTypeName", "@%systemroot%\\system32\\ieframe.dll,-903", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTP\DefaultIcon", string.Empty, "%systemroot%\\system32\\url.dll,0", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTP\shell\open\command", string.Empty, "\"%ProgramFiles%\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTP\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTP\shell\open\ddeexec", "NoActivateHandler", "", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTP\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTP\shell\open\ddeexec\Topic", string.Empty, "WWW_OpenURL", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTPS", string.Empty, "URL:HyperText Transfer Protocol with Privacy", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTPS", "EditFlags", 2, RegistryValueKind.DWord);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTPS", "FriendlyTypeName", "@%systemroot%\\system32\\ieframe.dll,-904", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTPS", "URL Protocol", "", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTPS\DefaultIcon", string.Empty, "%systemroot%\\system32\\url.dll,0", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTPS\shell\open\command", string.Empty, "\"%ProgramFiles%\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTPS\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTPS\shell\open\ddeexec", "NoActivateHandler", "", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTPS\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CLASSES_ROOT\IE.HTTPS\shell\open\ddeexec\Topic", string.Empty, "WWW_OpenURL", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CURRENT_USER\Software\Clients\StartmenuInternet", string.Empty, "IEXPLORE.EXE", RegistryValueKind.String);
                    DeleteAndSetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.htm\UserChoice", "Progid", "IE.AssocFile.HTM", RegistryValueKind.String);
                    DeleteAndSetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.html\UserChoice", "Progid", "IE.AssocFile.HTM", RegistryValueKind.String);
                    DeleteAndSetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.mht\UserChoice", "Progid", "IE.AssocFile.MHT", RegistryValueKind.String);
                    DeleteAndSetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.mhtml\UserChoice", "Progid", "IE.AssocFile.MHT", RegistryValueKind.String);
                    DeleteAndSetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.url\UserChoice", "Progid", "IE.AssocFile.URL", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Associations\MIMEAssociations\message/rfc822\UserChoice", "Progid", "IE.message/rfc822", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Associations\MIMEAssociations\text/html\UserChoice", "Progid", "IE.text/html", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\ftp\UserChoice", "Progid", "IE.FTP", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice", "Progid", "IE.HTTP", RegistryValueKind.String);
                    SetRegValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice", "Progid", "IE.HTTPS", RegistryValueKind.String);
                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\Capabilities", "ApplicationDescription", "@%ProgramFiles%\\Internet Explorer\\iexplore.exe,-706", RegistryValueKind.ExpandString);
                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\Capabilities\FileAssociations",".htm", "IE.AssocFile.HTM", RegistryValueKind.String);
                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\Capabilities\FileAssociations", ".html", "IE.AssocFile.HTM", RegistryValueKind.String);
                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\Capabilities\FileAssociations", ".mht", "IE.AssocFile.MHT", RegistryValueKind.String);
                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\Capabilities\FileAssociations", ".mhtml", "IE.AssocFile.MHT", RegistryValueKind.String);
                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\Capabilities\FileAssociations", ".url", "IE.AssocFile.URL", RegistryValueKind.String);
                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\Capabilities\MIMEAssociations","message/rfc822", "IE.message/rfc822", RegistryValueKind.String);
                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\Capabilities\MIMEAssociations", "text/html", "IE.text/html", RegistryValueKind.String);
                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\Capabilities\Startmenu", "StartmenuInternet", "IEXPLORE.EXE", RegistryValueKind.String);
                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\Capabilities\UrlAssociations", "http", "IE.HTTP", RegistryValueKind.String);
                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\Capabilities\UrlAssociations", "https", "IE.HTTPS", RegistryValueKind.String);
                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\Capabilities\UrlAssociations", "ftp", "IE.FTP", RegistryValueKind.String);
                    SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\RegisteredApplications", "Internet Explorer", "SOFTWARE\\Microsoft\\Internet Explorer\\Capabilities", RegistryValueKind.String);
                    break;
                default:
                    throw new System.NotImplementedException("Don't know how to make IE the Default browser for this version of Windows.");
            }
        }

        private void DeleteAndSetRegValue(string path, string keyName, object value, RegistryValueKind kind)
        {
            string correctedPath = RemapRegValuePathForRunAsSystem(currentUserSid, path);

            // HACK: For some unknown reason when the current user uses the control panel for setting default browser
            // All the UserChoice entries will be ACL'ed to deny that user.  WORKAROUND: Delete the key first, which sensibly we can do :)
            // COROMT totally thought of this awesome workaround :)
            if (correctedPath.StartsWith(@"HKEY_USERS\" + currentUserSid))
            {
                // Registry.CurrentUser.DeleteSubKey(correctedPath.Substring((@"HKEY_USERS\" + currentUserSid).Length), false);
                this.SetRegValue(correctedPath, keyName, null, kind);
                this.SetRegValue(correctedPath, keyName, value, kind);
            }
        }

        private void SetRegValue(string path, string keyName, object value, RegistryValueKind kind)
        {
            // Create a RegistryState object to represent this particular key...
            RegistryState registryState = new RegistryState(currentUserSid, path, keyName);
            // If value is null, we're deleting so don't back it up for ACL purposes.
            if (value != null)
            {
                // Back up its current value
                object originalValue = registryState.GetValue();
                if (originalValue != null)
                {
                    // Add the backup to the list of states to restore.
                    BrowserState.changedRegistryValues.Add(registryState, ((RegistryStateValue)originalValue));
                }
                else
                {
                    // Don't do this if the value is null since we won't be able to do anything with it.
                    // Instead set it to null so we know to delete it.
                    BrowserState.changedRegistryValues.Add(registryState, null);
                }
            }
            // finally, actually set the value
            registryState.SetValue(new RegistryStateValue(value, kind), null);
        }

        private static string RemapRegValuePathForRunAsSystem(string userSid, string keyName)
        {
            SecurityIdentifier sid = new SecurityIdentifier(userSid);
            if (!sid.IsAccountSid())
            {
                throw new ArgumentException("The sid must be from a valid user account");
            }

            if (keyName.Contains("HKCU"))
            {
                keyName = keyName.Replace("HKCU", @"HKEY_USERS\" + userSid);
            }
            else if (keyName.Contains("HKEY_CURRENT_USER"))
            {
                keyName = keyName.Replace("HKEY_CURRENT_USER", @"HKEY_USERS\" + userSid);
            }

            return keyName;
        }

        private void RestoreAllBackedUpFolders(Dictionary<string, string> backedUpFolders)
        {
            foreach (string s in backedUpFolders.Keys)
            {
                Directory.Move(backedUpFolders[s], s);
            }
        }

        private void BackupFolderToTemp(string path, Dictionary<string,string> backedUpFolders)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                path = path + Path.DirectorySeparatorChar.ToString();
            }

            if (!Directory.Exists(path))
            {
                return; // nothing to backup, leave it alone
            }
            else
            {
                // Create a folder in the temp dir to copy backed up Program Files / Appdata folders
                try
                {
                    DirectoryInfo originalDirectory = new DirectoryInfo(path);       
                    // Use random name to avoid collisions
                    string randomDirName = Path.GetRandomFileName();
                    string tempFolderName = Path.Combine(Path.GetTempPath(), @"BrowserStateBackup\");
                    tempFolderName = Path.Combine(tempFolderName, randomDirName); 
                    Directory.CreateDirectory(tempFolderName);
                    tempFolderName = Path.Combine(tempFolderName, originalDirectory.Name);                    
                    Directory.Move(path, tempFolderName);
                    // BackedUpFolders is later used to restore these folders upon return to this state.
                    backedUpFolders.Add(path, tempFolderName);
                }
                catch { }
            }
        }

        private static void KillFolder(string folder)
        {
            int CountDown = 3;
            bool completed = true;

            if (Directory.Exists(folder))
            {
                do
                {
                    if (CountDown < 3)
                    {
                        // Pause, loop and retry since there's a chance some random system process has a handle in this folder
                        // Chances are we can wait a second and retry succesfully.
                        Thread.Sleep(1000);
                    }
                    try
                    {
                        Directory.Delete(folder, true);
                        completed = true;
                    }
                    catch (System.IO.DirectoryNotFoundException)
                    {
                        // Something else has killed the folder, we're done here.
                        completed = true;
                    }
                    catch
                    {
                        completed = false;
                        CountDown--;
                    }
                } while (!completed && (CountDown > 0));
            }
        }

        private static void CleanupAfterFireFox(BrowserIdentifier version, Hashtable environmentVars)
        {
            KillFolder(FirefoxHelper.GetMozillaAppDataFolder(version, environmentVars));
            KillFolder(FirefoxHelper.GetMozillaProgFilesFolder(version));
        }                    

        #endregion
    }

    /// <summary>
    /// Browser state value class
    /// </summary>
    public class BrowserStateValue
    {
        private bool isInstalled = false;
        private BrowserIdentifier browser;
        private bool isDefaultBrowser = false; 

        #region Constructors

        /// <summary>
        /// Constructor for BrowserStateValue, represents state of installed 3rd party browser on the machine.
        /// </summary>
        /// <param name="whichBrowser">3rd party browser to be installed, or none.</param>
        /// <param name="installed">Whether whichBrowser should be installed or not.</param>
        /// <param name="isDefault">Whether whichBrowser is the default browser</param>
        public BrowserStateValue(BrowserIdentifier whichBrowser, bool installed, bool isDefault)
        {
            this.isInstalled = installed;
            this.isDefaultBrowser = isDefault;
            this.browser = whichBrowser;
            this.BackedUpFolders = new Dictionary<string, string>();
            this.FoldersToPreserve = new List<string>();
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Whether this browser is currently installed
        /// </summary>
        [XmlAttribute()]
        public bool Installed
        {
            get { return isInstalled; }
            set { isInstalled = value;}
        }

        /// <summary>
        /// Which Alternate browser to use.  Only can be changed if browser not currently installed.
        /// </summary>
        [XmlAttribute()]
        public BrowserIdentifier Browser
        {
            get { return browser; }
            set { browser = value;}
        }

        /// <summary>
        /// Which Alternate browser to use.  Only can be changed if browser not currently installed.
        /// </summary>
        [XmlAttribute()]
        public bool DefaultBrowser
        {
            get { return isDefaultBrowser; }
            set { isDefaultBrowser = value;}
        }
        /// <summary>
        /// Dictionary of Folders that have been backed up for state preservation and where they are backed-up-to
        /// </summary>
        public Dictionary<string, string> BackedUpFolders;

        /// <summary>
        /// When BrowserState.GetValue() is called, any folders that need to be preserved go into this list.
        /// When the state is transitioned away from, the folders are copied into the temp dir and this info goes into BackedUpFolders
        /// </summary>
        public List<string> FoldersToPreserve;
        

        #endregion

        #region Override Members

        /// <summary>
        /// Hash for BrowserStateValue
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            int hashVal = 1;
            if (isDefaultBrowser)
            {
                hashVal += 10;
            }
            if (isInstalled)
            {
                hashVal += 100;
            }
            switch (browser)
            {
                case BrowserIdentifier.None:
                    hashVal += 1000;
                    break;
                case BrowserIdentifier.FireFox15:
                    hashVal += 2000;
                    break;
                case BrowserIdentifier.FireFox20:
                    hashVal += 3000;
                    break;
                case BrowserIdentifier.FireFoxUnknown:
                    hashVal += 4000;
                    break;
            }
            return hashVal;
        }

        /// <summary>
        /// Equivalence method for BrowserStateValue
        /// </summary>
        /// <param name="obj">object to compare</param>
        /// <returns>True if values are the same, false otherwise</returns>
        public override bool Equals(object obj)
        {
            BrowserStateValue other = obj as BrowserStateValue;
            if (object.Equals(other, null))
            {
                return false;
            }
            
            return (other.isInstalled == isInstalled &&
                    other.isDefaultBrowser == isDefaultBrowser && 
                    other.browser == browser);
        }

        /// <summary>
        /// Equivalence operator for BrowserStateValue
        /// </summary>
        /// <param name="x">First BrowserStateValue</param>
        /// <param name="y">Second BrowserStateValue</param>
        /// <returns>true if the objects are equal, false otherwise</returns>
        public static bool operator ==(BrowserStateValue x, BrowserStateValue y)
        {
            if (object.Equals(x, null))
            {
                return object.Equals(y, null);
            }
            else
            {
                return x.Equals(y);
            }
        }

        /// <summary>
        /// Antiequivalence operator for BrowserStateValue
        /// </summary>
        /// <param name="x">First BrowserStateValue</param>
        /// <param name="y">Second BrowserStateValue</param>
        /// <returns>True if objects are nonequal, false otherwise.</returns>
        public static bool operator !=(BrowserStateValue x, BrowserStateValue y)
        {
            if (object.Equals(x, null))
            {
                return !object.Equals(y, null);
            }
            else
            {
                return !x.Equals(y);
            }
        }

        #endregion
    }
}
