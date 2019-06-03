// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Class that defines all the non-localizable constants 
// in Baml PomParser.
//

namespace MS.Internal.Globalization
{
    internal static class BamlConst
    {
        internal const string ContentSuffix = "$Content";
        internal const string LiteralContentSuffix = "$LiteralContent";
        // parsing $Content.
        internal const char KeySeperator = ':';
        internal const char ChildStart = '#';
        internal const char ChildEnd = ';';
        internal const char EscapeChar = '\\';
    }
}
