// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __INTEROPDEVMODE_HPP__
#define __INTEROPDEVMODE_HPP__
/*++
    Abstract:

        The file contains the definition for the managed classe that 
        wraps a DEVMODE Win32 structure and expose it as a byte array
        to the managed code.
--*/

#pragma once
namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
    using namespace System::Security;

    ref class SafeMemoryHandle;

    [StructLayout(LayoutKind::Sequential, CharSet=CharSet::Auto)]
    private ref class DeviceMode sealed
    {
        public:

        DeviceMode(
		    array<Byte>^    devMode
		    );

        //
        // SECURITY: Copies an unmanaged DEVMODEW blob into a managed byte[].
        // The dmSize/dmDriverExtra fields inside the blob are attacker-controlled
        // when the blob originates from PRINTER_INFO_*W::pDevMode / JOB_INFO_2W::pDevMode
        // returned by a remote (possibly hostile) print server. Callers MUST pass
        // maxBytes = number of bytes that can be safely read starting at devModeUnmanaged
        // (typically computed via ComputeBytesAvailable below). When validation fails,
        // the resulting Data property is null, matching the existing "no pDevMode" path.
        //
        DeviceMode(
		    void*       devModeUnmanaged,
		    Int32       maxBytes
		    );

        //
        // Returns the number of bytes that may be safely read starting at subPtr,
        // bounded by the end of buffer. Returns 0 if buffer is invalid, has unknown
        // size (Wrap()-ed handle), or subPtr lies outside [bufferStart, bufferEnd).
        // Caller MUST treat 0 as "do not copy".
        //
        static
        Int32
        ComputeBytesAvailable(
            SafeMemoryHandle^   buffer,
            void*               subPtr
            );

        property
	    array<Byte>^
	    Data
        {
            array<Byte>^ get();
        }

        private:

	    array<Byte>^    data; 
	    UInt32	size;
    };    

}
}
}
#endif
