// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//------------------------------------------------------------------------------
//
//
//  ABOUT THIS FILE:
//   -- This file contains native methods which are deemed NOT SAFE for partial trust callers
//   -- These methods DO NOT have the SuppressUnmanagedCodeSecurity attribute on them 
//      which means stalk walks for unmanaged code will bubble all the way up the stack
//   -- Put methods in here which are not needed in partial trust scenarios and/or when a stack walk is
//      appropriate
//   -- If you have questions about how to use this file, email avsee
//-----------------------------------------------------------------------------

namespace MS.Win32
{
    using Accessibility;
    using System.Runtime.InteropServices;
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using Microsoft.Win32;
    using System.Windows.Media.Composition;

    internal static partial class NativeMethods 
    {
    }
}

