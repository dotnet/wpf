// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   Xaml Writer Modes for values that are of type Expression.
//

using System;

namespace System.Windows.Markup
{
    /// <summary>
    ///    Xaml Writer Modes for values that 
    ///    are of type Expression.
    /// </summary>
    public enum XamlWriterMode
    {
        /// <summary>
        ///     Serialize the expression itself 
        ///     Eg. *Bind(...
        /// </summary>
        Expression,

        /// <summary>
        ///     Evaluated value of the expression will be serialized
        ///     Eg. The serialization requirements for printing 
        ///     always just require a snap shot of the tree 
        ///     and do not care about evaluating references 
        ///     etc. So in this case we will always want to 
        ///     serialize dereferenced values
        /// </summary>
        Value

        // NOTE: if you add or remove any values in this enum, be sure to update XamlDesignerSerializationManager.IsValidSerializationMode()
    }
}

