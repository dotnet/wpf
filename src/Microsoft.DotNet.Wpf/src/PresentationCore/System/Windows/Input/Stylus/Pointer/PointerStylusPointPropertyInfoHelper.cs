// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using MS.Utility;
using MS.Win32.Pointer;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input.StylusPointer
{
    /// <summary>
    /// Contains a WM_POINTER specific functions to parse out stylus property info
    /// </summary>
    internal class PointerStylusPointPropertyInfoHelper
    {
        #region Constants

        private const byte HidExponentMask = 0xF;

        #endregion

        #region Private Members

        /// <summary>
        /// Contains the mappings from WM_POINTER exponents to our local supported values.
        /// This mapping is taken from WISP code, see Stylus\Biblio.txt - 4,
        /// as an array of HidExponents.
        /// </summary>
        private static Dictionary<byte, short> _hidExponentMap = new Dictionary<byte, short>()
        {
            { 5, 5 },
            { 6, 6 },
            { 7, 7 },
            { 8, -8 },
            { 9, -7 },
            { 0xA, -6 },
            { 0xB, -5 },
            { 0xC, -4 },
            { 0xD, -3 },
            { 0xE, -2 },
            { 0xF, -1 },
        };

        #endregion

        #region Utility Functions

        /// <summary>
        /// Creates WPF property infos from WM_POINTER device properties.  This appropriately maps and converts HID spec
        /// properties found in WM_POINTER to their WPF equivalents.  This is based on code from the WISP implementation
        /// that feeds the legacy WISP based stack.
        /// </summary>
        /// <param name="prop">The pointer property to convert</param>
        /// <returns>The equivalent WPF property info</returns>
        internal static StylusPointPropertyInfo CreatePropertyInfo(UnsafeNativeMethods.POINTER_DEVICE_PROPERTY prop)
        {
            StylusPointPropertyInfo result = null;

            // Get the mapped GUID for the HID usages
            Guid propGuid =
                StylusPointPropertyIds.GetKnownGuid(
                    (StylusPointPropertyIds.HidUsagePage)prop.usagePageId,
                    (StylusPointPropertyIds.HidUsage)prop.usageId);

            if (propGuid != Guid.Empty)
            {
                StylusPointProperty stylusProp = new StylusPointProperty(propGuid, StylusPointPropertyIds.IsKnownButton(propGuid));

                // Set Units
                StylusPointPropertyUnit? unit = StylusPointPropertyUnitHelper.FromPointerUnit(prop.unit);

                // If the parsed unit is invalid, set the default
                if (!unit.HasValue)
                {
                    unit = StylusPointPropertyInfoDefaults.GetStylusPointPropertyInfoDefault(stylusProp).Unit;
                }

                // Set to default resolution
                float resolution = StylusPointPropertyInfoDefaults.GetStylusPointPropertyInfoDefault(stylusProp).Resolution;

                short mappedExponent = 0;

                if (_hidExponentMap.TryGetValue((byte)(prop.unitExponent & HidExponentMask), out mappedExponent))
                {
                    float exponent = (float)Math.Pow(10, mappedExponent);

                    // Guard against divide by zero or negative resolution
                    if (prop.physicalMax - prop.physicalMin > 0)
                    {
                        // Calculated resolution is a scaling factor from logical units into the physical space
                        // at the given exponentiation.
                        resolution =
                            (prop.logicalMax - prop.logicalMin) / ((prop.physicalMax - prop.physicalMin) * exponent);
                    }
                }

                result = new StylusPointPropertyInfo(
                      stylusProp,
                      prop.logicalMin,
                      prop.logicalMax,
                      unit.Value,
                      resolution);
            }

            return result;
        }

        #endregion
    }
}
