// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Win32.Pointer;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using System.Security;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Input.Tracing;

namespace System.Windows.Input.StylusPointer
{
    /// <summary>
    /// Maintains a collection of pointer device information for currently installed pointer devices
    /// </summary>
    internal class PointerTabletDeviceCollection : TabletDeviceCollection
    {
        #region Private Members

        /// <summary>
        /// Holds a mapping of TabletDevices from their WM_POINTER device id
        /// </summary>
        private Dictionary<IntPtr, PointerTabletDevice> _tabletDeviceMap = new Dictionary<IntPtr, PointerTabletDevice>();

        #endregion

        #region Properties

        internal bool IsValid { get; private set; } = false;

        #endregion

        #region Collection Manipulation

        /// <summary>
        /// Retrieve the TabletDevice associated with the device id
        /// </summary>
        /// <param name="deviceId">The device id</param>
        /// <returns>The TabletDevice associated with the device id</returns>
        internal PointerTabletDevice GetByDeviceId(IntPtr deviceId)
        {
            PointerTabletDevice tablet = null;

            _tabletDeviceMap.TryGetValue(deviceId, out tablet);

            return tablet;
        }

        /// <summary>
        /// Retrieve the StylusDevice associated with the cursor id
        /// </summary>
        /// <param name="cursorId">The cursor id</param>
        /// <returns>The StylusDevice associated with the device id</returns>
        internal PointerStylusDevice GetStylusDeviceByCursorId(uint cursorId)
        {
            PointerStylusDevice stylus = null;

            foreach (var tablet in _tabletDeviceMap.Values)
            {
                if ((stylus = tablet.GetStylusByCursorId(cursorId)) != null)
                {
                    break;
                }
            }

            return stylus;
        }

        /// <summary>
        /// Retrieves the latest device information from connected touch devices.
        /// </summary>
        internal void Refresh()
        {
            try
            {
                // Keep track of old tablets so that we can properly log connects/disconnects
                Dictionary<IntPtr, PointerTabletDevice> oldTablets = _tabletDeviceMap;

                _tabletDeviceMap = new Dictionary<IntPtr, PointerTabletDevice>();
                TabletDevices.Clear();

                uint deviceCount = 0;

                // Pattern is to first get the count, then declare an array of that size
                // which is then marshaled via the second call with the proper data.
                IsValid = UnsafeNativeMethods.GetPointerDevices(ref deviceCount, null);

                if (IsValid)
                {
                    UnsafeNativeMethods.POINTER_DEVICE_INFO[] deviceInfos
                         = new UnsafeNativeMethods.POINTER_DEVICE_INFO[deviceCount];

                    IsValid = UnsafeNativeMethods.GetPointerDevices(ref deviceCount, deviceInfos);

                    if (IsValid)
                    {
                        foreach (var deviceInfo in deviceInfos)
                        {
                            // Old PenIMC code gets this id via a straight cast from COM pointer address
                            // into an int32.  This does a very similar thing semantically using the pointer
                            // to the tablet from the WM_POINTER stack.  While it may have similar issues
                            // (chopping the upper bits, duplicate ids) we don't use this id internally
                            // and have never received complaints about this in the WISP stack.
                            int id = MS.Win32.NativeMethods.IntPtrToInt32(deviceInfo.device);

                            PointerTabletDeviceInfo ptdi = new PointerTabletDeviceInfo(id, deviceInfo);

                            
                            // Don't add a device that fails initialization.  This means we will try a refresh
                            // next time around if we receive stylus input and the device is not available.
                            // <see cref="HwndPointerInputProvider.UpdateCurrentTabletAndStylus">
                            if (ptdi.TryInitialize())
                            {
                                PointerTabletDevice tablet = new PointerTabletDevice(ptdi);

                                if(!oldTablets.Remove(tablet.Device))
                                {
                                    // We only create a TabletDevice when one is connected (physically or virtually).
                                    // As such we have to log when there is no corresponding old tablet being refreshed.
                                    StylusTraceLogger.LogDeviceConnect(
                                        new StylusTraceLogger.StylusDeviceInfo(
                                            tablet.Id,
                                            tablet.Name,
                                            tablet.ProductId,
                                            tablet.TabletHardwareCapabilities,
                                            tablet.TabletSize,
                                            tablet.ScreenSize,
                                            tablet.Type,
                                            tablet.StylusDevices.Count));
                                }

                                _tabletDeviceMap[tablet.Device] = tablet;
                                TabletDevices.Add(tablet.TabletDevice);
                            }
                        }
                    }

                    // Any tablet leftover here was not refreshed from the previous set of tablets
                    // and should be logged as disconnected.
                    foreach (var oldTablet in oldTablets.Values)
                    {
                        StylusTraceLogger.LogDeviceDisconnect(oldTablet.Id);
                    }
                }
            }
            catch (Win32Exception)
            {
                IsValid = false;
            }
        }
    }

    #endregion
}
