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
    /// CompressionMethodEnum is used to express a required compression on ZipArchive.AddPart calls. 
    /// Values in the enumeration correspond to the binary format of the ZipArchive's Compression Method field  
    /// </summary>
    internal enum CompressionMethodEnum : ushort // takes 2 bytes in data structure 
    {
        Stored = 0, 
        Deflated = 8
    }
}
