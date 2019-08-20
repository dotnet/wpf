// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Media;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     The StylusSystemGestureEventArgs class provides access to the logical
    ///     Stylus device for all derived event args.
    /// </summary>
    public class StylusSystemGestureEventArgs : StylusEventArgs
    {
        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Initializes a new instance of the StylusSystemGestureEventArgs class.
        /// </summary>
        /// <param name="stylusDevice">
        ///     The logical Stylus device associated with this event.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occured.
        /// </param>
        /// <param name="systemGesture"> 
        ///     The type of system gesture.
        /// </param>
        public StylusSystemGestureEventArgs(
            StylusDevice stylusDevice, int timestamp,
            SystemGesture systemGesture) :
            base(stylusDevice, timestamp)
        {
            if (!RawStylusSystemGestureInputReport.IsValidSystemGesture(systemGesture, false, false))
            {
                throw new InvalidEnumArgumentException(SR.Get(SRID.Enum_Invalid, "systemGesture"));
            }
            
            _id        = systemGesture;
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Initializes a new instance of the StylusSystemGestureEventArgs class.
        /// </summary>
        /// <param name="stylusDevice">
        ///     The logical Stylus device associated with this event.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occured.
        /// </param>
        /// <param name="systemGesture"> 
        ///     The type of system gesture.
        /// </param>
        /// <param name="gestureX"> 
        ///     The X location reported with this system gesture.  In tablet
        ///     device coordinates.
        /// </param>
        /// <param name="gestureY"> 
        ///     The Y location reported with this system gesture.  In tablet
        ///     device coordinates.
        /// </param>
        /// <param name="buttonState"> 
        ///     The button state at the time of the system gesture.
        ///     Note: A flick gesture will pass the flick data in the parameter.
        /// </param>
        internal StylusSystemGestureEventArgs(
                                StylusDevice stylusDevice, 
                                int timestamp,
                                SystemGesture systemGesture, 
                                int gestureX,
                                int gestureY,
                                int buttonState) :
                base(stylusDevice, timestamp)
        {
            if (!RawStylusSystemGestureInputReport.IsValidSystemGesture(systemGesture, true, false))
            {
                throw new InvalidEnumArgumentException(SR.Get(SRID.Enum_Invalid, "systemGesture"));
            }

            _id          = systemGesture;
            _buttonState = buttonState;
            _gestureX    = gestureX;
            _gestureY    = gestureY;
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///		Field to retrieve which gesture occurred.
        /// </summary>
        public SystemGesture SystemGesture
        {
            get
            {
                return _id;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///		Field to retrieve the button state reported with this
        ///     system gesture.  
        ///
        ///     NOTE: For a Flick gesture this param contains the flick 
        ///           and not the button state.
        /// </summary>
        internal int ButtonState
        {
            get
            {
                return _buttonState;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///		Field to retrieve the X location of the system gesture.
        ///     This is in tablet device coordinates.
        /// </summary>
        internal int GestureX
        {
            get
            {
                return _gestureX;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///		Field to retrieve the Y location of the system gesture.
        ///     This is in tablet device coordinates.
        /// </summary>
        internal int GestureY
        {
            get
            {
                return _gestureY;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     The mechanism used to call the type-specific handler on the
        ///     target.
        /// </summary>
        /// <param name="genericHandler">
        ///     The generic handler to call in a type-specific way.
        /// </param>
        /// <param name="genericTarget">
        ///     The target to call the handler on.
        /// </param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            StylusSystemGestureEventHandler handler = (StylusSystemGestureEventHandler) genericHandler;
            handler(genericTarget, this);
        }

        /////////////////////////////////////////////////////////////////////

        SystemGesture     _id;
        int               _buttonState;
        int               _gestureX;
        int               _gestureY;
    }
}
