// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿

namespace MS.Internal.Printing.Configuration
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;

    /// <summary>
    ///     Represents a printer handle used in spooler API's like OpenPrinter
    /// </summary>
    internal sealed class SafeWinSpoolPrinterHandle : SafeHandle
    {
        private SafeWinSpoolPrinterHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid
        {
            get
            {
                if (!base.IsClosed)
                {
                    return (base.handle == IntPtr.Zero);
                }
                return true;
            }
        }

        protected override bool ReleaseHandle()
        {
            return UnsafeNativeMethods.ClosePrinter(base.handle);
        }
    }
}
