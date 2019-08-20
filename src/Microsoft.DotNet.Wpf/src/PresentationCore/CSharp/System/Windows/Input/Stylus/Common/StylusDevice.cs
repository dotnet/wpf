// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper
using MS.Win32;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Windows;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Input.StylusWisp;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /// <summary>
    ///     The StylusDevice class represents the stylus device
    /// </summary>
    public sealed class StylusDevice : InputDevice
    {
        #region Implementation

        /// <summary>
        ///
        /// 
        /// The implementation root for the internal StylusDevice hierarchy.
        /// This exists since this class now operates as a public wrapper to an internal
        /// hierarchy rooted with StylusDeviceBase.
        /// </summary>
        internal StylusDeviceBase StylusDeviceImpl { get; set; } = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor taking a base implementation
        /// </summary>
        /// <param name="impl">The base of the internal hierarchy</param>
        internal StylusDevice(StylusDeviceBase impl)
        {
            if (impl == null)
            {
                throw new ArgumentNullException(nameof(impl));
            }

            StylusDeviceImpl = impl;
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
                return StylusDeviceImpl.Target;
            }
        }

        /// <summary>
        ///     Returns whether the StylusDevice object has been internally disposed.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return StylusDeviceImpl.IsValid;
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
                return StylusDeviceImpl.ActiveSource;
            }
        }

        /// <summary>
        ///     Returns the element that the stylus is over.
        /// </summary>
        public IInputElement DirectlyOver
        {
            get
            {
                VerifyAccess();
                return StylusDeviceImpl.DirectlyOver;
            }
        }

        /// <summary>
        ///     Returns the element that has captured the stylus.
        /// </summary>
        public IInputElement Captured
        {
            get
            {
                VerifyAccess();
                return StylusDeviceImpl.Captured;
            }
        }

        /// <summary>
        ///     Captures the stylus to a particular element.
        /// </summary>
        public bool Capture(IInputElement element, CaptureMode captureMode)
        {
            return StylusDeviceImpl.Capture(element, captureMode);
        }

        /// <summary>
        ///     Captures the stylus to a particular element.
        /// </summary>
        public bool Capture(IInputElement element)
        {
            // No need for calling ApplyTemplate since we forward the call.

            return Capture(element, CaptureMode.Element);
        }

        /// <summary>
        ///     Forces the stylusdevice to resynchronize at it's current location and state.
        ///     It can conditionally generate a Stylus Move/InAirMove (at the current location) if a change
        ///     in hittesting is detected that requires an event be generated to update elements 
        ///     to the current state (typically due to layout changes without Stylus changes).  
        ///     Has the same behavior as MouseDevice.Synchronize().
        /// </summary>
        public void Synchronize()
        {
            StylusDeviceImpl.Synchronize();
        }

        /// <summary>
        /// Returns the tablet associated with the StylusDevice
        /// </summary>
        public TabletDevice TabletDevice
        {
            get
            {
                return StylusDeviceImpl.TabletDevice;
            }
        }

        /// <summary>
        /// Returns the name of the StylusDevice
        /// </summary>
        public string Name
        {
            get
            {
                VerifyAccess();
                return StylusDeviceImpl.Name;
            }
        }

        /// <summary>
        /// Returns the friendly representation of the StylusDevice
        /// </summary>
        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}({1})", base.ToString(), this.Name);
        }

        /// <summary>
        /// Returns the hardware id of the StylusDevice
        /// </summary>
        public int Id
        {
            get
            {
                VerifyAccess();
                return StylusDeviceImpl.Id;
            }
        }

        /// <summary>
        ///     Returns a StylusPointCollection object for processing the data in the packet.
        ///     This method creates a new StylusPointCollection and copies the data.
        /// </summary>
        public StylusPointCollection GetStylusPoints(IInputElement relativeTo)
        {
            VerifyAccess();

            return StylusDeviceImpl.GetStylusPoints(relativeTo);
        }

        /// <summary>
        ///     Returns a StylusPointCollection object for processing the data in the packet.
        ///     This method creates a new StylusPointCollection and copies the data.
        /// </summary>
        public StylusPointCollection GetStylusPoints(IInputElement relativeTo, StylusPointDescription subsetToReformatTo)
        {
            return StylusDeviceImpl.GetStylusPoints(relativeTo, subsetToReformatTo);
        }

        /// <summary>
        /// Returns the button collection that is associated with the StylusDevice.
        /// </summary>
        public StylusButtonCollection StylusButtons
        {
            get
            {
                VerifyAccess();
                return StylusDeviceImpl.StylusButtons;
            }
        }

        /// <summary>
        ///     Calculates the position of the stylus relative to a particular element.
        /// </summary>
        public Point GetPosition(IInputElement relativeTo)
        {
            VerifyAccess();
            return StylusDeviceImpl.GetPosition(relativeTo);
        }

        /// <summary>
        ///     Indicates the stylus is not touching the surface.
        ///     InAir events are general sent at a lower frequency.
        /// </summary>
        public bool InAir
        {
            get
            {
                VerifyAccess();
                return StylusDeviceImpl.InAir;
            }
        }

        /// <summary>
        ///     Indicates stylusDevice is in the inverted state.
        /// </summary>
        public bool Inverted
        {
            get
            {
                VerifyAccess();
                return StylusDeviceImpl.Inverted;
            }
        }

        /// <summary>
        ///     Indicates stylusDevice is in the inverted state.
        /// </summary>
        public bool InRange
        {
            get
            {
                VerifyAccess();
                return StylusDeviceImpl.InRange;
            }
        }

        #endregion

        #region Internal API

        internal int DoubleTapDeltaX
        {
            get { return StylusDeviceImpl.DoubleTapDeltaX; }
        }

        internal int DoubleTapDeltaY
        {
            get { return StylusDeviceImpl.DoubleTapDeltaY; }
        }

        internal int DoubleTapDeltaTime
        {
            get { return StylusDeviceImpl.DoubleTapDeltaTime; }
        }

        /// <summary>
        ///     Gets the current position of the mouse in screen co-ords
        /// </summary>
        /// <param name="mouseDevice">
        ///     The MouseDevice that is making the request
        /// </param>
        /// <returns>
        ///     The current mouse location in screen co-ords
        /// </returns>
        /// <remarks>
        ///     This is the hook where the Input system (via the MouseDevice) can call back into
        ///     the Stylus system when we are processing Stylus events instead of Mouse events
        /// </remarks>
        internal Point GetMouseScreenPosition(MouseDevice mouseDevice)
        {
            return StylusDeviceImpl.GetMouseScreenPosition(mouseDevice);
        }

        /// <summary>
        ///     Gets the current state of the specified button
        /// </summary>
        /// <param name="mouseButton">
        ///     The mouse button to get the state of
        /// </param>
        /// <param name="mouseDevice">
        ///     The MouseDevice that is making the request
        /// </param>
        /// <returns>
        ///     The state of the specified mouse button
        /// </returns>
        /// <remarks>
        ///     This is the hook where the Input system (via the MouseDevice) can call back into
        ///     the Stylus system when we are processing Stylus events instead of Mouse events
        /// </remarks>
        internal MouseButtonState GetMouseButtonState(MouseButton mouseButton, MouseDevice mouseDevice)
        {
            return StylusDeviceImpl.GetMouseButtonState(mouseButton, mouseDevice);
        }

        #endregion

        #region Static Functions

        internal static IInputElement LocalHitTest(PresentationSource inputSource, Point pt)
        {
            return MouseDevice.LocalHitTest(pt, inputSource);
        }

        internal static IInputElement GlobalHitTest(PresentationSource inputSource, Point pt)
        {
            return MouseDevice.GlobalHitTest(pt, inputSource);
        }

        /// <summary>
        /// Gets the transform relative to a particular element in the visual tree.
        /// </summary>
        internal static GeneralTransform GetElementTransform(IInputElement relativeTo)
        {
            GeneralTransform elementTransform = Transform.Identity;
            DependencyObject doRelativeTo = relativeTo as DependencyObject;

            if (doRelativeTo != null)
            {
                Visual visualFirstAncestor = VisualTreeHelper.GetContainingVisual2D(InputElement.GetContainingVisual(doRelativeTo));
                Visual visualRoot = VisualTreeHelper.GetContainingVisual2D(InputElement.GetRootVisual(doRelativeTo));

                GeneralTransform g = visualRoot.TransformToDescendant(visualFirstAncestor);
                if (g != null)
                {
                    elementTransform = g;
                }
            }

            return elementTransform;
        }

        #endregion

        #region Casting

        /// <summary>
        /// Function to convert any implementation class
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <returns>An instance of the derived type</returns>
        internal T As<T>()
            where T : StylusDeviceBase
        {
            return StylusDeviceImpl as T;
        }

        #endregion
    }
}
