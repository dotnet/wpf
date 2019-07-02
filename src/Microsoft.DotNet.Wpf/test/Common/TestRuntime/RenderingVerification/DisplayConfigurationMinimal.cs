// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using Microsoft.Test.Logging;
using Microsoft.Test.Loaders;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;

namespace Microsoft.Test.RenderingVerification {

    /// <summary>
    /// A defined combination of theme, style, and color scheme
    /// </summary>
    public enum DesktopAppearance
    {
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
        WindowsClassic,

        /// <summary>
        /// Unrecognized combination of Theme and Color Scheme
        /// </summary>
        Unrecognized
    }

    /// <summary>
    /// Enables you to change the Display Configuration of the system
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public static partial class DisplayConfiguration {

        #region private data

        readonly static string themeDirectory = System.Environment.ExpandEnvironmentVariables(@"%WINDIR%\Resources\Themes");

        #endregion


        #region Public Members


        /// <summary>
        /// Gets a list of available themes installed on the system
        /// </summary>
        /// <returns>a list containing the names of the avalible themes</returns>
        public static string[] GetAvailableThemes()
        {
            string[] files = Directory.GetFiles(themeDirectory, "*.theme");
            string[] themeNames = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
                themeNames[i] = Path.GetFileNameWithoutExtension(files[i]);
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
        /// Gets the name of the current style
        /// </summary>
        /// <returns>the name of the current style</returns>
        public static string GetThemeStyle()
        {
            return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ThemeManager", "ColorName", "") as string;
        }

        /// <summary>
        /// Get the current defined appearance
        /// </summary>
        /// <returns></returns>
        public static DesktopAppearance GetAppearance()
        {
            string theme = GetTheme();

            if (theme.ToLowerInvariant() == AeroString.ToLowerInvariant())
            {
                if (IsDwmEnabled)
                    return DesktopAppearance.AeroWithComposition;
                else
                    return DesktopAppearance.AeroWithoutComposition;
            }
            else if (theme.ToLowerInvariant() == LunaString.ToLowerInvariant())
            {
                string colorScheme = GetThemeStyle();

                if (colorScheme.ToLowerInvariant() == LunaMetallicString.ToLowerInvariant())
                    return DesktopAppearance.LunaMetallic;
                else if (colorScheme.ToLowerInvariant() == LunaHomesteadString.ToLowerInvariant())
                    return DesktopAppearance.LunaHomestead;
                else
                    return DesktopAppearance.LunaNormalColor;
            }
            else if (theme.ToLowerInvariant() == WindowsClassicString.ToLowerInvariant())
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
            ArrayList appearanceList = new ArrayList();
            string[] availableThemes = GetAvailableThemes();

            foreach (string theme in availableThemes)
            {
                if (theme.ToLowerInvariant() == AeroString.ToLowerInvariant())
                {
                    appearanceList.Add(DesktopAppearance.AeroWithoutComposition);

                    if (IsDwmCompositionSupported)
                        appearanceList.Add(DesktopAppearance.AeroWithComposition);
                }
                else if (theme.ToLowerInvariant() == LunaString.ToLowerInvariant())
                {
                    appearanceList.Add(DesktopAppearance.LunaNormalColor);
                    appearanceList.Add(DesktopAppearance.LunaMetallic);
                    appearanceList.Add(DesktopAppearance.LunaHomestead);
                }
                else if (theme.ToLowerInvariant() == RoyaleString.ToLowerInvariant())
                    appearanceList.Add(DesktopAppearance.Royale);
                else if (theme.ToLowerInvariant() == WindowsClassicString.ToLowerInvariant())
                    appearanceList.Add(DesktopAppearance.WindowsClassic);
            }

            DesktopAppearance[] availableAppearances = new DesktopAppearance[appearanceList.Count];
            appearanceList.CopyTo(availableAppearances);

            return availableAppearances;
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
                    DwmIsCompositionEnabled(out isDwmEnabled);
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
            SwitchDwm(1);
        }

        /// <summary>
        /// Disable DWM
        /// </summary>
        public static void DisableDwm()
        {
            SwitchDwm(0);
        }

        static void SwitchDwm(uint enablecomposition)
        {
            try
            {
                int res = DwmEnableComposition(enablecomposition);
                if (res != 0) throw Marshal.GetExceptionForHR(res);
            }
            catch (DllNotFoundException e)
            {
                throw new InvalidOperationException("This method can only be used in a Vista machine", e);
            }
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
                DISPLAY_DEVICE dd = new DISPLAY_DEVICE();
                dd.cb = (int)Marshal.SizeOf(dd);
                for (uint i = 0; EnumDisplayDevices(null, i, ref dd, 0); i++)
                {
                    if ((dd.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) == DISPLAY_DEVICE_ATTACHED_TO_DESKTOP)
                    {

                        //Verify that DisplayDevice is not a mirroring driver
                        ret = ((dd.StateFlags & DISPLAY_DEVICE_MIRRORING_DRIVER) == 0);

                        if (!ret)
                        {
                            break;
                        }

                        //Verify that the DisplayDevice is using LDDM
                        DEVMODE dm = new DEVMODE();
                        dm.dmSize = (short)Marshal.SizeOf(dm);
                        EnumDisplaySettingsEx(dd.DeviceName.ToCharArray(), ENUM_CURRENT_SETTINGS, ref dm, 0);
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
                    DwmIsCompositionSupported(false, out isDwmCompositionSupported);
                }
                catch (DllNotFoundException)
                {
                }
                return isDwmCompositionSupported;
            }
        }

        #endregion


        #region Private Implementation

        private const string AeroString = "Aero";
        private const string LunaString = "Luna";
        private const string RoyaleString = "Royale";
        private const string WindowsClassicString = "Windows Classic";
        private const string LunaNormalColorString = "NormalColor";
        private const string LunaMetallicString = "Metallic";
        private const string LunaHomesteadString = "Homestead";

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            internal int x;
            internal int y;
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAY_DEVICE
        {
            internal int cb;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            internal string DeviceName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string DeviceString;
            internal int StateFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string DeviceID;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            internal string dmDeviceName;
            internal short dmSpecVersion;
            internal short dmDriverVersion;
            internal short dmSize;
            internal short dmDriverExtra;
            internal int dmFields;

            [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
            internal POINT dmPosition;
            internal int dmDisplayOrientation;
            internal int dmDisplayFixedOutput;
            internal short dmColor;
            internal short dmDuplex;
            internal short dmYResolution;
            internal short dmTTOption;
            internal short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            internal string dmFormName;
            internal short dmLogPixels;
            internal int dmBitsPerPel;
            internal int dmPelsWidth;
            internal int dmPelsHeight;
            internal int dmDisplayFlags;
            internal int dmDisplayFrequency;
            internal int dmICMMethod;
            internal int dmICMIntent;
            internal int dmMediaType;
            internal int dmDitherType;
            internal int dmReserved1;
            internal int dmReserved2;
            internal int dmPanningWidth;
            internal int dmPanningHeight;
        };

        private const int DISPLAY_DEVICE_MIRRORING_DRIVER = 0x00000008;
        private const int DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x00000001;
        private const int ENUM_CURRENT_SETTINGS = -1;

        [DllImport("User32.dll")]
        static extern bool EnumDisplayDevices(String lpDevice, uint iDevNum, ref DISPLAY_DEVICE ddevice, uint dwFlags);

        [DllImport("User32.dll")]
        static extern bool EnumDisplaySettingsEx(char[] lpDevice, int iModeNum, ref DEVMODE lpDevMode, int dwFlags);

        [DllImport("dwmapi", EntryPoint="#106")]
        static extern int DwmIsCompositionSupported(bool remoteSession, out bool supported);

        [DllImport("dwmapi")]
        static extern int DwmIsCompositionEnabled(out bool enabled);

        [DllImport("dwmapi")]
        static extern int DwmEnableComposition(uint composed);

        #endregion        
    }
}
