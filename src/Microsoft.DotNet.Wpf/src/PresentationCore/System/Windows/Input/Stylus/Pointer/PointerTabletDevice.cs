// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Win32.Pointer;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security;
using System.Windows.Input;

namespace System.Windows.Input.StylusPointer
{  
    /// <summary>
    /// A WM_POINTER based implementation of the TabletDeviceBase class.
    /// </summary>
    internal class PointerTabletDevice : TabletDeviceBase
    {
        #region Member Variables

        /// <summary>
        /// Device information with specific WM_POINTER extensions
        /// </summary>
        private PointerTabletDeviceInfo _deviceInfo;

        /// <summary>
        /// The StylusDevices owned by this tablet
        /// </summary>
        private StylusDeviceCollection _stylusDevices;

        /// <summary>
        /// A mapping from StylusDevice id to the actual StylusDevice for quick lookup.
        /// </summary>
        private Dictionary<uint, PointerStylusDevice> _stylusDeviceMap = new Dictionary<uint, PointerStylusDevice>();

        #endregion

        #region Properties

        /// <summary>
        /// Device information with specific WM_POINTER extensions
        /// </summary>
        internal PointerTabletDeviceInfo DeviceInfo { get { return _deviceInfo; } }

        /// <summary>
        /// The actual device pointer from the WM_POINTER information
        /// </summary>
        internal IntPtr Device { get { return _deviceInfo.Device; } }

        /// <summary>
        /// The max distance (in himetric, .1 mm units) between double (multi) taps
        /// </summary>
        internal int DoubleTapDelta
        {
            get
            {
                return _tabletInfo.DeviceType == TabletDeviceType.Touch ?
                    StylusLogic.CurrentStylusLogic.TouchDoubleTapDelta : StylusLogic.CurrentStylusLogic.StylusDoubleTapDelta;
            }
        }

        /// <summary>
        /// The max time (in milliseconds) between double (multi) taps
        /// </summary>
        internal int DoubleTapDeltaTime
        {
            get
            {
                return _tabletInfo.DeviceType == TabletDeviceType.Touch ? 
                    StylusLogic.CurrentStylusLogic.TouchDoubleTapDeltaTime : StylusLogic.CurrentStylusLogic.StylusDoubleTapDeltaTime;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the tablet and initializes its devices
        /// </summary>
        /// <param name="deviceInfo">Device information about this tablet</param>
        internal PointerTabletDevice(PointerTabletDeviceInfo deviceInfo)
            : base(deviceInfo)
        {
            _deviceInfo = deviceInfo;
            _tabletInfo = deviceInfo;

            UpdateSizeDeltas();

            BuildStylusDevices();
        }

        /// <summary>
        /// Creates all stylus devices for this specific tablet based on the tracked cursors in WM_POINTER.
        /// </summary>
        private void BuildStylusDevices()
        {
            UInt32 cursorCount = 0;

            List<PointerStylusDevice> pointerStylusDevices = new List<PointerStylusDevice>();

            if (UnsafeNativeMethods.GetPointerDeviceCursors(_deviceInfo.Device, ref cursorCount, null))
            {
                UnsafeNativeMethods.POINTER_DEVICE_CURSOR_INFO[] cursors = new UnsafeNativeMethods.POINTER_DEVICE_CURSOR_INFO[cursorCount];

                if (UnsafeNativeMethods.GetPointerDeviceCursors(_deviceInfo.Device, ref cursorCount, cursors))
                {
                    foreach (var cursor in cursors)
                    {
                        PointerStylusDevice stylus = new PointerStylusDevice(this, cursor);

                        _stylusDeviceMap.Add(stylus.CursorId, stylus);
                        pointerStylusDevices.Add(stylus);
                    }
                }
            }

            _stylusDevices = new StylusDeviceCollection(pointerStylusDevices.ToArray());
        }

        /// <summary>
        /// Updates the various size parameters for drag/drop/tap.
        /// </summary>
        /// <param name="description"></param>
        /// <param name="stylusLogic"></param>
        internal void UpdateSizeDeltas()
        {
            // Query default settings for mouse drag and double tap (with minimum of 1x1 size).
            Size mouseDragDefault = new Size(Math.Max(1, MS.Win32.SafeSystemMetrics.DragDeltaX / 2),
                                           Math.Max(1, MS.Win32.SafeSystemMetrics.DragDeltaY / 2));
            Size mouseDoubleTapDefault = new Size(Math.Max(1, MS.Win32.SafeSystemMetrics.DoubleClickDeltaX / 2),
                                           Math.Max(1, MS.Win32.SafeSystemMetrics.DoubleClickDeltaY / 2));

            StylusPointPropertyInfo xProperty = StylusPointDescription.GetPropertyInfo(StylusPointProperties.X);
            StylusPointPropertyInfo yProperty = StylusPointDescription.GetPropertyInfo(StylusPointProperties.Y);

            uint dwXValue = GetPropertyValue(xProperty);
            uint dwYValue = GetPropertyValue(yProperty);

            if (dwXValue != 0 && dwYValue != 0)
            {
                _doubleTapSize = new Size((int)Math.Round((ScreenSize.Width * DoubleTapDelta) / dwXValue),
                                          (int)Math.Round((ScreenSize.Height *DoubleTapDelta) / dwYValue));

                // Make sure we return whole numbers (pixels are whole numbers) and take the maximum
                // value between mouse and stylus settings to be safe.
                _doubleTapSize.Width = Math.Max(mouseDoubleTapDefault.Width, _doubleTapSize.Width);
                _doubleTapSize.Height = Math.Max(mouseDoubleTapDefault.Height, _doubleTapSize.Height);
            }
            else
            {
                // If no info to do the calculation then use the mouse settings for the default.
                _doubleTapSize = mouseDoubleTapDefault;
            }

            _forceUpdateSizeDeltas = false;
        }

        #endregion

        #region TabletDeviceBase Implementation

        /// <summary>
        /// The area that a double tap is considered in
        /// </summary>
        internal override Size DoubleTapSize
        {
            get
            {
                return _doubleTapSize;
            }
        }

        /// <summary>
        /// The StylusDevices owned by this tablet
        /// </summary>
        internal override StylusDeviceCollection StylusDevices
        {
            get
            {
                return _stylusDevices;
            }
        }

        /// <summary>
        /// The current target element for this tablet
        /// </summary>
        internal override IInputElement Target
        {
            get
            {
                return Stylus.CurrentStylusDevice?.Target;
            }
        }

        /// <summary>
        /// The currently active PresentationSource for this tablet
        /// </summary>
        internal override PresentationSource ActiveSource
        {
            get
            {
                return Stylus.CurrentStylusDevice?.ActiveSource;
            }
        }

        #endregion

        #region Pointer Stylus Specific Functions

        /// <summary>
        /// Retrieves the StylusDevice associated with the cursor id.
        /// </summary>
        /// <param name="cursorId">The id of the StylusDevice to retrieve</param>
        /// <returns>The StylusDevice associated with the id</returns>
        internal PointerStylusDevice GetStylusByCursorId(uint cursorId)
        {
            PointerStylusDevice stylus = null;
            _stylusDeviceMap.TryGetValue(cursorId, out stylus);
            return stylus;
        }

        #endregion
    }
}
