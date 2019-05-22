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
    using System.Security.Permissions;
    using System.Collections;
    using System.IO;
    using System.Text;
    using System.Windows.Media.Composition;
    using MS.Internal.PresentationCore;

    using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
    using DllImport=MS.Internal.PresentationCore.DllImport;
    
    internal static partial class SafeNativeMethods
    {
       ///<SecurityNote>
       ///  TreatAsSafe: The security model here is that these APIs could be publicly exposed to partial trust
       ///               callers - no risk. 
       ///  Critical: This code elevates priviliges by adding a SuppressUnmanagedCodeSecurity  
       ///</SecurityNote>
       [SecurityCritical, SecurityTreatAsSafe]
       internal static int MilCompositionEngine_InitializePartitionManager(int nPriority)
       {
            return SafeNativeMethodsPrivate.MilCompositionEngine_InitializePartitionManager(nPriority);
       }

       ///<SecurityNote>
       ///  TreatAsSafe: The security model here is that these APIs could be publicly exposed to partial trust
       ///               callers - no risk. 
       ///  Critical: This code elevates priviliges by adding a SuppressUnmanagedCodeSecurity  
       ///</SecurityNote>
       [SecurityCritical, SecurityTreatAsSafe]
       internal static int MilCompositionEngine_DeinitializePartitionManager()
       {
            return SafeNativeMethodsPrivate.MilCompositionEngine_DeinitializePartitionManager();
       }

       [SecurityCritical, SecurityTreatAsSafe]
       internal static long GetNextPerfElementId()
       {
           return SafeNativeMethodsPrivate.GetNextPerfElementId();
       }

       /// <SecurityNote>
       ///  Critical - Uses SuppressUnmanagedCodeSecurityAttribute.
       /// </SecurityNote>
       [SuppressUnmanagedCodeSecurity, SecurityCritical(SecurityCriticalScope.Everything)]
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

