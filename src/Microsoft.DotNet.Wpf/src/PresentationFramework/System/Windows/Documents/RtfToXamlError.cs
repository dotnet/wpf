// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description: Define the RtfToXaml conversion errors.
//

namespace System.Windows.Documents
{
    /// <summary>
    /// RtfToXaml content converting error enumeration.
    /// </summary>
    internal enum RtfToXamlError
    {
        None,
        InvalidFormat,
        InvalidParameter,
        InsufficientMemory,
        OutOfRange
    }
}
