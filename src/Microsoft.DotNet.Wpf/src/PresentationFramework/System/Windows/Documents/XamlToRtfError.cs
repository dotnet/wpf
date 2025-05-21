// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: XamlToRtf error enumeration that indicates the error from
//              converting XamlToRtf or RtfToXaml cases.
//

namespace System.Windows.Documents
{
    /// <summary>
    /// XamlToRtf or RtfToXaml content converting error enumeration.
    /// </summary>
    internal enum XamlToRtfError
    {
        None,
        InvalidFormat,
        InvalidParameter,
        InsufficientMemory,
        OutOfRange,
        Unknown
    }
}
