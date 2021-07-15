// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// The hardware capabilities of a tablet device.
    /// </summary>
    [Flags][Serializable]
    public enum TabletHardwareCapabilities
    {
        /// <summary> No capabilities bits set</summary>
        None                    = 0x0,

        /// <summary> Indicates that the digitizer is integrated with the display.</summary>
        Integrated              = 0x1,

        /// <summary> 
        /// Indicates that the StylusDevice must be in physical contact 
        /// with the device to report position.
        /// </summary>
        StylusMustTouch         = 0x2,

        /// <summary> 
        /// Indicates that the device can generate in-air packets when the 
        /// StylusDevice is in the physical detection range (proximity) of the device. 
        /// </summary>
        HardProximity           = 0x4,
    
        /// <summary> 
        /// Indicates that the device can uniquely identify the active StylusDevice.
        /// </summary>
        StylusHasPhysicalIds    = 0x8,

        /// <summary> 
        /// Indicates that the device supports pressure information
        /// </summary>
        SupportsPressure    = 0x40000000, //bit 31
    }
}
