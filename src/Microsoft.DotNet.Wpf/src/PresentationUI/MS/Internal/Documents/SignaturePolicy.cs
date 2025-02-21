// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: 
//    SignaturePolicy enum for determining what the applied signature will allow without breaking.
using System;

namespace MS.Internal.Documents
{
    [Flags]
    internal enum SignaturePolicy : int
    {
        AllowNothing = 0,
        ModifyDocumentProperties = 1,
        AllowSigning = 2
    }
}
