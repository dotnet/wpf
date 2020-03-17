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

    [StructLayout(LayoutKind::Sequential, CharSet=CharSet::Auto)]
    private ref class DeviceMode sealed
    {
        public:

        DeviceMode(
		    array<Byte>^    devMode
		    );

        DeviceMode(
		    void*       devModeUnmanaged
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
