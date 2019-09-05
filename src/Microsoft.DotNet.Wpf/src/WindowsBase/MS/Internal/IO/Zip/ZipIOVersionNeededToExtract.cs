// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
//
// Description:
//  This is an internal enumeration that maintains version needed to extract values 
//  for OPC scenarios 
//
//
//
//

    
namespace MS.Internal.IO.Zip
{
    internal enum ZipIOVersionNeededToExtract : ushort 
    {
        StoredData = 10,
        VolumeLabel = 11,
        DeflatedData = 20,
        Zip64FileFormat = 45,
    }
}
