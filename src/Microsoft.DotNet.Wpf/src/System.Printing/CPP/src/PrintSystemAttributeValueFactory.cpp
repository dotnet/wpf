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
using namespace System::Xml;
using namespace System::Xml::XPath;

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif

using namespace System::Printing;

#ifndef  __PRINTSYSTEMATTRIBUTEVALUEFACTORY_HPP__
#include <PrintSystemAttributeValueFactory.hpp>
#endif



using namespace System::Printing::Activation;

PrintPropertyFactory::
PrintPropertyFactory(
    void
    ):
valueDelegatesTable(nullptr),
noValueDelegatesTable(nullptr),
valueLinkedDelegatesTable(nullptr),
noValueLinkedDelegatesTable(nullptr)
{
    if(!((valueDelegatesTable           = gcnew Hashtable) &&
         (noValueDelegatesTable         = gcnew Hashtable) &&
         (valueLinkedDelegatesTable     = gcnew Hashtable) &&
         (noValueLinkedDelegatesTable   = gcnew Hashtable)))
    {
    }
}


PrintPropertyFactory::
~PrintPropertyFactory(
    void
    )
{
    InternalDispose(true);
}

PrintPropertyFactory::
!PrintPropertyFactory(
    void
    )
{
    InternalDispose(false);
}

void
PrintPropertyFactory::
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

PrintPropertyFactory^
PrintPropertyFactory::Value::
get(
    void
    )
{
    if(PrintPropertyFactory::value == nullptr)
    {
        System::Threading::Monitor::Enter(SyncRoot);
        {
            __try
            {
                if(PrintPropertyFactory::value == nullptr)
                {
                    PrintPropertyFactory::value = gcnew PrintPropertyFactory();
                }
            }
            __finally
            {
                System::Threading::Monitor::Exit(SyncRoot);
            }
        }
    }

    return const_cast<PrintPropertyFactory^>(PrintPropertyFactory::value);
}

Object^
PrintPropertyFactory::SyncRoot::
get(
    void
    )
{
    return const_cast<Object^>(syncRoot);
}

void
PrintPropertyFactory::
RegisterValueCreationDelegate(
    Type^                            type,
    PrintProperty::CreateWithValue^  delegate
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
PrintPropertyFactory::
RegisterNoValueCreationDelegate(
    Type^                              type,
    PrintProperty::CreateWithNoValue^  delegate
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
PrintPropertyFactory::
RegisterValueLinkedCreationDelegate(
    Type^                                  type,
    PrintProperty::CreateWithValueLinked^  delegate
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

void
PrintPropertyFactory::
RegisterNoValueLinkedCreationDelegate(
    Type^                                    type,
    PrintProperty::CreateWithNoValueLinked^  delegate
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
PrintPropertyFactory::
UnRegisterValueCreationDelegate(
    Type^   type
    )
{
    valueDelegatesTable->Remove(type->FullName);
}

void
PrintPropertyFactory::
UnRegisterNoValueCreationDelegate(
    Type^   type
    )
{
    noValueDelegatesTable->Remove(type->FullName);
}

void
PrintPropertyFactory::
UnRegisterValueLinkedCreationDelegate(
    Type^   type
    )
{
    valueLinkedDelegatesTable->Remove(type->FullName);
}

void
PrintPropertyFactory::
UnRegisterNoValueLinkedCreationDelegate(
    Type^   type
    )
{
    noValueLinkedDelegatesTable->Remove(type->FullName);
}

PrintProperty^
PrintPropertyFactory::
Create(
    Type^   type,
    String^ attribName
    )
{
    PrintProperty::CreateWithNoValue^ attributeValueDelegate =
    (PrintProperty::CreateWithNoValue^)noValueDelegatesTable[type->FullName];

    return attributeValueDelegate->Invoke(attribName);
}

PrintProperty^
PrintPropertyFactory::
Create(
    Type^   type,
    String^ attribName,
    Object^ attribValue
    )
{
    PrintProperty::CreateWithValue^ attributeValueDelegate =
    (PrintProperty::CreateWithValue^)valueDelegatesTable[type->FullName];

    return attributeValueDelegate->Invoke(attribName,attribValue);
}

PrintProperty^
PrintPropertyFactory::
Create(
    Type^                   type,
    String^                 attribName,
    MulticastDelegate^      delegate
    )
{
    PrintProperty::CreateWithNoValueLinked^ attributeValueDelegate =
    (PrintProperty::CreateWithNoValueLinked^)noValueLinkedDelegatesTable[type->FullName];

    return attributeValueDelegate->Invoke(attribName,delegate);
}

PrintProperty^
PrintPropertyFactory::
Create(
    Type^               type,
    String^             attribName,
    Object^             attribValue,
    MulticastDelegate^  delegate
    )
{
    PrintProperty::CreateWithValueLinked^ attributeValueDelegate =
    (PrintProperty::CreateWithValueLinked^)valueLinkedDelegatesTable[type->FullName];

    return attributeValueDelegate->Invoke(attribName,attribValue,delegate);
}

IEnumerator^
PrintPropertyFactory::
GetEnumerator(
    void
    )
{
    return nullptr;
}


