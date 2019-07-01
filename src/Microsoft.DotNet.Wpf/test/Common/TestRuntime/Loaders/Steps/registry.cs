// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.Win32;

namespace Microsoft.Test.Utilities
{
    /// <summary>
    /// RegistryHelper wraps read/write registry functionality
    /// </summary>
    [RegistryPermission(SecurityAction.Assert, Unrestricted = true)]
    internal class RegistryHelper
    {
        /// <summary>
        /// Writes a value in the registry. sample key format: "HKEY_LOCAL_MACHINE\Software\Microsoft\the folder". It redirects to WOW6432Node if required.
        /// </summary>
        internal static void Write(string key, string variable, object value, bool redirect)
        {
            Write(GetRedirectedKey(key, redirect), variable, value, redirect);
            Write(key, variable, value, redirect);
        }

        /// <summary>
        /// Writes a value in the registry. sample key format: "HKEY_LOCAL_MACHINE\Software\Microsoft\the folder"
        /// </summary>
        internal static void Write(string key, string variable, object value)
        {
            Win32RegistryHelper.DisableRedirection(key);
            Microsoft.Win32.Registry.SetValue(key, variable, value);
            Win32RegistryHelper.EnableRedirection(key);
        }

        /// <summary>
        /// Writes a value in the registry. sample key format: "HKEY_LOCAL_MACHINE\Software\Microsoft\the folder".
        /// valueStr gets parsed depending on the kindName, which has to match with a member of the RegistryValueKind enum.
        /// </summary>
        internal static void Write(string key, string variable, string valueStr, string kindName, bool redirect)
        {
            Write(GetRedirectedKey(key, redirect), variable, valueStr, kindName);
            Write(key, variable, valueStr, kindName);
        }

        /// <summary>
        /// Writes a value in the registry. sample key format: "HKEY_LOCAL_MACHINE\Software\Microsoft\the folder".
        /// valueStr gets parsed depending on the kindName, which has to match with a member of the RegistryValueKind enum.
        /// </summary>
        internal static void Write(string key, string variable, string valueStr, string kindName)
        {
            // get the kind
            RegistryValueKind kind = (RegistryValueKind)Enum.Parse(typeof(RegistryValueKind), kindName);

            // convert the value to the appropriate kind
            object value = null;
            switch (kind)
            {
                case RegistryValueKind.DWord:
                    value = Int32.Parse(valueStr);
                    break;
                case RegistryValueKind.String:
                    value = valueStr;
                    break;
                default:
                    throw (new NotSupportedException("not supported RegistryValueKind"));
            }

            Win32RegistryHelper.DisableRedirection(key);
            Microsoft.Win32.Registry.SetValue(key, variable, value);
            Win32RegistryHelper.EnableRedirection(key);
        }

        /// <summary>
        /// Reads a value from the registry.
        /// </summary>
        internal static object Read(string key, string var, object defaultValue, bool redirect)
        {
            return (Read(key, var, defaultValue));
        }

        /// <summary>
        /// Reads a value from the registry.
        /// </summary>
        internal static object Read(string key, string var, object defaultValue)
        {
            return (Microsoft.Win32.Registry.GetValue(key, var, defaultValue));
        }

        /// <summary>
        /// Deletes a key tree given its full path (for example, @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\Avalon\folder")
        /// </summary>
        /// <param name="fullPath"></param>
        internal static void DeleteKeyTree(string fullPath)
        {
            DeleteKeyTree(fullPath, false);
        }

        /// <summary>
        /// Deletes a key tree given its full path (for example, @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\Avalon\folder")
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="redirect"></param>
        internal static void DeleteKeyTree(string fullPath, bool redirect)
        {
            // without redirection
            string hive, key;
            ExtractEntitiesFromRegistryPath(fullPath, out hive, out key);
            DeleteKeyTree(GetRegistryKeyFromName(hive), key, false);

            // with redirection
            ExtractEntitiesFromRegistryPath(GetRedirectedKey(fullPath, redirect), out hive, out key);
            DeleteKeyTree(GetRegistryKeyFromName(hive), key, false);
        }

        /// <summary>
        /// Deletes a variable given its key's full path and variable name (for example, @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\Avalon\folder" and "variable")
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="variable"></param>
        internal static void DeleteVariable(string fullPath, string variable)
        {
            DeleteVariable(fullPath, variable, false);
        }

        /// <summary>
        /// Deletes a variable given its key's full path and variable name (for example, @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\Avalon\folder" and "variable")
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="variable"></param>
        /// <param name="redirect"></param>
        internal static void DeleteVariable(string fullPath, string variable, bool redirect)
        {
            string hive, key;
            const bool throwOnMissingValue = false;

            // without redirection
            ExtractEntitiesFromRegistryPath(fullPath, out hive, out key);
            using (RegistryKey rk = GetRegistryKeyFromName(hive).OpenSubKey(key, true))
            {
                DeleteValue(rk, variable, throwOnMissingValue);
            }

            // with redirection
            ExtractEntitiesFromRegistryPath(GetRedirectedKey(fullPath, redirect), out hive, out key);
            using (RegistryKey rk = GetRegistryKeyFromName(hive).OpenSubKey(key, true))
            {
                DeleteValue(rk, variable, throwOnMissingValue);
            }
        }

        /// <summary>
        /// GetRegistryKeyFromName
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static RegistryKey GetRegistryKeyFromName(string name)
        {
            switch (name)
            {
                case RootKeyName.HKEY_CLASSES_ROOT:
                    return(Registry.ClassesRoot);
                case RootKeyName.HKEY_CURRENT_CONFIG:
                    return (Registry.CurrentConfig);
                case RootKeyName.HKEY_CURRENT_USER:
                    return(Registry.CurrentUser);
                case RootKeyName.HKEY_LOCAL_MACHINE:
                    return(Registry.LocalMachine);
                case RootKeyName.HKEY_PERFORMANCE_DATA:
                    return(Registry.PerformanceData);
                case RootKeyName.HKEY_USERS:
                    return(Registry.Users);
                default:
                    throw new ArgumentException("Unknown key: '" + name + "'");
            }
        }

        /// <summary>
        /// DeleteKeyTree
        /// </summary>
        /// <param name="rk"></param>
        /// <param name="key"></param>
        /// <param name="throwOnMissingValue"></param>
        private static void DeleteKeyTree(RegistryKey rk, string key, bool throwOnMissingValue)
        {
            try
            {
                rk.DeleteSubKeyTree(key);
            }
            catch (ArgumentException e)
            {
                if (throwOnMissingValue)
                {
                    throw e;
                }
            }
        }

        /// <summary>
        /// DeleteValue
        /// </summary>
        /// <param name="rk"></param>
        /// <param name="variable"></param>
        /// <param name="throwOnMissingValue"></param>
        private static void DeleteValue(RegistryKey rk, string variable, bool throwOnMissingValue)
        {
            try
            {
                rk.DeleteValue(variable, throwOnMissingValue);
            }
            catch (NullReferenceException)
            {
                // ignore; this happens when the variable doesn't exist and the folder either
            }
        }

        /// <summary>
        /// Helper to extract hive and key from a registry key's full path
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="hive"></param>
        /// <param name="key"></param>
        private static void ExtractEntitiesFromRegistryPath(string fullPath, out string hive, out string key)
        {
            // extract the hive
            char[] backSlash = { '\\' };
            int firstBackSlash = fullPath.IndexOfAny(backSlash);
            hive = fullPath.Substring(0, firstBackSlash);

            // extract key
            key = fullPath.Substring(firstBackSlash + 1);
        }

        /// <summary>
        /// GetRedirectedKey
        /// </summary>
        /// <param name="key"></param>
        /// <param name="redirect"></param>
        /// <returns></returns>
        private static string GetRedirectedKey(string key, bool redirect)
        {
            //if (redirect)
            //{
                key = GetRedirectedKey(key);
            //}
            return (key);
        }

        /// <summary>
        /// GetRedirectedKey
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string GetRedirectedKey(string key)
        {
            string keyToRedirect = @"HKEY_LOCAL_MACHINE\SOFTWARE";
            string redirectedKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432node";
            return(key.Replace(keyToRedirect, redirectedKey));
        }
    }

    /// <summary>
    /// Win32RegistryHelper
    /// </summary>
    [RegistryPermission(SecurityAction.Assert, Unrestricted = true)]
    internal class Win32RegistryHelper
    {
        /// <summary>
        /// DisableRedirection
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static void DisableRedirection(string key)
        {
            // ignore null keys
            if (key == null)
            {
                return;
            }

            // no-op in Win32
            if (!Win32Helper.IsFunctionDefined("advapi32.dll", "RegDisableReflectionKey"))
            {
                return;
            }

            // no-op if Win7 or greater
            if ((Environment.OSVersion.Version.Major == 6) && (Environment.OSVersion.Version.Minor >= 1))
            {
                return;
            }

            // split name
            IntPtr root;
            string subkey;
            ExtractKeyComponents(key, out root, out subkey);
            IntPtr handle;

            // open key
            int r = Win32Helper.RegOpenKeyEx(root, subkey, 0, Win32Error.KEY_ALL_ACCESS, out handle);

            // ignore non-existent keys
            if (r == Win32Error.ERROR_FILE_NOT_FOUND)
            {
                // key doesn't exist, no op
                return;
            }

            // throw if error
            if (r != Win32Error.ERROR_SUCCESS)
            {
                throw (new Win32Exception(r));
            }

            // disable redirection
            Singleton.Instance.Log.StatusMessage = "Disabling registry redirection on '" + key + "'";
            r = Win32Helper.RegDisableReflectionKey(handle);
            if (r != Win32Error.ERROR_SUCCESS)
            {
                throw (new Win32Exception(r));
            }

            // close key
            Win32Helper.RegCloseKey(handle);
        }

        /// <summary>
        /// EnableRedirection
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static void EnableRedirection(string key)
        {
            // ignore null keys
            if (key == null)
            {
                return;
            }

            // no-op in Win32
            if (!Win32Helper.IsFunctionDefined("advapi32.dll", "RegEnableReflectionKey"))
            {
                return;
            }

            // no-op if Win7 or greater
            if ((Environment.OSVersion.Version.Major == 6) && (Environment.OSVersion.Version.Minor >= 1))
            {
                return;
            }

            // split name
            IntPtr root;
            string subkey;
            ExtractKeyComponents(key, out root, out subkey);
            IntPtr handle;

            // open key
            int r = Win32Helper.RegOpenKeyEx(root, subkey, 0, Win32Error.KEY_ALL_ACCESS, out handle);

            // ignore non-existent key
            if (r == Win32Error.ERROR_FILE_NOT_FOUND)
            {
                // key doesn't exist, no op
                return;
            }

            // throw if error
            if (r != Win32Error.ERROR_SUCCESS)
            {
                throw (new Win32Exception(r));
            }

            // enable redirection
            Singleton.Instance.Log.StatusMessage = "Enabling registry redirection on '" + key + "'";
            r = Win32Helper.RegEnableReflectionKey(handle);
            if (r != Win32Error.ERROR_SUCCESS)
            {
                throw (new Win32Exception(r));
            }

            // close key
            Win32Helper.RegCloseKey(handle);
        }

        /// <summary>
        /// ExtractKeyComponents
        /// </summary>
        /// <param name="key"></param>
        /// <param name="root"></param>
        /// <param name="subkey"></param>
        private static void ExtractKeyComponents(string key, out IntPtr root, out string subkey)
        {
            int firstBack = key.IndexOf(@"\");
            if (firstBack < 1)
            {
                throw (new ArgumentException("key"));
            }
            subkey = key.Substring(firstBack + 1);
            string rootName = key.Substring(0, firstBack);

            root = RootKey.FromString(rootName);
        }

        /// <summary>
        /// RootKey (from WinReg.h)
        /// </summary>
        internal struct RootKey
        {
            internal static IntPtr HKEY_CLASSES_ROOT = new IntPtr(unchecked((int)0x80000000));
            internal static IntPtr HKEY_CURRENT_USER = new IntPtr(unchecked((int)0x80000001));
            internal static IntPtr HKEY_LOCAL_MACHINE = new IntPtr(unchecked((int)0x80000002));
            internal static IntPtr HKEY_USERS = new IntPtr(unchecked((int)0x80000003));
            internal static IntPtr HKEY_PERFORMANCE_DATA = new IntPtr(unchecked((int)0x80000004));
            internal static IntPtr HKEY_PERFORMANCE_TEXT = new IntPtr(unchecked((int)0x80000050));
            internal static IntPtr HKEY_PERFORMANCE_NLSTEXT = new IntPtr(unchecked((int)0x80000060));
            internal static IntPtr HKEY_CURRENT_CONFIG = new IntPtr(unchecked((int)0x80000005));
            internal static IntPtr HKEY_DYN_DATA = new IntPtr(unchecked((int)0x80000006));

            /// <summary>
            /// FromString
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            internal static IntPtr FromString(string key)
            {
                switch (key)
                {
                    case RootKeyName.HKEY_CLASSES_ROOT:
                        return (HKEY_CLASSES_ROOT);
                    case RootKeyName.HKEY_CURRENT_CONFIG:
                        return (HKEY_CURRENT_CONFIG);
                    case RootKeyName.HKEY_CURRENT_USER:
                        return (HKEY_CURRENT_USER);
                    case RootKeyName.HKEY_LOCAL_MACHINE:
                        return (HKEY_LOCAL_MACHINE);
                    case RootKeyName.HKEY_PERFORMANCE_DATA:
                        return (HKEY_PERFORMANCE_DATA);
                    case RootKeyName.HKEY_USERS:
                        return (HKEY_USERS);
                    default:
                        throw new ArgumentException("Unknown key: '" + key + "'");
                }
            }
        }
    }

    /// <summary>
    /// RootKeyName
    /// </summary>
    internal struct RootKeyName
    {
        internal const string HKEY_CLASSES_ROOT = "HKEY_CLASSES_ROOT";
        internal const string HKEY_CURRENT_CONFIG = "HKEY_CURRENT_CONFIG";
        internal const string HKEY_CURRENT_USER = "HKEY_CURRENT_USER";
        internal const string HKEY_LOCAL_MACHINE = "HKEY_LOCAL_MACHINE";
        internal const string HKEY_PERFORMANCE_DATA = "HKEY_PERFORMANCE_DATA";
        internal const string HKEY_USERS = "HKEY_USERS";
    }

    /// <summary>
    /// RegistrySearch: registry walker. Takes a method to process found keys
    /// </summary>
    [RegistryPermission(SecurityAction.Assert, Unrestricted = true)]
    internal class RegistrySearch
    {
        /// <summary>
        /// the constructor of a RegistrySearch object.
        /// </summary>
        /// <param name="NodeProc">delegate to call for each found node</param>
        /// <param name="StartingKey">directory in which search will be started</param>
        internal RegistrySearch(NodeProcessor NodeProc, string StartingKey)
        {
            // make sure that the NodeProcessor is not null
            if (NodeProc == null)
            {
                throw (new ArgumentNullException("NodeProc", "NodeProcessor delegate must be set"));
            }

            nodeProcessor = NodeProc;

            // make sure that the startKey is not null
            if (StartKey == null)
            {
                throw (new ArgumentNullException("StartDir", "the start directory must be set"));
            }

            // set working variables
            StartKey = StartingKey;
            this.RegNodeProcessor = NodeProc;
        }

        /// <summary>
        /// RegistrySearch default constructor
        /// </summary>
        internal RegistrySearch()
        {
        }

        /// <summary>
        /// StartKey contains the directory to start with.
        /// </summary>
        private string startKey;
        internal string StartKey
        {
            set
            {
                startKey = value;
            }
            get
            {
                return (startKey);
            }
        }

        /// <summary>
        /// NodeProcessor is a delegate to call for each key found
        /// </summary>
        internal delegate bool NodeProcessor(string NodeName);
        private NodeProcessor nodeProcessor;
        internal NodeProcessor RegNodeProcessor
        {
            set
            {
                nodeProcessor = value;
            }
        }

        /// <summary>
        /// Pattern property contains the filter of the keys to look for.
        /// </summary>
        private string pattern = "";
        internal string Pattern
        {
            set
            {
                pattern = value;
            }
            get
            {
                return (pattern);
            }
        }

        /// <summary>
        /// Start launches the search.
        /// </summary>
        internal void Start()
        {
            // split startKey into hive and path
            char[] seps = { '\\' };
            string[] tokens = startKey.Split(seps);
            string relKey;//the path inside a hive

            if (tokens.Length == 0)
            {
                throw (new ArgumentException("startKey", "invalid start key"));
            }

            // set hive
            RegistryKey hive = null;

            if (tokens[0].ToUpper() == @"HKEY_LOCAL_MACHINE")
            {
                hive = Registry.LocalMachine;
            }
            else if (tokens[0].ToUpper() == @"HKEY_CURRENT_CONFIG")
            {
                hive = Registry.CurrentConfig;
            }
            else if (tokens[0].ToUpper() == @"HKEY_CURRENT_USER")
            {
                hive = Registry.CurrentUser;
            }
            else if (tokens[0].ToUpper() == @"HKEY_USERS")
            {
                hive = Registry.Users;
            }
            else if (tokens[0].ToUpper() == @"HKEY_CLASSES_ROOT")
            {
                hive = Registry.ClassesRoot;
            }
            else if (startKey == @"")
            {
                // special case, all the hives
                foreach (string key in Registry.CurrentConfig.GetSubKeyNames())
                {
                    WalkHelper(OpenSubKeyIgnoringSecurity(Registry.CurrentConfig, key));
                }

                foreach (string key in Registry.ClassesRoot.GetSubKeyNames())
                {
                    WalkHelper(OpenSubKeyIgnoringSecurity(Registry.ClassesRoot, key));
                }

                foreach (string key in Registry.CurrentUser.GetSubKeyNames())
                {
                    WalkHelper(OpenSubKeyIgnoringSecurity(Registry.CurrentUser, key));
                }

                foreach (string key in Registry.LocalMachine.GetSubKeyNames())
                {
                    WalkHelper(OpenSubKeyIgnoringSecurity(Registry.LocalMachine, key));
                }

                foreach (string key in Registry.Users.GetSubKeyNames())
                {
                    WalkHelper(OpenSubKeyIgnoringSecurity(Registry.Users, key));
                }
            }

            if (tokens.Length > 1)
            {
                relKey = startKey.Substring(tokens[0].Length + 1, startKey.Length - tokens[0].Length - 1);
                WalkHelper(hive.OpenSubKey(relKey));
            }
        }

        private RegistryKey OpenSubKeyIgnoringSecurity(RegistryKey reg, string Key)
        {
            RegistryKey key = null;

            try
            {
                // open the subkey
                key = reg.OpenSubKey(Key);
                return (key);
            }
            catch (System.Security.SecurityException)
            {
                // just ignore security exceptions in this case
                return (null);
            }
        }

        /// <summary>
        /// WalkHelper is a recursive function that goes through directories looking for the target keys
        /// </summary>
        /// <param name="reg"></param>
        private void WalkHelper(RegistryKey reg)
        {
            // check reg is not null
            if (reg == null)
            {
                return;
            }

            // process values
            string[] values = reg.GetValueNames();

            foreach (string val in values)
            {
                if (val.Contains(pattern))
                {
                    if (nodeProcessor != null)
                    {
                        nodeProcessor(reg.Name + @"\" + val);
                    }
                }
            }

            //process keys
            string[] keys = reg.GetSubKeyNames();

            foreach (string key in keys)
            {
                if (key.Contains(pattern))
                {
                    if (nodeProcessor != null)
                    {
                        nodeProcessor(reg.Name + @"\" + key);
                    }
                }

                WalkHelper(OpenSubKeyIgnoringSecurity(reg, key));
            }
        }

        static bool regNodeFound = false;
        internal static bool Find(string Key, string Value)
        {
            RegistrySearch rs = new RegistrySearch();

            rs.RegNodeProcessor = new NodeProcessor(NodeProc);
            rs.StartKey = Key;
            rs.Pattern = Value;
            regNodeFound = false;
            rs.Start();
            return (regNodeFound);
        }

        static bool NodeProc(string Node)
        {
            regNodeFound = true;
            return (true);
        }
    }

    /// <summary>
    /// RegistryVar: easy way to set and get variable/value pairs
    /// </summary>
    internal class RegistryVar
    {
        /// <summary>
        /// Set
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="value"></param>
        internal static void Set(string variable, string value)
        {
            KeyValuePairs.Set(variable, value);
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        internal static string Get(string variable)
        {
            return (string)KeyValuePairs.Get(variable);
        }
    }
}
