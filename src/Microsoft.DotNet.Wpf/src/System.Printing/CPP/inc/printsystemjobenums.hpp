// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTSYSTEMJOBENUMS_HPP__
#define __PRINTSYSTEMJOBENUMS_HPP__

namespace System
{
namespace Printing
{
    private enum class JobOperation
    {
        None           = 0,
        JobProduction  = 1,
        JobConsumption = 2
    };

}
}

#endif