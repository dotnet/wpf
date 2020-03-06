// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __DRIVERDATATYPES_HPP__
#define __DRIVERDATATYPES_HPP__

namespace System
{
namespace Printing
{
    [FlagsAttribute]
    public enum class DriverInstallationCommand : Int32
    {
        StrictUpgrade       = 0x00000001,
        StrictDowngrade     = 0x00000002,
        CopyAllFiles        = 0x00000004,
        CopyNewFiles        = 0x00000008,
        CopyFromDirectory   = 0x00000010,		
        InstallWarnedDriver = 0x00008000
    };
}
}
#endif