// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: BindingValueChanged event arguments
//
// Specs:       UIBinding.mht
//

using System;

namespace MS.Internal.Data
{
    /// <summary>
    /// Arguments for BindingValueChanged events.
    /// </summary>
    internal class BindingValueChangedEventArgs : EventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        internal BindingValueChangedEventArgs(object oldValue, object newValue) : base()
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// The old value of the binding.
        /// </summary>
        public object OldValue
        {
            get { return _oldValue; }
        }

        /// <summary>
        /// The new value of the binding.
        /// </summary>
        public object NewValue
        {
            get { return _newValue; }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private object _oldValue, _newValue;
    }
}
