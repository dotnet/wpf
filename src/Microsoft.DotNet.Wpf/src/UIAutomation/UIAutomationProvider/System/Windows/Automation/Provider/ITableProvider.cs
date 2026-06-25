// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: Table pattern provider interface

using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Identifies a grid that has header information.
    /// </summary>
    [ComVisible(true)]
    [Guid("9c860395-97b3-490a-b52a-858cc22af166")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface ITableProvider : IGridProvider
#else
    public interface ITableProvider : IGridProvider
#endif
    {
        /// <summary>Collection of all row headers for this table</summary>
        IRawElementProviderSimple [] GetRowHeaders();

        /// <summary>Collection of all column headers for this table</summary>
        IRawElementProviderSimple [] GetColumnHeaders();

        /// <summary>Indicates if the data is best presented by row or column</summary>
        RowOrColumnMajor RowOrColumnMajor
        {
            get;
        }
    }
}
