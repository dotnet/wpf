// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics; // For Assert

namespace System.Windows
{
    /// <summary>
    ///     Authorization key for access to read-only DependencyProperty.
    /// Acquired via DependencyProperty.RegisterReadOnly/RegisterAttachedReadOnly
    /// and used in DependencyObject.SetValue/ClearValue.
    /// </summary>
    /// <remarks>
    ///     This object can have a transient state upon creation where the _dp 
    /// field can be null until initialized.  However in use _dp needs to always
    /// be non-null.  Otherwise it is treated as a key that can't unlock anything.
    /// (When needed, this property is available via the static constant NoAccess.
    /// </remarks>
    public sealed class DependencyPropertyKey
    {
        /// <summary>
        ///     The DependencyProperty associated with this access key.  This key
        /// does not authorize access to any other property.
        /// </summary>
        public DependencyProperty DependencyProperty
        {
            get
            {
                return _dp;
            }
        }

        internal DependencyPropertyKey(DependencyProperty dp)
        {
            _dp = dp;
        }

        /// <summary>
        ///     Override the metadata of a property that is already secured with
        /// this key.
        /// </summary>
        public void OverrideMetadata( Type forType, PropertyMetadata typeMetadata )
        {
            if( _dp == null )
            {
                throw new InvalidOperationException();
            }

            _dp.OverrideMetadata( forType, typeMetadata, this );
        }

        // This is not a property setter because we can't have a public
        //  property getter and a internal property setter on the same property.
        internal void SetDependencyProperty(DependencyProperty dp)
        {
            Debug.Assert(_dp==null,"This should only be used when we need a placeholder and have a temporary value of null. It should not be used to change this property.");
            _dp = dp;
        }

        private DependencyProperty _dp = null;
    }
}

