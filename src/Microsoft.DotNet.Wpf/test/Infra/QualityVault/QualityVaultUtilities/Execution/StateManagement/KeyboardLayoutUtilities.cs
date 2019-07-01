// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Globalization;
using System.Threading;
using Microsoft.Win32;


namespace Microsoft.Test.Execution.StateManagement.KeyboardLayout
{
    internal class KeyboardLayoutUtilities
    {
        internal static void UninstallKeyboardLayouts(ArrayList keyboardLayoutExclusionList)
        {
            uint flags;
            bool result = false;

            if (Environment.OSVersion.Version.Major >= 6)
            {
                for (int count = 0; count < vistaTipString.Length; count++)
                {
                    if (QueryLayoutOrTipString(vistaTipString[count], 0) == IntPtr.Zero)
                    {
                        // Uninstall keyboard layouts which are not previously enabled
                        if (!keyboardLayoutExclusionList.Contains(vistaTipString[count]))
                        {
                            flags = ILOT_UNINSTALL;
                            result = InstallLayoutOrTip(vistaTipString[count], flags);
                        }
                    }
                    else
                    {
                        ExecutionEventLog.RecordStatus("Invalid Tip String.");
                    }
                }
            }           
        }

        internal static ArrayList QueryEnabledKeyboardLayouts()
        {
            ArrayList enabledKeyboardLayouts = new ArrayList();
            for (int count = 0; count < vistaTipString.Length; count++)
            {  
                if(IsLayoutOrTipEnabled(vistaTipString[count]))
                {
                    enabledKeyboardLayouts.Add(vistaTipString[count]);
                }
            }
            return enabledKeyboardLayouts;
        }

        private static bool IsLayoutOrTipEnabled(string layoutOrTip)
        {
            int colon = layoutOrTip.IndexOf(":");

            ushort lcid = ushort.Parse(layoutOrTip.Substring(0, colon), NumberStyles.HexNumber);

            string[] layoutOrGuid = layoutOrTip.Substring(colon + 1).Replace("}", "").Split('{');

            bool enabled = false;

            Guid clsid = new Guid(layoutOrGuid[1]);

            Guid profile = new Guid(layoutOrGuid[2]);

            string key = String.Format(@"SOFTWARE\Microsoft\CTF\TIP\{0}\LanguageProfile\0x{1:x8}\{2}",

                clsid.ToString("B"), lcid, profile.ToString("B"));

            using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(key))
            {
                if (regKey != null)
                {
                    object enable = regKey.GetValue("Enable");
                    int en = (int)enable;
                    ExecutionEventLog.RecordStatus("enabled." + en.ToString());

                    enabled = (enable == null) ? false : (0 != (int)enable);
                }
            }
            return enabled;
        }
       
        internal static readonly string[] vistaTipString = { @"0411:{03B5835F-F03C-411B-9CE2-AA23E1171E36}{A76C93D9-5523-4E90-AAFA-4DB112F9AC76}",  // Japanese 
                                                             @"0804:{81D4E9C9-1D3B-41BC-9E6C-4B40BF79E35E}{F3BA9077-6C7E-11D4-97FA-0080C882687E}",  // Chinese Simplified - MSPY
                                                             @"0804:{E429B25A-E5D3-4D1F-9BE3-0C608477E3A1}{54FC610E-6ABD-4685-9DDD-A130BDF1B170}",  // Chinese Simplified - Quanpiyin
                                                             @"0412:{A028AE76-01B1-46C2-99C4-ACD9858AE02F}{B5FE1F02-D5F2-4445-9C03-C568F23C99A1}",  // Korean
                                                             @"0404:{531FDEBF-9B4C-4A43-A2AA-960E8FCDC732}{B2F9C502-1742-11D4-9790-0080C882687E}",   // Chinese Traditional - New Phonetic 
                                                           };

        private static readonly uint ILOT_UNINSTALL = 0x00000001;

        // extern "C" HRESULT WINAPI QueryLayoutOrTipString(_In_ LPCWSTR psz, DWORD dwFlags);
        [DllImport("input.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr QueryLayoutOrTipString(string psz, uint dwFlags);

        // extern "C" BOOL WINAPI InstallLayoutOrTip(_In_ LPCWSTR psz, DWORD dwFlags);
        [DllImport("input.dll", CharSet = CharSet.Unicode)]
        private static extern bool InstallLayoutOrTip(string psz, uint dwFlags);
       
    }
}