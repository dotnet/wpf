// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                                              
    Abstract:        
        
        PInvoke methods definition.
                                                            
--*/

using System;
using System.Runtime.InteropServices;

namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
namespace Win32ApiThunk
{
    static internal class NativeMethodsForPrintExceptions
    {
        [DllImport("Kernel32.dll", EntryPoint="FormatMessageW",
                   CharSet=CharSet.Unicode,
                   SetLastError=true, 
                   CallingConvention = CallingConvention.Winapi)]
        
        public extern static int InvokeFormatMessage(int a, IntPtr b , int c, int d, System.Text.StringBuilder e, int f, IntPtr g);
        
    };

}
}
}
}
