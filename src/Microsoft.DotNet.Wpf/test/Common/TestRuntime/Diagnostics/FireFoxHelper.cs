// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Threading;
using Microsoft.Test.Deployment;

namespace Microsoft.Test.Diagnostics
{
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    internal static class FirefoxHelper
    {
        #region Constants

        public const string firefox15VersionString = "1.5";
        public const string firefox15UninstallExe = "UninstallFirefox.exe";
        public const string firefox15SilentInstall = "-ms";
        public const string firefox15SilentUninstall = "/ua \"1.5 (en-US)\" -ms";
        public const int    firefox15FakePrefsSize = 1083;

        public const string firefox20VersionString = "2.0.0.18";
        public const string firefox20UninstallExe = "helper.exe";
        public const string firefox20SilentInstall = "-ms";
        public const string firefox20SilentUninstall = "/S";
        public const int    fireFox_20_Fake_Prefs_Size = 1777;

        public const string firefox30VersionString = "3.0.4";
        public const string firefox30UninstallExe = "helper.exe";
        public const string firefox30SilentInstall = "-ms";
        public const string firefox30SilentUninstall = "/S";
        public const int fireFox_30_Fake_Prefs_Size = 1775;

        #endregion

        #region Public Methods

        public static string Firefox15Path
        {
            get 
            {
                string directory = RuntimeDeploymentHelper.GetDeploymentInstallLocation("FireFoxInstallers");
                if (directory != null)
                {
                    return directory + "\\Firefox Setup 1.5.exe";
                }
                else
                    return null;
            }            
        }

        public static string Firefox20Path
        {
            get
            {
                string directory = RuntimeDeploymentHelper.GetDeploymentInstallLocation("FireFoxInstallers");
                if (directory != null)
                {
                    return directory + "\\Firefox Setup 2.0.0.18.exe";
                }
                else
                    return null;
            }
        }

        public static string Firefox30Path
        {
            get
            {
                string directory = RuntimeDeploymentHelper.GetDeploymentInstallLocation("FireFoxInstallers");
                if (directory != null)
                {
                    return directory + "\\Firefox Setup 3.0.4.exe";
                }
                else
                    return null;
            }
        }

        public static void InstallFireFox(BrowserIdentifier version, Hashtable environmentVariables)
        {
            // Check the existing state.  Run cleanup code if we're being asked to install on a "dirty" state
            if (Directory.Exists(GetMozillaAppDataFolder(version, environmentVariables)) || Directory.Exists(GetMozillaProgFilesFolder(version)))
            {
                UninstallFireFox(version, environmentVariables);
            }

            // Copy over the files needed...
            //************************************************

            string installerPath = null;
            string installerArgs = null;

            switch (version)
            {
                case BrowserIdentifier.FireFox15:
                    installerPath = Firefox15Path;
                    installerArgs = firefox15SilentInstall;
                    break;
                case BrowserIdentifier.FireFox20:
                    installerPath = Firefox20Path;
                    installerArgs = firefox20SilentInstall;
                    break;
                case BrowserIdentifier.FireFox30:
                    installerPath = Firefox30Path;
                    installerArgs = firefox30SilentInstall;
                    break;
                default:
                    throw new NotImplementedException("Error: Don't have information for FireFox version " + version.ToString());
            }

            // Kick off the install ...
            //************************************************
            ProcessStartInfo pInfo = new ProcessStartInfo(installerPath, installerArgs);
            // Write in Environment variables from the user space
            // This is needed to fool the app into thinking it's running as the current user
            pInfo.EnvironmentVariables.Clear();
            foreach (string key in environmentVariables.Keys)
            {
                pInfo.EnvironmentVariables[key] = (string) environmentVariables[key];
            }

            pInfo.UseShellExecute = false;
            
            Process installProc = ProcessHelper.CreateProcessAsFirstActiveSessionUser(pInfo, true);

            // Give it 3 minutes.  Shouldn't ever take longer. (On my machine it takes about 25 seconds)
            installProc.WaitForExit(180000);

            // Set up the user profile as if the first run has already happened... (Prevents the "Import bookmarks" dialog)
            WriteFireFoxProfileIni(version, environmentVariables);

            // And write a prefs file that prevents the other dialogs. (HTTPS, tabs, etc)
            WriteFireFoxPrefsFile(version, environmentVariables);
        }

        /// <summary>
        /// Uninstalls FireFox, assuming passed-in version is accurate.  Currently supports 1.5 through 3.0
        /// </summary>
        /// <param name="version">Version that was previously installed</param>
        /// <param name="environmentVariables">Hashtable of the environment variables for the current user</param>
        public static void UninstallFireFox(BrowserIdentifier version, Hashtable environmentVariables)
        {
            switch (version)
            {
                case BrowserIdentifier.FireFox15:
                    InvokeMozillaSilentUninstall(firefox15SilentUninstall, version, firefox15UninstallExe, environmentVariables);
                    break;
                case BrowserIdentifier.FireFox20:
                    InvokeMozillaSilentUninstall(firefox20SilentUninstall, version, firefox20UninstallExe, environmentVariables);
                    break;
                case BrowserIdentifier.FireFox30:
                    InvokeMozillaSilentUninstall(firefox30SilentUninstall, version, firefox30UninstallExe, environmentVariables);
                    break;
                case BrowserIdentifier.None:
                    // Do nothing... this gets called on a machine with a dirty state to begin with, i.e. program files folder but no exe.
                    break;
                default:
                    throw new NotImplementedException("Don't have information on uninstalling this version of FireFox");
            }
        }

        public static void WriteFireFoxPrefsFile(BrowserIdentifier version, Hashtable environmentVars)
        {
            using (TextWriter tw = new StreamWriter(GetMozillaAppDataFolder(version, environmentVars) + "\\FireFox\\Profiles\\" + environmentVars["USERNAME"] + "\\prefs.js"))
            {
                tw.WriteLine("# Fake Mozilla User Preferences inserted for test use.  Contact MattGal for issues \n\n");
                tw.WriteLine("# Mozilla User Preferences\n");
                tw.WriteLine("/* Do not edit this file.");
                tw.WriteLine("*");
                tw.WriteLine("* If you make changes to this file while the application is running,");
                tw.WriteLine("* the changes will be overwritten when the application exits.");
                tw.WriteLine("*");
                tw.WriteLine("* To make a manual change to preferences, you can visit the URL about:config");
                tw.WriteLine("* For more information, see http://www.mozilla.org/unix/customizing.html#prefs");
                tw.WriteLine("*/\n");


                // NOTE: If you modify the generation of the prefs file below you will need to update the fireFox_<version>_Fake_Prefs_Size
                //       const with the new size of the generated file.

                switch (version)
                {
                    case BrowserIdentifier.FireFox15:
                        {
                            tw.WriteLine("user_pref(\"app.update.lastUpdateTime.addon-background-update-timer\", 1171569793);");
                            tw.WriteLine("user_pref(\"app.update.lastUpdateTime.background-update-timer\", 1171569791);");
                            tw.WriteLine("user_pref(\"browser.search.selectedEngine", "Google\");");
                            tw.WriteLine("user_pref(\"browser.startup.homepage_override.mstone\", \"rv:1.8\");");
                            tw.WriteLine("user_pref(\"browser.tabs.warnOnClose\", false);");
                            tw.WriteLine("user_pref(\"extensions.lastAppVersion\", \"1.5\");");
                            tw.WriteLine("user_pref(\"intl.charsetmenu.browser.cache\", \"ISO-8859-1, UTF-8\");");
                            tw.WriteLine("user_pref(\"network.cookie.prefsMigrated\", true);");
                            tw.WriteLine("user_pref(\"security.warn_entering_secure\", false);");
                            tw.WriteLine("user_pref(\"security.warn_viewing_mixed\", false);");
                            tw.WriteLine("user_pref(\"browser.shell.checkDefaultBrowser\", false);");
                            break;
                        }
                    case BrowserIdentifier.FireFox20:
                        {
                            tw.WriteLine("user_pref(\"app.update.enabled\", false);");
                            tw.WriteLine("user_pref(\"app.update.lastUpdateTime.addon-background-update-timer\", 1171491940);");
                            tw.WriteLine("user_pref(\"app.update.lastUpdateTime.background-update-timer\", 1171491939);");
                            tw.WriteLine("user_pref(\"app.update.lastUpdateTime.blocklist-background-update-timer\", 1171491940);");
                            tw.WriteLine("user_pref(\"app.update.lastUpdateTime.search-engine-update-timer\", 1171491942);");
                            tw.WriteLine("user_pref(\"browser.shell.checkDefaultBrowser\", false);");
                            tw.WriteLine("user_pref(\"browser.sessionstore.enabled\", false);");
                            tw.WriteLine("user_pref(\"browser.startup.homepage_override.mstone\", \"rv:1.8.1.1\");");
                            tw.WriteLine("user_pref(\"browser.tabs.warnOnClose\", false);");
                            tw.WriteLine("user_pref(\"browser.search.selectedEngine\", \"Live Search\");");
                            tw.WriteLine("user_pref(\"extensions.lastAppVersion\", \"2.0.0.18\");");
                            tw.WriteLine("user_pref(\"intl.charsetmenu.browser.cache\", \"ISO-8859-1, UTF-8\");");
                            tw.WriteLine("user_pref(\"network.cookie.prefsMigrated\", true);");
                            tw.WriteLine("user_pref(\"security.warn_entering_secure\", false);");
                            tw.WriteLine("user_pref(\"urlclassifier.keyupdatetime.https://sb-ssl.google.com/safebrowsing/getkey?client=navclient-auto-ffox2.0.0.3&\", 1171578343);");
                            tw.WriteLine("user_pref(\"urlclassifier.tableversion.goog-black-enchash\", \"1.18519\");");
                            tw.WriteLine("user_pref(\"urlclassifier.tableversion.goog-black-url\", \"1.8691\");");
                            tw.WriteLine("user_pref(\"urlclassifier.tableversion.goog-white-domain\", \"1.19\");");
                            tw.WriteLine("user_pref(\"urlclassifier.tableversion.goog-white-url\", \"1.371\");");
                            tw.WriteLine("user_pref(\"security.warn_viewing_mixed\", false);");
                            break;
                        }
                    case BrowserIdentifier.FireFox30:
                        {
                            tw.WriteLine("user_pref(\"app.update.enabled\", false);");
                            tw.WriteLine("user_pref(\"app.update.lastUpdateTime.addon-background-update-timer\", 1171491940);");
                            tw.WriteLine("user_pref(\"app.update.lastUpdateTime.background-update-timer\", 1171491939);");
                            tw.WriteLine("user_pref(\"app.update.lastUpdateTime.blocklist-background-update-timer\", 1171491940);");
                            tw.WriteLine("user_pref(\"app.update.lastUpdateTime.search-engine-update-timer\", 1171491942);");
                            tw.WriteLine("user_pref(\"browser.shell.checkDefaultBrowser\", false);");
                            tw.WriteLine("user_pref(\"browser.sessionstore.enabled\", false);");
                            tw.WriteLine("user_pref(\"browser.startup.homepage_override.mstone\", \"rv:1.8.1.1\");");
                            tw.WriteLine("user_pref(\"browser.tabs.warnOnClose\", false);");
                            tw.WriteLine("user_pref(\"browser.search.selectedEngine\", \"Live Search\");");
                            tw.WriteLine("user_pref(\"extensions.lastAppVersion\", \"3.0.4\");");
                            tw.WriteLine("user_pref(\"intl.charsetmenu.browser.cache\", \"ISO-8859-1, UTF-8\");");
                            tw.WriteLine("user_pref(\"network.cookie.prefsMigrated\", true);");
                            tw.WriteLine("user_pref(\"security.warn_entering_secure\", false);");
                            tw.WriteLine("user_pref(\"urlclassifier.keyupdatetime.https://sb-ssl.google.com/safebrowsing/getkey?client=navclient-auto-ffox2.0.0.3&\", 1171578343);");
                            tw.WriteLine("user_pref(\"urlclassifier.tableversion.goog-black-enchash\", \"1.18519\");");
                            tw.WriteLine("user_pref(\"urlclassifier.tableversion.goog-black-url\", \"1.8691\");");
                            tw.WriteLine("user_pref(\"urlclassifier.tableversion.goog-white-domain\", \"1.19\");");
                            tw.WriteLine("user_pref(\"urlclassifier.tableversion.goog-white-url\", \"1.371\");");
                            tw.WriteLine("user_pref(\"security.warn_viewing_mixed\", false);");
                            break;
                        }

                    default:
                        throw new System.InvalidOperationException("Unimplemented version of FireFox prefs specified!");
                }
                tw.Close();
            }
        }

        public static void WriteFireFoxProfileIni(BrowserIdentifier version, Hashtable environmentVars)
        {
            Directory.CreateDirectory(GetMozillaAppDataFolder(version, environmentVars));
            Directory.CreateDirectory(GetMozillaAppDataFolder(version, environmentVars) + "\\FireFox");
            Directory.CreateDirectory(GetMozillaAppDataFolder(version, environmentVars) + "\\FireFox\\updates");
            Directory.CreateDirectory(GetMozillaAppDataFolder(version, environmentVars) + "\\FireFox\\Profiles");
            Directory.CreateDirectory(GetMozillaAppDataFolder(version, environmentVars) + "\\FireFox\\Profiles\\" + environmentVars["USERNAME"]);

            // This format is identical for at least versions 1.5 and 2.0.  If it changes in the future we need to reflect that.
            using (TextWriter tw = new StreamWriter(GetMozillaAppDataFolder(version, environmentVars) + "\\FireFox\\Profiles.ini"))
            {
                tw.WriteLine("[General]");
                tw.WriteLine("StartWithLastProfile=1");
                tw.WriteLine("[Profile0]");
                tw.WriteLine("Name=default");
                tw.WriteLine("IsRelative=1");
                tw.WriteLine("Default=1");
                tw.WriteLine("Path=Profiles/" + environmentVars["USERNAME"]);
                tw.Close();
            }
        }

        public static string FireFoxVersion
        {
            get
            {
                if (File.Exists(GetMozillaProgFilesFolder(BrowserIdentifier.FireFoxUnknown) + "\\firefox.exe"))
                {
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(GetMozillaProgFilesFolder(BrowserIdentifier.FireFoxUnknown) + "\\firefox.exe");
                    return fvi.ProductVersion;
                }
                return null;
            }
        }

        public static string GetMozillaProgFilesFolder(BrowserIdentifier version)
        {
            // FireFox is an x86-only program for Windows.  So, for x64 platforms,
            // We need to forcibly check in the x86 version of the program files folder.
            // Note: Currently no need to use browseridentifier here, it's only included for future releases.
            string x86ProgramFilesPath = Environment.GetEnvironmentVariable("ProgramFiles(x86)");

            if (!string.IsNullOrEmpty(x86ProgramFilesPath))
            {
                return x86ProgramFilesPath + "\\Mozilla Firefox";
            }
            else
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Mozilla Firefox";
            }
        }

        public static string GetMozillaAppDataFolder(BrowserIdentifier version, Hashtable currentEnvironment)
        {
            // Need to fix this to have multiple logic if it doesnt work on XP or between versions
            return currentEnvironment["APPDATA"] + "\\Mozilla";
        }

        public static void InvokeMozillaSilentUninstall(string args, BrowserIdentifier version, string uninstallExe, Hashtable environmentVariables)
        {
            // Need to fix this to have multiple logic if it doesnt work on XP or between versions
            string uninstallPath = GetMozillaProgFilesFolder(version) + "\\uninstall\\" + uninstallExe;

            if (!File.Exists(uninstallPath))
            {
                return;
            }

            // Kick off the uninstall ...
            //************************************************
            ProcessStartInfo uninstallProcessInfo = new ProcessStartInfo(uninstallPath, args);
            // Write in Environment variables from the user space
            // This is needed to fool the app into thinking it's running as the current user
            uninstallProcessInfo.EnvironmentVariables.Clear();
            foreach (string key in environmentVariables.Keys)
            {
                uninstallProcessInfo.EnvironmentVariables[key] = (string) environmentVariables[key];
            }

            uninstallProcessInfo.UseShellExecute = false;
            Process uninstallProc = Microsoft.Test.Diagnostics.ProcessHelper.CreateProcessAsFirstActiveSessionUser(uninstallProcessInfo, true);
            uninstallProc.WaitForExit();
        }
        #endregion
    }

    /// <summary>
    /// Third Party Browser Identification
    /// </summary>
    public enum BrowserIdentifier
    {
        /// <summary>
        /// Default state of machine, which will have IE6 or 7 depending on OS.
        /// No 3rd party browsers that BrowserState knows about were found.
        /// </summary>
        None = 0,
        /// <summary>
        /// FireFox 1.5
        /// </summary>
        FireFox15 = 1,
        /// <summary>
        /// FireFox 2.0.0.18, current legacy release
        /// Version won't need to change unless security patches are released.
        /// </summary>
        FireFox20 = 2,
        /// <summary>
        /// FireFox 3.0.X, current latest release 
        /// </summary>
        FireFox30 = 3,
        /// <summary>
        /// Machine has a previously undefined install of FireFox on it,
        /// which we will back up along with its AppData stuff into the %Temp% folder
        /// </summary>
        FireFoxUnknown = 4
    }
}
