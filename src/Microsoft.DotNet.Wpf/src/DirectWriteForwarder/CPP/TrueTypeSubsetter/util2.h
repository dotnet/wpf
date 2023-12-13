// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <vcclr.h>

namespace MS { namespace Internal { namespace FontCache {

ref class Util2 abstract sealed
{
public:
    static bool GetRegistryKeyLastWriteTimeUtc(System::String ^ registryKey, [System::Runtime::InteropServices::Out] System::Int64 % lastWriteTime);
};

}}}
