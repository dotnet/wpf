// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace MS.Win32.PresentationCore
{
    internal static partial class SafeNativeMethods
    {
        [DllImport(DllImport.MilCore)]
        internal static extern HRESULT MilCompositionEngine_InitializePartitionManager(int nPriority);

        [DllImport(DllImport.MilCore)]
        internal static extern HRESULT MilCompositionEngine_DeinitializePartitionManager();

        [DllImport(DllImport.MilCore)]
        internal static extern long GetNextPerfElementId();
    }
}

