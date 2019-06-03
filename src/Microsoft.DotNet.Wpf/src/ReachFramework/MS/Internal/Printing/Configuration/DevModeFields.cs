// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿/*++
All rights reserved.

--*/

namespace MS.Internal.Printing.Configuration
{
    using System;

    /// <remarks>
    /// From http://msdn.microsoft.com/en-us/library/cc244659(PROT.13).aspx
    /// </remarks>
    [Flags]
    internal enum DevModeFields : uint
    {
        DM_ORIENTATION = (uint)0x00000001,
        DM_PAPERSIZE = (uint)0x00000002,
        DM_PAPERLENGTH = (uint)0x00000004,
        DM_PAPERWIDTH = (uint)0x00000008,
        DM_SCALE = (uint)0x00000010,
        DM_POSITION = (uint)0x00000020,
        DM_NUP = (uint)0x00000040,
        DM_DISPLAYORIENTATION = (uint)0x00000080,
        DM_COPIES = (uint)0x00000100,
        DM_DEFAULTSOURCE = (uint)0x00000200,
        DM_PRINTQUALITY = (uint)0x00000400,
        DM_COLOR = (uint)0x00000800,
        DM_DUPLEX = (uint)0x00001000,
        DM_YRESOLUTION = (uint)0x00002000,
        DM_TTOPTION = (uint)0x00004000,
        DM_COLLATE = (uint)0x00008000,
        DM_FORMNAME = (uint)0x00010000,
        DM_LOGPIXELS = (uint)0x00020000,
        DM_BITSPERPEL = (uint)0x00040000,
        DM_PELSWIDTH = (uint)0x00080000,
        DM_PELSHEIGHT = (uint)0x00100000,
        DM_DISPLAYFLAGS = (uint)0x00200000,
        DM_DISPLAYFREQUENCY = (uint)0x00400000,
        DM_ICMMETHOD = (uint)0x00800000,
        DM_ICMINTENT = (uint)0x01000000,
        DM_MEDIATYPE = (uint)0x02000000,
        DM_DITHERTYPE = (uint)0x04000000,
        DM_DISPLAYFIXEDOUTPUT = (uint)0x20000000,
        All = (uint)0xFFFFFFFF
    }
}