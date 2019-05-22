// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Controls;
using System.Windows;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// This DragCompletedEventArgs class contains additional information about the
    /// DragCompleted event.
    /// </summary>
    /// <seealso cref="Thumb.DragCompletedEvent" />
    /// <seealso cref="RoutedEventArgs" />
    public class DragCompletedEventArgs: RoutedEventArgs
    {
        /// <summary>
        /// This is an instance constructor for the DragCompletedEventArgs class.  It
        /// is constructed with a reference to the event being raised.
        /// </summary>
        /// <returns>Nothing.</returns>
        public DragCompletedEventArgs(double horizontalChange, double verticalChange, bool canceled) : base()
        {
            _horizontalChange = horizontalChange;
            _verticalChange = verticalChange;
            _wasCanceled = canceled;
            RoutedEvent=Thumb.DragCompletedEvent;
        }

        /// <value>
        /// Read-only access to the horizontal distance between the point where mouse's left-button
        /// was pressed and the point where mouse's left-button was released
        /// </value>
        public double HorizontalChange
        {
            get { return _horizontalChange; }
        }

        /// <value>
        /// Read-only access to the vertical distance between the point where mouse's left-button
        /// was pressed and the point where mouse's left-button was released
        /// </value>
        public double VerticalChange
        {
            get { return _verticalChange; }
        }

        /// <summary>
        /// Read-only access to boolean states whether the drag operation was canceled or not.
        /// </summary>
        /// <value></value>
        public bool Canceled
        {
            get { return _wasCanceled; }
        }

        /// <summary>
        /// This method is used to perform the proper type casting in order to
        /// call the type-safe DragCompletedEventHandler delegate for the DragCompletedEvent event.
        /// </summary>
        /// <param name="genericHandler">The handler to invoke.</param>
        /// <param name="genericTarget">The current object along the event's route.</param>
        /// <returns>Nothing.</returns>
        /// <seealso cref="Thumb.DragCompletedEvent" />
        /// <seealso cref="DragCompletedEventHandler" />
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            DragCompletedEventHandler handler = (DragCompletedEventHandler)genericHandler;
            handler(genericTarget, this);
        }

        private double _horizontalChange;
        private double _verticalChange;
        private bool _wasCanceled;
    }

    /// <summary>
    ///     This delegate must used by handlers of the DragCompleted event.
    /// </summary>
    /// <param name="sender">The current element along the event's route.</param>
    /// <param name="e">The event arguments containing additional information about the event.</param>
    /// <returns>Nothing.</returns>
    public delegate void DragCompletedEventHandler(object sender, DragCompletedEventArgs e);
}

