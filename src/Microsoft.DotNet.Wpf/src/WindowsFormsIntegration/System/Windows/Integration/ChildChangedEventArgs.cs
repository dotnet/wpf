// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Windows.Forms.Integration
{
    /// <summary>
    /// Occurs when the Child property changes.
    /// </summary>
    public class ChildChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the ChildChangedEventArgs class.
        /// </summary>
        public ChildChangedEventArgs(object previousChild)
        {
            _previousChild = previousChild;
        }

        private object _previousChild;

        /// <summary>
        /// The value of the Child property before it was set to a new value.
        /// </summary>
        public object PreviousChild
        {
            get
            {
                return _previousChild;
            }
        }
    }
}
