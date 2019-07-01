// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Test.Win32;

namespace Microsoft.Test.Display
{        
    ///<Summary>Contains information about display settings</Summary>
    public class DisplaySettings
    {
        #region Private Members

        private DisplaySettingsInfo[] _supported = null;
        private string _deviceName = string.Empty;

        #endregion

        #region Constructor

        internal DisplaySettings(string deviceName)
        {
            // TODO : Check params.            
            _deviceName = deviceName;

        }

        #endregion

        #region Public Members

        ///<Summary>Get / Set the display setting on this device </Summary>
        public DisplaySettingsInfo Current
        {
            get 
            {
                DisplaySettingsInfo retVal = null;
                User32.DEVMODE devMode = new User32.DEVMODE();
                devMode.dmSize = (short)Marshal.SizeOf(devMode);
                
                if (!User32.EnumDisplaySettingsEx(_deviceName, User32.ENUM_CURRENT_SETTINGS, ref devMode, 0))
                {
                    throw new Win32Exception();
                }

                retVal =  new DisplaySettingsInfo(devMode);
                return retVal;
            }
            set 
            {
                if (value == null) { throw new ArgumentNullException("Current", "value passed in cannot be null"); }

                User32.DEVMODE devMode = value._devMode;
                int result = User32.ChangeDisplaySettingsEx(_deviceName, ref devMode, IntPtr.Zero, User32.CDS_UPDATEREGISTRY, IntPtr.Zero);
                switch(result)
                {
                    case User32.DISP_CHANGE_SUCCESSFUL:    // success
                        break;
                    case User32.DISP_CHANGE_RESTART:
                        throw new ApplicationException("The computer must be restarted for the graphics mode to work");
                    case User32.DISP_CHANGE_BADMODE:
                        throw new ApplicationException("The graphics mode is not supported");
                    case User32.DISP_CHANGE_FAILED:
                        throw new Win32Exception();
                    case User32.DISP_CHANGE_NOTUPDATED:
                        throw new ApplicationException("Unable to write settings to the registry.");
                    case User32.DISP_CHANGE_BADFLAGS:
                        throw new ApplicationException("An invalid set of flags was passed in.");
                    case User32.DISP_CHANGE_BADPARAM:
                        throw new ApplicationException("An invalid parameter was passed in. This can include an invalid flag or combination of flags.");
                    case User32.DISP_CHANGE_BADDUALVIEW:
                        throw new ApplicationException("The settings change was unsuccessful because the system is DualView capable.");
                    default:
                        throw new ApplicationException("Internal error, this code path should never be reached (result=" + result.ToString() + ")");
                }
            }
        }

        ///<Summary>Return a collection of all the display setting supported by this device</Summary>
        public DisplaySettingsInfo[] GetSupported()
        {
            DisplaySettingsInfo[]  retVal = SupportedConfiguration();
            return retVal;
        }

        ///<Summary>Query this device to get the setting specified</Summary>
        public DisplaySettingsInfo Query(int width, int height, int bitPerPixel, int frequency)
        {
            if (width <= 0) { throw new ArgumentOutOfRangeException("width", "Argument must be strictly positive"); }
            if (height <= 0) { throw new ArgumentOutOfRangeException("height", "Argument must be strictly positive"); }
            if (bitPerPixel <= 0) { throw new ArgumentOutOfRangeException("bitPerPixel", "Argument must be strictly positive"); }
            if (frequency <= 0) { throw new ArgumentOutOfRangeException("frequency", "Argument must be strictly positive"); }

            DisplaySettingsInfo retVal = null;

            List<DisplaySettingsInfo> displayInfo = Query_Internal(width, height);
            for (int t = 0; t < displayInfo.Count; t++)
            {
                if (displayInfo[t].BitsPerPixel == bitPerPixel && displayInfo[t].Frequency == frequency)
                {
                    retVal = displayInfo[t];
                    break;
                }
            }
            return retVal;
        }

        ///<Summary>Query this device to get a list of support settings with the specified parameters</Summary>
        public DisplaySettingsInfo[] Query(int width, int height, int bitPerPixel)
        {
            if (width <= 0) { throw new ArgumentOutOfRangeException("width", "Argument must be strictly positive"); }
            if (height <= 0) { throw new ArgumentOutOfRangeException("height", "Argument must be strictly positive"); }
            if (bitPerPixel <= 0) { throw new ArgumentOutOfRangeException("bitPerPixel", "Argument must be strictly positive"); }

            List<DisplaySettingsInfo> displayInfo = Query_Internal(width, height);
            List<DisplaySettingsInfo> retVal = new List<DisplaySettingsInfo>();

            for (int t = 0; t < displayInfo.Count; t++)
            {
                if (displayInfo[t].BitsPerPixel == bitPerPixel)
                {
                    retVal.Add(displayInfo[t]);
                }
            }

            return retVal.ToArray();
        }

        ///<Summary>Query this device to get a list of support settings with the specified parameters</Summary>
        public DisplaySettingsInfo[] Query(int width, int height)
        {
            if (width <= 0) { throw new ArgumentOutOfRangeException("width", "Argument must be strictly positive"); }
            if (height <= 0) { throw new ArgumentOutOfRangeException("height", "Argument must be strictly positive"); }

            List<DisplaySettingsInfo> displayInfo = Query_Internal(width, height);

            return displayInfo.ToArray();

        }

        #endregion

        #region Private Members

        private List<DisplaySettingsInfo> Query_Internal(int width, int height)
        {
            if (width <= 0) { throw new ArgumentOutOfRangeException("width", "Argument must be strictly positive"); }
            if (height <= 0) { throw new ArgumentOutOfRangeException("height", "Argument must be strictly positive"); }

            List<DisplaySettingsInfo> retVal = new List<DisplaySettingsInfo>();
            DisplaySettingsInfo[] displaySettings = SupportedConfiguration();
            for (int t = 0; t < displaySettings.Length; t++)
            {
                if (displaySettings[t].Width == width && displaySettings[t].Height == height)
                {
                    retVal.Add(displaySettings[t]);
                }
            }
            return retVal;
        }

        private DisplaySettingsInfo[] SupportedConfiguration()
        {
            if (_supported == null)
            {
                List<DisplaySettingsInfo> retVal = new List<DisplaySettingsInfo>();
                bool success = true;
                uint index = 0;
                while (success)
                {
                    User32.DEVMODE devMode = new User32.DEVMODE();
                    devMode.dmSize = (short)Marshal.SizeOf(devMode);
                    success = User32.EnumDisplaySettings(_deviceName, index++, ref devMode);
                    if (success)
                    {
                        retVal.Add(new DisplaySettingsInfo(devMode));
                    }
                }
                _supported = retVal.ToArray();
            }

            return _supported;

        }

        #endregion
    }
}
