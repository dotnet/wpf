// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Windows 
{
    using MS.Internal.WindowsBase;
    using System;

    /// <summary>
    ///     This is the base class for all attached property browsable attributes.  
    ///     TypeDescriptor will call IsBrowsable for each attribute it discovers 
    ///     on the method metadata.  Note that the method TypeDescriptor examines 
    ///     is always the method on the class returned from the dependency property's 
    ///     OwnerType property.  If another type calls AddOwner, the new property is 
    ///     considered a "direct" property, not an attached property, and no search 
    ///     for a matching method will be performed.
    /// </summary>
    public abstract class AttachedPropertyBrowsableAttribute : Attribute
    {
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        /// <summary>
        ///     Used to determine the browsable algorithm.  Normally, all 
        ///     AttachedPropertyBrowsable attributes must return true from 
        ///     IsBrowsable in order for the property to be considered browsable 
        ///     for the given dependency object.  If UnionResults is true, the 
        ///     IsBrowsable result from all AttachedPropertyBrowsable attributes 
        ///     of the same type will be logically or-ed together, and the result 
        ///     will be used to test for browsability.  UnionResults only applies 
        ///     to attributes of the same type.
        /// </summary>
        internal virtual bool UnionResults { get { return false; } }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        /// <summary>
        ///     Returns true if the object allows the given dependency property 
        ///     should be visible on the given dependency object.
        /// </summary>
        [FriendAccessAllowed] // Built into Base, also used by Framework.
        internal abstract bool IsBrowsable(DependencyObject d, DependencyProperty dp);
    }
}

