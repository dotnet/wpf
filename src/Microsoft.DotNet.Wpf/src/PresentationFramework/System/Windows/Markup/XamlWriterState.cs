// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description:
//   Xaml Writer States for values that are of type Expression.
//

namespace System.Windows.Markup
{
    /// <summary>
    ///    Xaml Writer States are possible serialization events
    /// </summary>
    public enum XamlWriterState
    {
        /// <summary>
        ///    Serialization is starting
        /// </summary>
        Starting
            = 0,
        /// <summary>
        ///     Serialization is done
        /// </summary>
        Finished = 1
    }
}

