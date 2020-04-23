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

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif

#ifndef  __PRINTSYSTEMPATHRESOLVER_HPP__
#include <PrintSystemPathResolver.hpp>
#endif

using namespace System::Printing;
using namespace System::Printing::IndexedProperties;


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

ObjectsAttributesValuesFactory::
ObjectsAttributesValuesFactory(
    void
    ):
valueDelegatesTable(nullptr),
noValueDelegatesTable(nullptr),
valueLinkedDelegatesTable(nullptr),
noValueLinkedDelegatesTable(nullptr)
{
    valueDelegatesTable           = gcnew Hashtable;
    noValueDelegatesTable         = gcnew Hashtable;
    valueLinkedDelegatesTable     = gcnew Hashtable;
    noValueLinkedDelegatesTable   = gcnew Hashtable;
}

ObjectsAttributesValuesFactory::
~ObjectsAttributesValuesFactory(
    void
    )
{
    InternalDispose(true);
}

ObjectsAttributesValuesFactory::
!ObjectsAttributesValuesFactory(
    void
    )
{
    InternalDispose(false);
}

void
ObjectsAttributesValuesFactory::
InternalDispose(
    bool disposing
    )
{
    if(!this->isDisposed)
    {
        System::Threading::Monitor::Enter(SyncRoot);
        {
            __try
            {
                if(!this->isDisposed)
                {
                    if(disposing)
                    {
                        isDisposed = true;
                        valueDelegatesTable         = nullptr;
                        noValueDelegatesTable       = nullptr;
                        valueLinkedDelegatesTable   = nullptr;
                        noValueLinkedDelegatesTable = nullptr;
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

ObjectsAttributesValuesFactory^
ObjectsAttributesValuesFactory::Value::
get(
    void
    )
{
    if(ObjectsAttributesValuesFactory::value == nullptr)
    {
        System::Threading::Monitor::Enter(SyncRoot);
        {
            __try
            {
                if(ObjectsAttributesValuesFactory::value == nullptr)
                {
                    ObjectsAttributesValuesFactory^ instanceValue = gcnew ObjectsAttributesValuesFactory();

                    //
                    // 1. Register all the managed classes that follow the frame work
                    //    instantiation method
                    //
                    for(Int32 numOfRegisterations = 0;
                        numOfRegisterations < registerationDelegate->Length;
                        numOfRegisterations++)
                    {
                        registerationDelegate[numOfRegisterations]->DynamicInvoke();
                    }

                    //
                    // 2.Register creation methods for different attributes with different types
                    //   for different objects
                    //
                    for(Int32 numOfObjectTypeDelegates = 0;
                        numOfObjectTypeDelegates < objectTypeDelegate->Length;
                        numOfObjectTypeDelegates++)
                    {
                        instanceValue->
                        RegisterObjectAttributeValueCreationMethod(objectTypeDelegate[numOfObjectTypeDelegates]->type,
                                                                   objectTypeDelegate[numOfObjectTypeDelegates]->delegateValue);

                        instanceValue->
                        RegisterObjectAttributeNoValueCreationMethod(objectTypeDelegate[numOfObjectTypeDelegates]->type,
                                                                     objectTypeDelegate[numOfObjectTypeDelegates]->delegateNoValue);

                        instanceValue->
                        RegisterObjectAttributeValueLinkedCreationMethod(objectTypeDelegate[numOfObjectTypeDelegates]->type,
                                                                         objectTypeDelegate[numOfObjectTypeDelegates]->delegateValueLinked);

                        instanceValue->
                        RegisterObjectAttributeNoValueLinkedCreationMethod(objectTypeDelegate[numOfObjectTypeDelegates]->type,
                                                                           objectTypeDelegate[numOfObjectTypeDelegates]->delegateNoValueLinked);
                    }

                    //
                    // 3.Register the attribute type creation methods
                    //
                    for(Int32 numOfAttributeValueTypeDelegate = 0;
                        numOfAttributeValueTypeDelegate < attributeValueTypeDelegate->Length;
                        numOfAttributeValueTypeDelegate++)
                    {
                        PrintPropertyFactory::Value->
                        RegisterValueCreationDelegate(attributeValueTypeDelegate[numOfAttributeValueTypeDelegate]->type,
                                                      attributeValueTypeDelegate[numOfAttributeValueTypeDelegate]->delegateValue);

                        PrintPropertyFactory::Value->
                        RegisterNoValueCreationDelegate(attributeValueTypeDelegate[numOfAttributeValueTypeDelegate]->type,
                                                        attributeValueTypeDelegate[numOfAttributeValueTypeDelegate]->delegateNoValue);

                        PrintPropertyFactory::Value->
                        RegisterValueLinkedCreationDelegate(attributeValueTypeDelegate[numOfAttributeValueTypeDelegate]->type,
                                                            attributeValueTypeDelegate[numOfAttributeValueTypeDelegate]->delegateValueLinked);

                        PrintPropertyFactory::Value->
                        RegisterNoValueLinkedCreationDelegate(attributeValueTypeDelegate[numOfAttributeValueTypeDelegate]->type,
                                                            attributeValueTypeDelegate[numOfAttributeValueTypeDelegate]->delegateNoValueLinked);
                    }

                    ObjectsAttributesValuesFactory::value = instanceValue;
                }
            }
            __finally
            {
                System::Threading::Monitor::Exit(SyncRoot);
            }
        }
    }

    return const_cast<ObjectsAttributesValuesFactory^>(ObjectsAttributesValuesFactory::value);
}

Object^
ObjectsAttributesValuesFactory::SyncRoot::
get(
    void
    )
{
    return const_cast<Object^>(syncRoot);
}

void
ObjectsAttributesValuesFactory::
RegisterObjectAttributeNoValueCreationMethod(
    Type^                                           type,
    PrintSystemObject::CreateWithNoValue^           delegate
    )
{
    try
    {
        noValueDelegatesTable->Add(type->FullName,
                                   delegate);
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
ObjectsAttributesValuesFactory::
RegisterObjectAttributeNoValueLinkedCreationMethod(
    Type^                                           type,
    PrintSystemObject::CreateWithNoValueLinked^     delegate
    )
{
    try
    {
        noValueLinkedDelegatesTable->Add(type->FullName,
                                         delegate);
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
ObjectsAttributesValuesFactory::
RegisterObjectAttributeValueCreationMethod(
    Type^                                           type,
    PrintSystemObject::CreateWithValue^             delegate
    )
{
    try
    {
        valueDelegatesTable->Add(type->FullName,
                                 delegate);
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
ObjectsAttributesValuesFactory::
RegisterObjectAttributeValueLinkedCreationMethod(
    Type^                                           type,
    PrintSystemObject::CreateWithValueLinked^       delegate
    )
{
    try
    {
        valueLinkedDelegatesTable->Add(type->FullName,
                                       delegate);
    }
    catch(ArgumentException^)
    {
        //
        // Nothing to do, this means that the item has been already
        // added to the hashtable
        //
    }

}

PrintProperty^
ObjectsAttributesValuesFactory::
Create(
    Type^   type,
    String^ attributeName
    )
{
    PrintSystemObject::CreateWithNoValue^ attributeValueDelegate =
    (PrintSystemObject::CreateWithNoValue^)noValueDelegatesTable[type->FullName];

    return attributeValueDelegate->Invoke(attributeName);
}

PrintProperty^
ObjectsAttributesValuesFactory::
Create(
    Type^   type,
    String^ attributeName,
    Object^ attributeValue
    )
{
   PrintSystemObject::CreateWithValue^ attributeValueDelegate =
    (PrintSystemObject::CreateWithValue^)valueDelegatesTable[type->FullName];

    return attributeValueDelegate->Invoke(attributeName,attributeValue);
}

PrintProperty^
ObjectsAttributesValuesFactory::
Create(
    Type^               type,
    String^             attributeName,
    MulticastDelegate^  delegate
    )
{
   PrintSystemObject::CreateWithNoValueLinked^ attributeValueDelegate =
    (PrintSystemObject::CreateWithNoValueLinked^)noValueLinkedDelegatesTable[type->FullName];

    return attributeValueDelegate->Invoke(attributeName,delegate);
}

PrintProperty^
ObjectsAttributesValuesFactory::
Create(
    Type^               type,
    String^             attributeName,
    Object^             attributeValue,
    MulticastDelegate^  delegate
    )
{
    PrintSystemObject::CreateWithValueLinked^ attributeValueDelegate =
    (PrintSystemObject::CreateWithValueLinked^)valueLinkedDelegatesTable[type->FullName];

    return attributeValueDelegate->Invoke(attributeName,attributeValue,delegate);
}
