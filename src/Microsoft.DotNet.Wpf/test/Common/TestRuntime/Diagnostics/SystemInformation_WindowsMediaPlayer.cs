// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.Test.Win32;
using System.Globalization;
using Microsoft.Win32;
using System.Windows.Controls;

namespace Microsoft.Test.Diagnostics
{
    /// <summary>
    /// Partial class to return the version of  Windows Media Player installed on this OS
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public partial class SystemInformation
    {
        /// <summary>
        /// Gets the value indicating the current version of WindowsMediaPlayer on the machine.
        /// </summary>
        public static System.Version WindowsMediaPlayerVersion
        {
            get
            {
                RegistryKey wMPKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\MediaPlayer\\Setup\\Installed Versions");
                if (wMPKey == null)
                {
                    return null;
                }
                else
                {
                    // registry key is in the form of      wmplayer.exe   00 00 0b 00 19 14 59 16
                    // with the third byte coresponding to the major version of the player, 4th the minor,
                    // 7th & 8th are the build, with the 5th and 6th the revision

                    Byte[] version = (Byte[])wMPKey.GetValue("wmplayer.exe");

                    if ((version != null) && (version.Length == 8))
                    {
                        Version fullVersion = new Version((int)version[2], (int)version[3], Convert.ToInt32(version[7].ToString("X") + version[6].ToString("X"), 16), Convert.ToInt32(version[5].ToString("X") + version[4].ToString("X"), 16)); 
                        return fullVersion;
                    }
                    else
                    {
                        return null;
                    }


                }
            }
        }
    }

    
}
