// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Security;
using System.Windows.Input.StylusWisp;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /// <summary>
    /// Represents a gesture input from the stylus/touch stack.
    /// </summary>
    internal class RawStylusSystemGestureInputReport : RawStylusInputReport
    {
        // We don't want to publically expose the double tap system gesture so we define it here.
        internal const SystemGesture InternalSystemGestureDoubleTap = (SystemGesture)0x11;

        /// <summary>
        ///     Validates whether a given SystemGesture has an allowable value.
        /// </summary>
        /// <param name="systemGesture">The SysemGesture to test.</param>
        /// <param name="allowFlick">Whether Flick is allowed.</param>
        /// <param name="allowDoubleTap">Whether DoubleTab is allowed.</param>
        /// <returns>True if the SystemGesture matches an allowed gesture. False otherwise.</returns>
        internal static bool IsValidSystemGesture(SystemGesture systemGesture, bool allowFlick, bool allowDoubleTap)
        {
            switch (systemGesture)
            {
                case SystemGesture.None:
                case SystemGesture.Tap:
                case SystemGesture.RightTap:
                case SystemGesture.Drag:
                case SystemGesture.RightDrag:
                case SystemGesture.HoldEnter:
                case SystemGesture.HoldLeave:
                case SystemGesture.HoverEnter:
                case SystemGesture.HoverLeave:
                case SystemGesture.TwoFingerTap:
                    return true;

                case SystemGesture.Flick:
                    return allowFlick;

                case InternalSystemGestureDoubleTap:
                    return allowDoubleTap;

                default:
                    return false;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Constructs an instance of the RawStylusSystemGestureInputReport class.
        /// </summary>
        /// <param name="mode">
        ///     The mode in which the input is being provided.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occured.
        /// </param>
        /// <param name="inputSource">
        ///     The PresentationSource over which the stylus moved.
        /// </param>
        /// <param name="penContext">
        ///     PenContext that generated this event.
        /// </param>
        /// <param name="tabletId">
        ///     tablet id.
        /// </param>
        /// <param name="stylusDeviceId">
        ///     Stylus device id.
        /// </param>
        /// <param name="systemGesture">
        ///     System Gesture.
        /// </param>
        /// <param name="gestureX">
        ///     X location of the system gesture (in tablet device coordindates).
        /// </param>
        /// <param name="gestureY">
        ///     Y location of the system gesture (in tablet device coordindates).
        /// </param>
        /// <param name="buttonState">
        ///     Button state info data.
        /// </param>
        internal RawStylusSystemGestureInputReport(
            InputMode mode,
            int timestamp,
            PresentationSource inputSource,
            Func<StylusPointDescription> stylusPointDescGenerator,
            int tabletId,
            int stylusDeviceId,
            SystemGesture systemGesture,
            int gestureX,
            int gestureY,
            int buttonState)
            : base(mode, timestamp, inputSource,
                    RawStylusActions.SystemGesture,
                    stylusPointDescGenerator, tabletId,
                    stylusDeviceId, new int[] { })
        {
            Initialize(systemGesture, gestureX, gestureY, buttonState);
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Constructs an instance of the RawStylusSystemGestureInputReport class.
        /// </summary>
        /// <param name="mode">
        ///     The mode in which the input is being provided.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occured.
        /// </param>
        /// <param name="inputSource">
        ///     The PresentationSource over which the stylus moved.
        /// </param>
        /// <param name="penContext">
        ///     PenContext that generated this event.
        /// </param>
        /// <param name="tabletId">
        ///     tablet id.
        /// </param>
        /// <param name="stylusDeviceId">
        ///     Stylus device id.
        /// </param>
        /// <param name="systemGesture">
        ///     System Gesture.
        /// </param>
        /// <param name="gestureX">
        ///     X location of the system gesture (in tablet device coordindates).
        /// </param>
        /// <param name="gestureY">
        ///     Y location of the system gesture (in tablet device coordindates).
        /// </param>
        /// <param name="buttonState">
        ///     Button state info data.
        /// </param>
        internal RawStylusSystemGestureInputReport(
            InputMode mode,
            int timestamp,
            PresentationSource inputSource,
            PenContext penContext,
            int tabletId,
            int stylusDeviceId,
            SystemGesture systemGesture,
            int gestureX,
            int gestureY,
            int buttonState)
            : base(mode, timestamp, inputSource, penContext, RawStylusActions.SystemGesture, tabletId, stylusDeviceId, new int[] { })
        {
            Initialize(systemGesture, gestureX, gestureY, buttonState);
        }

        private void Initialize(SystemGesture systemGesture, int gestureX, int gestureY, int buttonState)
        {
            if (!RawStylusSystemGestureInputReport.IsValidSystemGesture(systemGesture, true, true))
            {
                throw new InvalidEnumArgumentException(SR.Get(SRID.Enum_Invalid, nameof(systemGesture)));
            }

            _id = systemGesture;
            _gestureX = gestureX;
            _gestureY = gestureY;
            _buttonState = buttonState;
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Read-only access to the stylus gesture id.
        /// </summary>
        internal SystemGesture SystemGesture { get { return _id; } }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Read-only access to the X location of the system gesture
        ///      in tablet device coordinates.
        /// </summary>
        internal int GestureX { get { return _gestureX; } }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Read-only access to the Y location of the system gesture
        ///      in tablet device coordinates.
        /// </summary>
        internal int GestureY { get { return _gestureY; } }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Read-only access to the button state info (useful for flicks).
        /// </summary>
        internal int ButtonState { get { return _buttonState; } }

        /////////////////////////////////////////////////////////////////////

        SystemGesture _id;
        int _gestureX;// gesture location in tablet device coordinates
        int _gestureY;
        int _buttonState;
}
}
