// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#pragma once

#include "scope.h"

template<typename T>
class ScopedArrayPolicy
{
  public:
    static T DefaultValue()
    {
        return NULL;
    }
    
    static void Close(T t)
    {
        delete [] t;
    }
};

typedef Scope<TCHAR*, ScopedArrayPolicy<TCHAR*> > ScopedString;

template<typename T>
class ScopedLocalPolicy
{
  public:
    static T DefaultValue()
    {
        return NULL;
    }

    static void Close(T t)
    {
        LocalFree(t);
    }
};

typedef Scope<TCHAR*, ScopedLocalPolicy<TCHAR*> > ScopedLocalString;

typedef Scope<PSECURITY_DESCRIPTOR, ScopedLocalPolicy<PSECURITY_DESCRIPTOR> > ScopedSecurityDescriptor;

