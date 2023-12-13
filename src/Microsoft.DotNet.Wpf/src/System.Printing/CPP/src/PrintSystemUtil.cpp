// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#include "win32inc.hpp"

using namespace System;
using namespace System::IO;
using namespace System::Collections;
using namespace System::Reflection;

#ifndef  __PRINTSYSTEMUTIL_HPP__
#include <PrintSystemUtil.hpp>
#endif



using namespace System::Printing;


/*-----------------------------------------------------------------------------------------------
                        InternalExceptionResourceManager Implementation
-------------------------------------------------------------------------------------------------*/
InternalExceptionResourceManager::
InternalExceptionResourceManager(
    void
    ) : ResourceManager("System.Printing",
                        Assembly::GetExecutingAssembly())
{
}
