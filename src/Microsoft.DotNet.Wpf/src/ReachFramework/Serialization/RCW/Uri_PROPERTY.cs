// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Xps.Serialization.RCW
{
    /// <summary>
    /// RCW for xpsobjectmodel.idl found in Windows SDK
    /// This is generated code with minor manual edits. 
    /// i.  Generate TLB
    ///      MIDL /TLB xpsobjectmodel.tlb xpsobjectmodel.IDL //xpsobjectmodel.IDL found in Windows SDK
    /// ii. Generate RCW in a DLL
    ///      TLBIMP xpsobjectmodel.tlb // Generates xpsobjectmodel.dll
    /// iii.Decompile the DLL and copy out the RCW by hand.
    ///      ILDASM xpsobjectmodel.dll
    /// </summary>
    
    internal enum Uri_PROPERTY
    {
        Uri_PROPERTY_ABSOLUTE_URI = 0,
        Uri_PROPERTY_STRING_START = 0,
        Uri_PROPERTY_AUTHORITY = 1,
        Uri_PROPERTY_DISPLAY_URI = 2,
        Uri_PROPERTY_DOMAIN = 3,
        Uri_PROPERTY_EXTENSION = 4,
        Uri_PROPERTY_FRAGMENT = 5,
        Uri_PROPERTY_HOST = 6,
        Uri_PROPERTY_PASSWORD = 7,
        Uri_PROPERTY_PATH = 8,
        Uri_PROPERTY_PATH_AND_QUERY = 9,
        Uri_PROPERTY_QUERY = 10,
        Uri_PROPERTY_RAW_URI = 11,
        Uri_PROPERTY_SCHEME_NAME = 12,
        Uri_PROPERTY_USER_INFO = 13,
        Uri_PROPERTY_STRING_LAST = 14,
        Uri_PROPERTY_USER_NAME = 14,
        Uri_PROPERTY_DWORD_START = 15,
        Uri_PROPERTY_HOST_TYPE = 15,
        Uri_PROPERTY_PORT = 16,
        Uri_PROPERTY_SCHEME = 17,
        Uri_PROPERTY_DWORD_LAST = 18,
        Uri_PROPERTY_ZONE = 18
    }
}