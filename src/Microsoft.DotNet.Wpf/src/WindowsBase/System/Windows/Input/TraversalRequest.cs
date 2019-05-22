// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Input
{
    /// <summary>
    /// Represents a request to an element to move focus to another control.
    /// </summary>
    [Serializable()]
    public class TraversalRequest
    {
        /// <summary>
        /// Constructor that requests passing FocusNavigationDirection
        /// </summary>
        /// <param name="focusNavigationDirection">Type of focus traversal to perform</param>
        public TraversalRequest(FocusNavigationDirection focusNavigationDirection)
        {
            if (focusNavigationDirection != FocusNavigationDirection.Next &&
                 focusNavigationDirection != FocusNavigationDirection.Previous &&
                 focusNavigationDirection != FocusNavigationDirection.First &&
                 focusNavigationDirection != FocusNavigationDirection.Last &&
                 focusNavigationDirection != FocusNavigationDirection.Left &&
                 focusNavigationDirection != FocusNavigationDirection.Right &&
                 focusNavigationDirection != FocusNavigationDirection.Up &&
                 focusNavigationDirection != FocusNavigationDirection.Down)
            {
                throw new System.ComponentModel.InvalidEnumArgumentException("focusNavigationDirection", (int)focusNavigationDirection, typeof(FocusNavigationDirection));
            }

            _focusNavigationDirection = focusNavigationDirection;
        }

        /// <summary>
        /// true if reached the end of child elements that should have focus
        /// </summary>
        public bool Wrapped
        {
            get{return _wrapped;}
            set{_wrapped = value;}
        }

        /// <summary>
        /// Determine how to move the focus
        /// </summary>
        public FocusNavigationDirection FocusNavigationDirection { get { return _focusNavigationDirection; } }

        private bool _wrapped;
        private FocusNavigationDirection _focusNavigationDirection;
}

    /// <summary>
    /// Determine how to move the focus
    /// </summary>
    public enum FocusNavigationDirection
    {
        /// <summary>
        /// Move the focus to the next Control in Tab order.
        /// </summary>
        Next,

        /// <summary>
        /// Move the focus to the previous Control in Tab order. Shift+Tab
        /// </summary>
        Previous,

        /// <summary>
        /// Move the focus to the first Control in Tab order inside the subtree.
        /// </summary>
        First,

        /// <summary>
        /// Move the focus to the last Control in Tab order inside the subtree.
        /// </summary>
        Last,

        /// <summary>
        /// Move the focus to the left.
        /// </summary>
        Left,

        /// <summary>
        /// Move the focus to the right.
        /// </summary>
        Right,

        /// <summary>
        /// Move the focus to the up.
        /// </summary>
        Up,

        /// <summary>
        /// Move the focus to the down.
        /// </summary>
        Down,

        // If you add a new value you should also add a validation check to TraversalRequest constructor
    }
}
