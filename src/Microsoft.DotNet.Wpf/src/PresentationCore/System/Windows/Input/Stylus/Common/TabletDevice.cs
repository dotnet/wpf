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

namespace System.Windows.Input
{
    /// <summary>
    ///     Class that represents a physical digitizer connected to the system.
    ///     Tablets are the source of events for the Stylus devices.
    /// </summary>
    public sealed class TabletDevice : InputDevice
    {
        #region Implementation

        /// <summary>
        /// The base implementation in the private hierarchy
        /// </summary>
        internal TabletDeviceBase TabletDeviceImpl { get; set; } = null;

        #endregion

        #region Constructor

        internal TabletDevice(TabletDeviceBase impl)
        {
            if (impl == null)
            {
                throw new ArgumentNullException(nameof(impl));
            }

            TabletDeviceImpl = impl;
        }

        #endregion

        #region Public API

        /// <summary>
        ///     Returns the element that input from this device is sent to.
        /// </summary>
        public override IInputElement Target
        {
            get
            {
                VerifyAccess();
                return TabletDeviceImpl.Target;
            }
        }

        /// <summary>
        ///     Returns the PresentationSource that is reporting input for this device.
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public override PresentationSource ActiveSource
        {
            get
            {
                VerifyAccess();
                return TabletDeviceImpl.ActiveSource;
            }
        }

        /// <summary>
        ///     Returns an id of the tablet object unique within the process.
        /// </summary>
        public int Id
        {
            get
            {
                VerifyAccess();
                return TabletDeviceImpl.Id;
            }
        }

        /// <summary>
        ///     Returns the friendly name of the tablet.
        /// </summary>
        public string Name
        {
            get
            {
                VerifyAccess();
                return TabletDeviceImpl.Name;
            }
        }

        /// <summary>
        ///     Returns the hardware Product ID of the tablet (was PlugAndPlayId).
        /// </summary>
        public string ProductId
        {
            get
            {
                VerifyAccess();
                return TabletDeviceImpl.ProductId;
            }
        }

        /// <summary>
        ///     Returns the capabilities of the tablet hardware.
        /// </summary>
        public TabletHardwareCapabilities TabletHardwareCapabilities
        {
            get
            {
                VerifyAccess();
                return TabletDeviceImpl.TabletHardwareCapabilities;
            }
        }

        /// <summary>
        ///     Returns the list of StylusPointProperties supported by this TabletDevice.
        /// </summary>
        public ReadOnlyCollection<StylusPointProperty> SupportedStylusPointProperties
        {
            get
            {
                VerifyAccess();
                return TabletDeviceImpl.SupportedStylusPointProperties;
            }
        }

        /// <summary>
        ///     Returns the type of tablet device hardware (Stylus, Touch)
        /// </summary>
        public TabletDeviceType Type
        {
            get
            {
                VerifyAccess();
                return TabletDeviceImpl.Type;
            }
        }

        /// <summary>
        ///     Returns the friendly string representation of the Tablet object
        /// </summary>
        public override string ToString()
        {
            return TabletDeviceImpl.ToString();
        }

        /// <summary>
        ///     Returns the collection of StylusDevices defined on this tablet.
        ///     An Empty collection is returned if the device has not seen any Stylus Pointers.
        /// </summary>
        public StylusDeviceCollection StylusDevices
        {
            get
            {
                VerifyAccess();
                return TabletDeviceImpl.StylusDevices;
            }
        }

        #endregion

        #region Casting

        /// <summary>
        /// Converts to an implementation type
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <returns>The implementation</returns>
        internal T As<T>()
            where T : TabletDeviceBase
        {
            return TabletDeviceImpl as T;
        }

        #endregion
    }
}
