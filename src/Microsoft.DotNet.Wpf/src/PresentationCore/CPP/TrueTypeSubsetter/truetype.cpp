// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is the main DLL file.

#include "precomp.hxx"

#define FSCFG_INTERNAL


#ifndef ttBoolean
#define ttBoolean int
#endif

typedef signed char int8;
typedef unsigned char uint8;
typedef short int16;
typedef unsigned short uint16;


// TrueType subsetter from TtfDelta

namespace MS { namespace Internal { namespace TtfDelta { 

// Disable subsetter include of assert.h (and other system headers in future)
// as they are broken by the __cdecl redefinition below.
#define NO_CRT_ASSERT

#pragma warning(push)
#pragma warning(disable : 4100 4127 4245 4702 4706)

#include "TtfDelta\ttfdelta.cpp"
#include "TtfDelta\ttmem.cpp"
#include "TtfDelta\ttfcntrl.cpp"
#include "TtfDelta\ttfacc.cpp"
#include "TtfDelta\ttftabl1.cpp"
#include "TtfDelta\ttftable.cpp"
#include "TtfDelta\modcmap.cpp"
#include "TtfDelta\modglyf.cpp"
#include "TtfDelta\modsbit.cpp"
#include "TtfDelta\modtable.cpp"
#include "TtfDelta\makeglst.cpp"
#include "TtfDelta\mtxcalc.cpp"
#include "TtfDelta\automap.cpp"
#include "TtfDelta\util.cpp"
#pragma warning(pop)

}}} // namespace MS::Internal::TtfDelta


#include "truetype.h"
#include "fsassert.h"

[module:System::CLSCompliant(true)];
using MS::Internal::TtfDelta::Mem_Free;
using MS::Internal::TtfDelta::Mem_Alloc;
using MS::Internal::TtfDelta::Mem_ReAlloc;
using MS::Internal::TtfDelta::CreateDeltaTTF;

namespace MS { namespace Internal {

array<System::Byte> ^ TrueTypeSubsetter::ComputeSubset(void * fontData, int fileSize, System::Uri ^ sourceUri, int directoryOffset, array<System::UInt16> ^ glyphArray)
{
    uint8 * puchDestBuffer = NULL;
    unsigned long ulDestBufferSize = 0, ulBytesWritten = 0;

    assert(glyphArray != nullptr && glyphArray->Length > 0 && glyphArray->Length <= USHRT_MAX);

    pin_ptr<const System::UInt16> pinnedGlyphArray = &glyphArray[0];
    int16 errCode = CreateDeltaTTF(
        static_cast<CONST uint8 *>(fontData),
        fileSize,
        &puchDestBuffer,
        &ulDestBufferSize,
        &ulBytesWritten,
        0, // format of the subset font to create. 0 = Subset
        0, // all languages in the Name table should be retained
        0, // Ignored for usListType = 1
        0, // Ignored for usListType = 1
        1, // usListType, 1 means the KeepCharCodeList represents raw Glyph indices from the font
        pinnedGlyphArray, // glyph indices array
        static_cast<USHORT>(glyphArray->Length), // number of glyph indices
        Mem_ReAlloc,   // call back function to reallocate temp and output buffers
        Mem_Free,      // call back function to output buffers on error
        directoryOffset,
        NULL // Reserved
        );

    array<System::Byte> ^ retArray = nullptr;

    try
    {
        if (errCode == NO_ERROR)
        {
            retArray = gcnew array<System::Byte>(ulBytesWritten);
            System::Runtime::InteropServices::Marshal::Copy((System::IntPtr)puchDestBuffer, retArray, 0, ulBytesWritten);
        }
    }
    finally
    {
        Mem_Free(puchDestBuffer);
    }

    // If subsetting would grow the font, just use the original one as it's the best we can do.
    if (errCode == ERR_WOULD_GROW)
    {
        retArray = gcnew array<System::Byte>(fileSize);
        System::Runtime::InteropServices::Marshal::Copy((System::IntPtr)fontData, retArray, 0, fileSize);
    }
    else if (errCode != NO_ERROR)
    {
        throw gcnew FileFormatException(sourceUri);
    }

    return retArray;
}

} } // MS.Internal

