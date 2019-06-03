// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;

namespace System.Windows.Input
{
    /// <summary>
    /// Stylus data is made up of n number of properties. Each property can contain one or more
    /// values such as x or y coordinate or button states.
    /// This enum defines the various possible units for the values in the stylus data
    /// </summary>
    /// <ExternalAPI/>
    public enum StylusPointPropertyUnit
    {
        /// <summary>Specifies that the units are unknown.</summary>
        /// <ExternalAPI/>
        None = 0,
        /// <summary>Specifies that the property value is in inches (distance units).</summary>
        /// <ExternalAPI/>
        Inches = 1,
        /// <summary>Specifies that the property value is in centimeters (distance units).</summary>
        /// <ExternalAPI/>
        Centimeters = 2,
        /// <summary>Specifies that the property value is in degrees (angle units).</summary>
        /// <ExternalAPI/>
        Degrees = 3,
        /// <summary>Specifies that the property value is in radians (angle units).</summary>
        /// <ExternalAPI/>
        Radians = 4,
        /// <summary>Specifies that the property value is in seconds (angle units).</summary>
        /// <ExternalAPI/>
        Seconds = 5,
        /// <ExternalAPI/>
        /// <summary>Specifies that the property value is in pounds (force, or mass, units).</summary>
        Pounds = 6,
        /// <ExternalAPI/>
        /// <summary>Specifies that the property value is in grams (force, or mass, units).</summary>
        Grams = 7
    }

    /// <summary>
    /// Used to validate the enum
    /// 
    ///
    /// Added various functions to support WM_POINTER based stack
    /// </summary>
    internal static class StylusPointPropertyUnitHelper
    {
        #region Constants

        /// <summary>
        /// Mask to extract units from raw WM_POINTER data
        /// <see cref="http://www.usb.org/developers/hidpage/Hut1_12v2.pdf"/> 
        /// </summary>
        private const uint UNIT_MASK = 0x000F;

        #endregion

        #region Conversion Maps

        /// <summary>
        /// Mapping for WM_POINTER based unit, taken from legacy WISP code
        /// </summary>
        private static Dictionary<uint, StylusPointPropertyUnit> _pointerUnitMap = new Dictionary<uint, StylusPointPropertyUnit>()
        {
            { 1, StylusPointPropertyUnit.Centimeters },
            { 2, StylusPointPropertyUnit.Radians },
            { 3, StylusPointPropertyUnit.Inches },
            { 4, StylusPointPropertyUnit.Degrees },
        };

        #endregion

        #region Utility Functions

        /// <summary>
        /// Convert WM_POINTER units to WPF units
        /// </summary>
        /// <param name="pointerUnit"></param>
        /// <returns></returns>
        internal static StylusPointPropertyUnit? FromPointerUnit(uint pointerUnit)
        {
            StylusPointPropertyUnit unit = StylusPointPropertyUnit.None;

            _pointerUnitMap.TryGetValue(pointerUnit & UNIT_MASK, out unit);

            return (IsDefined(unit)) ? unit : (StylusPointPropertyUnit?)null;
        }

        internal static bool IsDefined(StylusPointPropertyUnit unit)
        {
            if (unit >= StylusPointPropertyUnit.None && unit <= StylusPointPropertyUnit.Grams)
            {
                return true;
            }
            return false;
        }

        #endregion
    }
}
