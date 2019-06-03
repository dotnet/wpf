// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Grid Item pattern provider interface

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Represents an item that is within a grid.  Has no methods, only properties.
    /// </summary>
    [ComVisible(true)]
    [Guid("d02541f1-fb81-4d64-ae32-f520f8a6dbd1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IGridItemProvider
#else
    public interface IGridItemProvider
#endif
    {
        /// <summary>
        /// the row number of the element.  This is zero based.
        /// </summary>
        int Row
        {
            get;
        }

        /// <summary>
        /// the column number of the element.  This is zero based.
        /// </summary>
        int Column
        {
            get;
        }

        /// <summary>
        /// count of how many rows the element spans
        /// -- non merged cells should always return 1
        /// </summary>
        int RowSpan
        {
            get;
        }

        /// <summary>
        /// count of how many columns the element spans
        /// -- non merged cells should always return 1
        ///</summary>
        int ColumnSpan
        {
            get;
        }

        /// <summary>
        /// The logical element that supports the GripPattern for this Item
        ///</summary>
        IRawElementProviderSimple ContainingGrid
        {
            get;
        }
    }
}
