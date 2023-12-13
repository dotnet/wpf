// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// compressstub.cpp : Implementation of compress.lib (and hopefully in 
//                    the future shared.lib stubs)
//
// NOTE: Currently I could not directly export functions declared in a .lib
//   file we link in so stubs have been created to which reference the .lib
//   routines we want to use and thing then get resolved properly and we can
//   then export these stubs (with a rename trick in the .def file).
//   If we can figure out how to directly expose .lib functions then these
//   stubs can be removed.

#include "stdafx.h"
#include <compress.h>

// Stubs to allow us to resolve to the compress.lib routines we want to expose from our
// DLL.

extern "C" ISF_RESULT IsfCompressPropertyData_stub(
            IN     const BYTE * pbInput,    // Input data
            IN     ULONG        cbInput,    // Number of bytes in buffer
            IN OUT BYTE       * pnAlgoByte, // in: desired alg. identifier byte, out: really the best
            IN OUT ULONG      * pcbOutput,  // in: cb of the buffer; out: cb needed to compress
            OUT    BYTE       * pbOutput    // OUT, output buffer
        )
{
    return IsfCompressPropertyData(pbInput,cbInput,pnAlgoByte,pcbOutput,pbOutput);
}

extern "C" ISF_RESULT IsfDecompressPropertyData_stub(
            IN     const BYTE * pbCompressed,   // in, compressed input bytes
            IN     ULONG        cbCompressed,   // in: size of the input bytes
            OUT    ULONG      * pcbOutput,      // in: cb in pbOutput, out: required
            OUT    BYTE       * pbOutput,       // Uncompressed data 
            OUT    BYTE       * pnAlgoByte      // Algorithm used
        )
{
    return IsfDecompressPropertyData(pbCompressed, cbCompressed, pcbOutput, pbOutput, pnAlgoByte);
}

extern "C" ISF_RESULT IsfCompressPacketData_stub(
            IN     HCOMPRESS    hCompress,  // compressor handle,
            IN     const LONG * pbInput,    // Input data, always LONG
            IN     ULONG        cInCount,   // Number of LONGs in buffer
            IN OUT BYTE       * pnAlgoByte, // in: preferred algo byte out: really the best
            IN OUT ULONG      * pcbOutput,  // in: cb of the buffer; out: cb needed to compress
            OUT    BYTE       * pbOutput    // OUT, output buffer
        )
{
    return IsfCompressPacketData(hCompress, pbInput, cInCount, pnAlgoByte, pcbOutput, pbOutput);
}

extern "C" ISF_RESULT IsfDecompressPacketData_stub(
            IN     HCOMPRESS    hCompress,      // Compressor handle
            IN     const BYTE * pbCompressed,   // Compressed input bytes
            IN OUT ULONG      * pcbCompressed, 	// in: cb of the input bytes out: cb read
            IN     ULONG        cInCount,       // Number of elements in input buffer
            OUT    LONG       * pbOutput,       // Uncompressed data 
            OUT    BYTE       * pnAlgoData      // Algorithm used
        )
{
    return IsfDecompressPacketData(hCompress, pbCompressed, pcbCompressed, cInCount, pbOutput, pnAlgoData);
}

extern "C" HCOMPRESS IsfLoadCompressor_stub(
            IN const BYTE * pbInput, 
            IN ULONG      * pcbInput
        )
{
    return IsfLoadCompressor(pbInput, pcbInput);
}


extern "C" void IsfReleaseCompressor_stub(HCOMPRESS hCompress)
{
    IsfReleaseCompressor(hCompress);
}


