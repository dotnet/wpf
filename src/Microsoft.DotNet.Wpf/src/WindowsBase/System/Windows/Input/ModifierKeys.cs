// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.ComponentModel;
using System.Windows.Markup;

namespace System.Windows.Input
{
    /// <summary>
    ///     The ModifierKeys enumeration describes a set of common keys
    ///     used to modify other input operations.
    /// </summary>
    [TypeConverter(typeof(ModifierKeysConverter))]
    [ValueSerializer(typeof(ModifierKeysValueSerializer))]
    [Flags]
    public enum ModifierKeys
    {
        /// <summary>
        ///    No modifiers are pressed.
        /// </summary>
        None = 0,

        /// <summary>
        ///    An alt key.
        /// </summary>
        Alt = 1,
        
        /// <summary>
        ///    A control key.
        /// </summary>
        Control = 2,

        /// <summary>
        ///    A shift key.
        /// </summary>
        Shift = 4,
        
        /// <summary>
        ///    A windows key.
        /// </summary>
        Windows = 8
    }
}


