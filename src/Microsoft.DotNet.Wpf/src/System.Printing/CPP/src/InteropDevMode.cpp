// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "win32inc.hpp"

#ifndef  __INTEROPNAMESPACEUSAGE_HPP__
#include <InteropNamespaceUsage.hpp>
#endif

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif



DeviceMode::
DeviceMode(
    array<Byte>^   devMode
    )
{
    data = (array<Byte>^)devMode;
}

DeviceMode::
DeviceMode(
    void* devModeUnmanaged
    ) : data(nullptr) 
{
    if (devModeUnmanaged)
    {
        DEVMODEW* devmode = reinterpret_cast<DEVMODEW*>(devModeUnmanaged);
        size = devmode->dmSize + devmode->dmDriverExtra;
        data = gcnew array<Byte>(size);
        Marshal::Copy((IntPtr)devmode, data, 0 , size);
    }
}

/*
DeviceMode::
DeviceMode(
    DEVMODEW* devModeUnmanaged
    ) : data(nullptr) 
{
    if (devModeUnmanaged)
    {
        size = devModeUnmanaged->dmSize + devModeUnmanaged->dmDriverExtra;
        data = gcnew array<Byte>(size);
        Marshal::Copy((IntPtr)devModeUnmanaged, data, 0 , size);
    }
}
*/

array<Byte>^
DeviceMode::Data::
get(
    void
    )
{
    return data;
}
