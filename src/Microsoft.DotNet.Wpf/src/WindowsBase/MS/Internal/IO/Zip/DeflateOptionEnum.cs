// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
//
// Description:
//  This is an internal class that enables interactions with Zip archives
//  for OPC scenarios 
//
//
//
//

using System;

namespace MS.Internal.IO.Zip
{
    /// <summary>
    /// DeflateOptionEnum is used to express a required compression on ZipArchive.AddPart calls 
    /// Values in the enumeration (except None 0xFF) correspond to the binary format of the 
    /// ZipArchive's General Purpose Bit flags (bits 1 and 2). In order to match this value with the 
    /// General Purpose Bit flags, DeflateOptionEnum must be masked by the value 0x06
    /// 0xFF is a special value that is used to indicate "not applicable" for cases when data isn't deflated.  
    /// </summary>
    internal enum DeflateOptionEnum : byte // takes 2 bits in the data structure 
    {
        Normal = 0,         //values are selected based on their position in the General purpose bit flag 
        Maximum = 2,    // bits 1 and 2 
        Fast = 4,
        SuperFast = 6,
        None = 0xFF
    }
}
