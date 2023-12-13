// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#include "win32inc.hpp"

using namespace System;
using namespace System::IO;
using namespace System::Collections;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;
using namespace System::Xml;
using namespace System::Xml::XPath;
using namespace System::Collections::Specialized;

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#ifndef  __PRITNSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif



using namespace System::Printing;

PrintFilter::
PrintFilter(
    String^  filterName
    )
{
}

void
PrintFilter::
InternalDispose(
    bool    disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                PrintSystemObject::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}
