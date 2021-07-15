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
    ///     Describes a particular position and bounds of a TouchDevice.
    /// </summary>
    public class TouchPoint : IEquatable<TouchPoint>
    {
        /// <summary>
        ///     Creates an instance of this class and initializes its properties.
        /// </summary>
        /// <param name="device">
        ///     The TouchDevice that this TouchPoint describes. Must be non-null.
        /// </param>
        /// <param name="position">
        ///     The current location of the device.
        ///     The coordinate space of this parameter is defined by the caller and should be
        ///     consistent with the rectBounds parameter.
        /// </param>
        /// <param name="bounds">
        ///     The bounds of the area that the TouchDevice (i.e. finger) is in contact with the screen. 
        ///     The coordinate space of this parameter is defined by the caller and should be
        ///     consistent with the position parameter.</param>
        /// <param name="action">
        ///     Indicates the last action that occurred by this device at this location.
        /// </param>
        public TouchPoint(TouchDevice device, Point position, Rect bounds, TouchAction action)
        {
            if (device == null)
            {
                throw new ArgumentNullException("device");
            }

            TouchDevice = device;
            Position = position;
            Bounds = bounds;
            Action = action;
        }

        /// <summary>
        ///     The device associated with this TouchPoint.
        /// </summary>
        public TouchDevice TouchDevice
        {
            get;
            private set;
        }

        /// <summary>
        ///     The position of this device. The coordinate space is defined
        ///     by the provider of this object.
        /// </summary>
        public Point Position
        {
            get;
            private set;
        }

        /// <summary>
        ///     The bounds of the area that the finger is in contact with
        ///     the screen. The coordinate space is defined by the
        ///     provider of this object.
        /// </summary>
        public Rect Bounds
        {
            get;
            private set;
        }

        /// <summary>
        ///     Equivalent to Bounds.Size.
        /// </summary>
        public Size Size
        {
            get
            {
                return Bounds.Size;
            }
        }

        /// <summary>
        ///     The last action associated with this device.
        /// </summary>
        public TouchAction Action
        {
            get;
            private set;
        }

        #region IEquatable

        /// <summary>
        ///     Whether two TouchPoints are equivalent.
        /// </summary>
        /// <param name="other">Another TouchPoint.</param>
        /// <returns>true if this TouchPoint and the other TouchPoint are equivalent.</returns>
        bool IEquatable<TouchPoint>.Equals(TouchPoint other)
        {
            if (other != null)
            {
                return (other.TouchDevice == TouchDevice) &&
                    (other.Position == Position) &&
                    (other.Bounds == Bounds) &&
                    (other.Action == Action);
            }

            return false;
        }

        #endregion
    }
}
