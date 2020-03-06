// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTSYSTEMOBJECTDATATYPES_HPP__
#define __PRINTEYSTEMOBJECTDATATYPES_HPP__

namespace System
{
namespace Printing
{
    __value public enum PrintSystemObjectCreationType
    {
        CreateInstance            = 0x00000000,
        RetrieveInstance          = 0x00000001,
        CreateAndRetreiveInstance = 0x00000002
    };
}
}

#endif
