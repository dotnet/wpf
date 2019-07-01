// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Windows;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Threading; 
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using Microsoft.Test.Logging;
using Microsoft.Test.Loaders;
using Microsoft.Test.Input;
using MTI = Microsoft.Test.Input;
using System.Security;
using System.Security.Policy;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Reflection;
using System.Windows.Automation;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Test.Diagnostics;

/***********************************************************
 * The logic in this file is maintained by the AppModel team
 * contact: mattgal
 ***********************************************************/

namespace Microsoft.Test.Loaders 
{

    /// <summary>
    /// Helper functions for handling ClickOnce deployment of WPF applications and browser interop.
    /// </summary>
    [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
    public static class ApplicationDeploymentHelper 
    {        
        #region Private Members
        const string STORE_FOLDER_NAME = "Apps"; 
        const string STATE_MANAGER_SUBKEY_NAME = @"Software\Microsoft\Windows\CurrentVersion\StateManager"; 
        const string SIDEBYSIDE_REGISTRY_KEY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\SideBySide";
        const string UNINSTALL_REGISTRY_KEY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        const string DEPLOYMENT_REGISTRY_KEY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Deployment";
        const int CLICKONCE_STORE_FORMAT_VERSION = 4;

        // used to cache these properties, as the lookup is nontrivial
        private static string errorPageTitle = null;
        private static string errorPageMoreInfo = null;
        private static string errorPageLessInfo = null;
        private static string errorPageRestartApp = null;
        private static string cancelPageUIButtonName = null;
        private static string openFileDialogTitle = null;
        private static string saveFileDialogTitle = null;
        #endregion

        #region Private Methods

        private static string getComDlgStringResource(int resourceId)
        {
            string resourceLoadPath = Environment.GetEnvironmentVariable("SystemRoot") + @"\system32\" + System.Globalization.CultureInfo.CurrentUICulture.Name + @"\comdlg32.dll.mui";
            if (!File.Exists(resourceLoadPath))
            {
                resourceLoadPath = Environment.GetEnvironmentVariable("SystemRoot") + @"\system32\en-us\comdlg32.dll.mui";
            }
            LogManager.LogMessageDangerously("Using ComDlg32 path : " + resourceLoadPath);
            IntPtr hMod = LoadLibraryEx(resourceLoadPath, IntPtr.Zero, Microsoft.Test.Win32.NativeConstants.LOAD_LIBRARY_AS_DATAFILE);
            StringBuilder sb = new StringBuilder();
            int ln = LoadString(hMod, resourceId, sb, 512); // Magic # = 512 byte buffer size. 
            LogManager.LogMessageDangerously("Loaded String from ComDlg32.dll: " + ((string)sb.ToString()));
            return sb.ToString();
        }

        static private void LogDebug(string s)
        {
            GlobalLog.LogDebug(s);
        }

        /// <summary>
        /// Gets a list of Process names that should be monitored for the specified process
        /// </summary>
        /// <param name="startInfo">Start info of the process that will be invoked</param>
        /// <returns>array of process names that should be monitored</returns>
        internal static ProcessMonitorInfo[] GetProcessesToMonitor(ProcessStartInfo startInfo) 
        {
            //Need to rethink whether this should be an explicit list of processes or implicitly assume any new processes
            List<ProcessMonitorInfo> list = new List<ProcessMonitorInfo>();
            list.Add(new ProcessMonitorInfo("dfsvc", ProcessLifetime.Unknown));
            list.Add(new ProcessMonitorInfo("PresentationStartup", ProcessLifetime.Unknown));
            list.Add(new ProcessMonitorInfo("PresentationHost", ProcessLifetime.Application));
            // When FireFox launches us, PresentationHost.exe is given an 8 character name.  Treat either scenario the same.
            list.Add(new ProcessMonitorInfo("Presen~1", ProcessLifetime.Application));
            // Needed for FireFox ClickOnce .application testing, since dfsvc is not keeping tests alive for that and there's no PresentationHost
            list.Add(new ProcessMonitorInfo("firefox", ProcessLifetime.Application));

            if (startInfo != null) 
            {
                string extension;
                // Try to create a URI to get the extension, avoiding cases with URL parameters.
                // Failing this, do it the old way.
                try
                {
                    extension = Path.GetExtension(new Uri(startInfo.FileName).AbsolutePath).ToLowerInvariant();
                }
                catch (System.UriFormatException)
                {
                    extension = Path.GetExtension(startInfo.FileName).ToLowerInvariant();
                }

                // Only register IE or FireFox for Application lifetime if this is hosted-in-browser content
                // If other browser support is added, they need to go here too.
                if ((extension == (ApplicationDeploymentHelper.BROWSER_APPLICATION_EXTENSION)) ||
                    (extension == (ApplicationDeploymentHelper.AVALON_MARKUP_EXTENSION)) ||
                    (extension == ".htm") ||
                    (extension == ".html"))                    
                {
                    list.Add(new ProcessMonitorInfo("iexplore", ProcessLifetime.Application));
                    list.Add(new ProcessMonitorInfo("firefox", ProcessLifetime.Application));
                }

                //HACK: For PD3 CLR the name of the application host is the name of the exe (assuming its the same as the .application)
                string ext = Path.GetExtension(startInfo.FileName).ToLowerInvariant();
                if (ext == ApplicationDeploymentHelper.STANDALONE_APPLICATION_EXTENSION || ext == ".exe")
                    list.Add(new ProcessMonitorInfo(Path.GetFileNameWithoutExtension(startInfo.FileName), ProcessLifetime.Application));
            }
            return list.ToArray();
        }

        /// <summary>
        ///  Returns true if hwnd is WPF content. (used for xbaps)
        /// </summary>
        /// <param name="eventhWnd">hwnd to check if it's WPF or not</param>
        internal static bool IsAvalonApplicationHwnd(IntPtr eventhWnd)
        {
            StringBuilder buf = new StringBuilder(255);
            Win32api.GetClassName(eventhWnd, buf, (uint)buf.Capacity);
            if (buf.ToString().Contains(BROWSER_APPLICATION_EXTENSION + "#"))
            {
                return true;
            }
            return false;
        }

        // Get File Path helpers
        // Since Vista has some bizarre security around "magic" file paths, these interop methods are needed.
        private const int MAX_PATH = 260;
        private const int MAX_ALTERNATE = 14;

        [StructLayout(LayoutKind.Sequential)]
        private struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_DATA
        {
            public FileAttributes dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public int nFileSizeHigh;
            public int nFileSizeLow;
            public int dwReserved0;
            public int dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_ALTERNATE)]
            public string cAlternate;
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll")]
        private static extern bool FindClose(IntPtr hFindFile);

        private static string SearchDirectory(string directory, int level, string fileToSearchFor)
        {
            IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
            WIN32_FIND_DATA findData;
            IntPtr findHandle;
            findHandle = FindFirstFile(@"\\?\" + directory + @"\*", out findData);

            if (findHandle != INVALID_HANDLE_VALUE)
            {
                do
                {
                    if ((findData.dwFileAttributes & FileAttributes.Directory) != 0)
                    {

                        if (findData.cFileName != "." && findData.cFileName != "..")
                        {
                            // int subfiles, subfolders;
                            string subdirectory = directory + (directory.EndsWith(@"\") ? "" : @"\") + findData.cFileName;
                            if (level != 0)  // allows -1 to do complete search.
                            {
                                string result = SearchDirectory(subdirectory, level - 1, fileToSearchFor);
                                if (result != null)
                                {
                                    FindClose(findHandle);
                                    return result;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (findData.cFileName.ToLowerInvariant() == fileToSearchFor.ToLowerInvariant())
                        {
                            FindClose(findHandle);
                            return directory + "\\" + findData.cFileName;
                        }
                    }
                }
                while (FindNextFile(findHandle, out findData));
                FindClose(findHandle);
            }
            return null;
        }

        private static bool isAvalonAssembly(string asmName)
        {
            return (asmName.ToLowerInvariant().Equals("presentationframework") ||
                    asmName.ToLowerInvariant().Equals("uiautomationprovider") ||
                    asmName.ToLowerInvariant().Equals("uiautomationtypes") ||
                    asmName.ToLowerInvariant().Equals("windowsbase") ||
                    asmName.ToLowerInvariant().Equals("milcore") ||
                    asmName.ToLowerInvariant().Equals("presentationcore") ||
                    asmName.ToLowerInvariant().Equals("presentationframework") ||
                    asmName.ToLowerInvariant().Equals("presentationui") ||
                    asmName.ToLowerInvariant().Equals("uiautomationclient") ||
                    asmName.ToLowerInvariant().Equals("uiautomationclientsideproviders"));
        }

        private static string GetRandomString()
        {
            // get the magical "random string" used to find the app cache 
            RegistryKey key = Registry.CurrentUser.OpenSubKey(SIDEBYSIDE_REGISTRY_KEY_PATH);
            if (key == null)
                return null;
            string randomString = key.GetValue("ComponentStore_RandomString") as string;
            key.Close();
            return randomString;
        }

        private static void DeleteRegKey(string keyname)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyname))
            {
                if (key == null)
                    return;
            }
            Registry.CurrentUser.DeleteSubKeyTree(keyname);
        }
        
        #region Browser App ExceptionPage helper methods

        const string errorPageResourceName = "ERRORPAGE.HTML";
        const string IE7errorPageResourceName = "IE7ERRORPAGE.HTML";

        /// <summary>
        /// Returns an int representing the major version of IE installed on the machine
        /// </summary>
        /// <returns></returns>
        public static int GetIEVersion()
        {
            return (int) (new Version(SystemInformation.Current.IEVersion).Major);
        }

        private static string ExtractTextFromUnhandledExceptionPage(string matchPattern)
        {
            string resourceName = null;

            // set appropriate resource depending on IE version
            int IEVersion = GetIEVersion();
            switch (IEVersion)
            {
                case 6:
                    resourceName = errorPageResourceName;
                    break;
                case 7:
                case 8:
                    resourceName = IE7errorPageResourceName;
                    break;
                default:// to IE7
                    resourceName = IE7errorPageResourceName;
                    break;
            }

            // build file name

            string file = SystemInformation.Current.FrameworkWpfPath + CultureInfo.CurrentUICulture.ToString() + PresentationHostDllMui;

            // extract resource; 23 is the Resource type for HTML... 
            string page = GetResource(resourceName, file, 23);

            string[] matches = GetMatches(page, matchPattern);
            if (matches.Length != 1)
            {
                throw (new System.Exception("ExtractTextFromUnhandledExceptionPage - text not found or multiple ocurrences found"));
            }
            return matches[0];
        }

        // used to load string-based resources from unmanaged dlls
        private static string GetResource(string name, string libraryPath, int resourceType)
        {
            const int LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
            IntPtr _library = IntPtr.Zero;

            _library = LoadLibraryEx(libraryPath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);

            // find the resource
            IntPtr hresinfo = FindResource(_library, name, resourceType);
            if (hresinfo == IntPtr.Zero)
            {
                throw (new ArgumentException("name/resource Type was not found"));
            }

            // get resource size
            int size = SizeofResource(_library, hresinfo);

            // load it
            IntPtr h = LoadResource(_library, hresinfo);

            // get a pointer to its content in memory
            IntPtr pstr = LockResource(h);

            // create a string from that pointer - Assumes resources stored as Unicode
            String s = null;
            unsafe
            {
                // string is Unicode
                s = new string((char*)pstr, 0, size / sizeof(char));
            }
            return (s);
        }

        // Returns an array with the matches of the pattern in the text
        private static string[] GetMatches(string text, string pattern)
        {
            ArrayList list = new ArrayList();

            // compile the regular expression
            Regex r = new Regex(pattern, RegexOptions.IgnoreCase);

            // match the regular expression pattern against a text string
            Match m = r.Match(text);
            while (m.Success)
            {
                for (int i = 1; i < m.Length; i++)
                {
                    foreach (Capture c in m.Groups[i].Captures)
                    {
                        list.Add(c.Value);
                    }
                }
                m = m.NextMatch();
            }

            // return a string array
            object[] o = list.ToArray();
            string[] s = new string[list.Count];
            for (uint i = 0; i < o.Length; i++)
            {
                s[i] = (string)o[i];
            }
            return (s);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr FindResource(IntPtr mod, String sFilename, int type);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadResource(IntPtr mod, IntPtr hresinfo);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LockResource(IntPtr h);

        [DllImport("kernel32.dll")]
        private static extern int SizeofResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibraryEx(string fileName, IntPtr reservedHandle, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int LoadString(IntPtr hInstance, int uID, StringBuilder lpBuffer, int nBufferMax);

        #endregion

        #region Browser Progress Page Helper methods


        private static string ExtractTextFromBrowserProgressPage(string matchPattern)
        {
            string resourceName = "ProgressPage.html";

            // build file name
            string pHostDllPath = Microsoft.Test.Diagnostics.SystemInformation.Current.FrameworkWpfPath + CultureInfo.CurrentUICulture.Name + ApplicationDeploymentHelper.PresentationHostDllMui;

            // extract resource; 23 is the Resource type for HTML... 
            string page = GetResource(resourceName, pHostDllPath, 23);

            string[] matches = GetMatches(page, matchPattern);
            if (matches.Length != 1)
            {
                throw (new System.Exception("ExtractTextFromBrowserProgressPage - text not found or multiple ocurrences found"));
            }
            return matches[0];
        }

        #endregion

        #endregion

        #region Public Members

        /// <summary>
        /// Extension for WPF Express Apps
        /// The extension will likely change.
        /// </summary>
        public const string BROWSER_APPLICATION_EXTENSION = ".xbap";

        /// <summary>
        /// Extension for WPF Stand Alone Apps
        /// </summary>
        public const string STANDALONE_APPLICATION_EXTENSION = ".application";

        /// <summary>
        /// Extension for loose WPF markup
        /// </summary>
        public const string AVALON_MARKUP_EXTENSION = ".xaml";

        /// <summary>
        /// Returns the correct title for the ComDlg32.dll Open File Dialog
        /// </summary>
        public static string OpenFileDialogTitle
        {
            get
            {
                if (openFileDialogTitle == null)
                {
                    try
                    {
                        // 370 = String resource ID for Open in ComDlg32
                        openFileDialogTitle = getComDlgStringResource(370).Replace("(&O)", "").Replace("&", "");
                    }
                    // Fall back to English.  This is good because the #1 reason we have to fall back is lack of LOC resources
                    catch
                    {
                        openFileDialogTitle = "Open";
                    }
                }
                return openFileDialogTitle;
            }
        }

        /// <summary>
        /// Returns the correct title for the ComDlg32.dll Save As File Dialog
        /// </summary>
        public static string SaveFileDialogTitle
        {
            get
            {
                if (saveFileDialogTitle == null)
                {
                    try
                    {
                        // 369 = String resource ID for Save As in ComDlg32
                        saveFileDialogTitle = getComDlgStringResource(369).Replace("(&S)", "").Replace("&", "");
                    }
                    // Fall back to English.  This is good because the #1 reason we have to fall back is lack of LOC resources
                    catch
                    {
                        saveFileDialogTitle = "Save As";
                    }
                }
                return saveFileDialogTitle;
            }
        }

        // Methods for extracting the localized text for the Browser-app exception page
        /// <summary>
        /// Returns the correct title for the XBAP error html page for current locale
        /// </summary>
        public static string ErrorPageTitle
        {
            get
            {
                if (errorPageTitle == null)
                {
                    try
                    {
                        errorPageTitle = ExtractTextFromUnhandledExceptionPage("\"title\">(.*?)</title>");
                    }
                    // Fall back to English.  This is good because the #1 reason we have to fall back is lack of LOC resources
                    catch
                    {
                        errorPageTitle = "An error occurred in the application you were using";
                    }
                }
                return errorPageTitle;
            }
        }

        /// <summary>
        /// Returns the correct string for the XBAP error html page "More Info Button" for current locale
        /// </summary>
        public static string ErrorPageMoreInfo
        {
            get
            {
                if (errorPageMoreInfo == null)
                {
                    try
                    {
                        errorPageMoreInfo = ExtractTextFromUnhandledExceptionPage(@"var L_MoreInfo_TEXT = ""(.*)""");
                    }
                    // Fall back to English.  This is good because the #1 reason we have to fall back is lack of LOC resources
                    catch
                    {
                        errorPageMoreInfo = "More Information";
                    }
                }
                return errorPageMoreInfo;
            }
        }

        /// <summary>
        /// Returns the correct string for the XBAP error html page "Less Info Button" for current locale
        /// </summary>
        public static string ErrorPageLessInfo
        {
            get
            {
                if (errorPageLessInfo == null)
                {
                    try
                    {
                        errorPageLessInfo = ExtractTextFromUnhandledExceptionPage(@"var L_LessInfo_TEXT = ""(.*)""");
                    }
                    // Fall back to English.  This is good because the #1 reason we have to fall back is lack of LOC resources
                    catch
                    {
                        errorPageLessInfo = "Less Information";
                    }
                }
                return errorPageLessInfo;
            }
        }

        /// <summary>
        /// Returns the correct string for the Xbap error html page "Restart" button for current locale
        /// </summary>
        public static string ErrorPageRestartApp
        {
            get
            {
                if (errorPageRestartApp == null)
                {
                    try
                    {
                        errorPageRestartApp = ExtractTextFromUnhandledExceptionPage(@"<a id=""Restart"" href="""" onclick=""event.returnValue=false"">(.*)</a>");
                    }
                    // Fall back to English.  This is good because the #1 reason we have to fall back is lack of LOC resources
                    catch
                    {
                        errorPageRestartApp = "Restart";
                    }
                }
                return errorPageRestartApp;
            }
        }

        /// <summary>
        /// Returns the correct string for the Xbap html progress page "Cancel" button for current locale
        /// </summary>
        public static string CancelPageUIButtonName
        {
            get
            {
                if (cancelPageUIButtonName == null)
                {
                    try
                    {
                        cancelPageUIButtonName = ExtractTextFromBrowserProgressPage(@"<button id=""cancelButton"" onclick=""OnCancel\(\)"">(.*)</button>");
                    }
                    // Fall back to English.  This is good because the #1 reason we have to fall back is lack of LOC resources
                    catch
                    {
                        cancelPageUIButtonName = "Cancel";
                    }
                }
                return cancelPageUIButtonName;
            }
        }

        /// <summary>
        /// Returns a string representing the WinSxS path in the user profile directory
        /// </summary>
        /// <returns></returns>
        public static string FusionCachePath
        {
            get
            {
                // get the magical "random string" used to find the app cache 
                string randomString = GetRandomString();

                if (randomString == null)
                    return null;

                // construct the path to the app cache
                string fusionCachePath = Environment.ExpandEnvironmentVariables("%userprofile%") + @"\Local Settings\Apps\" + randomString.Substring(0, 8) + "." + randomString.Substring(8, 3) + @"\" + randomString.Substring(11, 8) + "." + randomString.Substring(19, 3);
                if (!Directory.Exists(fusionCachePath))
                {
                    // Try again with Longhorn flavor...
                    fusionCachePath = Environment.ExpandEnvironmentVariables("%userprofile%") + @"\AppData\Apps\" + randomString.Substring(0, 8) + "." + randomString.Substring(8, 3) + @"\" + randomString.Substring(11, 8) + "." + randomString.Substring(19, 3);
                    // Make sure we fail nicely... return null if we can't return a real directory.
                    if (!Directory.Exists(fusionCachePath))
                    {
                        LogDebug("Warning... couldn't find the Fusion Cache! Test will likely fail");
                        return null;
                    }
                }
                return fusionCachePath;
            }
        }


        /// <summary>
        /// Returns a string containing the name of the proper PresentationHost MUI file, prepended with a backslash.
        /// </summary>
        /// <returns></returns>
        public static string PresentationHostDllMui
        {
            get
            {
                if(Environment.Version >= new Version(4,0))
                {
                    return @"\PresentationHost_v0400.dll.mui";
                }
                else
                {
                    return @"\PresentationHostDLL.dll.mui";
                }
            }
        }

        /// <summary>
        /// Gets first found path to the App Store for app in args.
        /// Returns null if none found.
        /// </summary>
        /// <param name="AppName">Name of App we're looking for</param>
        /// <returns>Path to app in cache</returns>
        public static string GetAppFusionPath(string AppName)
        {
            // Get the local store path where this .exe should show up
            string localStorePath = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            string newLocalStorePath = Path.Combine(localStorePath, "Apps");
            if (!Directory.Exists(newLocalStorePath))
            {
                newLocalStorePath = Path.Combine(localStorePath, @"Local\Apps\2.0");
            }

            // Find every dir in the app store...
            string[] directoriesToCheck = Directory.GetDirectories(newLocalStorePath, "*", SearchOption.AllDirectories);

            foreach (string path in directoriesToCheck)
            {
                string exeFile = path + @"\" + AppName + ".exe";
                if (File.Exists(exeFile))
                {
                    return path;
                }
            }
            return null;
        }
      
        /// <summary>
        /// Cleans the IE History cache
        /// </summary>
        public static void ClearIEHistory()
        {
            try
            {
                IUrlHistoryStg2 obj = (IUrlHistoryStg2)new UrlHistoryClass();
                obj.ClearHistory();
                Thread.Sleep(1000);
                obj.ClearHistory();
            }
            catch (System.NotImplementedException)
            {
                LogDebug("Unable to clear the IE History... ignoring exception so this can continue, but this may cause TC failure.");
            }
        }

        /// <summary>
        /// Finds the path (including file name) to the app shortcut for a shell-visible app.
        /// If the shortcut does not exist, returns null
        /// </summary>
        /// <param name="Application"></param>
        /// <returns></returns>
        public static string GetAppShortcutPath(string Application)
        {
            string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            return SearchDirectory(startMenuPath, -1, Application + ".appref-ms");
        }

        /// <summary>
        /// Finds the directory in which the app shortcut and support link are in for a shell-visible app.
        /// If the shortcut does not exist, returns null
        /// </summary>
        /// <param name="Application"></param>
        /// <returns></returns>
        public static string GetAppShortcutDirectory(string Application)
        {
            string path = GetAppShortcutPath(Application);
            if (path != null)
            {
                return Path.GetDirectoryName(path);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Adds a particular URL to IE's zone mappings.
        /// This completely short circuits IE's standard analysis of the URL
        /// Needed for testing as if content were coming from all different zones.
        /// Will remove this URL from other zones if it has already been added there.
        /// </summary>
        /// <param name="zone">Zone to place URL into</param>
        /// <param name="url">URL to add to zone</param>
        public static void AddUrlToZone(IEUrlZone zone, string url)
        {
            IEZoneSecurityManager sm = null;
            try
            {
                sm = new IEZoneSecurityManager();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to Create SecurityMgr", ex);
            }

            try
            {
                sm.SetZoneMapping(zone, url, IEZoneSecurityManagerUtils.SZM_FLAGS.SZM_CREATE);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to Add URL " + url + " to zone " + zone.ToString(), ex);
            }
        }

        /// <summary>
        /// Removes a particular URL from IE's zone mappings.
        /// </summary>
        /// <param name="zone">Zone to remove URL from </param>
        /// <param name="url">URL to remove from zone</param>
        public static void RemoveUrlFromZone(IEUrlZone zone, string url)
        {
            IEZoneSecurityManager sm = null;

            try
            {
                sm = new IEZoneSecurityManager();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to Create IEZoneSecurityManager!", ex);
            }

            try
            {
                sm.SetZoneMapping(zone, url, IEZoneSecurityManagerUtils.SZM_FLAGS.SZM_DELETE);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to Remove URL " + url + " from zone " + zone.ToString(), ex);
            }
        }
        #endregion
    }

    /// <summary>
    /// Possible security Zones that IE may map content into
    /// Numbers are not necessary but are used to be explicitly 
    /// sure that the int values are correct, since these values
    /// (0..4) are how IE classifies zones.
    /// </summary>
    public enum IEUrlZone : int
    {
        /// <summary>
        ///  Local Machine zone, fully trusted
        /// </summary>
        URLZONE_LOCAL_MACHINE = 0,
        /// <summary>
        /// Intranet zone
        /// </summary>
        URLZONE_INTRANET = URLZONE_LOCAL_MACHINE + 1,
        /// <summary>
        /// Trusted Sites (Elevated)
        /// </summary>
        URLZONE_TRUSTED = URLZONE_INTRANET + 1,
        /// <summary>
        /// Internet Zone
        /// </summary>
        URLZONE_INTERNET = URLZONE_TRUSTED + 1,
        /// <summary>
        /// Untrusted sites
        /// </summary>
        URLZONE_UNTRUSTED = URLZONE_INTERNET + 1,
    }

    // IE Zone Security helper class.
    // Lets us put any URL we want, including partials, into any zone we want.
    internal class IEZoneSecurityManager
    {
        // HResult value for no error.
        private const int S_OK   = 0;
        private const int E_FAIL = -2147467259;

        // Object for handling the interop calls we will make.
        IEZoneSecurityManagerUtils.IInternetSecurityManager ism = null;

        public IEZoneSecurityManager()
        {
            Type comType = null;
            object comObj = null;
            try
            {
                comType = Type.GetTypeFromCLSID(IEZoneSecurityManagerUtils.CLSID_IInternetSecurityManager);
                if (comType == null)
                {
                    throw new NotSupportedException("Type.GetTypeFromCLSID(SecurityMgrLib.CLSID_IInternetSecurityManager");
                }
            }
            catch (Exception)
            {
                GlobalLog.LogDebug("SecurityMgr: Type.GetTypeFromCLSID(SecurityMgrLib.CLSID_IInternetSecurityManager threw an exception");
            }
            try
            {
                comObj = Activator.CreateInstance(comType);
                ism = (IEZoneSecurityManagerUtils.IInternetSecurityManager)comObj;
                comObj = null;
            }
            catch (Exception ex)
            {
                GlobalLog.LogDebug("SecurityMgr: Failed Activator.CreateInstance" + ex.ToString());
                throw;
            }
        }

        public void SetZoneMapping(IEUrlZone zone, string pattern, IEZoneSecurityManagerUtils.SZM_FLAGS flag)
        {
            int hr; 

            //Clear them if they exist in any other zone.  This ensures there will only ever be one entry in one zone.
            if (flag != IEZoneSecurityManagerUtils.SZM_FLAGS.SZM_DELETE)
            {
                hr = (int)ism.SetZoneMapping((int)IEUrlZone.URLZONE_LOCAL_MACHINE, pattern, (int)IEZoneSecurityManagerUtils.SZM_FLAGS.SZM_DELETE);
                hr = (int)ism.SetZoneMapping((int)IEUrlZone.URLZONE_INTRANET, pattern, (int)IEZoneSecurityManagerUtils.SZM_FLAGS.SZM_DELETE);
                hr = (int)ism.SetZoneMapping((int)IEUrlZone.URLZONE_TRUSTED, pattern, (int)IEZoneSecurityManagerUtils.SZM_FLAGS.SZM_DELETE);
                hr = (int)ism.SetZoneMapping((int)IEUrlZone.URLZONE_INTERNET, pattern, (int)IEZoneSecurityManagerUtils.SZM_FLAGS.SZM_DELETE);
                hr = (int)ism.SetZoneMapping((int)IEUrlZone.URLZONE_UNTRUSTED, pattern, (int)IEZoneSecurityManagerUtils.SZM_FLAGS.SZM_DELETE);
            }

            //Use zone mapping API from urlmon.dll
            int flags = 0;
            if (zone == IEUrlZone.URLZONE_TRUSTED)
            {
                try
                {
                    flags = GetZoneAttributeFlags();
                }
                catch
                {
                    GlobalLog.LogDebug("Failed to Get Zone Attributes Flags");
                }

                try
                {
                    SetZoneAttributeFlags(0x43);  //No https verification required 
                }
                catch
                {
                    GlobalLog.LogDebug("Failed to Set Zone Attributes Flags");
                }
            }

            // Commenting this out but intentionally not deleting... On more recent builds, this is not actually needed on LH Server + Server 2k3.
            // Further, it has caused weird COM exceptions to be thrown on certain Server builds ("The File Exists"?)
            // Leaving in code since it may need to re-enable this for some subset of server builds (Was added on an "R2" run) 

            //// Deals with issue seen on some unusual Server configs..
            //// Without this flag, it won't work in the Enhanced Security configuration
            //// This isnt good... it's one of the reasons this workaround was introduced in the first place.
            //// See http://msdn2.microsoft.com/en-us/library/ms537181.aspx for more info.
            //if (SystemInformation.Current.IsServer)
            //{
            //    int enhancedSecurityZone = (int)zone | IEZoneSecurityManagerUtils.URLZONE_ESC_FLAG;
            //    hr = ism.SetZoneMapping(enhancedSecurityZone, pattern, (int)flag);
            //}
            //else
            //{
                hr = ism.SetZoneMapping((int)zone, pattern, (int)flag);
            //}
            if (hr == E_FAIL)
            {
                GlobalLog.LogDebug("SetZoneMapping Pattern already exists:  " + zone.ToString() + " pattern: " + pattern + " SecurityMgrLib.SZM_FLAGS: " + flag.ToString());
                if (zone == IEUrlZone.URLZONE_TRUSTED)
                {
                    SetZoneAttributeFlags(flags); // Reset to original mappings
                }
                return;
            }

            if (zone == IEUrlZone.URLZONE_TRUSTED)
            {
                // Reset to original mappings
                SetZoneAttributeFlags(flags); 
            }

            if (hr != S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            else
            {
                GlobalLog.LogDebug("SetZoneMapping succeeded for zone: " + zone.ToString() + " pattern: " + pattern + " SecurityMgrLib.SZM_FLAGS: " + flag.ToString());
            }
        }

        public IEnumString GetZoneMapping(IEUrlZone zone)
        {
            IEnumString enumStr;

            int hr = ism.GetZoneMappings((int)zone, out enumStr, 0);
            if (hr != S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            else
            {
                String[] url = new String[1];
                System.IntPtr fetchted = System.IntPtr.Zero;
                while ((hr = enumStr.Next(1, url, fetchted)) == S_OK)
                {
                    GlobalLog.LogDebug("Enumerating zone mappings for : " + zone.ToString());
                    foreach (string u in url)
                    {
                        GlobalLog.LogDebug(u);
                    }
                }
            }
            return enumStr;
        }

        /// <summary>
        /// Methods to allow skip verification skipping. 
        /// </summary>
        public static int GetZoneAttributeFlags()
        {
            int flags;
            RegistryKey rkMachine = Registry.CurrentUser;
            RegistryKey rkZones;
            string strZones = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\2";
            string strMappingFlags = @"Flags";
            object value = null;

            try
            {
                rkZones = rkMachine.OpenSubKey(strZones, true);
            }
            catch (Exception ex)
            {
                GlobalLog.LogDebug("Failed to Open Subkey" + strMappingFlags);
                GlobalLog.LogDebug(ex.ToString());
                throw;
            }

            try
            {
                value = rkZones.GetValue(strMappingFlags);
                flags = (int)value;
            }
            catch (Exception ex)
            {
                GlobalLog.LogDebug("Failed to get registry value " + strMappingFlags);
                GlobalLog.LogDebug(ex.ToString());
                throw;
            }
            return flags;
        }

        public static void SetZoneAttributeFlags(int value) //skip https verification
        {
            RegistryKey rkMachine = Registry.CurrentUser;
            RegistryKey rkZones;
            string strZones = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\2";
            string strMappingFlags = @"Flags";
            object obj;

            obj = value;

            try
            {
                rkZones = rkMachine.OpenSubKey(strZones, true);
            }
            catch (Exception ex)
            {
                GlobalLog.LogDebug("Failed to Open Subkey" + strMappingFlags);
                GlobalLog.LogDebug(ex.ToString());
                throw;
            }

            try
            {
                rkZones.SetValue(strMappingFlags, obj);
            }
            catch (Exception ex)
            {
                GlobalLog.LogDebug("Failed to set registry value " + strMappingFlags + " to " + value);
                GlobalLog.LogDebug(ex.ToString());
                throw;
            }
        }

        private class ZoneUrlPair
        {
            public IEUrlZone _zone;
            public string _url;

            public ZoneUrlPair(string url, IEUrlZone zone)
            {
                _url = url;
                _zone = zone;
            }
        }
    }
    // Helper methods for IE Zone Security code.
    internal class IEZoneSecurityManagerUtils
    {
        public const int S_OK = unchecked((int)0x00000000);
        public const int S_FALSE = unchecked((int)0x00000001);
        public const int E_NOINTERFACE = unchecked((int)0x80004002);
        public const int INET_E_DEFAULT_ACTION = unchecked((int)0x800C0011);

        public static Guid CLSID_IInternetSecurityManager = new Guid("7b8a2d94-0ac9-11d1-896c-00c04fb6bfc4");
        public static Guid IID_IProfferService = new Guid("cb728b20-f786-11ce-92ad-00aa00a74cd0");
        public static Guid SID_SProfferService = new Guid("cb728b20-f786-11ce-92ad-00aa00a74cd0");
        public static Guid IID_IInternetSecurityManager = new Guid("79eac9ee-baf9-11ce-8c82-00aa004ba90b");

        [ComImport,
            GuidAttribute("6d5140c1-7436-11ce-8034-00aa006009fa"),
            InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown),
            ComVisible(false)]
        public interface IServiceProvider
        {
            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int QueryService(ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppvObject);
        }

        [ComImport,
            GuidAttribute("cb728b20-f786-11ce-92ad-00aa00a74cd0"),
            InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown),
            ComVisible(false)]
        public interface IProfferService
        {
            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int ProfferService(ref Guid guidService, IEZoneSecurityManagerUtils.IServiceProviderForIInternetSecurityManager psp, ref int cookie);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int RevokeService(int cookie);
        }

        [ComImport,
            GuidAttribute("6d5140c1-7436-11ce-8034-00aa006009fa"),
            InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown),
            ComVisible(false)]
        public interface IServiceProviderForIInternetSecurityManager
        {
            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int QueryService(ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IEZoneSecurityManagerUtils.IInternetSecurityManager ppvObject);
        }

        [ComImport,
            GuidAttribute("79eac9ed-baf9-11ce-8c82-00aa004ba90b"),
            InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown),
            ComVisible(false)]
        public interface IInternetSecurityMgrSite
        {
            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int GetWindow(out IntPtr hwnd);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int EnableModeless([In, MarshalAs(UnmanagedType.Bool)] Boolean fEnable);
        }

        public enum SZM_FLAGS
        {
            SZM_CREATE = 0,
            SZM_DELETE = SZM_CREATE + 1
        }

        public const int URLACTION_MIN = unchecked((int)0x00001000);

        public const int URLACTION_DOWNLOAD_MIN = unchecked((int)0x00001000);
        public const int URLACTION_DOWNLOAD_SIGNED_ACTIVEX = unchecked((int)0x00001001);
        public const int URLACTION_DOWNLOAD_UNSIGNED_ACTIVEX = unchecked((int)0x00001004);
        public const int URLACTION_DOWNLOAD_CURR_MAX = unchecked((int)0x00001004);
        public const int URLACTION_DOWNLOAD_MAX = unchecked((int)0x000011FF);

        public const int URLACTION_ACTIVEX_MIN = unchecked((int)0x00001200);
        public const int URLACTION_ACTIVEX_RUN = unchecked((int)0x00001200);
        public const int URLPOLICY_ACTIVEX_CHECK_LIST = unchecked((int)0x00010000);
        public const int URLACTION_ACTIVEX_OVERRIDE_OBJECT_SAFETY = unchecked((int)0x00001201);
        public const int URLACTION_ACTIVEX_OVERRIDE_DATA_SAFETY = unchecked((int)0x00001202);
        public const int URLACTION_ACTIVEX_OVERRIDE_SCRIPT_SAFETY = unchecked((int)0x00001203);
        public const int URLACTION_SCRIPT_OVERRIDE_SAFETY = unchecked((int)0x00001401);
        public const int URLACTION_ACTIVEX_CONFIRM_NOOBJECTSAFETY = unchecked((int)0x00001204);
        public const int URLACTION_ACTIVEX_TREATASUNTRUSTED = unchecked((int)0x00001205);
        public const int URLACTION_ACTIVEX_NO_WEBOC_SCRIPT = unchecked((int)0x00001206);
        public const int URLACTION_ACTIVEX_CURR_MAX = unchecked((int)0x00001206);
        public const int URLACTION_ACTIVEX_MAX = unchecked((int)0x000013ff);

        public const int URLACTION_SCRIPT_MIN = unchecked((int)0x00001400);
        public const int URLACTION_SCRIPT_RUN = unchecked((int)0x00001400);
        public const int URLACTION_SCRIPT_JAVA_USE = unchecked((int)0x00001402);
        public const int URLACTION_SCRIPT_SAFE_ACTIVEX = unchecked((int)0x00001405);
        public const int URLACTION_CROSS_DOMAIN_DATA = unchecked((int)0x00001406);
        public const int URLACTION_SCRIPT_PASTE = unchecked((int)0x00001407);
        public const int URLACTION_SCRIPT_CURR_MAX = unchecked((int)0x00001407);
        public const int URLACTION_SCRIPT_MAX = unchecked((int)0x000015ff);

        public const int URLACTION_HTML_MIN = unchecked((int)0x00001600);
        public const int URLACTION_HTML_SUBMIT_FORMS = unchecked((int)0x00001601); // aggregate next two
        public const int URLACTION_HTML_SUBMIT_FORMS_FROM = unchecked((int)0x00001602); //
        public const int URLACTION_HTML_SUBMIT_FORMS_TO = unchecked((int)0x00001603); //
        public const int URLACTION_HTML_FONT_DOWNLOAD = unchecked((int)0x00001604);
        public const int URLACTION_HTML_JAVA_RUN = unchecked((int)0x00001605); // derive from Java custom policy
        public const int URLACTION_HTML_USERDATA_SAVE = unchecked((int)0x00001606);
        public const int URLACTION_HTML_SUBFRAME_NAVIGATE = unchecked((int)0x00001607);
        public const int URLACTION_HTML_META_REFRESH = unchecked((int)0x00001608);
        public const int URLACTION_HTML_MIXED_CONTENT = unchecked((int)0x00001609);
        public const int URLACTION_HTML_MAX = unchecked((int)0x000017ff);

        public const int URLACTION_SHELL_MIN = unchecked((int)0x00001800);
        public const int URLACTION_SHELL_INSTALL_DTITEMS = unchecked((int)0x00001800);
        public const int URLACTION_SHELL_MOVE_OR_COPY = unchecked((int)0x00001802);
        public const int URLACTION_SHELL_FILE_DOWNLOAD = unchecked((int)0x00001803);
        public const int URLACTION_SHELL_VERB = unchecked((int)0x00001804);
        public const int URLACTION_SHELL_WEBVIEW_VERB = unchecked((int)0x00001805);
        public const int URLACTION_SHELL_SHELLEXECUTE = unchecked((int)0x00001806);
        public const int URLACTION_SHELL_CURR_MAX = unchecked((int)0x00001806);
        public const int URLACTION_SHELL_MAX = unchecked((int)0x000019ff);

        public const int URLACTION_NETWORK_MIN = unchecked((int)0x00001A00);

        public const int URLACTION_CREDENTIALS_USE = unchecked((int)0x00001A00);
        public const int URLPOLICY_CREDENTIALS_SILENT_LOGON_OK = unchecked((int)0x00000000);
        public const int URLPOLICY_CREDENTIALS_MUST_PROMPT_USER = unchecked((int)0x00010000);
        public const int URLPOLICY_CREDENTIALS_CONDITIONAL_PROMPT = unchecked((int)0x00020000);
        public const int URLPOLICY_CREDENTIALS_ANONYMOUS_ONLY = unchecked((int)0x00030000);

        public const int URLACTION_AUTHENTICATE_CLIENT = unchecked((int)0x00001A01);
        public const int URLPOLICY_AUTHENTICATE_CLEARTEXT_OK = unchecked((int)0x00000000);
        public const int URLPOLICY_AUTHENTICATE_CHALLENGE_RESPONSE = unchecked((int)0x00010000);
        public const int URLPOLICY_AUTHENTICATE_MUTUAL_ONLY = unchecked((int)0x00030000);

        public const int URLACTION_COOKIES = unchecked((int)0x00001A02);
        public const int URLACTION_COOKIES_SESSION = unchecked((int)0x00001A03);
        public const int URLACTION_CLIENT_CERT_PROMPT = unchecked((int)0x00001A04);
        public const int URLACTION_COOKIES_THIRD_PARTY = unchecked((int)0x00001A05);
        public const int URLACTION_COOKIES_SESSION_THIRD_PARTY = unchecked((int)0x00001A06);
        public const int URLACTION_COOKIES_ENABLED = unchecked((int)0x00001A10);

        public const int URLACTION_NETWORK_CURR_MAX = unchecked((int)0x00001A10);
        public const int URLACTION_NETWORK_MAX = unchecked((int)0x00001Bff);

        public const int URLACTION_JAVA_MIN = unchecked((int)0x00001C00);
        public const int URLACTION_JAVA_PERMISSIONS = unchecked((int)0x00001C00);
        public const int URLPOLICY_JAVA_PROHIBIT = unchecked((int)0x00000000);
        public const int URLPOLICY_JAVA_HIGH = unchecked((int)0x00010000);
        public const int URLPOLICY_JAVA_MEDIUM = unchecked((int)0x00020000);
        public const int URLPOLICY_JAVA_LOW = unchecked((int)0x00030000);
        public const int URLPOLICY_JAVA_CUSTOM = unchecked((int)0x00800000);
        public const int URLACTION_JAVA_CURR_MAX = unchecked((int)0x00001C00);
        public const int URLACTION_JAVA_MAX = unchecked((int)0x00001Cff);

        // The following Infodelivery actions should have no default policies
        // in the registry.  They assume that no default policy means fall
        // back to the global restriction.  If an admin sets a policy per
        // zone, then it overrides the global restriction.

        public const int URLACTION_INFODELIVERY_MIN = unchecked((int)0x00001D00);
        public const int URLACTION_INFODELIVERY_NO_ADDING_CHANNELS = unchecked((int)0x00001D00);
        public const int URLACTION_INFODELIVERY_NO_EDITING_CHANNELS = unchecked((int)0x00001D01);
        public const int URLACTION_INFODELIVERY_NO_REMOVING_CHANNELS = unchecked((int)0x00001D02);
        public const int URLACTION_INFODELIVERY_NO_ADDING_SUBSCRIPTIONS = unchecked((int)0x00001D03);
        public const int URLACTION_INFODELIVERY_NO_EDITING_SUBSCRIPTIONS = unchecked((int)0x00001D04);
        public const int URLACTION_INFODELIVERY_NO_REMOVING_SUBSCRIPTIONS = unchecked((int)0x00001D05);
        public const int URLACTION_INFODELIVERY_NO_CHANNEL_LOGGING = unchecked((int)0x00001D06);
        public const int URLACTION_INFODELIVERY_CURR_MAX = unchecked((int)0x00001D06);
        public const int URLACTION_INFODELIVERY_MAX = unchecked((int)0x00001Dff);
        public const int URLACTION_CHANNEL_SOFTDIST_MIN = unchecked((int)0x00001E00);
        public const int URLACTION_CHANNEL_SOFTDIST_PERMISSIONS = unchecked((int)0x00001E05);
        public const int URLPOLICY_CHANNEL_SOFTDIST_PROHIBIT = unchecked((int)0x00010000);
        public const int URLPOLICY_CHANNEL_SOFTDIST_PRECACHE = unchecked((int)0x00020000);
        public const int URLPOLICY_CHANNEL_SOFTDIST_AUTOINSTALL = unchecked((int)0x00030000);
        public const int URLACTION_CHANNEL_SOFTDIST_MAX = unchecked((int)0x00001Eff);

        // For each action specified above the system maintains
        // a set of policies for the action. 
        // The only policies supported currently are permissions (i.e. is something allowed)
        // and logging status. 
        // IMPORTANT: If you are defining your own policies don't overload the meaning of the
        // loword of the policy. You can use the hiword to store any policy bits which are only
        // meaningful to your action.
        // For an example of how to do this look at the URLPOLICY_JAVA above

        // Permissions 
        public const int URLPOLICY_ALLOW = unchecked((int)0x00);
        public const int URLPOLICY_QUERY = unchecked((int)0x01);
        public const int URLPOLICY_DISALLOW = unchecked((int)0x03);

        // Notifications are not done when user already queried.
        public const int URLPOLICY_NOTIFY_ON_ALLOW = unchecked((int)0x10);
        public const int URLPOLICY_NOTIFY_ON_DISALLOW = unchecked((int)0x20);

        // Logging is done regardless of whether user was queried.
        public const int URLPOLICY_LOG_ON_ALLOW = unchecked((int)0x40);
        public const int URLPOLICY_LOG_ON_DISALLOW = unchecked((int)0x80);

        public const int URLPOLICY_MASK_PERMISSIONS = unchecked((int)0x0f);

        public const int URLPOLICY_DONTCHECKDLGBOX = unchecked((int)0x100);

        public const int URLZONE_ESC_FLAG = unchecked((int)0x100);

        [ComImport, GuidAttribute("79eac9ee-baf9-11ce-8c82-00aa004ba90b"),
            InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown),
            ComVisible(false)]
        public interface IInternetSecurityManager
        {
            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int SetSecuritySite([In] IInternetSecurityMgrSite pSite);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int GetSecuritySite([Out] IInternetSecurityMgrSite pSite);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int MapUrlToZone([In, MarshalAs(UnmanagedType.LPWStr)] String pwszUrl, out int pdwZone, int dwFlags);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int GetSecurityId([MarshalAs(UnmanagedType.LPWStr)] string pwszUrl, [MarshalAs(UnmanagedType.LPArray)] byte[] pbSecurityId, ref uint pcbSecurityId, uint dwReserved);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int ProcessUrlAction([In, MarshalAs(UnmanagedType.LPWStr)] String pwszUrl, int dwAction, out byte pPolicy, int cbPolicy, byte pContext, int cbContext, int dwFlags, int dwReserved);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int QueryCustomPolicy([In, MarshalAs(UnmanagedType.LPWStr)] String pwszUrl, ref Guid guidKey, byte ppPolicy, int pcbPolicy, byte pContext, int cbContext, int dwReserved);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int SetZoneMapping(int dwZone, [In, MarshalAs(UnmanagedType.LPWStr)] String lpszPattern, int dwFlags);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int GetZoneMappings(int dwZone, out IEnumString ppenumString, int dwFlags);
        }
    }

    internal enum ProcessLifetime 
    {
        Unknown,
        Application
    }

    internal struct ProcessMonitorInfo 
    {
        public string Name;
        public ProcessLifetime Lifetime;

        public ProcessMonitorInfo(string name, ProcessLifetime lifetime) 
        {
            Name = name;
            Lifetime = lifetime;
        }
    }

    #region Imports for Clearing IE History
    /// <summary>
    /// Used by QueryUrl method
    /// </summary>
    internal enum STATURL_QUERYFLAGS : uint
    {
        /// <summary>
        /// The specified URL is in the content cache.
        /// </summary>
        STATURL_QUERYFLAG_ISCACHED = 0x00010000,
        /// <summary>
        /// Space for the URL is not allocated when querying for STATURL.
        /// </summary>
        STATURL_QUERYFLAG_NOURL = 0x00020000,
        /// <summary>
        /// Space for the Web page's title is not allocated when querying for STATURL.
        /// </summary>
        STATURL_QUERYFLAG_NOTITLE = 0x00040000,
        /// <summary>
        /// //The item is a top-level item.
        /// </summary>
        STATURL_QUERYFLAG_TOPLEVEL = 0x00080000,

    }
    /// <summary>
    /// Flag on the dwFlags parameter of the STATURL structure, used by the SetFilter method.
    /// </summary>
    internal enum STATURLFLAGS : uint
    {
        /// <summary>
        /// Flag on the dwFlags parameter of the STATURL structure indicating that the item is in the cache.
        /// </summary>
        STATURLFLAG_ISCACHED = 0x00000001,
        /// <summary>
        /// Flag on the dwFlags parameter of the STATURL structure indicating that the item is a top-level item.
        /// </summary>
        STATURLFLAG_ISTOPLEVEL = 0x00000002,
    }
    /// <summary>
    /// Used bu the AddHistoryEntry method.
    /// </summary>
    internal enum ADDURL_FLAG : uint
    {
        /// <summary>
        /// Write to both the visited links and the dated containers. 
        /// </summary>
        ADDURL_ADDTOHISTORYANDCACHE = 0,
        /// <summary>
        /// Write to only the visited links container.
        /// </summary>
        ADDURL_ADDTOCACHE = 1
    }


    /// <summary>
    /// The structure that contains statistics about a URL. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct STATURL
    {
        /// <summary>
        /// Struct size
        /// </summary>
        internal int cbSize;
        /// <summary>
        /// URL
        /// </summary>                                                                   
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string pwcsUrl;
        /// <summary>
        /// Page title
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string pwcsTitle;
        /// <summary>
        /// Last visited date (UTC)
        /// </summary>
        internal System.Runtime.InteropServices.ComTypes.FILETIME ftLastVisited;
        /// <summary>
        /// Last updated date (UTC)
        /// </summary>
        internal System.Runtime.InteropServices.ComTypes.FILETIME ftLastUpdated;
        /// <summary>
        /// The expiry date of the Web page's content (UTC)
        /// </summary>
        internal System.Runtime.InteropServices.ComTypes.FILETIME ftExpires;
        /// <summary>
        /// Flags. STATURLFLAGS Enumaration.
        /// </summary>
        internal STATURLFLAGS dwFlags;

        /// <summary>
        /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
        /// </summary>
        internal string URL
        {
            get { return pwcsUrl; }
        }
        /// <summary>
        /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
        /// </summary>
        internal string Title
        {
            get
            {
                if (pwcsUrl.StartsWith("file:"))
                    return Win32api.CannonializeURL(pwcsUrl, Win32api.shlwapi_URL.URL_UNESCAPE).Substring(8).Replace('/', '\\');
                else
                    return pwcsTitle;
            }
        }
        /// <summary>
        /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
        /// </summary>
        internal DateTime LastVisited
        {
            get
            {
                return Win32api.FileTimeToDateTime(ftLastVisited).ToLocalTime();
            }
        }
        /// <summary>
        /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
        /// </summary>
        internal DateTime LastUpdated
        {
            get
            {
                return Win32api.FileTimeToDateTime(ftLastUpdated).ToLocalTime();
            }
        }
        /// <summary>
        /// sets a column header in the DataGrid control. This property is not needed if you do not use it.
        /// </summary>
        internal DateTime Expires
        {
            get
            {
                try
                {
                    return Win32api.FileTimeToDateTime(ftExpires).ToLocalTime();
                }
                catch (Exception e)
                {
                    if (e is System.NullReferenceException || e is System.Runtime.InteropServices.SEHException)
                    {
                        throw;
                    }
                    else
                    {
                        return DateTime.Now;
                    }
                }
            }
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct UUID
    {
        internal int Data1;
        internal short Data2;
        internal short Data3;
        internal byte[] Data4;
    }

    //Enumerates the cached URLs
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("3C374A42-BAE4-11CF-BF7D-00AA006946EE")]
    internal interface IEnumSTATURL
    {
        void Next(int celt, ref STATURL rgelt, out int pceltFetched);	//Returns the next \"celt\" URLS from the cache
        void Skip(int celt);	//Skips the next \"celt\" URLS from the cache. doed not work.
        void Reset();	//Resets the enumeration
        void Clone(out IEnumSTATURL ppenum);	//Clones this object
        void SetFilter([MarshalAs(UnmanagedType.LPWStr)] string poszFilter, STATURLFLAGS dwFlags);	//Sets the enumeration filter

    }


    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("3C374A41-BAE4-11CF-BF7D-00AA006946EE")]
    internal interface IUrlHistoryStg
    {
        void AddUrl(string pocsUrl, string pocsTitle, ADDURL_FLAG dwFlags);	//Adds a new history entry
        void DeleteUrl(string pocsUrl, int dwFlags);	//Deletes an entry by its URL. does not work!
        void QueryUrl([MarshalAs(UnmanagedType.LPWStr)] string pocsUrl, STATURL_QUERYFLAGS dwFlags, ref STATURL lpSTATURL);	//Returns a STATURL for a given URL
        void BindToObject([In] string pocsUrl, [In] UUID riid, IntPtr ppvOut); //Binds to an object. does not work!
        object EnumUrls { [return: MarshalAs(UnmanagedType.IUnknown)] get;}	//Returns an enumerator for URLs


    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("AFA0DC11-C313-11D0-831A-00C04FD5AE38")]
    internal interface IUrlHistoryStg2 : IUrlHistoryStg
    {
        new void AddUrl(string pocsUrl, string pocsTitle, ADDURL_FLAG dwFlags);	//Adds a new history entry
        new void DeleteUrl(string pocsUrl, int dwFlags);	//Deletes an entry by its URL. does not work!
        new void QueryUrl([MarshalAs(UnmanagedType.LPWStr)] string pocsUrl, STATURL_QUERYFLAGS dwFlags, ref STATURL lpSTATURL);	//Returns a STATURL for a given URL
        new void BindToObject([In] string pocsUrl, [In] UUID riid, IntPtr ppvOut);	//Binds to an object. does not work!
        new object EnumUrls { [return: MarshalAs(UnmanagedType.IUnknown)] get;}	//Returns an enumerator for URLs

        void AddUrlAndNotify(string pocsUrl, string pocsTitle, int dwFlags, int fWriteHistory, object poctNotify, object punkISFolder);//does not work!
        void ClearHistory();	//Removes all history items


    }

    //UrlHistory class
    [ComImport]
    [Guid("3C374A40-BAE4-11CF-BF7D-00AA006946EE")]
    internal class UrlHistoryClass
    {
    }

    /// <summary>
    /// Some Win32Api Pinvoke.
    /// </summary>
    internal class Win32api
    {
        /// <summary>
        /// Used by CannonializeURL method.
        /// </summary>
        [Flags]
        internal enum shlwapi_URL : uint
        {
            /// <summary>
            /// Treat "/./" and "/../" in a URL string as literal characters, not as shorthand for navigation. 
            /// </summary>
            URL_DONT_SIMPLIFY = 0x08000000,
            /// <summary>
            /// Convert any occurrence of "%" to its escape sequence.
            /// </summary>
            URL_ESCAPE_PERCENT = 0x00001000,
            /// <summary>
            /// Replace only spaces with escape sequences. This flag takes precedence over URL_ESCAPE_UNSAFE, but does not apply to opaque URLs.
            /// </summary>
            URL_ESCAPE_SPACES_ONLY = 0x04000000,
            /// <summary>
            /// Replace 
            /// </summary>
            URL_ESCAPE_UNSAFE = 0x20000000,
            /// <summary>
            /// Combine URLs with client-defined pluggable protocols, according to the World Wide Web Consortium (W3C) specification. This flag does not apply to standard protocols such as ftp, http, gopher, and so on. If this flag is set, UrlCombine does not simplify URLs, so there is no need to also set URL_DONT_SIMPLIFY.
            /// </summary>
            URL_PLUGGABLE_PROTOCOL = 0x40000000,
            /// <summary>
            /// Un-escape any escape sequences that the URLs contain, with two exceptions. The escape sequences for "?" and "#" are not un-escaped. If one of the URL_ESCAPE_XXX flags is also set, the two URLs are first un-escaped, then combined, then escaped.
            /// </summary>
            URL_UNESCAPE = 0x10000000
        }

        [DllImport("shlwapi.dll")]
        internal static extern int UrlCanonicalize(
            string pszUrl,
            StringBuilder pszCanonicalized,
            ref int pcchCanonicalized,
            shlwapi_URL dwFlags
            );


        /// <summary>
        /// Takes a URL string and converts it into canonical form
        /// </summary>
        /// <param name="pszUrl">URL string</param>
        /// <param name="dwFlags">shlwapi_URL Enumeration. Flags that specify how the URL is converted to canonical form.</param>
        /// <returns>The converted URL</returns>
        internal static string CannonializeURL(string pszUrl, shlwapi_URL dwFlags)
        {
            StringBuilder buff = new StringBuilder(260);
            int s = buff.Capacity;
            int c = UrlCanonicalize(pszUrl, buff, ref s, dwFlags);
            if (c == 0)
                return buff.ToString();
            else
            {
                buff.Capacity = s;
                c = UrlCanonicalize(pszUrl, buff, ref s, dwFlags);
                return buff.ToString();
            }
        }


        internal struct SYSTEMTIME
        {
            internal Int16 Year;
            internal Int16 Month;
            internal Int16 DayOfWeek;
            internal Int16 Day;
            internal Int16 Hour;
            internal Int16 Minute;
            internal Int16 Second;
            internal Int16 Milliseconds;
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        static extern bool FileTimeToSystemTime
            (ref System.Runtime.InteropServices.ComTypes.FILETIME FileTime, ref SYSTEMTIME SystemTime);


        /// <summary>
        /// Converts a file time to DateTime format.
        /// </summary>
        /// <param name="filetime">FILETIME structure</param>
        /// <returns>DateTime structure</returns>
        internal static DateTime FileTimeToDateTime(System.Runtime.InteropServices.ComTypes.FILETIME filetime)
        {
            SYSTEMTIME st = new SYSTEMTIME();
            FileTimeToSystemTime(ref filetime, ref st);
            return new DateTime(st.Year, st.Month, st.Day, st.Hour, st.Minute, st.Second, st.Milliseconds);

        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        static extern bool SystemTimeToFileTime([In] ref SYSTEMTIME lpSystemTime,
            out System.Runtime.InteropServices.ComTypes.FILETIME lpFileTime);


        /// <summary>
        /// Converts a DateTime to file time format.
        /// </summary>
        /// <param name="datetime">DateTime structure</param>
        /// <returns>FILETIME structure</returns>
        internal static System.Runtime.InteropServices.ComTypes.FILETIME DateTimeToFileTime(DateTime datetime)
        {
            SYSTEMTIME st = new SYSTEMTIME();
            st.Year = (short)datetime.Year;
            st.Month = (short)datetime.Month;
            st.Day = (short)datetime.Day;
            st.Hour = (short)datetime.Hour;
            st.Minute = (short)datetime.Minute;
            st.Second = (short)datetime.Second;
            st.Milliseconds = (short)datetime.Millisecond;
            System.Runtime.InteropServices.ComTypes.FILETIME filetime;
            SystemTimeToFileTime(ref st, out filetime);
            return filetime;

        }
        //compares two file times.
        [DllImport("Kernel32.dll")]
        internal static extern int CompareFileTime([In] ref System.Runtime.InteropServices.ComTypes.FILETIME lpFileTime1, [In] ref System.Runtime.InteropServices.ComTypes.FILETIME lpFileTime2);
        
	//gets the classname of an hwnd
        [DllImport("user32", CharSet = CharSet.Unicode)]
        internal static extern uint GetClassName(IntPtr hwnd, StringBuilder lpszClassName, uint cchFileNameMax);

        //Retrieves information about an object in the file system.
        [DllImport("shell32.dll")]
        internal static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        internal const uint SHGFI_ATTR_SPECIFIED =
            0x20000;
        internal const uint SHGFI_ATTRIBUTES = 0x800;
        internal const uint SHGFI_PIDL = 0x8;
        internal const uint SHGFI_DISPLAYNAME =
            0x200;
        internal const uint SHGFI_USEFILEATTRIBUTES
            = 0x10;
        internal const uint FILE_ATTRIBUTRE_NORMAL =
            0x4000;
        internal const uint SHGFI_EXETYPE = 0x2000;
        internal const uint SHGFI_SYSICONINDEX =
            0x4000;
        internal const uint ILC_COLORDDB = 0x1;
        internal const uint ILC_MASK = 0x0;
        internal const uint ILD_TRANSPARENT = 0x1;
        internal const uint SHGFI_ICON = 0x100;
        internal const uint SHGFI_LARGEICON = 0x0;
        internal const uint SHGFI_SHELLICONSIZE =
            0x4;
        internal const uint SHGFI_SMALLICON = 0x1;
        internal const uint SHGFI_TYPENAME = 0x400;
        internal const uint SHGFI_ICONLOCATION =
            0x1000;
    }



    /// <summary>
    /// Contains information about a file object.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SHFILEINFO
    {
        internal IntPtr hIcon;
        internal IntPtr iIcon;
        internal uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        internal string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        internal string szTypeName;
    };



    /// <summary>
    /// The helper class to sort in ascending order by FileTime(LastVisited).
    /// </summary>
    internal class SortFileTimeAscendingHelper : IComparer
    {
        [DllImport("Kernel32.dll")]
        static extern int CompareFileTime([In] ref System.Runtime.InteropServices.ComTypes.FILETIME lpFileTime1, [In] ref System.Runtime.InteropServices.ComTypes.FILETIME lpFileTime2);


        int IComparer.Compare(object a, object b)
        {
            STATURL c1 = (STATURL)a;
            STATURL c2 = (STATURL)b;

            return (CompareFileTime(ref c1.ftLastVisited, ref c2.ftLastVisited));

        }

        internal static IComparer SortFileTimeAscending()
        {
            return (IComparer)new SortFileTimeAscendingHelper();
        }

    }
    #endregion

}
