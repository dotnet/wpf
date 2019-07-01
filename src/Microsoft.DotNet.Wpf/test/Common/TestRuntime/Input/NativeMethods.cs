// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//***************************************************************************
// HOW TO USE THIS FILE
//
// If you need access to a Win32 API that is not exposed, simply uncomment
// it in one of the following files:
// 
// NativeMethods.cs
// UnsafeNativeMethods.cs
// SafeNativeMethods.cs
//
// Only uncomment what you need to avoid code bloat.
//
// DO NOT adjust the visibility of anything in these files.  They are marked
// internal on pupose.
//***************************************************************************

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Test.Input.Win32
{
    internal class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct HWND
        {
            public IntPtr h;

            public static HWND Cast(IntPtr h)
            {
                HWND hTemp = new HWND();
                hTemp.h = h;
                return hTemp;
            }

            public static implicit operator IntPtr(HWND h)
            {
                return h.h;
            }

            public static HWND NULL
            {
                get
                {
                    HWND hTemp = new HWND();
                    hTemp.h = IntPtr.Zero;
                    return hTemp;
                }
            }

            public static bool operator ==(HWND hl, HWND hr)
            {
                return hl.h == hr.h;
            }

            public static bool operator !=(HWND hl, HWND hr)
            {
                return hl.h != hr.h;
            }

            override public bool Equals(object oCompare)
            {
                HWND hr = Cast((HWND)oCompare);
                return h == hr.h;
            }

            public override int GetHashCode()
            {
                return (int)h;
            }
        }
    }
}

