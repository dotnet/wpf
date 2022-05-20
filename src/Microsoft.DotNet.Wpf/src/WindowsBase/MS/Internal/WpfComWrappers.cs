// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal;

using System;
using System.Collections;
using System.Runtime.InteropServices;

internal partial class WpfComWrappers : ComWrappers
{
    // AA80E801-2021-11D2-93E0-0060B067B86E
    private static readonly Guid IID_ITfThreadMgr = new Guid(0xAA80E801, 0x2021, 0x11D2, 0x93, 0xE0, 0x00, 0x60, 0xB0, 0x67, 0xB8, 0x6E);

    // AA80E7F4-2021-11D2-93E0-0060B067B86E
    private static readonly Guid IID_ITfDocumentMgr = new Guid(0xAA80E7F4, 0x2021, 0x11D2, 0x93, 0xE0, 0x00, 0x60, 0xB0, 0x67, 0xB8, 0x6E);

    public static WpfComWrappers Instance { get; } = new WpfComWrappers();

    protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        throw new NotImplementedException();
    }

    protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
    {
        var tfThreadMgrIID = IID_ITfThreadMgr;
        if (Marshal.QueryInterface(externalComObject, ref tfThreadMgrIID, out var tfThreadMgrPtr) >= 0)
        {
            Marshal.Release(externalComObject);
            return new TfThreadMgrWrapper(tfThreadMgrPtr);
        }

        throw new NotImplementedException();
    }

    protected override void ReleaseObjects(IEnumerable objects)
    {
        throw new NotImplementedException();
    }
}
