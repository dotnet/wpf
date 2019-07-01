// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace Microsoft.Test.Execution.StateManagement.Color
{
    internal class ColorProfileUtilities
    {
        internal static string GetActiveName()
        {
            string profileName;
            StringBuilder outProfileNameBuffer = null;
            int outProfileNameBufferSize = 0;
            // First call to GetStandardColorSpaceProfile() is to obatain the needed buffer size for profile name
            GetStandardColorSpaceProfile(IntPtr.Zero, (int)LogicalColorSpace.LCS_sRGB, null, out outProfileNameBufferSize);
            outProfileNameBuffer = new StringBuilder((int)outProfileNameBufferSize);
            // Second call to GetStandardColorSpaceProfile() is to actually get the profile name
            bool result = GetStandardColorSpaceProfile(IntPtr.Zero, (int)LogicalColorSpace.LCS_sRGB, outProfileNameBuffer, out outProfileNameBufferSize);

            if (result == false)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get Color Space Profile.");
            }
            profileName = outProfileNameBuffer.ToString();

            return profileName;
        }
        internal static void SetActiveName(string activeName)
        {
            bool result = SetStandardColorSpaceProfile(IntPtr.Zero, (int)LogicalColorSpace.LCS_sRGB, activeName);
            if (result == false)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set Color Space Profile.");
            }
        }

        enum LogicalColorSpace : int
        {
            LCS_CALIBRATED_RGB = 0x00000000,
            LCS_sRGB = 0x73524742,
            LCS_WINDOWS_COLOR_SPACE = 0x57696E20
        };

        [DllImport("Mscms.dll")]
        public extern static bool SetStandardColorSpaceProfile(
            IntPtr pMachineName,
            int dwProfileID,
            string pProfilename
            );

        [DllImport("Mscms.dll")]
        public extern static bool GetStandardColorSpaceProfile(
            IntPtr pMachineName,
            int dwProfileID,
            [Out] StringBuilder pProfileName,
            out int pdwSize
            );
    }
}