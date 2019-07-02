// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Text;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Interop;

namespace Microsoft.Test.RenderingVerification
{
    /// <summary>
    /// Change/Query Display setting (screen resolution, Hardware Acceleration, ...)
    /// </summary>
    internal class DisplaySettings
    {
        static Type _renderingMode = null;
        /// <summary>
        /// Get/set the screen resolution
        /// </summary>
        /// <value></value>
        static internal Size ScreenResolution
        {
            get
            {
                throw new NotImplementedException ();
            }
            set
            {
                throw new NotImplementedException ();
            }
        }
        /// <summary>
        /// Set the hardware rendering on/off the for the entire system
        /// </summary>
        /// <param name="hardwareRenderingOn"></param>
        static internal void SetSystemHardwareAcceleration(bool hardwareRenderingOn)
        {
            if (System.Environment.Version.Major < 4)
            {
                throw new NotImplementedException("Does not work for OS <= Win2k");
            }
/*
    //
    // Let's find all the video drivers that are installed in the system
    //
    DISPLAY_DEVICE DisplayDevice;
    DisplayDevice.cb = sizeof (DisplayDevice);


    //BUGBUG: only works on Win2K and above...

    for (int d=0, index=0; EnumDisplayDevices(NULL, index, &DisplayDevice, 0); index++) 
    {
       HKEY hKey = NULL;

       if (DisplayDevice.StateFlags & DISPLAY_DEVICE_MIRRORING_DRIVER) 
       {
           continue;
       }

       if (nMonitor != d++) 
       {
           continue;
       }

       TCHAR device_key[256] = {0};     // Name of service (drivers)

       _tcscpy (device_key, DisplayDevice.DeviceKey+18);  // chop "\Registry\Machine\" off

       // Set reg keys to needed level
       //

       if (RegOpenKeyEx(HKEY_LOCAL_MACHINE,device_key,(DWORD)0,KEY_ALL_ACCESS,&hKey)==ERROR_SUCCESS)
       {
          if( RegSetValueEx(hKey, _TEXT("Acceleration.Level"), 0, REG_DWORD, (CONST BYTE *)&nLevel, sizeof(DWORD)) != ERROR_SUCCESS)
          {
            // log error
            //
            _ftprintf(stderr, _TEXT("Error: Unable to set REG_DWORD reg key Acceleration.Level to value: %d with last error: %u\r\n"), nLevel, GetLastError());
          }
       }
       else
       {
          // log error
          //
          _ftprintf(stderr, _TEXT("Error: Unable to open reg key under HKLM for: %s with last error: %u\r\n"), device_key, GetLastError());
       }

        
       // Now enforce display change settings
       //

       LONG lResult = 0;

       lResult = ChangeDisplaySettings(NULL, 0);

       switch(lResult) 
       {
          case DISP_CHANGE_BADDUALVIEW:
             _ftprintf(stdout, _TEXT("Windows XP: The settings change was unsuccessful because system is DualView capable.\r\n"));
             break;
          case DISP_CHANGE_BADMODE:
             _ftprintf(stdout, _TEXT("The graphics mode is not supported.\r\n"));
             break;
          case DISP_CHANGE_FAILED:
             _ftprintf(stdout, _TEXT("The display driver failed the specified graphics mode.\r\n"));
             break;
          case DISP_CHANGE_NOTUPDATED:
             _ftprintf(stdout, _TEXT("Windows NT/2000/XP: Unable to write settings to the registry\r\n"));
             break;
          case DISP_CHANGE_RESTART:
             _ftprintf(stdout, _TEXT("The computer must be restarted in order for the graphics mode to work.\r\n"));
             break;
          case DISP_CHANGE_SUCCESSFUL:
             _ftprintf(stdout, _TEXT("Successfully changed dislpay settings for hardware acceleration\r\n"));
             break;
          default:
             _ftprintf(stderr, _TEXT("Error:  Unable to change display settings with error: %l\r\n"), lResult);
             break;
       }
       
       switch (nLevel)
       {
          case 0:
             _ftprintf(stdout, _TEXT("Level 0: Full acceleration enabled\r\n"));
             break;
          case 1:
             _ftprintf(stdout, _TEXT("Level 1: Only cursor and birmap acceleration disabled\r\n"));
             break;
          case 2:
             _ftprintf(stdout, _TEXT("Level 2: Cursor and advanced drawing acceleration disabled\r\n"));
             break;
          case 3:
             _ftprintf(stdout, _TEXT("Level 3: DirectX, cursor and advanced drawing acceleration disabled\r\n"));
             break;
          case 4:
             _ftprintf(stdout, _TEXT("Level 4: All but basic accelerations disabled\r\n"));
             break;
          case 5:
             _ftprintf(stdout, _TEXT("Level 5: All accelerations disabled\r\n"));
             break;
          default:
             _ftprintf(stderr, _TEXT("Error:  Unsupported level: %d\r\n"), nLevel);
             break;
       }
       
       if (hKey)
       {
          RegCloseKey(hKey);
          hKey = NULL;
       }
    
    } // for each device

    return S_OK;

*/
            throw new NotImplementedException ();
        }
        /// <summary>
        /// Get the hardware rendering on/off the for the entire system
        /// </summary>
        /// <param name="hardwareRenderingOn"></param>
        static internal void GetSystemHardwareAcceleration(out bool hardwareRenderingOn)
        {
            throw new NotImplementedException ();
        }
        /// <summary>
        /// Set the hardware rendering on/off the for a specific avalon application
        /// </summary>
        /// <param name="hwndTarget"></param>
        /// <param name="hardwareAccelerationOn"></param>
        // @ review : change name from Avalon to ???
        static internal void SetRenderingMode(object hwndTarget, bool hardwareAccelerationOn)
        {
            RenderMode renderMode = hardwareAccelerationOn ? RenderMode.Default : RenderMode.SoftwareOnly;

            ((HwndTarget)hwndTarget).RenderMode = renderMode;
        }
        /// <summary>
        /// Get the hardware rendering on/off the for a specific avalon application
        /// </summary>
        /// <param name="hwndTarget"></param>
        /// <param name="hardwareAccelerationOn"></param>
        // @ review : change name from Avalon to ???
        static internal void GetRenderingMode(object hwndTarget, out bool hardwareAccelerationOn)
        {
            Type renderingMode = RenderingMode (hwndTarget);
            string retVal = hwndTarget.GetType ().InvokeMember ("GetRenderMode", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, hwndTarget, null).ToString ();

            hardwareAccelerationOn = (retVal == "Hardware") ? true : false;
        }

        static private Type RenderingMode (object hwndTarget)
        {
            if (_renderingMode == null)
            {
                if (hwndTarget == null)
                {
                    throw new ArgumentNullException ("hwndTarget", "object must be set to a valid instance (null passed in)");
                }

                Type hwndTargetType = hwndTarget.GetType();

                if (hwndTargetType.ToString () != "System.Windows.Interop.HwndTarget")
                {
                    throw new System.ArgumentException ("Argument is of unexpected type, should be of type 'HwndTarget' (type '" + hwndTargetType.ToString () + "' passed in)", "hwndTarget");
                }

                Assembly vmAssembly = hwndTargetType.Assembly;

                _renderingMode = vmAssembly.GetType ("System.Windows.Interop.RenderingMode");
            }

            return _renderingMode;
        }
    }
    
    /// <summary>
    /// Get the CPU architecture (x86 / IA64 / AMD64)
    /// </summary>
    internal enum CPUType
    {
        /// <summary>
        /// Undefined / Unknown CPU type
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// CPU is from the x86 familly (8086 -- Pentium / AMD / Cyrix)
        /// </summary>
        x86 = 86,
        /// <summary>
        /// CPU is an Intel 64 bit (code name Itanium)
        /// </summary>
        IA64 = 64,
        /// <summary>
        /// CPU is an AMD 64 bits (code name ClawHammer)
        /// </summary>
        AMD64 = 65
    }

    /// <summary>
    /// Get information about this computer environment
    /// </summary>
    public sealed class Environment
    { 
        private Environment() { }    // block instantiation

        /// <summary>
        /// Parse a string for Environment variable and translate them.
        /// </summary>
        /// <param name="environmentVar">The path containing the environment variable(s)</param>
        /// <returns>The path translated</returns>
        static internal string TranslateEnvironmentVariable(string environmentVar)
        {
            MatchCollection matches = Regex.Matches (environmentVar, "(%.+?%)");
            string val = string.Empty;
            string translatedVariable = string.Empty;
            for (int t = 0; t < matches.Count; t++)
            {
                val = matches[t].Value;
                translatedVariable = System.Environment.GetEnvironmentVariable (val.Substring (1, val.Length - 2));
                if (translatedVariable != null)
                {
                    environmentVar = Regex.Replace(environmentVar, matches[t].Value, translatedVariable);
                }
            }
            return environmentVar;
        }
        /// <summary>
        /// Return the user locale setting
        /// </summary>
        /// <value></value>
        static public int UserLocale
        {
            get
            {
                return GetLocale (Kernel32.LOCALE_USER_DEFAULT);
            }
        }
        /// <summary>
        /// Return the system locale setting
        /// </summary>
        /// <value></value>
        static internal int SystemLocale
        {
            get
            {
                return GetLocale(Kernel32.LOCALE_SYSTEM_DEFAULT);
            }
        }
        /// <summary>
        /// Returns the CPU Architecture of the current machine (x86 / IA64 / AMD64)
        /// </summary>
        /// <value></value>
        static internal CPUType CPUArchitecture
        {
            get
            {
                if (IntPtr.Size == 4)
                {
                    return CPUType.x86;
                }

                throw new NotImplementedException("Cannot currently determine the difference between IA64 and AMD64 (look for Wow ?)");
            }
        }

        static private int GetLocale(uint localDefault)
        { 
            const int INFOSIZE = 6;
            StringBuilder info = new StringBuilder (INFOSIZE + 1);
            int charCount = Kernel32.GetLocaleInfo (Kernel32.LOCALE_USER_DEFAULT, Kernel32.LOCALE_IDEFAULTANSICODEPAGE, info, INFOSIZE);

            if (charCount == 0)
            {
                int error = Marshal.GetLastWin32Error ();

                Console.WriteLine ("Error occured");
                throw new ExternalException ("Native call to GetLocaleInfo failed !", error);
            }

            return int.Parse (info.ToString ());
        }
    }
}
