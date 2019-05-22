// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Range Value pattern provider interface

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Exposes a related set of properties that reflect a controls ability to manage a value
    /// within a finite range.  It conveys a controls valid minimum and maximum values and its
    /// current value.
    ///
    /// Examples:
    ///
    ///     Numeric Spinners
    ///     Progress Bar,
    ///     IP Control (on the individual octets)
    ///     some Color Pickers
    ///     ScrollBars
    ///     some Sliders
    ///
    /// public interface that represents UI elements that are expressing a current value and a value range.
    ///
    /// public interface has same definition as IValueProvider.  The two patterns' difference is that
    /// RangeValue has additional properties, and properties generally do not appear in the pattern
    /// public interfaces.
    /// </summary>
    [ComVisible(true)]
    [Guid("36dc7aef-33e6-4691-afe1-2be7274b3d33")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IRangeValueProvider
#else
    public interface IRangeValueProvider
#endif
    {
        /// <summary>
        /// Request to set the value that this UI element is representing
        /// </summary>
        /// <param name="value">Value to set the UI to</param>
        /// <returns>true if the UI element was successfully set to the specified value</returns>
        void SetValue(double value);

        ///<summary>Value of a value control, as a a string.</summary>
        double Value
        {
            get;
        }

        ///<summary>Indicates that the value can only be read, not modified.
        ///returns True if the control is read-only</summary>
        bool IsReadOnly
        {
            [return: MarshalAs(UnmanagedType.Bool)] //  Without this, only lower SHORT of BOOL*pRetVal param is updated.
            get;
        }

        ///<summary>maximum value </summary>
        double Maximum
        {
            get;
        }

        ///<summary>minimum value</summary>
        double Minimum
        {
            get;
        }

        ///<summary>The amount a large change will change the value by</summary>
        double LargeChange
        {
            get;
        }

        ///<summary>The amount a small change will change the value by</summary>
        double SmallChange
        {
            get;
        }
    }
}
