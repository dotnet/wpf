// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// This DragStartedEventArgs class contains additional information about the
    /// DragStarted event.
    /// </summary>
    /// <seealso cref="Thumb.DragStartedEvent" />
    /// <seealso cref="RoutedEventArgs" />
    public class DragStartedEventArgs: RoutedEventArgs
    {
        /// <summary>
        /// This is an instance constructor for the DragStartedEventArgs class.  It
        /// is constructed with a reference to the event being raised.
        /// </summary>
        /// <returns>Nothing.</returns>
        public DragStartedEventArgs(double horizontalOffset, double verticalOffset) : base()
        {
            _horizontalOffset = horizontalOffset;
            _verticalOffset = verticalOffset;
            RoutedEvent=Thumb.DragStartedEvent;
        }

        /// <value>
        /// Read-only access to the horizontal offset (relative to Thumb's co-ordinate).
        /// </value>
        public double HorizontalOffset
        {
            get { return _horizontalOffset; }
        }

        /// <value>
        /// Read-only access to the vertical offset (relative to Thumb's co-ordinate).
        /// </value>
        public double VerticalOffset
        {
            get { return _verticalOffset; }
        }

        /// <summary>
        /// This method is used to perform the proper type casting in order to
        /// call the type-safe DragStartedEventHandler delegate for the DragStartedEvent event.
        /// </summary>
        /// <param name="genericHandler">The handler to invoke.</param>
        /// <param name="genericTarget">The current object along the event's route.</param>
        /// <returns>Nothing.</returns>
        /// <seealso cref="Thumb.DragStartedEvent" />
        /// <seealso cref="DragStartedEventHandler" />
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            DragStartedEventHandler handler = (DragStartedEventHandler)genericHandler;
            handler(genericTarget, this);
        }

        private double _horizontalOffset;
        private double _verticalOffset;
    }

    /// <summary>
    ///     This delegate must used by handlers of the DragStarted event.
    /// </summary>
    /// <param name="sender">The current element along the event's route.</param>
    /// <param name="e">The event arguments containing additional information about the event.</param>
    /// <returns>Nothing.</returns>
    public delegate void DragStartedEventHandler(object sender, DragStartedEventArgs e);
}

