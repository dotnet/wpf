// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//    IOwnedObject is an internal interface that identifies objects in the
//    Annotation Framework Object Model that can belong to only one parent.
//    This restriction is in place to prevent OM structures that cannot be
//    reproduced from a round-trip serialization/deserialization.
//

using System;

namespace MS.Internal.Annotations
{
    /// <summary>
    ///     Interface that identifies classes in the Annotation Framework Object
    ///     Model that can belong to only one parent.
    /// </summary>
    internal interface IOwnedObject
    {
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     Sets/gets the ownership status of this object.
        /// </summary>
        bool Owned
        {
            get;
            set;
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
    }
}
