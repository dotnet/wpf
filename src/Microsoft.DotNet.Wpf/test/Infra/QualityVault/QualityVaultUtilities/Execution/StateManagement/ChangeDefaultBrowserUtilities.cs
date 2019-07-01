// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;

namespace Microsoft.Test.Execution.StateManagement.Browser
{
    // For the purpose of this state API, we really only care about whether IE or FireFox is default.
    // Other will be an error state.
    internal enum DefaultWebBrowser
    {
        InternetExplorer,
        FireFox,
        Other
    }

    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    internal class ChangeDefaultBrowserUtilities
    {
        internal static DefaultWebBrowser CurrentDefaultWebBrowser
        {
            get
            {
                // StartMenuInternet is the most reliable check for the default browser... 
                string startMenuInternet = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Clients\StartmenuInternet", null, "iexplore.exe");
                // If it's not defined per user, check per machine...
                if (startMenuInternet == null)
                {
                    startMenuInternet = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Clients\StartmenuInternet", null, "iexplore.exe");
                }

                // Not always defined... 
                if (startMenuInternet != null)
                {
                    if (startMenuInternet.ToLowerInvariant() == "firefox.exe")
                    {
                        return DefaultWebBrowser.FireFox;
                    }
                    else if (startMenuInternet.ToLowerInvariant() == "iexplore.exe")
                    {
                        return DefaultWebBrowser.InternetExplorer;
                    }
                    else
                    {
                        return DefaultWebBrowser.Other;
                    }
                }
                else
                {
                    // Never hit this in testing, so I'd like to be notified with a fail when it happens...
                    throw new Exception("Error: Failed to perform check for default browser state!");
                }
            }
        }

        internal static Version cachedInternetExplorerVersion = null;
        internal static Version InternetExplorerVersion
        {
            get
            {
                if (cachedInternetExplorerVersion != null)
                {
                    return cachedInternetExplorerVersion;
                }
                // This path is constant in all languages / OSes thus far.
                else if (File.Exists(Environment.GetEnvironmentVariable("ProgramFiles") + @"\Internet Explorer\iexplore.exe"))
                {
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(Environment.GetEnvironmentVariable("ProgramFiles") + @"\Internet Explorer\iexplore.exe");
# if TESTBUILD_CLR40
                    try
                    {
                        cachedInternetExplorerVersion = Version.Parse(fvi.ProductVersion);
                    }                    
                    catch { };
#endif
#if TESTBUILD_CLR20
                    cachedInternetExplorerVersion = new Version(fvi.ProductMajorPart, fvi.ProductMinorPart);
#endif
                }
                return cachedInternetExplorerVersion;
            }
        }

        internal static Version cachedFireFoxVersion = null;
        internal static Version FireFoxVersion
        {
            get
            {
                if (cachedFireFoxVersion != null)
                {
                    return cachedFireFoxVersion;
                }
                else if (File.Exists(MozillaProgFilesFolder + "\\firefox.exe"))
                {
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(MozillaProgFilesFolder + "\\firefox.exe");
# if TESTBUILD_CLR40
                    try
                    {
                        cachedFireFoxVersion = Version.Parse(fvi.ProductVersion);
                    }                    
                    catch { };
#endif
#if TESTBUILD_CLR20
                    cachedFireFoxVersion = new Version(fvi.ProductMajorPart, fvi.ProductMinorPart);
#endif
                }
                return cachedFireFoxVersion;
            }
        }

        internal static string MozillaProgFilesFolder
        {
            get
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
        }

        internal static string MozillaAppDataFolder
        {
            get
            {
                return Environment.GetEnvironmentVariable("APPDATA") + "\\Mozilla";
            }
        }

        internal static void SetFireFoxAsDefaultBrowser(Version version)
        {
            if (!File.Exists(MozillaProgFilesFolder + "\\firefox.exe"))
            {
                throw new InvalidOperationException("Can't set FireFox as default browser if it's not installed!");
            }

            // This part makes it the default browser:
            SetFireFoxAsDefaultBrowserInRegistry(version);
            // And these handle all "user's first run" and first-security dialog parts:
            WriteFireFoxProfileIni(version);
            WriteFireFoxPrefsFile(version);
        }

        internal static void TryDeleteSubKeyTree(RegistryKey key, string subkey)
        {
            try
            {
                key.DeleteSubKeyTree(subkey);
            }
            catch { }
        }

        internal static void SetFireFoxAsDefaultBrowserInRegistry(Version version)
        {
            string home = (string)Environment.GetEnvironmentVariable("HOMEDRIVE");
            string progFilesX86 = (string)Environment.GetEnvironmentVariable("ProgramFiles");

            // Needed because this is set by explorer, and will mess up shell execution when rolling back and forth between browsers.
            // This will get re-set as soon as any .xbap or .xaml is launched.            
            TryDeleteSubKeyTree(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.xbap");
            TryDeleteSubKeyTree(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.xaml");

            Console.WriteLine("Setting FireFox version: " + version.ToString());

            if (version.Major == 3)
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
                startInfo.UseShellExecute = false;
                Process fireFoxHelperProcess = Process.Start(startInfo);
                fireFoxHelperProcess.WaitForExit(20000);
            }
            // This is far messier but has been extensively tested and will not be used in common QV runs.
            else switch (Environment.OSVersion.Version.Major)
                {
                    case 5:
                        {
                            // X64 Platforms put their x86 apps (like FireFox) into a folder that is the 2nd Program Files folder.
                            // Normally this is Progra~2, but to avoid weird configurations (like a dirty drive that has been 
                            // migrated back and forth between Vista + XP64), use Win32 API to get the "real" short path.
                            string shortProgramFilesFullPath = ToShortPathName((string)Environment.GetEnvironmentVariable("ProgramFiles"));
                            string pfShortPath = shortProgramFilesFullPath.Substring(shortProgramFilesFullPath.LastIndexOf("\\") + 1);

                            if (Environment.GetEnvironmentVariable("ProgramFiles(x86)") != null)
                            {
                                shortProgramFilesFullPath = ToShortPathName((string)Environment.GetEnvironmentVariable("ProgramFiles(x86)"));
                                pfShortPath = shortProgramFilesFullPath.Substring(shortProgramFilesFullPath.LastIndexOf("\\") + 1);
                            }

                            switch (version.Major)
                            {
                                case 1:
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

                                case 2:
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

        internal static void SetIEAsDefaultBrowserInRegistry()
        {
            // COM Interface version is much more reliable but only available on Vista+
            if (Environment.OSVersion.Version.Major >= 6)
            {
                var iAppAssoc = CoCreateInstance<IApplicationAssociationRegistration>(CLSID_ApplicationAssociationRegistration);
                iAppAssoc.SetAppAsDefaultAll("Internet Explorer");
            }
            else
            {
                SetIEAsDefaultBrowserInRegistry_PreVista();
            }
        }

        /// <summary>
        /// Messier (but very tested) version for XP / Server, but works reliably there due to no UAC / Trusted Installer issues.
        /// </summary>
        internal static void SetIEAsDefaultBrowserInRegistry_PreVista()
        {
            // Needed because this is set by explorer, and will mess up shell execution when rolling back and forth between browsers.
            // This will get re-set as soon as any .xbap or .xaml is launched.
            TryDeleteSubKeyTree(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.xbap");
            TryDeleteSubKeyTree(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.xaml");

            string home = (string)Environment.GetEnvironmentVariable("HOMEDRIVE");
            string progFilesX86 = (string)Environment.GetEnvironmentVariable("ProgramFiles");
            string progFilesWOW64 = (string)Environment.GetEnvironmentVariable("ProgramFiles(x86)");

            // XP can have either IE6 or 7.
            switch (InternetExplorerVersion.Major)
            {
                case 6:
                    {
                        // Need to set these specially after others since the normal setting writes "wrong" values in WOW scenarios.
                        // On XP, we always want to run 32 bit IE + 32 bit presentationhost.
                        if (Environment.GetEnvironmentVariable("ProgramFiles(x86)") != null)
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
                        if (Environment.GetEnvironmentVariable("ProgramFiles(x86)") != null)
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
                        SetRegValue(@"HKEY_CLASSES_ROOT\gopher\shell\open\ddeexec", "NoActivateHandler", "", RegistryValueKind.String);
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
                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ftp\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.String);
                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ftp\shell\open\ddeexec", "NoActivateHandler", "", RegistryValueKind.String);
                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ftp\shell\open\ddeexec\Application", string.Empty, "IExplore", RegistryValueKind.String);
                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ftp\shell\open\ddeexec\ifExec", string.Empty, "*", RegistryValueKind.String);
                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ftp\shell\open\ddeexec\Topic", string.Empty, "WWW_OpenURL", RegistryValueKind.String);
                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\gopher\shell\open\command", string.Empty, "\"" + progFilesX86 + "\\Internet Explorer\\iexplore.exe\" -nohome", RegistryValueKind.String);
                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\gopher\shell\open\ddeexec", string.Empty, "\"%1\",,-1,0,,,,", RegistryValueKind.String);
                        SetRegValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\gopher\shell\open\ddeexec", "NoActivateHandler", "", RegistryValueKind.String);
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
        }

        internal static void DeleteAndSetRegValue(string path, string keyName, object value, RegistryValueKind kind)
        {
            SetRegValue(path, keyName, null, kind);
            SetRegValue(path, keyName, value, kind);
        }

        internal static void SetRegValue(string path, string keyName, object value, RegistryValueKind kind)
        {
            try
            {
                Registry.SetValue(path, keyName, value, kind);
            }
            catch
            {
                // Do nothing?  I'd like to log this.                
            }
        }

        internal static void WriteFireFoxPrefsFile(Version version)
        {
            using (TextWriter tw = new StreamWriter(MozillaAppDataFolder + "\\FireFox\\Profiles\\" + Environment.GetEnvironmentVariable("USERNAME") + "\\user.js"))
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

                switch (version.Major)
                {
                    case 1:
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
                    case 2:
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
                    case 3:
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

        internal static void WriteFireFoxProfileIni(Version version)
        {
            Directory.CreateDirectory(MozillaAppDataFolder);
            Directory.CreateDirectory(MozillaAppDataFolder + "\\FireFox");
            Directory.CreateDirectory(MozillaAppDataFolder + "\\FireFox\\updates");
            Directory.CreateDirectory(MozillaAppDataFolder + "\\FireFox\\Profiles");
            Directory.CreateDirectory(MozillaAppDataFolder + "\\FireFox\\Profiles\\" + Environment.GetEnvironmentVariable("USERNAME"));

            // This format is identical for at least versions 1.5 and 2.0.  If it changes in the future we need to reflect that.
            using (TextWriter tw = new StreamWriter(MozillaAppDataFolder + "\\FireFox\\Profiles.ini"))
            {
                tw.WriteLine("[General]");
                tw.WriteLine("StartWithLastProfile=1");
                tw.WriteLine("[Profile0]");
                tw.WriteLine("Name=default");
                tw.WriteLine("IsRelative=1");
                tw.WriteLine("Default=1");
                tw.WriteLine("Path=Profiles/" + Environment.GetEnvironmentVariable("USERNAME"));
                tw.Close();
            }
        }

        internal static void SetDefaultBrowserInRegistry(DefaultWebBrowser browser)
        {
            switch (browser)
            {
                case DefaultWebBrowser.InternetExplorer:
                    ChangeDefaultBrowserUtilities.SetIEAsDefaultBrowserInRegistry();
                    break;
                case DefaultWebBrowser.FireFox:
                    ChangeDefaultBrowserUtilities.SetFireFoxAsDefaultBrowser(ChangeDefaultBrowserUtilities.FireFoxVersion);
                    break;
                case DefaultWebBrowser.Other:
                    // Do nothing?
                    break;
            }
        } 

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

        #region Native Imports & COM Interfaces

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern uint GetShortPathName(
        [MarshalAs(UnmanagedType.LPTStr)]
        string lpszLongPath,
        [MarshalAs(UnmanagedType.LPTStr)]
        StringBuilder lpszShortPath,
        uint cchBuffer);


        /// <summary>ASSOCIATIONLEVEL, AL_*</summary>
        internal enum AL
        {
            MACHINE,
            EFFECTIVE,
            USER,
        }

        /// <summary>ASSOCIATIONTYPE, AT_*</summary>
        internal enum AT
        {
            FILEEXTENSION,
            URLPROTOCOL,
            STARTMENUCLIENT,
            MIMETYPE,
        }

        public static T CoCreateInstance<T>(string clsid)
        {
            return (T)System.Activator.CreateInstance(System.Type.GetTypeFromCLSID(new System.Guid(clsid)));
        }

        /// <summary>CLSID_ApplicationAssociationRegistration</summary>
        /// <remarks>IID_IApplicationAssociationRegistration</remarks>
        public const string CLSID_ApplicationAssociationRegistration = "591209c7-767b-42b2-9fba-44ee4615f2c7";

        // Application File Extension and URL Protocol Registration
        [
            ComImport,
            InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
            Guid("4e530b0a-e611-4c77-a3ac-9031d022281b"),
        ]
        internal interface IApplicationAssociationRegistration
        {
            [return: MarshalAs(UnmanagedType.LPWStr)]
            string QueryCurrentDefault(
                [MarshalAs(UnmanagedType.LPWStr)] string pszQuery,
                AT atQueryType,
                AL alQueryLevel);

            [return: MarshalAs(UnmanagedType.Bool)]
            bool QueryAppIsDefault(
                [MarshalAs(UnmanagedType.LPWStr)] string pszQuery,
                AT atQueryType,
                AL alQueryLevel,
                [MarshalAs(UnmanagedType.LPWStr)] string pszAppRegistryName);

            [return: MarshalAs(UnmanagedType.Bool)]
            bool QueryAppIsDefaultAll(
                AL alQueryLevel,
                [MarshalAs(UnmanagedType.LPWStr)] string pszAppRegistryName);

            void SetAppAsDefault(
                [MarshalAs(UnmanagedType.LPWStr)] string pszAppRegistryName,
                [MarshalAs(UnmanagedType.LPWStr)] string pszSet,
                AT atSetType);

            void SetAppAsDefaultAll([MarshalAs(UnmanagedType.LPWStr)] string pszAppRegistryName);

            void ClearUserAssociations();
        }

        #endregion
    }
}