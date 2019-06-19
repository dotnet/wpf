// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Internal;
using MS.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Windows.Input.StylusWisp;
using System.Windows.Input.Tracing;
using System.Windows.Media;
using System.Windows.Threading;

namespace System.Windows.Input
{
    /// <summary>
    ///
    /// The base of a private hierarchy for TabletDevices as the TabletDevice class is
    /// public and sealed and should not be changed.
    /// </summary>
    internal abstract class TabletDeviceBase : DispatcherObject, IDisposable
    {
        #region Constants

        /// <summary>
        /// Legacy default value for properties.  Maintained for compat with WISP stack.
        /// </summary>
        private const uint DefaultPropertyValue = 1000;

        #endregion

        #region Casting/Wrapper Access

        /// <summary>
        /// The TabletDevice wrapper associated with this implementation instance.
        /// </summary>
        internal TabletDevice TabletDevice { get; private set; }

        internal T As<T>()
            where T : TabletDeviceBase
        {
            return this as T;
        }

        #endregion

        #region Constructor

        protected TabletDeviceBase(TabletDeviceInfo info)
        {
            // Generate a wrapper for public use
            TabletDevice = new TabletDevice(this);

            _tabletInfo = info;

            if (_tabletInfo.DeviceType == TabletDeviceType.Touch)
            {
                // A touch device requires multi-touch logic
                _multiTouchSystemGestureLogic = new MultiTouchSystemGestureLogic();
            }
        }

        #endregion

        #region IDisposable

        protected bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Allow for custom disposal from derived types
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            _disposed = true;
        }

        ~TabletDeviceBase()
        {
            Dispose(false);
        }

        #endregion

        #region InputDevice

        /// <summary>
        ///     Returns the element that input from this device is sent to.
        /// </summary>
        internal abstract IInputElement Target { get; }

        /// <summary>
        ///     Returns the PresentationSource that is reporting input for this device.
        /// </summary>
        internal abstract PresentationSource ActiveSource { get; }

        #endregion

        #region Wrapper Functions/Properties

        /// <summary>
        ///     Returns an id of the tablet object unique within the process.
        /// </summary>
        internal int Id
        {
            get
            {
                VerifyAccess();
                return _tabletInfo.Id;
            }
        }

        /// <summary>
        ///     Returns the friendly name of the tablet.
        /// </summary>
        internal string Name
        {
            get
            {
                VerifyAccess();
                return _tabletInfo.Name;
            }
        }

        /// <summary>
        ///     Returns the hardware Product ID of the tablet (was PlugAndPlayId).
        /// </summary>
        internal string ProductId
        {
            get
            {
                VerifyAccess();
                return _tabletInfo.PlugAndPlayId;
            }
        }


        /// <summary>
        ///     Returns the capabilities of the tablet hardware.
        /// </summary>
        internal TabletHardwareCapabilities TabletHardwareCapabilities
        {
            get
            {
                VerifyAccess();
                return _tabletInfo.HardwareCapabilities;
            }
        }

        /// <summary>
        ///     Returns the list of StylusPointProperties supported by this TabletDevice.
        /// </summary>
        internal ReadOnlyCollection<StylusPointProperty> SupportedStylusPointProperties
        {
            get
            {
                VerifyAccess();
                return _tabletInfo.StylusPointProperties;
            }
        }

        /// <summary>
        ///     Returns the type of tablet device hardware (Stylus, Touch)
        /// </summary>
        internal TabletDeviceType Type
        {
            get
            {
                VerifyAccess();
                return _tabletInfo.DeviceType;
            }
        }

        /// <summary>
        ///     Returns the friendly string representation of the Tablet object
        /// </summary>
        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}({1})", base.ToString(), Name);
        }

        /// <summary>
        ///     Returns the collection of StylusDevices defined on this tablet.
        ///     An Empty collection is returned if the device has not seen any Stylus Pointers.
        /// </summary>
        internal abstract StylusDeviceCollection StylusDevices { get; }

        #endregion

        #region Internal API
                            
        /// <summary>
        ///     Sends input reports to the system gesture logic object.
        /// </summary>
        /// <param name="stylusInputReport">A new input report.</param>
        /// <returns>A SystemGesture that was detected, null otherwise.</returns>
        internal SystemGesture? GenerateStaticGesture(RawStylusInputReport stylusInputReport)
        {
            return _multiTouchSystemGestureLogic?.GenerateStaticGesture(stylusInputReport);
        }

        /// <summary>
        ///     Returns the maximum coordinate dimensions that the device can collect.
        ///     This value is in Tablet Coordinates
        /// </summary>
        internal Matrix TabletToScreen
        {
            get
            {
                return new Matrix(_tabletInfo.SizeInfo.ScreenSize.Width / _tabletInfo.SizeInfo.TabletSize.Width, 0,
                                   0, _tabletInfo.SizeInfo.ScreenSize.Height / _tabletInfo.SizeInfo.TabletSize.Height,
                                   0, 0);
            }
        }

        /// <summary>
        ///     Returns the size of the digitizer for this TabletDevice.
        ///     This value is in Tablet Coordinates.
        /// </summary>
        internal Size TabletSize
        {
            get
            {
                return _tabletInfo.SizeInfo.TabletSize;
            }
        }

        /// <summary>
        ///     Returns the size for the display that this TabletDevice is
        ///     mapped to.
        ///     This value is in Screen Coordinates.
        /// </summary>
        internal Size ScreenSize
        {
            get
            {
                return _tabletInfo.SizeInfo.ScreenSize;
            }
        }

        /// <summary>
        /// The size (in device pixels) of a double tap range
        /// </summary>
        internal abstract Size DoubleTapSize { get; }

        // Helper to return a StylusPointDescription using the SupportedStylusProperties info.
        internal StylusPointDescription StylusPointDescription
        {
            get
            {
                if (_stylusPointDescription == null)
                {
                    ReadOnlyCollection<StylusPointProperty> properties = SupportedStylusPointProperties;

                    // InitializeSupportStylusPointProperties must be called first!
                    Debug.Assert(properties != null);

                    List<StylusPointPropertyInfo> propertyInfos = new List<StylusPointPropertyInfo>();

                    foreach (var prop in properties)
                    {
                        propertyInfos.Add((prop is StylusPointPropertyInfo) ? (StylusPointPropertyInfo)prop : new StylusPointPropertyInfo(prop));
                    }

                    _stylusPointDescription = new StylusPointDescription(propertyInfos, _tabletInfo.PressureIndex);
                }

                return _stylusPointDescription;
            }
        }

        #endregion

        #region Utility Functions

        protected static uint GetPropertyValue(StylusPointPropertyInfo propertyInfo)
        {
            uint dw = DefaultPropertyValue;

            switch (propertyInfo.Unit)
            {
                case StylusPointPropertyUnit.Inches:
                    if (propertyInfo.Resolution != 0)
                        dw = (uint)(((propertyInfo.Maximum - propertyInfo.Minimum) * 254) / propertyInfo.Resolution);
                    break;

                case StylusPointPropertyUnit.Centimeters:
                    if (propertyInfo.Resolution != 0)
                        dw = (uint)(((propertyInfo.Maximum - propertyInfo.Minimum) * 100) / propertyInfo.Resolution);
                    break;
            }

            return dw;
        }

        #endregion

        #region Member Variables

        // Calculated size in screen coordinates for Drag and DoubleTap detection.
        protected Size _doubleTapSize = Size.Empty;
        protected bool _forceUpdateSizeDeltas;

        private MultiTouchSystemGestureLogic _multiTouchSystemGestureLogic;

        protected TabletDeviceInfo _tabletInfo;

        protected StylusPointDescription _stylusPointDescription;

        #endregion
    }
}
