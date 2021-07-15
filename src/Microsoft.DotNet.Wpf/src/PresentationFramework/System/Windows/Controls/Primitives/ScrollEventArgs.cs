// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Controls;
using System.Windows;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// Occurs when the Value property has changed, either by a Scroll event or programmatically.
    /// </summary>
    /// <seealso cref="ScrollBar.ScrollEvent" />
    /// <seealso cref="RoutedEventArgs" />
    public class ScrollEventArgs: RoutedEventArgs
    {
        /// <summary>
        /// This is an instance constructor for the ScrollEventArgs class.  It
        /// is constructed with a reference to the event being raised.
        /// </summary>
        /// <returns>Nothing.</returns>
        public ScrollEventArgs(ScrollEventType scrollEventType, double newValue) : base()
        {
            _scrollEventType = scrollEventType;
            _newValue = newValue;
            RoutedEvent =ScrollBar.ScrollEvent;
        }

        /// <value>
        /// Read-only access to the type of scroll event.
        /// </value>
        public ScrollEventType ScrollEventType
        {
            get { return _scrollEventType; }
        }

        /// <value>
        /// Read-only access to new value of ScrollBar.
        /// </value>
        public double NewValue
        {
            get { return _newValue; }
        }

	
        /// <summary>
        /// This method is used to perform the proper type casting in order to
        /// call the type-safe ScrollEventHandler delegate for the ScrollEvent event.
        /// </summary>
        /// <param name="genericHandler">The handler to invoke.</param>
        /// <param name="genericTarget">The current object along the event's route.</param>
        /// <returns>Nothing.</returns>
        /// <seealso cref="ScrollBar.ScrollEvent" />
        /// <seealso cref="ScrollEventHandler" />
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            ScrollEventHandler  handler = (ScrollEventHandler)genericHandler;
            handler(genericTarget, this);
        }

        private ScrollEventType _scrollEventType;
        private double _newValue;
    }

    /// <summary>
    ///     This delegate must used by handlers of the Scroll event.
    /// </summary>
    /// <param name="sender">The current element along the event's route.</param>
    /// <param name="e">The event arguments containing additional information about the event.</param>
    /// <returns>Nothing.</returns>
    public delegate void ScrollEventHandler(object sender, ScrollEventArgs e);
}

