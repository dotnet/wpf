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

#ifndef  __OBJECTSATTRIBUTESVALUESFACTORY_HPP__
#include <ObjectsAttributesValuesFactory.hpp>
#endif

#ifndef  __PRINTSYSTEMATTRIBUTEVALUEFACTORY_HPP__
#include <PrintSystemAttributeValueFactory.hpp>
#endif

#ifndef  __PRINTSYSTEMOBJECTFACTORY_HPP__
#include <PrintSystemObjectFactory.hpp>
#endif



using namespace System::Printing::Activation;

PrintSystemObjectFactory::
PrintSystemObjectFactory(
    void
    ):
instantiationDelegatesTable(nullptr),
optimizedInstantiationDelegatesTable(nullptr)
{
    instantiationDelegatesTable           = gcnew Hashtable();
    optimizedInstantiationDelegatesTable  = gcnew Hashtable();
}

PrintSystemObjectFactory::
~PrintSystemObjectFactory(
    void
    )
{
    InternalDispose(true);
}

PrintSystemObjectFactory::
!PrintSystemObjectFactory(
    void
    )
{
    InternalDispose(false);
}

void
PrintSystemObjectFactory::
RegisterInstantiationDelegates(
    Type^                            type,
    PrintSystemObject::Instantiate^  instantiationDelegate
    )
{
    try
    {
        instantiationDelegatesTable->Add(type->FullName,
                                         instantiationDelegate);
    }
    catch(ArgumentException^)
    {
        //
        // Nothing to do, this means that the item has been already
        // added to the hashtable
        //
    }

}

void
PrintSystemObjectFactory::
RegisterOptimizedInstantiationDelegates(
    Type^                                     type,
    PrintSystemObject::InstantiateOptimized^  optimizedInstantiationDelegate
    )
{
    try
    {
        optimizedInstantiationDelegatesTable->Add(type->FullName,
                                                  optimizedInstantiationDelegate);
    }
    catch(ArgumentException^)
    {
        //
        // Nothing to do, this means that the item has been already
        // added to the hashtable
        //
    }

}


PrintSystemObject^
PrintSystemObjectFactory::
Instantiate(
    Type^           objType,
    array<String^>^ propertiesFilter
    )
{
    PrintSystemObject::Instantiate^ instantiationDelegate =
    (PrintSystemObject::Instantiate^)instantiationDelegatesTable[objType->FullName];

    return instantiationDelegate->Invoke(propertiesFilter);
}


PrintSystemObject^
PrintSystemObjectFactory::
InstantiateOptimized(
    Type^           objType,
    Object^         object,
    array<String^>^ propertiesFilter
    )
{
    PrintSystemObject::InstantiateOptimized^ optimizedInstantiationDelegate =
    (PrintSystemObject::InstantiateOptimized^)optimizedInstantiationDelegatesTable[objType->FullName];

    return optimizedInstantiationDelegate->Invoke(object,propertiesFilter);
}


PrintSystemObjectFactory^
PrintSystemObjectFactory::Value::
get(
    void
    )
{
    if(PrintSystemObjectFactory::value == nullptr)
    {
        System::Threading::Monitor::Enter(SyncRoot);
        {
            __try
            {
                if(PrintSystemObjectFactory::value == nullptr)
                {
                    PrintSystemObjectFactory::value = gcnew PrintSystemObjectFactory();

                    PrintSystemObjectFactory::value->RegisterInstantiationDelegates(PrintQueue::typeid,
                                                                                    gcnew PrintSystemObject::Instantiate(&PrintQueue::Instantiate));

                    PrintSystemObjectFactory::value->RegisterOptimizedInstantiationDelegates(PrintQueue::typeid,
                                                                                             gcnew PrintSystemObject::InstantiateOptimized(&PrintQueue::InstantiateOptimized));

                    PrintSystemObjectFactory::value->RegisterOptimizedInstantiationDelegates(PrintSystemJobInfo::typeid,
                                                                                             gcnew PrintSystemObject::InstantiateOptimized(&PrintSystemJobInfo::Instantiate));
                }
            }
            __finally
            {
                System::Threading::Monitor::Exit(SyncRoot);
            }
        }
    }

    return const_cast<PrintSystemObjectFactory^>(PrintSystemObjectFactory::value);
}


void
PrintSystemObjectFactory::
InternalDispose(
    bool disposing
    )
{
    if(!this->disposed)
    {
        System::Threading::Monitor::Enter(SyncRoot);
        {
            __try
            {
                if(!this->disposed)
                {
                    if(disposing)
                    {
                        disposed = true;
                        instantiationDelegatesTable = nullptr;
                    }
                }
            }
            __finally
            {
                System::Threading::Monitor::Exit(SyncRoot);
            }
        }
    }
}

Object^
PrintSystemObjectFactory::SyncRoot::
get(
    void
    )
{
    return const_cast<Object^>(syncRoot);
}

