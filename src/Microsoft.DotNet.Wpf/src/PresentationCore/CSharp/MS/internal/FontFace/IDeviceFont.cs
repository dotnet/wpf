// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Base definition of device font
//
//

using System;
using System.Security;


namespace MS.Internal.FontFace
{
    internal interface IDeviceFont
    {
        /// <summary>
        /// Well known name of device font
        /// </summary>
        string Name
        { get; }

        /// <summary>
        /// Returns true if the device font maps the specified character.
        /// </summary>
        bool ContainsCharacter(int unicodeScalar);

        /// <summary>
        /// Return advance widths corresponding to characters in a given string.
        /// </summary>
        unsafe void GetAdvanceWidths(
            char*   characterString,
            int     characterLength,
            double  emSize,
            int*    pAdvances
        );
    }
}
