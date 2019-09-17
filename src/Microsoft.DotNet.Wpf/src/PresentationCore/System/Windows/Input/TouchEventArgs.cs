// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace System.Windows.Input
{
    public class TouchEventArgs : InputEventArgs
    {
        public TouchEventArgs(TouchDevice touchDevice, int timestamp)
            : base (touchDevice, timestamp)
        {
        }

        /// <summary>
        ///     The device associated with these event arguments.
        /// </summary>
        public TouchDevice TouchDevice
        {
            get { return (TouchDevice)Device; }
        }

        /// <summary>
        ///     Retrieves the current state related to postion of the TouchDevice.
        /// </summary>
        /// <param name="relativeTo">The element that defines the coordinate space of the returned data.</param>
        /// <returns>A TouchPoint object that describes the position and other data regarding the TouchDevice.</returns>
        public TouchPoint GetTouchPoint(IInputElement relativeTo)
        {
            return TouchDevice.GetTouchPoint(relativeTo);
        }

        /// <summary>
        ///     Retrieves the positions that the TouchDevice went through between the 
        ///     last time a touch event occurred and this one.
        /// </summary>
        /// <param name="relativeTo">The elmeent that defines the coordinate space of the returned data.</param>
        /// <returns>The positions that the TouchDevice went through.</returns>
        public TouchPointCollection GetIntermediateTouchPoints(IInputElement relativeTo)
        {
            return TouchDevice.GetIntermediateTouchPoints(relativeTo);
        }

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
            EventHandler<TouchEventArgs> handler = (EventHandler<TouchEventArgs>)genericHandler;
            handler(genericTarget, this);
        }
    }
}
