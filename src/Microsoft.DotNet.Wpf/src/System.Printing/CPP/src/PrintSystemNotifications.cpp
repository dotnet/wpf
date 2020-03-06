// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#include "win32inc.hpp"

using namespace System;
using namespace System::IO;
using namespace System::Collections;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;
using namespace System::Collections::Specialized;

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif

#ifndef  __PRINTSYSTEMPATHRESOLVER_HPP__
#include <PrintSystemPathResolver.hpp>
#endif

using namespace System::Printing;

#ifndef  __PRINTSYSTEMPATHRESOLVER_HPP__
#include <PrintSystemPathResolver.hpp>
#endif


PrintSystemObjectPropertyChangedEventArgs::
PrintSystemObjectPropertyChangedEventArgs(
    String^ eventName
    ) : propertyName(eventName)
{
}

PrintSystemObjectPropertyChangedEventArgs::
~PrintSystemObjectPropertyChangedEventArgs(
    void
    )
{
}

String^
PrintSystemObjectPropertyChangedEventArgs::PropertyName::
get(
    void
    )
{
    return propertyName;
}

PrintSystemObjectPropertiesChangedEventArgs::
PrintSystemObjectPropertiesChangedEventArgs(
    StringCollection^   events
    ) : propertiesNames(events)
{
}

PrintSystemObjectPropertiesChangedEventArgs::
~PrintSystemObjectPropertiesChangedEventArgs(
    void
    )
{
}

StringCollection^
PrintSystemObjectPropertiesChangedEventArgs::PropertiesNames::
get(
    void
    )
 {
     return propertiesNames;
 }
