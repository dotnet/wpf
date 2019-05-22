// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Windows;
using System.Windows.Input;

namespace System.Windows.Input
{
    /// <summary>
    ///     Event arguments for the Touch.FrameReported event.
    /// </summary>
    public sealed class TouchFrameEventArgs : EventArgs
    {
        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        /// <param name="timestamp"></param>
        internal TouchFrameEventArgs(int timestamp)
        {
            Timestamp = timestamp;
        }

        /// <summary>
        ///     The timestamp for this event.
        /// </summary>
        public int Timestamp
        {
            get;
            private set;
        }

        /// <summary>
        ///     Retrieves the current touch point for ever touch device that is currently active.
        /// </summary>
        /// <param name="relativeTo">Defines the coordinate space of the touch point.</param>
        /// <returns>A collection of touch points.</returns>
        public TouchPointCollection GetTouchPoints(IInputElement relativeTo)
        {
            return TouchDevice.GetTouchPoints(relativeTo);
        }

        /// <summary>
        ///     Retrieves the current touch point of the primary touch device, if one exists.
        /// </summary>
        /// <param name="relativeTo">Defines the coordinate space of the touch point.</param>
        /// <returns>The touch point of the primary device or null if no device is a primary device.</returns>
        public TouchPoint GetPrimaryTouchPoint(IInputElement relativeTo)
        {
            return TouchDevice.GetPrimaryTouchPoint(relativeTo);
        }

        /// <summary>
        ///     Suspends mouse promotion from this point until a touch up.
        /// </summary>
        /// <remarks>
        ///     This API is provided for Silverlight compatibility, but due to device
        ///     implementation differences, this method doesn't actually do anything.
        /// </remarks>
        public void SuspendMousePromotionUntilTouchUp()
        {
            // Needs implementation
        }
    }
}
