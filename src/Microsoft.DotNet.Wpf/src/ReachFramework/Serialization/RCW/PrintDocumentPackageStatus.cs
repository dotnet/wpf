// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Runtime.InteropServices;

namespace System.Windows.Xps.Serialization.RCW
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct PrintDocumentPackageStatus
    {
        public uint JobId;

        public int CurrentDocument;

        public int CurrentPage;

        public int CurrentPageTotal;

        public PrintDocumentPackageCompletion Completion;

        [MarshalAs(UnmanagedType.Error)]
        public int PackageStatus;
    }
}
