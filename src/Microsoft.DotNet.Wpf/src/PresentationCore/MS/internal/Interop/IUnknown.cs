// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace MS.Internal.Interop
{
    internal unsafe interface IUnknown
    {
        int QueryInterface(Guid* guid, void** comObject);

        uint AddRef();

        uint Release();
    }
}
