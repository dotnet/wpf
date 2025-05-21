// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Rtf token type that is the enumeration of Rtf token type.
//

namespace System.Windows.Documents
{
    /// <summary>
    /// An enumeration of the Rtf token type
    /// </summary>
    internal enum RtfTokenType
    {
        TokenInvalid,
        TokenEOF,
        TokenText,
        TokenTextSymbol,
        TokenPictureData,
        TokenNewline,
        TokenNullChar,
        TokenControl,
        TokenDestination,
        TokenHex,
        TokenGroupStart,
        TokenGroupEnd
    };
}
