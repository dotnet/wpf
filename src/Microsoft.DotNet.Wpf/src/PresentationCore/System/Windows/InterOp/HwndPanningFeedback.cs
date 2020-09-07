// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using MS.Win32;

namespace System.Windows.Interop
{
    /// <summary>
    ///     Helper class to provide window feedback that panning has hit an edge.
    /// </summary>
    internal class HwndPanningFeedback
    {
        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        /// <param name="hwndSource">The HWND on which to provide feedback.</param>
        public HwndPanningFeedback(HwndSource hwndSource)
        {
            if (hwndSource == null)
            {
                throw new ArgumentNullException("hwndSource");
            }

            _hwndSource = hwndSource;
        }

        private static bool IsSupported
        {
            get
            {
                return OperatingSystemVersionCheck.IsVersionOrLater(OperatingSystemVersion.Windows7);
            }
        }

        /// <summary>
        ///     Returns the handle of the current window.
        /// </summary>
        private HandleRef Handle
        {
            get
            {
                if (_hwndSource != null)
                {
                    IntPtr handle = _hwndSource.CriticalHandle;
                    if (handle != IntPtr.Zero)
                    {
                        return new HandleRef(_hwndSource, handle);
                    }
                }

                return new HandleRef();
            }
        }

        /// <summary>
        ///     Moves the window by the offset.
        ///     Used to provide feedback that panning has hit an edge.
        /// </summary>
        /// <param name="totalOverpanOffset">The total offset relative to the original location.</param>
        /// <param name="inInertia">Whether the edge was hit due to inertia or the user panning.</param>
        public void UpdatePanningFeedback(Vector totalOverpanOffset, bool inInertia)
        {
            if ((_hwndSource != null) && IsSupported)
            {
                if (!_isProvidingPanningFeedback)
                {
                    _isProvidingPanningFeedback = UnsafeNativeMethods.BeginPanningFeedback(Handle);
                }

                if (_isProvidingPanningFeedback)
                {
                    var deviceOffset = _hwndSource.TransformToDevice((Point)totalOverpanOffset);
                    _deviceOffsetX = (int)deviceOffset.X;
                    _deviceOffsetY = (int)deviceOffset.Y;
                    _inInertia = inInertia;

                    if (_updatePanningOperation == null)
                    {
                        _updatePanningOperation = _hwndSource.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            new DispatcherOperationCallback(OnUpdatePanningFeedback), 
                            this);
                    }
                }
            }
        }

        private object OnUpdatePanningFeedback(object args)
        {
            HwndPanningFeedback panningFeedback = (HwndPanningFeedback)args;
            _updatePanningOperation = null;
            UnsafeNativeMethods.UpdatePanningFeedback(panningFeedback.Handle, panningFeedback._deviceOffsetX, panningFeedback._deviceOffsetY, panningFeedback._inInertia);
            return null;
        }

        private int _deviceOffsetX;
        private int _deviceOffsetY;
        private bool _inInertia;
        private DispatcherOperation _updatePanningOperation;

        /// <summary>
        ///     Moves the window back to its original position.
        ///     Used to provide feedback that panning has hit an edge.
        /// </summary>
        /// <param name="animateBack">Whether to animate or snap back to the original position</param>
        public void EndPanningFeedback(bool animateBack)
        {
            if (_hwndSource != null && _isProvidingPanningFeedback)
            {
                _isProvidingPanningFeedback = false;
                if (_updatePanningOperation != null)
                {
                    _updatePanningOperation.Abort();
                    _updatePanningOperation = null;
                }
                UnsafeNativeMethods.EndPanningFeedback(Handle, animateBack);
            }
        }

        /// <summary>
        ///     Whether panning feedback is currently in progress.
        /// </summary>
        private bool _isProvidingPanningFeedback;

        /// <summary>
        ///     The HwndSource being manipulated.
        /// </summary>
        private HwndSource _hwndSource;
    }
}
