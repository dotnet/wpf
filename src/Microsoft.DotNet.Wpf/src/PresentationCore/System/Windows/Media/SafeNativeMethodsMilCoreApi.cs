// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  ABOUT THIS FILE:
//   -- This file contains native methods which are deemed SAFE for partial trust callers
//   -- These methods DO have the SuppressUnmanagedCodeSecurity attribute which means 
//       stalk walks for unmanaged 
//      code will stop with the immediate caler. 
//   -- Put methods in here which are needed in partial trust scenarios
//   -- If you have questions about how to use this file, email avsee

namespace MS.Win32.PresentationCore
{
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using System;
    using System.Security;
    using System.Collections;
    using System.IO;
    using System.Text;
    using System.Windows.Media.Composition;
    using MS.Internal.PresentationCore;

    using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
    using DllImport=MS.Internal.PresentationCore.DllImport;
    
    internal static partial class SafeNativeMethods
    {
       internal static int MilCompositionEngine_InitializePartitionManager(int nPriority)
       {
            return SafeNativeMethodsPrivate.MilCompositionEngine_InitializePartitionManager(nPriority);
       }

       internal static int MilCompositionEngine_DeinitializePartitionManager()
       {
            return SafeNativeMethodsPrivate.MilCompositionEngine_DeinitializePartitionManager();
       }

       internal static long GetNextPerfElementId()
       {
           return SafeNativeMethodsPrivate.GetNextPerfElementId();
       }

       private static partial class SafeNativeMethodsPrivate
       {
            [DllImport(DllImport.MilCore)]
            internal static extern int MilCompositionEngine_InitializePartitionManager(int nPriority);

            [DllImport(DllImport.MilCore)]
            internal static extern int MilCompositionEngine_DeinitializePartitionManager();

            [DllImport(DllImport.MilCore)]
            internal static extern long GetNextPerfElementId();
       }
    }
}

