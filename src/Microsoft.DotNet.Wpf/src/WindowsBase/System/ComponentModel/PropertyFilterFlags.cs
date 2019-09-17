// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.ComponentModel 
{
    using System;

    /// <summary>
    ///     This set of flags can be wrapped in
    ///     a PropertyFilterAttribute to filter
    ///     the set of properties returned from
    ///     a TypeDescriptor query.
    /// </summary>
    [Flags]
    public enum PropertyFilterOptions
    {
        /// <summary>
        ///     Return no properties
        /// </summary>
        None = 0x00,

        /// <summary>
        ///     Returns those properties that are not valid 
        ///     given the current context of the object.
        /// </summary>
        Invalid = 0x01,

        /// <summary>
        ///     Return only those properties that have
        ///     local values currently set.
        /// </summary>
        SetValues = 0x02,

        /// <summary>
        ///     Return those properties whose local values are 
        ///     not set or do not have properties set in an 
        ///     external expression store.
        /// </summary>
        UnsetValues = 0x04,

        /// <summary>
        ///     Return any property that is valid on the
        ///     object in the current scope (really only
        ///     affects attached properties).
        /// </summary>
        Valid = 0x08,

        /// <summary>
        ///     Return all properties, even those that
        ///     are not valid in the current scope (again,
        ///     really only affects attached properties).
        /// </summary>
        All = 0x0F
    }
}

