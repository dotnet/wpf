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

#ifndef  __PRINTSYSTEMATTRIBUTEVALUEFACTORY_HPP__
#include <PrintSystemAttributeValueFactory.hpp>
#endif

#ifndef  __OBJECTSATTRIBUTESVALUESFACTORY_HPP__
#include <ObjectsAttributesValuesFactory.hpp>
#endif



using namespace System::Printing::Activation;

using namespace System::Printing::AsyncNotify;


PrintSystemObject::
PrintSystemObject(
    void
    ):
name(nullptr),
parent(nullptr)
{
    propertiesCollection = gcnew PrintPropertyDictionary();
    syncRoot             = gcnew Object();

    if((propertiesCollection != nullptr) && (syncRoot != nullptr))
    {
        Initialize();
    }
}

PrintSystemObject::
PrintSystemObject(
    PrintSystemObjectLoadMode   mode
    ):
name(nullptr),
parent(nullptr)
{
    switch(mode)
    {
        case PrintSystemObjectLoadMode::LoadInitialized:
        {

            if(propertiesCollection = gcnew PrintPropertyDictionary())
            {
                Initialize();
            }
            break;
        }

        case PrintSystemObjectLoadMode::LoadUninitialized:
        {
            break;
        }
    }
    syncRoot = gcnew Object();
}

PrintSystemObject::
!PrintSystemObject(
    )
{
    InternalDispose(false);
}

PrintSystemObject::
~PrintSystemObject(
    )
{
    InternalDispose(true);
}

void
PrintSystemObject::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        if (disposing)
        {
            if(parent)
            {
                delete parent;
                parent = nullptr;
            }
            propertiesCollection = nullptr;
            syncRoot             = nullptr;
        }

        this->IsDisposed     = true;
    }
}

bool
PrintSystemObject::IsDisposed::
get(
    void
    )
{
    return isDisposed;
}

void
PrintSystemObject::IsDisposed::
set(
    bool disposingStatus
    )
{
    isDisposed = disposingStatus;
}


void
PrintSystemObject::
RegisterAttributesNamesTypes(
    Hashtable^ attributeNameTypes
    )
{
    for(Int32 numOfAttributes = 0;
        numOfAttributes < PrintSystemObject::baseAttributeNames->Length;
        numOfAttributes++)
    {
        attributeNameTypes->Add(PrintSystemObject::baseAttributeNames[numOfAttributes],
                                PrintSystemObject::baseAttributeTypes[numOfAttributes]);
    }
}


void
PrintSystemObject::
Initialize(
    void
    )
{
    array<MulticastDelegate^>^ propertiesDelegates = CreatePropertiesDelegates();

    for(Int32 numOfAttributes=0;
        numOfAttributes < PrintSystemObject::baseAttributeNames->Length;
        numOfAttributes++)
    {
        PrintProperty^ printSystemAttributeValue = nullptr;
        //
        // Each type like PrintQueue or PrintServer knows the types
        // of the properties contained within its collection. This is
        // is why we delegate to those types the ability to create
        // all properties
        //
        printSystemAttributeValue =
        ObjectsAttributesValuesFactory::Value->Create(this->GetType(),
                                                      baseAttributeNames[numOfAttributes],
                                                      propertiesDelegates[numOfAttributes]);

        PropertiesCollection->Add(printSystemAttributeValue);
    }
}

array<MulticastDelegate^>^
PrintSystemObject::
CreatePropertiesDelegates(
    void
    )
{
    array<MulticastDelegate^>^ propertiesDelegates = gcnew array<MulticastDelegate^>(PrintSystemObject::baseAttributeNames->Length);

    propertiesDelegates[0]  = gcnew PrintSystemDelegates::StringValueChanged(this,&PrintSystemObject::Name::set);

    return propertiesDelegates;
}


PrintPropertyDictionary^
PrintSystemObject::PropertiesCollection::
get(
    void
    )
{
    return propertiesCollection;
}

void
PrintSystemObject::PropertiesCollection::
set(
    PrintPropertyDictionary^ collection
    )
{
    propertiesCollection = collection;
}

String^
PrintSystemObject::Name::
get(
    void
    )
{
    return name;
}

void
PrintSystemObject::Name::
set(
    String^ inName
    )
{
    name = inName;
}

PrintSystemObject^
PrintSystemObject::Parent::
get(
    void
    )
{
    return parent;
}

Object^
PrintSystemObject::SyncRoot::
get(
    void
    )
{
    return syncRoot;
}


void
PrintSystemObject::
OnPropertyChanged(
    PrintSystemObject^                          sender,
    PrintSystemObjectPropertyChangedEventArgs^  e
    )
{
}

void
PrintSystemObject::
OnPropertiesChanged(
    PrintSystemObject^                           sender,
    PrintSystemObjectPropertiesChangedEventArgs^ e
    )
{
}

__declspec(noinline)
PrintSystemObjects::
PrintSystemObjects(
    void
    )
{
}

PrintSystemObjects::
~PrintSystemObjects(
    void
    )
{
}
