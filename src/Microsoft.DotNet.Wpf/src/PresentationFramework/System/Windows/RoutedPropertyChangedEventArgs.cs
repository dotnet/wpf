// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Input;

// Disable CS3001, CS3003, CS3024: Warning as Error: not CLS-compliant
#pragma warning disable 3001, 3003, 3024

namespace System.Windows
{
    /// <summary>
    ///     This delegate must used by handlers of the RoutedPropertyChangedEvent event.
    /// </summary>
    /// <param name="sender">The current element along the event's route.</param>
    /// <param name="e">The event arguments containing additional information about the event.</param>
    /// <returns>Nothing.</returns>
    public delegate void RoutedPropertyChangedEventHandler<T>(object sender, RoutedPropertyChangedEventArgs<T> e);

    /// <summary>
    /// This RoutedPropertyChangedEventArgs class contains old and new value when 
    /// RoutedPropertyChangedEvent is raised.
    /// </summary>
    /// <seealso cref="RoutedEventArgs" />
    /// <typeparam name="T"></typeparam>
    public class RoutedPropertyChangedEventArgs<T> : RoutedEventArgs
    {
        /// <summary>
        /// This is an instance constructor for the RoutedPropertyChangedEventArgs class.
        /// It is constructed with a reference to the event being raised.
        /// </summary>
        /// <param name="oldValue">The old property value</param>
        /// <param name="newValue">The new property value</param>
        /// <returns>Nothing.</returns>
        public RoutedPropertyChangedEventArgs(T oldValue, T newValue)
            : base()
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        /// <summary>
        /// This is an instance constructor for the RoutedPropertyChangedEventArgs class.
        /// It is constructed with a reference to the event being raised.
        /// </summary>
        /// <param name="oldValue">The old property value</param>
        /// <param name="newValue">The new property value</param>
        /// <param name="routedEvent">RoutedEvent</param>
        /// <returns>Nothing.</returns>
        public RoutedPropertyChangedEventArgs(T oldValue, T newValue, RoutedEvent routedEvent)
            : this(oldValue, newValue)
        {
            RoutedEvent = routedEvent;
        }

        /// <summary>
        /// Return the old value
        /// </summary>
        public T OldValue
        {
            get { return _oldValue; }
        }

        /// <summary>
        /// Return the new value
        /// </summary>
        public T NewValue
        {
            get { return _newValue; }
        }

        /// <summary>
        /// This method is used to perform the proper type casting in order to
        /// call the type-safe RoutedPropertyChangedEventHandler delegate for the IsCheckedChangedEvent event.
        /// </summary>
        /// <param name="genericHandler">The handler to invoke.</param>
        /// <param name="genericTarget">The current object along the event's route.</param>
        /// <returns>Nothing.</returns>
        /// <seealso cref="RoutedPropertyChangedEventHandler<T>" />
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            RoutedPropertyChangedEventHandler<T> handler = (RoutedPropertyChangedEventHandler<T>)genericHandler;
            handler(genericTarget, this);
        }

        private T _oldValue;
        private T _newValue;
    }
}
