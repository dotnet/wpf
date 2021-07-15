// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
All rights reserved.

--*/

namespace MS.Internal.Printing.Configuration
{
    /// <remarks>
    /// From http://msdn.microsoft.com/en-us/library/dd144877(VS.85).aspx
    /// </remarks>
    internal enum DeviceCap : int
    {
        HORZRES         = 8,
        VERTRES         = 10,
        LOGPIXELSX      = 88,
        LOGPIXELSY      = 90,
        PHYSICALWIDTH   = 110,
        PHYSICALHEIGHT  = 111,
        PHYSICALOFFSETX = 112,
        PHYSICALOFFSETY = 113
    }
}