// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Test.Win32;

namespace Microsoft.Test.Display
{
    ///<Summary>Device resolution information</Summary>
    public class DisplaySettingsInfo
    { 
        internal User32.DEVMODE _devMode;

        internal DisplaySettingsInfo(User32.DEVMODE devMode)
        {
            // Do not log here, it will result in too much stuff (GetSupported create all supported display 
            //   settings :  can be in thousand, often in hundreds)

            if (devMode.dmSize == 0) { throw new ArgumentNullException("devMode", "Must be a valid instance of the struct (uninitialized struct passed in)"); }
            _devMode = devMode;
        }

        ///<Summary>Return the device width setting</Summary>
        public int Width
        {
            get { return _devMode.dmPelsWidth; }
        }
        ///<Summary>Return the device height setting</Summary>
        public int Height
        {
            get { return _devMode.dmPelsHeight; }
        }
        ///<Summary>Return the device color depth</Summary>
        public int BitsPerPixel
        {
            get { return _devMode.dmBitsPerPel; }
        }
        ///<Summary>Return the device frequency setting</Summary>
        public int Frequency
        {
            get { return _devMode.dmDisplayFrequency; }
        }

        /// <summary>X position</summary>
        public int PositionX
        {
            get { return _devMode.dmPositionX; }
        }

        /// <summary>Y position</summary>
        public int PositionY
        {
            get { return _devMode.dmPositionY; }
        }

        /// <summary>
        /// Return a user friendly representation of this class
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "DisplaySettingsInfo { width : " + Width.ToString() + 
                " / height : " + Height.ToString() + 
                " / bpp : " + BitsPerPixel.ToString() + 
                " / frequency : " + Frequency.ToString() + 
                " / PositionX: " + PositionX.ToString() + 
                " / PositionY: " + PositionY.ToString() + " }";
        }

        /// <summary>
        /// Get a hash value associated with this object
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Compare against another DisplaySettingsInfo for equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if ((obj is DisplaySettingsInfo) == false) { return false; }
            return this == (DisplaySettingsInfo)obj;
        }

        /// <summary>
        /// Check if 2 DisplaySettingsInfo are the same Display setting
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        public static bool operator ==(DisplaySettingsInfo d1, DisplaySettingsInfo d2)
        {
            if ( (d1 is DisplaySettingsInfo) == false && (d2 is DisplaySettingsInfo) == false) { return true; }
            if ( (d1 is DisplaySettingsInfo) == false || (d2 is DisplaySettingsInfo) == false)  { return false; }
            return d1.Width == d2.Width && d1.Height == d2.Height&& d1.Frequency== d2.Frequency && d1.BitsPerPixel== d2.BitsPerPixel && d1.PositionX == d2.PositionX && d1.PositionY == d2.PositionY;
        }

        /// <summary>
        /// Check if 2 DisplaySettingsInfo are different Display settings  
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        public static bool operator !=(DisplaySettingsInfo d1, DisplaySettingsInfo d2)
        {
            return !(d1 == d2);
        }
    }
}
