// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿/*++


 * Abstract:

    Flags for UnsafeNativeMethods.LoadLibraryEx
 
--*/

namespace MS.Internal.Printing.Configuration
{
    using System;

    [Flags]
    internal enum LoadLibraryExFlags : uint
    {
        LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
        LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040
    }
}
