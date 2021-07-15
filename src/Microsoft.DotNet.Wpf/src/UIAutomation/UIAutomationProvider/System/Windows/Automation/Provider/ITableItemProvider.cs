// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Table Item pattern provider interface

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Used to expose grid items with header information.
    /// </summary>
    [ComVisible(true)]
    [Guid("b9734fa6-771f-4d78-9c90-2517999349cd")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface ITableItemProvider : IGridItemProvider
#else
    public interface ITableItemProvider : IGridItemProvider
#endif
    {
        /// <summary>Collection of all row headers for this cell</summary>
        IRawElementProviderSimple [] GetRowHeaderItems();

        /// <summary>Collection of all column headers for this cell</summary>
        IRawElementProviderSimple [] GetColumnHeaderItems();
    }
}
