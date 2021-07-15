// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
        
    Abstract:

        This file contains the definitions of the managed classes corresponding 
        to DOC_INFO_X Win32 structures. DocInfoX class has the same
        memory layout as DOC_INFO_X structure.
--*/

#ifndef __INTEROPDOCINFO_HPP__
#define __INTEROPDOCINFO_HPP__

namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{

    [StructLayout(LayoutKind::Sequential, CharSet=CharSet::Unicode)]
    private ref class  DocInfoThree
    { 
        public:

        DocInfoThree(
            String^     name,
            String^     outputFile,
            String^     dataType,
            Int32       flags
            );
        
        static String^  defaultDataType = "RAW";

        public:

        [MarshalAs(UnmanagedType::LPWStr)] 
        String^             docName;

        [MarshalAs(UnmanagedType::LPWStr)] 
        String^             docOutputFile;

        [MarshalAs(UnmanagedType::LPWStr)] 
        String^             docDataType;

        [MarshalAs(UnmanagedType::U4)]
        Int32               docFlags;
    };

}
}
}

#endif // __INTEROPDOCINFO_HPP__
