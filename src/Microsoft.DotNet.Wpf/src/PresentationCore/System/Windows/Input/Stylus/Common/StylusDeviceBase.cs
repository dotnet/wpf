// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using MS.Internal.PresentationCore;
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
    ///     
    ///     Note:  This class implements InputDevice without deriving from InputDevice.
    ///     The reason for this is so no one can set the InputDevice of any InputReport
    ///     to the base.  Due to the amount of code that uses StylusDevice (the wrapper)
    ///     there would be ambigious situations where some code casted to the wrapper 
    ///     and some code the base.  This is confusing and prone to introducting bugs.
    /// </summary>
    internal abstract class StylusDeviceBase : DispatcherObject, IDisposable
    {
        #region Casting/Wrapper Access

        /// <summary>
        /// The StylusDevice wrapper associated with this implementation instance.
        /// Wrappers are lifetime of the instance and should never change.
        /// </summary>
        internal StylusDevice StylusDevice { get; private set; }

        /// <summary>
        /// Function to convert to any derived class
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <returns>An instance of the derived type</returns>
        internal T As<T>()
            where T : StylusDeviceBase
        {
            return this as T;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Create a wrapper.
        /// </summary>
        protected StylusDeviceBase()
        {
            StylusDevice = new StylusDevice(this);
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
        /// Allow derived classes to provide custom disposal semantics
        /// </summary>
        /// <param name="disposing">If this is called from Dispose or the finalizer</param>
        protected abstract void Dispose(bool disposing);

        ~StylusDeviceBase()
        {
            Dispose(false);
        }

        #endregion

        #region InputDevice Wrapper Abstraction

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

        internal abstract void UpdateEventStylusPoints(RawStylusInputReport report, bool resetIfNoOverride);

        /// <summary>
        ///     Returns the PresentationSource that is reporting input for this device.
        /// </summary>
        internal abstract PresentationSource CriticalActiveSource { get; }

        /// <summary>
        /// The latest stylus point in raw data
        /// </summary>
        internal abstract StylusPoint RawStylusPoint { get; }

        /// <summary>
        ///     Returns whether the StylusDevice object has been internally disposed.
        /// </summary>
        internal abstract bool IsValid { get; }

        /// <summary>
        ///     Returns the element that the stylus is over.
        /// </summary>
        internal abstract IInputElement DirectlyOver { get; }

        /// <summary>
        ///     Returns the element that has captured the stylus.
        /// </summary>
        internal abstract IInputElement Captured { get; }

        /// <summary>
        ///     Returns the element that has captured the stylus.
        /// </summary>
        internal abstract CaptureMode CapturedMode { get; }

        /// <summary>
        ///     Captures the stylus to a particular element.
        /// </summary>
        internal abstract bool Capture(IInputElement element, CaptureMode captureMode);

        /// <summary>
        ///     Captures the stylus to a particular element.
        /// </summary>
        internal abstract bool Capture(IInputElement element);

        /// <summary>
        ///     Forces the stylusdevice to resynchronize at it's current location and state.
        /// </summary>
        internal abstract void Synchronize();

        /// <summary>
        /// Returns the tablet associated with the StylusDevice
        /// </summary>
        internal abstract TabletDevice TabletDevice { get; }

        /// <summary>
        /// Returns the name of the StylusDevice
        /// </summary>
        internal abstract string Name { get; }

        /// <summary>
        /// Returns the hardware id of the StylusDevice
        /// </summary>
        internal abstract int Id { get; }

        /// <summary>
        ///     Returns a StylusPointCollection object for processing the data in the packet.
        ///     This method creates a new StylusPointCollection and copies the data.
        /// </summary>
        internal abstract StylusPointCollection GetStylusPoints(IInputElement relativeTo);

        /// <summary>
        ///     Returns a StylusPointCollection object for processing the data in the packet.
        ///     This method creates a new StylusPointCollection and copies the data.
        /// </summary>
        internal abstract StylusPointCollection GetStylusPoints(IInputElement relativeTo, StylusPointDescription subsetToReformatTo);

        /// <summary>
        /// Returns the button collection that is associated with the StylusDevice.
        /// </summary>
        internal abstract StylusButtonCollection StylusButtons { get; }

        /// <summary>
        ///     Calculates the position of the stylus relative to a particular element.
        /// </summary>
        internal abstract Point GetPosition(IInputElement relativeTo);

        /// <summary>
        ///     Indicates the stylus is not touching the surface.
        ///     InAir events are general sent at a lower frequency.
        /// </summary>
        internal abstract bool InAir { get; }

        /// <summary>
        ///     Indicates stylusDevice is in the inverted state.
        /// </summary>
        internal abstract bool Inverted { get; }

        /// <summary>
        ///     Indicates stylusDevice is in the inverted state.
        /// </summary>
        internal abstract bool InRange { get; }

        internal abstract int DoubleTapDeltaX { get; }

        internal abstract int DoubleTapDeltaY { get; }

        internal abstract int DoubleTapDeltaTime { get; }

        internal abstract Point GetMouseScreenPosition(MouseDevice mouseDevice);

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
        internal abstract MouseButtonState GetMouseButtonState(MouseButton mouseButton, MouseDevice mouseDevice);

        internal abstract int TapCount { get; set; }

        internal abstract StylusPlugInCollection GetCapturedPlugInCollection(ref bool elementHasCapture);

        internal abstract StylusPlugInCollection CurrentVerifiedTarget { get; set; }

        #endregion
    }
}
