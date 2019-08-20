// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Win32.Pointer;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security;
using System.Windows.Input;

namespace System.Windows.Input.StylusPointer
{
    /// <summary>
    /// WM_POINTER specific information about a TabletDevice
    /// </summary>
    internal class PointerTabletDeviceInfo : TabletDeviceInfo
    {
        #region Member Variables

        /// <summary>
        /// Store the WM_POINTER device information directly
        /// </summary>
        private UnsafeNativeMethods.POINTER_DEVICE_INFO _deviceInfo;

        #endregion

        #region Properties

        /// <summary>
        /// The pointer properties supported by this TabletDevice
        /// </summary>
        internal UnsafeNativeMethods.POINTER_DEVICE_PROPERTY[] SupportedPointerProperties { get; private set; }

        /// <summary>
        /// The index of the start of button properties in SupportedPointerProperties.
        /// If there are no buttons, this will be -1.
        /// </summary>
        internal int SupportedButtonPropertyIndex { get; private set; }

        /// <summary>
        /// The buttons supported by this tablet
        /// </summary>
        internal StylusButtonCollection StylusButtons { get; private set; }

        /// <summary>
        /// The specific id for this TabletDevice
        /// </summary>
        internal IntPtr Device { get { return _deviceInfo.device; } }

        /// <summary>
        /// Indicates if the hardware did not specify pressure but we have inserted it anyway.
        /// </summary>
        internal bool UsingFakePressure { get; private set; } = false;

        /// <summary>
        /// The rectangle bounds for the entire device
        /// </summary>
        internal UnsafeNativeMethods.RECT DeviceRect { get; private set; } = new UnsafeNativeMethods.RECT();

        /// <summary>
        /// The rectangle bounds for the display associated with the device
        /// </summary>
        internal UnsafeNativeMethods.RECT DisplayRect { get; private set; } = new UnsafeNativeMethods.RECT();

        #endregion

        #region Constructor/Initialization

        /// <summary>
        /// Constructor to convert WM_POINTER stack device info to the more generic TabletDeviceInfo
        /// </summary>
        /// <param name="deviceInfo">The WM_POINTER device info</param>
        internal PointerTabletDeviceInfo(int id, UnsafeNativeMethods.POINTER_DEVICE_INFO deviceInfo)
        {
            _deviceInfo = deviceInfo;

            Id = id;
            Name = _deviceInfo.productString;
            PlugAndPlayId = _deviceInfo.productString;
        }

        /// <summary>
        /// Initializes the device information for this device
        /// </summary>
        /// <returns>
        /// True if initialization succeeds, false otherwise.
        /// </returns>
        internal bool TryInitialize()
        {
            bool success = true;

            InitializeDeviceType();
            
            success = TryInitializeSupportedStylusPointProperties();

            // If we fail to initialize properties we may or may not get a failure in
            // the win32 call in TryInitializeDeviceRects.  If we don't fail, we will crash
            // so add a guard here as well.
            if (success)
            {
                success = TryInitializeDeviceRects();
            }

            return success;
        }

        /// <summary>
        /// Convert from WM_POINTER device types to WPF equivalents and set appropriate hardware caps.
        /// </summary>
        /// <remarks>
        /// Surprisingly enough, WISP only supports Integreated and HardProximity 
        /// See Stylus\Biblio.txt - 3
        /// As such we mirror their support here.
        /// </remarks>
        private void InitializeDeviceType()
        {
            switch (_deviceInfo.pointerDeviceType)
            {
                case UnsafeNativeMethods.POINTER_DEVICE_TYPE.POINTER_DEVICE_TYPE_EXTERNAL_PEN:
                    {
                        DeviceType = TabletDeviceType.Stylus;
                    }
                    break;
                case UnsafeNativeMethods.POINTER_DEVICE_TYPE.POINTER_DEVICE_TYPE_INTEGRATED_PEN:
                    {
                        DeviceType = TabletDeviceType.Stylus;
                        HardwareCapabilities |= TabletHardwareCapabilities.Integrated;
                    }
                    break;
                case UnsafeNativeMethods.POINTER_DEVICE_TYPE.POINTER_DEVICE_TYPE_TOUCH:
                    {
                        DeviceType = TabletDeviceType.Touch;
                        HardwareCapabilities |= TabletHardwareCapabilities.Integrated;
                    }
                    break;
                case UnsafeNativeMethods.POINTER_DEVICE_TYPE.POINTER_DEVICE_TYPE_TOUCH_PAD:
                    {
                        DeviceType = TabletDeviceType.Touch;
                    }
                    break;
            }

            HardwareCapabilities |= TabletHardwareCapabilities.HardProximity;
        }

        /// <summary>
        /// Query all supported properties from WM_POINTER stack and convert to WPF equivalents.
        /// 
        /// This maintains a set of properties from WM_POINTER that are supported and the equivalent
        /// properties in WPF.  This way, the WM_POINTER properties can be used to directly query
        /// raw data from the stack and associate it 1 to 1 with raw stylus data in WPF.
        /// </summary>
        /// <returns>
        /// True if initialization succeeds, false otherwise.
        /// </returns>
        /// <remarks>
        /// A note on the pressure property.  If the device does not support pressure as a property,
        /// we still insert pressure on the WPF side.  This is a requirement in many places in the
        /// stylus code.  However, the raw data queried from WM_POINTER will NOT have pressure data.
        /// This is ok because StylusPointCollection pivots on the ContainsTruePressure property in
        /// the StylusPointDescription.  If this is true, every point will have X, Y, Pressure and then
        /// additional raw data from index 3..N where N is the number of properties per StylusPoint.
        /// If this is false, it will add a default pressure so WPF ends up with X, Y, DefaultPressure
        /// and then 2..N per StylusPoint.  This can be slightly confusing at first glance, since pressure
        /// of one kind or another is always required in the StylusPointDescription, but it allows for
        /// backfilling of pressure data further up (as in towards the public boundary) and the deeper
        /// portions of the stack just worry about querying device capabilities.
        /// </remarks>
        private bool TryInitializeSupportedStylusPointProperties()
        {
            bool success = false;

            uint propCount = 0;

            // Initialize to having not seen a real pressure property
            PressureIndex = -1;
            UsingFakePressure = true;

            // Retrieve all properties from the WM_POINTER stack
            success = UnsafeNativeMethods.GetPointerDeviceProperties(Device, ref propCount, null);

            if (success)
            {
                SupportedPointerProperties = new UnsafeNativeMethods.POINTER_DEVICE_PROPERTY[propCount];

                success = UnsafeNativeMethods.GetPointerDeviceProperties(Device, ref propCount, SupportedPointerProperties);

                if (success)
                {
                    // Prepare a location for X, Y, and Pressure
                    List<StylusPointProperty> properties = new List<StylusPointProperty>()
                    {
                        StylusPointPropertyInfoDefaults.X,
                        StylusPointPropertyInfoDefaults.Y,
                        StylusPointPropertyInfoDefaults.NormalPressure,
                    };

                    List<StylusPointProperty> buttonProperties = new List<StylusPointProperty>();

                    // Prepare a location for X and Y.  Pressure does not need a location as it will be added later if applicable.
                    List<UnsafeNativeMethods.POINTER_DEVICE_PROPERTY> supportedProperties = new List<UnsafeNativeMethods.POINTER_DEVICE_PROPERTY>()
                    {
                        new UnsafeNativeMethods.POINTER_DEVICE_PROPERTY(),
                        new UnsafeNativeMethods.POINTER_DEVICE_PROPERTY(),
                    };

                    List<UnsafeNativeMethods.POINTER_DEVICE_PROPERTY> supportedButtonProperties = new List<UnsafeNativeMethods.POINTER_DEVICE_PROPERTY>();

                    bool seenX = false, seenY = false, seenPressure = false;

                    foreach (var prop in SupportedPointerProperties)
                    {
                        StylusPointPropertyInfo propInfo = PointerStylusPointPropertyInfoHelper.CreatePropertyInfo(prop);

                        if (propInfo != null)
                        {
                            // If seeing a required property, just overwrite the default placeholder
                            // otherwise tack it onto the end of the appropriate list.
                            if (propInfo.Id == StylusPointPropertyIds.NormalPressure)
                            {
                                seenPressure = true;
                                properties[StylusPointDescription.RequiredPressureIndex] = propInfo;

                                // Pressure is not in the pointer properties by default so we must insert it.
                                supportedProperties.Insert(StylusPointDescription.RequiredPressureIndex, prop);
                            }
                            else if (propInfo.Id == StylusPointPropertyIds.X)
                            {
                                seenX = true;
                                properties[StylusPointDescription.RequiredXIndex] = propInfo;
                                supportedProperties[StylusPointDescription.RequiredXIndex] = prop;
                            }
                            else if (propInfo.Id == StylusPointPropertyIds.Y)
                            {
                                seenY = true;
                                properties[StylusPointDescription.RequiredYIndex] = propInfo;
                                supportedProperties[StylusPointDescription.RequiredYIndex] = prop;
                            }
                            else if (propInfo.IsButton)
                            {
                                buttonProperties.Add(propInfo);
                                supportedButtonProperties.Add(prop);
                            }
                            else
                            {
                                properties.Add(propInfo);
                                supportedProperties.Add(prop);
                            }
                        }
                    }

                    // If we saw a real pressure property, we should mark that down
                    if (seenPressure)
                    {
                        PressureIndex = StylusPointDescription.RequiredPressureIndex;
                        UsingFakePressure = false;
                        HardwareCapabilities |= TabletHardwareCapabilities.SupportsPressure;
                    }

                    Debug.Assert(properties[StylusPointDescription.RequiredXIndex /*0*/].Id == StylusPointPropertyIds.X || !seenX,
                        "X isn't where we expect it! Fix pointer stack to ask for X at index 0");
                    Debug.Assert(properties[StylusPointDescription.RequiredYIndex /*1*/].Id == StylusPointPropertyIds.Y || !seenY,
                        "Y isn't where we expect it! Fix pointer stack to ask for Y at index 1");
                    Debug.Assert(properties[StylusPointDescription.RequiredPressureIndex /*1*/].Id == StylusPointPropertyIds.NormalPressure /*2*/,
                        "Fix pointer stack to ask for NormalPressure at index 2!");

                    // Append buttons to the end of normal properties
                    properties.AddRange(buttonProperties);

                    SupportedButtonPropertyIndex = supportedProperties.Count;
                    supportedProperties.AddRange(supportedButtonProperties);

                    // Reset the properties to only what we support, this way we can generate raw data directly from them
                    StylusPointProperties = new ReadOnlyCollection<StylusPointProperty>(properties);
                    SupportedPointerProperties = supportedProperties.ToArray();
                }
            }

            return success;
        }

        /// <summary>
        /// Queries WM_POINTER device/screen rectangles
        /// </summary>
        /// <returns>
        /// True if initialization succeeds, false otherwise.
        /// </returns>
        private bool TryInitializeDeviceRects()
        {
            bool success = false;

            var deviceRect = new UnsafeNativeMethods.RECT();
            var displayRect = new UnsafeNativeMethods.RECT();

            success = UnsafeNativeMethods.GetPointerDeviceRects(_deviceInfo.device, ref deviceRect, ref displayRect);

            if (success)
            {
                DeviceRect = deviceRect;

                DisplayRect = displayRect;

                // We use the max X and Y properties here as this is more readily useful for raw data
                // which is where all conversions come from.
                SizeInfo = new TabletDeviceSizeInfo(
                    new Size(SupportedPointerProperties[StylusPointDescription.RequiredXIndex].logicalMax,
                    SupportedPointerProperties[StylusPointDescription.RequiredYIndex].logicalMax),
                    new Size(displayRect.right - displayRect.left, displayRect.bottom - displayRect.top));
            }

            return success;
        }

        #endregion
    }
}
