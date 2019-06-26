// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using MS.Internal;
using MS.Internal.Interop;
using MS.Internal.PresentationCore;
using MS.Win32.Pointer;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Input.StylusPointer;
using System.Windows.Media;
using System.Windows.Threading;

namespace System.Windows.Interop
{
    /// <summary>
    /// Implements an input provider per hwnd for WM_POINTER messages
    /// </summary>
    internal sealed class HwndPointerInputProvider : DispatcherObject, IStylusInputProvider
    {
        #region Member Variables

        private bool _disposed = false;

        /// <summary>
        /// The HwndSource for WM_POINTER messages
        /// </summary>
        private SecurityCriticalDataClass<HwndSource> _source;

        /// <summary>
        /// The Input site to inject messages
        /// </summary>
        private SecurityCriticalDataClass<InputProviderSite> _site;

        /// <summary>
        /// The current pointer logic for this thread
        /// </summary>
        private SecurityCriticalDataClass<PointerLogic> _pointerLogic;

        /// <summary>
        /// The current stylus device we are using
        /// </summary>
        private PointerStylusDevice _currentStylusDevice;

        /// <summary>
        /// The current tablet device we are using
        /// </summary>
        private PointerTabletDevice _currentTabletDevice;

        #endregion

        #region Properties

        /// <summary>
        /// If the window we are associated with is currently enabled.
        /// </summary>
        internal bool IsWindowEnabled { get; private set; } = false;

        #endregion

        #region Constructor/IDisposable

        /// <summary>
        /// Creates a new input provider for a particular source that handles WM_POINTER messages
        /// </summary>
        /// <param name="source">The source to handle messages for</param>
        internal HwndPointerInputProvider(HwndSource source)
        {
            _site = new SecurityCriticalDataClass<InputProviderSite>(InputManager.Current.RegisterInputProvider(this));

            _source = new SecurityCriticalDataClass<HwndSource>(source);
            _pointerLogic = new SecurityCriticalDataClass<PointerLogic>(StylusLogic.GetCurrentStylusLogicAs<PointerLogic>());

            // Register the stylus plugin manager
            _pointerLogic.Value.PlugInManagers[_source.Value] = new PointerStylusPlugInManager(_source.Value);

            // Store if this window is enabled or disabled
            int style = MS.Win32.UnsafeNativeMethods.GetWindowLong(new HandleRef(this, source.CriticalHandle), MS.Win32.NativeMethods.GWL_STYLE);
            IsWindowEnabled = (style & MS.Win32.NativeMethods.WS_DISABLED) == 0;
        }

        ~HwndPointerInputProvider()
        {
            Dispose(false);
        }

        /// <summary>
        /// Clean up any held resources
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_site != null)
                    {
                        _site.Value.Dispose();
                        _site = null;
                    }

                    _pointerLogic.Value.PlugInManagers.Remove(_source.Value);
                }
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Pointer Data Extraction

        /// <summary>
        /// Extracts the pointer id
        /// </summary>
        /// <param name="wParam">The parameter containing the id</param>
        /// <returns>The pointer id</returns>
        private uint GetPointerId(IntPtr wParam)
        {
            return (uint)MS.Win32.NativeMethods.SignedLOWORD(wParam);
        }

        /// <summary>
        /// Creates raw stylus data from the raw WM_POINTER properties
        /// </summary>
        /// <param name="pointerData">The current pointer info</param>
        /// <param name="tabletDevice">The current TabletDevice</param>
        /// <returns>An array of raw pointer data</returns>
        private int[] GenerateRawStylusData(PointerData pointerData, PointerTabletDevice tabletDevice)
        {
            // Since we are copying raw pointer data, we want to use every property supported by this pointer.
            // We may never access some of the unknown (unsupported by WPF) properties, but they should be there
            // for consumption by the developer.
            int pointerPropertyCount = tabletDevice.DeviceInfo.SupportedPointerProperties.Length;

            // The data is as wide as the pointer properties and is per history point
            int[] rawPointerData = new int[pointerPropertyCount * pointerData.Info.historyCount];

            int[] data = new int[0];

            // Get the raw data formatted to our supported properties
            if (UnsafeNativeMethods.GetRawPointerDeviceData(
                pointerData.Info.pointerId,
                pointerData.Info.historyCount,
                (uint)pointerPropertyCount,
                tabletDevice.DeviceInfo.SupportedPointerProperties,
                rawPointerData))
            {
                // Get the X and Y offsets to translate device coords to the origin of the hwnd
                int originOffsetX, originOffsetY;
                GetOriginOffsetsLogical(out originOffsetX, out originOffsetY);

                int numButtons = tabletDevice.DeviceInfo.SupportedPointerProperties.Length - tabletDevice.DeviceInfo.SupportedButtonPropertyIndex;

                int rawDataPointSize = (numButtons > 0) ? pointerPropertyCount - numButtons + 1 : pointerPropertyCount;

                // Instead of a single entry for each button we use one entry for all buttons so reflect that in the raw data size
                data = new int[rawDataPointSize * pointerData.Info.historyCount];

                // Skip to the beginning of each stylus point in both the target WPF array and the pointer data array.
                // The pointer data is arranged from last point to first point in the history while WPF data is arranged
                // the reverse of this (in whole stylus points).  Therefore we need to fill backward from pointer data
                // via stylus point strides.
                for (int i = 0, j = rawPointerData.Length - pointerPropertyCount; i < data.Length; i += rawDataPointSize, j -= pointerPropertyCount)
                {
                    Array.Copy(rawPointerData, j, data, i, rawDataPointSize);

                    // Apply offsets from the origin to raw pointer data here
                    data[i + StylusPointDescription.RequiredXIndex] -= originOffsetX;
                    data[i + StylusPointDescription.RequiredYIndex] -= originOffsetY;

                    if (numButtons > 0)
                    {
                        int buttonIndex = i + rawDataPointSize - 1;

                        // The last data point probably has garbage in it, so clear it to store button info
                        data[buttonIndex] = 0;

                        // Condense any leftover button properties into a single entry
                        for (int k = tabletDevice.DeviceInfo.SupportedButtonPropertyIndex; k < pointerPropertyCount; k++)
                        {
                            int mask = rawPointerData[j + k] << (k - tabletDevice.DeviceInfo.SupportedButtonPropertyIndex);
                            data[buttonIndex] |= mask;
                        }
                    }
                }
            }

            return data;
        }

        #endregion

        #region Stylus Event Firing

        /// <summary>
        /// Processes the latest WM_POINTER message and forwards it to the WPF input stack.
        /// </summary>
        /// <param name="pointerId">The id of the pointer message</param>
        /// <param name="action">The stylus action being done</param>
        /// <param name="timestamp">The time (in ticks) the message arrived</param>
        /// <returns>True if successfully processed (handled), false otherwise</returns>
        private bool ProcessMessage(uint pointerId, RawStylusActions action, int timestamp)
        {
            bool handled = false;

            // Acquire all pointer data needed
            PointerData data = new PointerData(pointerId);

            // Only process touch or pen messages, do not process mouse or touchpad
            if (data.IsValid
                && (data.Info.pointerType == UnsafeNativeMethods.POINTER_INPUT_TYPE.PT_TOUCH
                || data.Info.pointerType == UnsafeNativeMethods.POINTER_INPUT_TYPE.PT_PEN))
            {               
                uint cursorId = 0;

                if (UnsafeNativeMethods.GetPointerCursorId(pointerId, ref cursorId))
                {
                    IntPtr deviceId = data.Info.sourceDevice;

                    // If we cannot acquire the latest tablet and stylus then wait for the
                    // next message.
                    if (!UpdateCurrentTabletAndStylus(deviceId, cursorId))
                    {
                        return false;
                    }
                                     
                    // Convert move to InAirMove if applicable
                    if (action == RawStylusActions.Move
                        && (!data.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_INCONTACT)
                        && data.Info.pointerFlags.HasFlag(UnsafeNativeMethods.POINTER_FLAGS.POINTER_FLAG_INRANGE)))
                    {
                        action = RawStylusActions.InAirMove;
                    }

                    // Generate a raw input to send to the input manager to start the event chain in PointerLogic
                    RawStylusInputReport rsir =
                        new RawStylusInputReport(
                            InputMode.Foreground,
                            timestamp,
                            _source.Value,
                            action,
                            () => { return _currentTabletDevice.StylusPointDescription; },
                            _currentTabletDevice.Id,
                            _currentStylusDevice.Id,
                            GenerateRawStylusData(data, _currentTabletDevice))
                        {
                            StylusDevice = _currentStylusDevice.StylusDevice,
                        };

                    // Send the input report to the stylus plugins if we're not doing a drag and the window
                    // is currently enabled.
                    if (!_pointerLogic.Value.InDragDrop && IsWindowEnabled)
                    {
                        PointerStylusPlugInManager manager;

                        if (_pointerLogic.Value.PlugInManagers.TryGetValue(_source.Value, out manager))
                        {
                            manager.InvokeStylusPluginCollection(rsir);
                        }
                    }

                    // Update the data in the stylus device with the latest pointer data
                    _currentStylusDevice.Update(this, _source.Value, data, rsir);

                    // Call the StylusDevice to process and fire any interactions that
                    // might have resulted from the input.  If the originating inputs
                    // have been handled, we don't want to generate any gestures.
                    _currentStylusDevice.UpdateInteractions(rsir);

                    InputReportEventArgs irea = new InputReportEventArgs(_currentStylusDevice.StylusDevice, rsir)
                    {
                        RoutedEvent = InputManager.PreviewInputReportEvent,
                    };

                    // Now send the input report
                    InputManager.UnsecureCurrent.ProcessInput(irea);

                    // If this is not a primary pointer input, we don't want to 
                    // allow it to go to DefWindowProc, so we should handle it.
                    // This ensures that primary pointer to mouse promotions
                    // will occur in multi-touch scenarios.
                    // We don't use the results of the input processing here as doing
                    // so could possibly cause some messages of a pointer chain as
                    // being handled, and some as being unhandled.  This results in
                    // undefined behavior in the WM_POINTER stack.
                    // <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/hh454923(v=vs.85).aspx"/>
                    handled = !_currentStylusDevice.IsPrimary;
                }
            }

            return handled;
        }

        #endregion

        #region Utility

        /// <summary>
        /// This function uses the logical origin of the current hwnd as the offsets for
        /// logical pointer coordinates.
        /// 
        /// This is needed as WISP's concept of tablet coordinates is not the entire tablet.
        /// Instead, WISP transforms tablet X and Y into the tablet context.  This does not
        /// change the max, min, or resolution, merely translates the origin point to the hwnd
        /// origin.  Since the inking system in WPF was based on this raw data, we need to 
        /// recreate the same thing here.
        /// 
        /// See Stylus\Biblio.txt - 7
        ///     
        /// </summary>
        /// <param name="originOffsetX">The X offset in logical coordinates</param>
        /// <param name="originOffsetY">The Y offset in logical coordiantes</param>
        private void GetOriginOffsetsLogical(out int originOffsetX, out int originOffsetY)
        {
            Point originScreenCoord = _source.Value.RootVisual.PointToScreen(new Point(0, 0));

            // Use the inverse of our logical tablet to screen matrix to generate tablet coords
            MatrixTransform screenToTablet = new MatrixTransform(_currentTabletDevice.TabletToScreen);
            screenToTablet = (MatrixTransform)screenToTablet.Inverse;

            Point originTabletCoord = originScreenCoord * screenToTablet.Matrix;

            originOffsetX = (int)Math.Round(originTabletCoord.X);
            originOffsetY = (int)Math.Round(originTabletCoord.Y);
        }

        /// <summary>
        /// Attempts to update the current stylus and tablet devices for the latest WM_POINTER message.
        /// Will attempt retries if the tablet collection is invalid or does not contain the proper ids.
        /// </summary>
        /// <param name="deviceId">The id of the TabletDevice</param>
        /// <param name="cursorId">The id of the StylusDevice</param>
        /// <returns>True if successfully updated, false otherwise.</returns>
        private bool UpdateCurrentTabletAndStylus(IntPtr deviceId, uint cursorId)
        {
            PointerTabletDeviceCollection tablets = Tablet.TabletDevices?.As<PointerTabletDeviceCollection>();

            // We have an invalid tablet collection, we should refresh to make sure 
            // we have the latest.
            if (!tablets.IsValid)
            {
                tablets.Refresh();

                // If the refresh fails, we need to skip input here, nothing we can do.
                // We'll try to pick up the proper state on the next WM_POINTER message.
                if (!tablets.IsValid)
                {
                    return false;
                }
            }

            _currentTabletDevice = tablets?.GetByDeviceId(deviceId);

            _currentStylusDevice = _currentTabletDevice?.GetStylusByCursorId(cursorId);

            // Something went wrong when querying the tablet or stylus, attempt a refresh
            if (_currentTabletDevice == null || _currentStylusDevice == null)
            {
                tablets.Refresh();

                _currentTabletDevice = tablets?.GetByDeviceId(deviceId);

                _currentStylusDevice = _currentTabletDevice?.GetStylusByCursorId(cursorId);

                // Still can't get the proper devices, just wait for the next message
                if (_currentTabletDevice == null || _currentStylusDevice == null)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Message Filtering

        /// <summary>
        /// Processes the message loop for the HwndSource, filtering WM_POINTER messages where needed
        /// </summary>
        /// <param name="hwnd">The hwnd the message is for</param>
        /// <param name="msg">The message</param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled">If this has been successfully processed</param>
        /// <returns></returns>
        IntPtr IStylusInputProvider.FilterMessage(IntPtr hwnd, WindowMessage msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;

            // Do not process any messages if the stack was disabled via reflection hack
            if (PointerLogic.IsEnabled)
            {
                switch (msg)
                {
                    case WindowMessage.WM_ENABLE:
                        {
                            IsWindowEnabled = MS.Win32.NativeMethods.IntPtrToInt32(wParam) == 1;
                        }
                        break;
                    case WindowMessage.WM_POINTERENTER:
                        {
                            // Enter can be processed as an InRange.  
                            // The MSDN documentation is not correct for InRange (according to feisu)
                            // As such, using enter is the correct way to generate this.  This is also what DirectInk uses.
                            handled = ProcessMessage(GetPointerId(wParam), RawStylusActions.InRange, Environment.TickCount);
                        }
                        break;
                    case WindowMessage.WM_POINTERUPDATE:
                        {
                            handled = ProcessMessage(GetPointerId(wParam), RawStylusActions.Move, Environment.TickCount);
                        }
                        break;
                    case WindowMessage.WM_POINTERDOWN:
                        {
                            handled = ProcessMessage(GetPointerId(wParam), RawStylusActions.Down, Environment.TickCount);
                        }
                        break;
                    case WindowMessage.WM_POINTERUP:
                        {
                            handled = ProcessMessage(GetPointerId(wParam), RawStylusActions.Up, Environment.TickCount);
                        }
                        break;
                    case WindowMessage.WM_POINTERLEAVE:
                        {
                            // Leave can be processed as an OutOfRange.  
                            // The MSDN documentation is not correct for OutOfRange (according to feisu)
                            // As such, using leave is the correct way to generate this.  This is also what DirectInk uses.
                            handled = ProcessMessage(GetPointerId(wParam), RawStylusActions.OutOfRange, Environment.TickCount);
                        }
                        break;
                }
            }

            return IntPtr.Zero;
        }

        #endregion

        #region IInputProvider

        /// <summary>
        ///     Indicates if the provider is responsible for providing
        ///     input for the specified visual.
        /// </summary>
        public bool ProvidesInputForRootVisual(Visual v)
        {
            return false;
        }

        /// <summary>
        ///     Notifies the input provider that it is no longer 
        ///     the active input provider.  If the input provider
        ///     needs to report more input, it will need to reactivate.
        /// </summary>
        public void NotifyDeactivate()
        {
}

        #endregion
    }
}
