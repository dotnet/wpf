// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTSYSTEMSECURITY_HPP__
#define __PRINTSYSTEMSECURITY_HPP__

namespace System
{
namespace Printing
{

    public enum class PrintSystemDesiredAccess
    {
        None                  = 0x00000000,
        AdministrateServer    = 0x000f0001,
        EnumerateServer       = 0x00020002,
        UsePrinter            = 0x00020008,
        AdministratePrinter   = 0x000f000c
    };
}
}

#endif

