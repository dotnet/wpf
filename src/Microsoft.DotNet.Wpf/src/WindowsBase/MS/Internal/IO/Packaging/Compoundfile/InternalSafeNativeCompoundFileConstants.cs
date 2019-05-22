// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  The COM and P/Invoke interop code necessary for the managed compound
//  file layer to call the existing APIs in OLE32.DLL.
//
//  Constants related to CompoundFile

//

using System;

namespace MS.Internal.IO.Packaging.CompoundFile
{
    internal static class SafeNativeCompoundFileConstants
    {
        /***********************************************************************/
        // STG constants

        // Create-time constants
        //  Access  
        internal const int STGM_READ             =0x00000000;
        internal const int STGM_WRITE            =0x00000001;

        internal const int STGM_READWRITE = 0x00000002; 

        internal const int STGM_READWRITE_Bits   =0x00000003; // Not a STGM enumeration, used to strip out all STGM bits not relating to read/write access
        //  Sharing 
        internal const int STGM_SHARE_DENY_NONE  =0x00000040; 
        internal const int STGM_SHARE_DENY_READ  =0x00000030; 
        internal const int STGM_SHARE_DENY_WRITE =0x00000020; 
        internal const int STGM_SHARE_EXCLUSIVE  =0x00000010; 
        internal const int STGM_PRIORITY         =0x00040000; // Not Currently Supported
        //  Creation 
        internal const int STGM_CREATE           =0x00001000; 
        internal const int STGM_CONVERT          =0x00020000; 
        internal const int STGM_FAILIFTHERE      =0x00000000; 
        //  Transactioning 
        internal const int STGM_DIRECT           =0x00000000; // Not Currently Supported 
        internal const int STGM_TRANSACTED       =0x00010000; // Not Currently Supported 
        //  Transactioning Performance 
        internal const int STGM_NOSCRATCH        =0x00100000; // Not Currently Supported
        internal const int STGM_NOSNAPSHOT       =0x00200000; // Not Currently Supported
        //  Direct SWMR and Simple 
        internal const int STGM_SIMPLE           =0x08000000; // Not Currently Supported
        internal const int STGM_DIRECT_SWMR      =0x00400000; // Not Currently Supported
        //  Delete On Release 
        internal const int STGM_DELETEONRELEASE  =0x04000000; // Not Currently Supported

        // Seek constants
        internal const int STREAM_SEEK_SET    = 0;
        internal const int STREAM_SEEK_CUR    = 1; 
        internal const int STREAM_SEEK_END    = 2;

        // ::Stat flag
        //internal const int STATFLAG_DEFAULT   = 0;  // this constant is not used anywhere in code, but is a valid value of a StatFlag
        internal const int STATFLAG_NONAME    = 1;
        internal const int STATFLAG_NOOPEN    = 2;

        // STATSTG type values
        internal const int STGTY_STORAGE      = 1;
        internal const int STGTY_STREAM       = 2; 
        internal const int STGTY_LOCKBYTES    = 3; 
        internal const int STGTY_PROPERTY     = 4;

        // PROPSETFLAG enumeration.
        internal const uint PROPSETFLAG_ANSI  = 2;

        // Errors that we care about
        internal const int S_OK                    = 0;
        internal const int S_FALSE                 = 1;
        internal const int STG_E_FILENOTFOUND      = -2147287038; //0x80030002;
        internal const int STG_E_ACCESSDENIED      = -2147287035; //0x80030005;
        internal const int STG_E_FILEALREADYEXISTS = -2147286960; //0x80030050;
        internal const int STG_E_INVALIDNAME       = -2147286788; //0x800300FC;
        internal const int STG_E_INVALIDFLAG       = -2147286785; //0x800300FF;
    }
}

