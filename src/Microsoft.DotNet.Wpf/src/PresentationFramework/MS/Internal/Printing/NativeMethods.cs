// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace MS.Internal.Printing
{
    internal static class NativeMethods
    {
        internal const UInt32 PD_ALLPAGES = 0x00000000;
        internal const UInt32 PD_SELECTION = 0x00000001;
        internal const UInt32 PD_PAGENUMS = 0x00000002;
        internal const UInt32 PD_NOSELECTION = 0x00000004;
        internal const UInt32 PD_NOPAGENUMS = 0x00000008;
        internal const UInt32 PD_USEDEVMODECOPIESANDCOLLATE = 0x00040000;
        internal const UInt32 PD_DISABLEPRINTTOFILE = 0x00080000;
        internal const UInt32 PD_HIDEPRINTTOFILE = 0x00100000;
        internal const UInt32 PD_CURRENTPAGE = 0x00400000;
        internal const UInt32 PD_NOCURRENTPAGE = 0x00800000;
        internal const UInt32 PD_RESULT_CANCEL = 0x0;
        internal const UInt32 PD_RESULT_PRINT = 0x1;
        internal const UInt32 PD_RESULT_APPLY = 0x2;
        internal const UInt32 START_PAGE_GENERAL = 0xFFFFFFFF;

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Auto)]
        internal class PRINTDLGEX32
        {
            public int lStructSize = Marshal.SizeOf(typeof(PRINTDLGEX32));
            public IntPtr hwndOwner = IntPtr.Zero;
            public IntPtr hDevMode = IntPtr.Zero;
            public IntPtr hDevNames = IntPtr.Zero;
            public IntPtr hDC = IntPtr.Zero;
            public UInt32 Flags = 0;
            public UInt32 Flags2 = 0;
            public UInt32 ExclusionFlags = 0;
            public UInt32 nPageRanges = 0;
            public UInt32 nMaxPageRanges = 0;
            public IntPtr lpPageRanges = IntPtr.Zero;
            public UInt32 nMinPage = 0;
            public UInt32 nMaxPage = 0;
            public UInt32 nCopies = 0;
            public IntPtr hInstance = IntPtr.Zero;
            public IntPtr lpPrintTemplateName = IntPtr.Zero;
            public IntPtr lpCallback = IntPtr.Zero;
            public UInt32 nPropertyPages = 0;
            public IntPtr lphPropertyPages = IntPtr.Zero;
            public UInt32 nStartPage = START_PAGE_GENERAL;
            public UInt32 dwResultAction = 0;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Auto)]
        internal class PRINTDLGEX64
        {
            public int lStructSize = Marshal.SizeOf(typeof(PRINTDLGEX64));
            public IntPtr hwndOwner = IntPtr.Zero;
            public IntPtr hDevMode = IntPtr.Zero;
            public IntPtr hDevNames = IntPtr.Zero;
            public IntPtr hDC = IntPtr.Zero;
            public UInt32 Flags = 0;
            public UInt32 Flags2 = 0;
            public UInt32 ExclusionFlags = 0;
            public UInt32 nPageRanges = 0;
            public UInt32 nMaxPageRanges = 0;
            public IntPtr lpPageRanges = IntPtr.Zero;
            public UInt32 nMinPage = 0;
            public UInt32 nMaxPage = 0;
            public UInt32 nCopies = 0;
            public IntPtr hInstance = IntPtr.Zero;
            public IntPtr lpPrintTemplateName = IntPtr.Zero;
            public IntPtr lpCallback = IntPtr.Zero;
            public UInt32 nPropertyPages = 0;
            public IntPtr lphPropertyPages = IntPtr.Zero;
            public UInt32 nStartPage = START_PAGE_GENERAL;
            public UInt32 dwResultAction = 0;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Auto)]
        internal struct DEVMODE
        {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;
            public UInt16 dmSpecVersion;
            public UInt16 dmDriverVersion;
            public UInt16 dmSize;
            public UInt16 dmDriverExtra;
            public UInt32 dmFields;

            public int dmPositionX;
            public int dmPositionY;
            public UInt32 dmDisplayOrientation;
            public UInt32 dmDisplayFixedOutput;

            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            public UInt16 dmLogPixels;
            public UInt32 dmBitsPerPel;
            public UInt32 dmPelsWidth;
            public UInt32 dmPelsHeight;
            public UInt32 dmDisplayFlags;
            public UInt32 dmDisplayFrequency;
            public UInt32 dmICMMethod;
            public UInt32 dmICMIntent;
            public UInt32 dmMediaType;
            public UInt32 dmDitherType;
            public UInt32 dmReserved1;
            public UInt32 dmReserved2;
            public UInt32 dmPanningWidth;
            public UInt32 dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Auto)]
        internal struct DEVNAMES
        {
            public ushort wDriverOffset;
            public ushort wDeviceOffset;
            public ushort wOutputOffset;
            public ushort wDefault;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Auto)]
        internal struct PRINTPAGERANGE
        {
            public UInt32 nFromPage;
            public UInt32 nToPage;
        }
    }
}
