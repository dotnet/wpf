// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Define the Xaml token type to parse Xaml on XamlToRtf converter.
//

namespace System.Windows.Documents
{
    /// <summary>
    /// XamlTokenType
    /// </summary>
    internal enum XamlTokenType
    {
        XTokInvalid,
        XTokEOF,
        XTokCharacters,
        XTokEntity,
        XTokStartElement,
        XTokEndElement,
        XTokCData,
        XTokPI,
        XTokComment,
        XTokWS
    };
}
