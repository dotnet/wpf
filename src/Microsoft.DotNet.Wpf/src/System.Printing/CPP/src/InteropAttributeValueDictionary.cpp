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
using namespace System::Threading;

using namespace System::Printing;
using namespace System::Printing::Interop;

#ifndef  __INTEROPNAMESPACEUSAGE_HPP__
#include <InteropNamespaceUsage.hpp>
#endif

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif


#ifndef  __GENERICTHUNKINGINC_HPP__
#include <GenericThunkingInc.hpp>
#endif

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif

#ifndef  __PRINTSYSTEMATTRIBUTEVALUEFACTORY_HPP__
#include <PrintSystemAttributeValueFactory.hpp>
#endif

#ifndef  __PRINTSYSTEMOBJECTFACTORY_HPP__
#include <PrintSystemObjectFactory.hpp>
#endif

#ifndef  __INTEROPATTRIBUTEVALUEDICTIONARY_HPP__
#include <InteropAttributeValueDictionary.hpp>
#endif



using namespace System::Printing::Activation;

using namespace MS::Internal::PrintWin32Thunk;


AttributeValueInteropHandler::
AttributeValueInteropHandler(
    void
    )
{
}

void
AttributeValueInteropHandler::
RegisterStaticMaps(
    void
    )
{
    Array^ interopPropertiesTypes = Enum::GetValues(PrintPropertyTypeInterop::typeid);

    for(Int32 numOfUnmanagedProperties = 0;
        numOfUnmanagedProperties < interopPropertiesTypes->Length;
        numOfUnmanagedProperties++)
    {
        unmanagedToManagedTypeMap->Add(interopPropertiesTypes->GetValue(numOfUnmanagedProperties),
                                       PrintSystemAttributePrimitiveTypes[numOfUnmanagedProperties]);
        managedToUnmanagedTypeMap->Add(PrintSystemAttributePrimitiveTypes[numOfUnmanagedProperties],
                                       interopPropertiesTypes->GetValue(numOfUnmanagedProperties));
        unmanagedPropertyToObjectDelegateMap->Add(PrintSystemAttributePrimitiveTypes[numOfUnmanagedProperties],
                                                  GetValueFromUnmanagedValueDelegateTable[numOfUnmanagedProperties]);
        attributeValueToUnmanagedTypeMap->Add(PrintSystemAttributeValueTypes[numOfUnmanagedProperties],
                                              interopPropertiesTypes->GetValue(numOfUnmanagedProperties));
    }

}

Object^
AttributeValueInteropHandler::SyncRoot::
get(
    void
    )
{
    return (Object^)syncRoot;
}

AttributeValueInteropHandler^
AttributeValueInteropHandler::Value::
get(
    void
    )
{
    if(AttributeValueInteropHandler::value == nullptr)
    {
        System::Threading::Monitor::Enter(SyncRoot);
        {
            __try
            {
                if(AttributeValueInteropHandler::value == nullptr)
                {
                    AttributeValueInteropHandler::value = gcnew AttributeValueInteropHandler();
                }
            }
            __finally
            {
                System::Threading::Monitor::Exit(SyncRoot);
            }
        }
    }

    return const_cast<AttributeValueInteropHandler^>(AttributeValueInteropHandler::value);
}

IntPtr
AttributeValueInteropHandler::
AllocateUnmanagedPrintPropertiesCollection(
    PrintPropertyDictionary^    managedCollection
    )
{
    return AllocateUnmanagedPrintPropertiesCollection(managedCollection->Count);
}

IntPtr
AttributeValueInteropHandler::
AllocateUnmanagedPrintPropertiesCollection(
    Int32     propertyCount  
    )
{
    PrintPropertiesCollection *unmanagedCollection = NULL;

    if(propertyCount < 0)
    {
        return IntPtr::Zero;
    }

    UIntPtr cbPrintNamedProp = UIntPtr(sizeof(PrintNamedProperty) * propertyCount);
    IntPtr hPrintNamedProp = Marshal::AllocHGlobal(IntPtr(cbPrintNamedProp.ToPointer()));

    if(IntPtr::Zero != hPrintNamedProp)
    {
        UIntPtr cbPrintPropertiesCollection = UIntPtr(sizeof(PrintPropertiesCollection));
        IntPtr hPrintPropCollection = Marshal::AllocHGlobal(IntPtr(cbPrintPropertiesCollection.ToPointer()));
        if(IntPtr::Zero != hPrintPropCollection)
        {
            unmanagedCollection = reinterpret_cast<PrintPropertiesCollection*>(hPrintPropCollection.ToPointer());
            unmanagedCollection->numberOfProperties   = propertyCount;
            unmanagedCollection->propertiesCollection = reinterpret_cast<PrintNamedProperty*>(hPrintNamedProp.ToPointer());
        }
        else
        {
            Marshal::FreeHGlobal(hPrintNamedProp);
            hPrintNamedProp = IntPtr::Zero;
        }
    }

    return (IntPtr)(unmanagedCollection);
}

void
AttributeValueInteropHandler::
FreeUnmanagedPrintPropertiesCollection(
    IntPtr         win32UnmanagedCollection
    )
{
    PrintPropertiesCollection* unmanagedCollection = (PrintPropertiesCollection*)(win32UnmanagedCollection.ToPointer());

    if (unmanagedCollection)
    {
        for (ULONG index = 0; index < unmanagedCollection->numberOfProperties; index++)
        {
            Marshal::FreeHGlobal(IntPtr(unmanagedCollection->propertiesCollection[index].propertyName));

            Int32   unmanagedPropertyType = Int32(unmanagedCollection->propertiesCollection[index].propertyValue.ePropertyType);

            PrintPropertyTypeInterop   propertyType = *((PrintPropertyTypeInterop^)Enum::Parse(PrintPropertyTypeInterop::typeid,
                                                                                               unmanagedPropertyType.ToString(System::Globalization::CultureInfo::CurrentCulture)));


            switch (propertyType)
            {
                case PrintPropertyTypeInterop::StringPrintType:
                {
                    if (unmanagedCollection->propertiesCollection[index].propertyValue.value.propertyString != NULL)
                    {
                        Marshal::FreeHGlobal(IntPtr(unmanagedCollection->propertiesCollection[index].propertyValue.value.propertyString));
                    }
                    break;
                }
                case PrintPropertyTypeInterop::ByteBufferPrintType:
                {
                    if (unmanagedCollection->propertiesCollection[index].propertyValue.value.propertyBlob.pBuf)
                    {
                        Marshal::FreeHGlobal(IntPtr(unmanagedCollection->propertiesCollection[index].propertyValue.value.propertyBlob.pBuf));
                    }
                    break;
                }
                case PrintPropertyTypeInterop::DataTimePrintType:
                case PrintPropertyTypeInterop::Int32PrintType:
                default:
                {
                    break;
                }
            }
        }

        Marshal::FreeHGlobal(IntPtr(unmanagedCollection->propertiesCollection));
        Marshal::FreeHGlobal(IntPtr(unmanagedCollection));
    }
}

void
AttributeValueInteropHandler::
AssignUnmanagedPrintPropertyValue(
    PrintNamedProperty*             unmanagedPropertyValue,
    PrintProperty^                  managedAttributeValue
    )
{
    IntPtr propertyNamePtr = (IntPtr)nullptr;

    try
    {
        propertyNamePtr = Marshal::StringToHGlobalUni(managedAttributeValue->Name);

        unmanagedPropertyValue->propertyName                = reinterpret_cast<WCHAR*>(propertyNamePtr.ToPointer());

        PrintPropertyTypeInterop interopPropertyType       =  *((PrintPropertyTypeInterop^)attributeValueToUnmanagedTypeMap[managedAttributeValue->GetType()]);

        unmanagedPropertyValue->propertyValue.ePropertyType = static_cast<EPrintPropertyType>(interopPropertyType);

        switch (interopPropertyType)
        {
            case PrintPropertyTypeInterop::StringPrintType:
            {
                String^ managedValue = (String^)managedAttributeValue->Value;
                IntPtr managedValuePtr = Marshal::StringToHGlobalUni(managedValue);
                unmanagedPropertyValue->propertyValue.value.propertyString = reinterpret_cast<WCHAR*>(managedValuePtr.ToPointer());

                break;
            }
            case PrintPropertyTypeInterop::Int32PrintType:
            {
                Int32 managedValue = *(Int32 ^)managedAttributeValue->Value;
                unmanagedPropertyValue->propertyValue.value.propertyInt32 = managedValue;

                break;
            }
            case PrintPropertyTypeInterop::ByteBufferPrintType:
            {
                Stream^         managedValue    = (Stream ^)managedAttributeValue->Value;
                Int32           size            = static_cast<Int32>(managedValue->Length);
                Int64           savePosition    = managedValue->Position;
                array<Byte>^    helperByteArray = gcnew array<Byte>(size);

                managedValue->Position         = 0;
                managedValue->Read(helperByteArray, 0, size);
                managedValue->Position = savePosition;

                IntPtr   unmanagedBuffer = Marshal::AllocHGlobal(size);
                Marshal::Copy(helperByteArray, 0, unmanagedBuffer, size);

                unmanagedPropertyValue->propertyValue.value.propertyBlob.cbBuf = size;
                unmanagedPropertyValue->propertyValue.value.propertyBlob.pBuf  = unmanagedBuffer.ToPointer();

                break;
            }
            case PrintPropertyTypeInterop::DataTimePrintType:
            default:
            {
                break;
            }

        }
    }
    catch (SystemException^ exception)
    {
        if (propertyNamePtr != IntPtr::Zero)
        {
            Marshal::FreeHGlobal(propertyNamePtr);
        }
        throw exception;
    }
}

void
AttributeValueInteropHandler::
SetValue(
    IntPtr      unmanagedCollectionPtr,
    String^     propertyName,
    UInt32      index,
    Object^     value
    )
{
    IntPtr propertyNamePtr = (IntPtr)nullptr;
    IntPtr managedValuePtr = (IntPtr)nullptr;

    try
    {
        if (propertyName != nullptr && value != nullptr)
        {
            PrintPropertiesCollection*  unmanagedCollection = (PrintPropertiesCollection*)(unmanagedCollectionPtr.ToPointer());

            PrintNamedProperty* unmanagedPropertyValue = &unmanagedCollection->propertiesCollection[index];
            
            propertyNamePtr = Marshal::StringToHGlobalUni(propertyName);

            unmanagedPropertyValue->propertyName = reinterpret_cast<WCHAR*>(propertyNamePtr.ToPointer());

            PrintPropertyTypeInterop interopPropertyType =  *((PrintPropertyTypeInterop^)managedToUnmanagedTypeMap[value->GetType()]);

            unmanagedPropertyValue->propertyValue.ePropertyType = static_cast<EPrintPropertyType>(interopPropertyType);

            switch (interopPropertyType)
            {
                case PrintPropertyTypeInterop::StringPrintType:
                {
                    managedValuePtr = Marshal::StringToHGlobalUni((String^)value);
                    unmanagedPropertyValue->propertyValue.value.propertyString = reinterpret_cast<WCHAR*>(managedValuePtr.ToPointer());
                    break;
                }
                case PrintPropertyTypeInterop::Int32PrintType:
                {
                    Int32 managedValue = *(Int32 ^)value;
                    unmanagedPropertyValue->propertyValue.value.propertyInt32 = managedValue;

                    break;
                }
                case PrintPropertyTypeInterop::ByteBufferPrintType:
                {
                    Stream^         managedValue    = (Stream ^)value;
                    Int32           size            = static_cast<Int32>(managedValue->Length);
                    Int64           savePosition    = managedValue->Position;
                    array<Byte>^    helperByteArray = gcnew array<Byte>(size);

                    managedValue->Position         = 0;
                    managedValue->Read(helperByteArray, 0, size);
                    managedValue->Position = savePosition;

                    managedValuePtr = Marshal::AllocHGlobal(size);
                    Marshal::Copy(helperByteArray, 0, managedValuePtr, size);

                    unmanagedPropertyValue->propertyValue.value.propertyBlob.cbBuf = size;
                    unmanagedPropertyValue->propertyValue.value.propertyBlob.pBuf  = managedValuePtr.ToPointer();
                    break;
                }
                case PrintPropertyTypeInterop::DataTimePrintType:
                default:
                {
                    break;
                }

            }
        }
    }
    catch (SystemException^ exception)
    {
        if (propertyNamePtr != IntPtr::Zero)
        {
            Marshal::FreeHGlobal(propertyNamePtr);
        }

        if (managedValuePtr != IntPtr::Zero)
        {
            Marshal::FreeHGlobal(managedValuePtr);
        }
        throw exception;
    }
}


void
AttributeValueInteropHandler::
SetValue(
    IntPtr          unmanagedCollectionPtr,
    String^         propertyName,
    UInt32          index,
    System::Type^   type
    )
{
    IntPtr propertyNamePtr = (IntPtr)nullptr;
    IntPtr managedValuePtr = (IntPtr)nullptr;

    try
    {
        if (propertyName != nullptr && type != nullptr)
        {
            PrintPropertiesCollection*  unmanagedCollection = (PrintPropertiesCollection*)(unmanagedCollectionPtr.ToPointer());

            PrintNamedProperty* unmanagedPropertyValue = &unmanagedCollection->propertiesCollection[index];
            
            propertyNamePtr = Marshal::StringToHGlobalUni(propertyName);

            unmanagedPropertyValue->propertyName = reinterpret_cast<WCHAR*>(propertyNamePtr.ToPointer());

            PrintPropertyTypeInterop interopPropertyType =  *((PrintPropertyTypeInterop^)managedToUnmanagedTypeMap[type]);

            unmanagedPropertyValue->propertyValue.ePropertyType = static_cast<EPrintPropertyType>(interopPropertyType);

            switch (interopPropertyType)
            {
                case PrintPropertyTypeInterop::StringPrintType:
                {
                    unmanagedPropertyValue->propertyValue.value.propertyString = NULL;
                    break;
                }
                case PrintPropertyTypeInterop::Int32PrintType:
                {
                    unmanagedPropertyValue->propertyValue.value.propertyInt32 = 0;
                    break;
                }
                case PrintPropertyTypeInterop::ByteBufferPrintType:
                {
                    unmanagedPropertyValue->propertyValue.value.propertyBlob.cbBuf = 0;
                    unmanagedPropertyValue->propertyValue.value.propertyBlob.pBuf  = NULL;
                    break;
                }
                case PrintPropertyTypeInterop::DataTimePrintType:
                default:
                {
                    break;
                }

            }
        }
    }
    catch (SystemException^ exception)
    {
        if (propertyNamePtr != IntPtr::Zero)
        {
            Marshal::FreeHGlobal(propertyNamePtr);
        }

        if (managedValuePtr != IntPtr::Zero)
        {
            Marshal::FreeHGlobal(managedValuePtr);
        }
        throw exception;
    }
}


IntPtr
AttributeValueInteropHandler::
BuildUnmanagedPrintPropertiesCollection(
    PrintPropertyDictionary^    managedCollection
    )
{
    PrintPropertiesCollection*  unmanagedCollection = NULL;

    if (managedCollection && managedCollection->Count)
    {
        unmanagedCollection = (PrintPropertiesCollection*)((AllocateUnmanagedPrintPropertiesCollection(managedCollection)).ToPointer());

        System::Collections::IEnumerator^    managedPropertiesEnumerator = managedCollection->GetEnumerator();

        for (Int32 index =0 ; managedPropertiesEnumerator->MoveNext(); index++)
        {
            PrintProperty^  attributeValue =
            (PrintProperty^)(((DictionaryEntry^)managedPropertiesEnumerator->Current)->Value);

            PrintNamedProperty* unmanagedProperty = &unmanagedCollection->propertiesCollection[index];

            AssignUnmanagedPrintPropertyValue(unmanagedProperty, attributeValue);
        }
    }

    return IntPtr(unmanagedCollection);
}
PrintPropertyDictionary^
AttributeValueInteropHandler::
BuildManagedPrintPropertiesCollection(
    IntPtr    unmanagedCollection0
    )
{
	PrintPropertiesCollection * unmanagedCollection = (PrintPropertiesCollection*) (unmanagedCollection0.ToPointer());
	PrintPropertyDictionary^    managedCollection = nullptr;

    if (unmanagedCollection && unmanagedCollection->numberOfProperties)
    {
        managedCollection = gcnew PrintPropertyDictionary;

        for (ULONG index = 0; index < unmanagedCollection->numberOfProperties; index++)
        {
            String^ attributeName         = gcnew String(unmanagedCollection->propertiesCollection[index].propertyName);
            Int32   unmanagedPropertyType = Int32(unmanagedCollection->propertiesCollection[index].propertyValue.ePropertyType);

            PrintPropertyTypeInterop   propertyType = *((PrintPropertyTypeInterop^)Enum::Parse(PrintPropertyTypeInterop::typeid,
                                                                                                 unmanagedPropertyType.ToString(System::Globalization::CultureInfo::CurrentCulture)));

            Type^ type = (Type^)unmanagedToManagedTypeMap[propertyType];

            GetValueFromUnmanagedValue^ getValueDelegate = (GetValueFromUnmanagedValue^)unmanagedPropertyToObjectDelegateMap[propertyType];

            Object ^ attributeValue = getValueDelegate->Invoke(unmanagedCollection->propertiesCollection[index].propertyValue);

            PrintProperty^ attributeValueObject = PrintPropertyFactory::Value->Create(type, attributeName, attributeValue);

            managedCollection->Add(attributeValueObject);
        }
    }
    
    return managedCollection;
}


Object^
AttributeValueInteropHandler::
GetValue(
    IntPtr                      unmanagedCollectionPtr,
    String^                     propertyName,
    Type^                       propertyType,
    Boolean%                    isPropertyPresent
    )
{
    Object^                     value               = nullptr;
    PrintPropertiesCollection*  unmanagedCollection = (PrintPropertiesCollection*)(unmanagedCollectionPtr.ToPointer());
	
    isPropertyPresent  = false;

    for (ULONG index = 0; index < unmanagedCollection->numberOfProperties; index++)
    {
        Int32   unmanagedEnumPropertyType = Int32(unmanagedCollection->propertiesCollection[index].propertyValue.ePropertyType);

        PrintPropertyTypeInterop   unmanagedPropertyType = *((PrintPropertyTypeInterop^)Enum::Parse(PrintPropertyTypeInterop::typeid,
                                                                                                      unmanagedEnumPropertyType.ToString(System::Globalization::CultureInfo::CurrentCulture)));

        Type^ type = (Type^)unmanagedToManagedTypeMap[unmanagedPropertyType];

        if (propertyType->Equals(type))
        {
            isPropertyPresent = true;

            GetValueFromUnmanagedValue^ getValueDelegate = (GetValueFromUnmanagedValue^)unmanagedPropertyToObjectDelegateMap[propertyType];

            value = getValueDelegate->Invoke(unmanagedCollection->propertiesCollection[index].propertyValue);
            break;
        }
    }
    return value;
}

void
AttributeValueInteropHandler::
CopyManagedPrintPropertiesCollection(
    IntPtr					unmanagedCollection0,
    PrintSystemObject^      printSystemObject
    )
{
    PrintPropertiesCollection * unmanagedCollection = (PrintPropertiesCollection*) (unmanagedCollection0.ToPointer());

    if (unmanagedCollection && unmanagedCollection->numberOfProperties)
    {
        for (ULONG index = 0; index < unmanagedCollection->numberOfProperties; index++)
        {
            String^ attributeName         = gcnew String(unmanagedCollection->propertiesCollection[index].propertyName);
            Int32   unmanagedPropertyType = Int32(unmanagedCollection->propertiesCollection[index].propertyValue.ePropertyType);

            PrintPropertyTypeInterop   propertyType = *((PrintPropertyTypeInterop^)Enum::Parse(PrintPropertyTypeInterop::typeid,
                                                                                                 unmanagedPropertyType.ToString(System::Globalization::CultureInfo::CurrentCulture)));

            GetValueFromUnmanagedValue^ getValueDelegate = (GetValueFromUnmanagedValue^)unmanagedPropertyToObjectDelegateMap[propertyType];

            //
            // We might come here with a collection of properties unsupported by the managed object. Skip them.
            //
            PrintPropertyDictionary^ propertiesCollection = printSystemObject->get_InternalPropertiesCollection(attributeName);

            if (propertiesCollection)
            {
                PrintProperty^  attributeValue = propertiesCollection->GetProperty(attributeName);

                attributeValue->IsInternallyInitialized = true;
                attributeValue->Value = getValueDelegate->Invoke(unmanagedCollection->propertiesCollection[index].propertyValue);
            }
        }
    }
}


Object^
AttributeValueInteropHandler::
GetString(
    PrintPropertyValue                 unmanagedPropertyValue
    )
{
    return gcnew String(unmanagedPropertyValue.value.propertyString);
}


Object^
AttributeValueInteropHandler::
GetInt32(
    PrintPropertyValue                 unmanagedPropertyValue
    )
{
    return Int32(unmanagedPropertyValue.value.propertyInt32);
}

Object^
AttributeValueInteropHandler::
GetDateTime(
    PrintPropertyValue                 unmanagedPropertyValue
    )
{
    return nullptr;
}

Object^
AttributeValueInteropHandler::
GetStream(
    PrintPropertyValue                 unmanagedPropertyValue
    )
{
    Int32 size   = unmanagedPropertyValue.value.propertyBlob.cbBuf;
    array<Byte>^  data = gcnew array<Byte>(size);

    Marshal::Copy((IntPtr)unmanagedPropertyValue.value.propertyBlob.pBuf, data, 0 , size);

    return gcnew MemoryStream(data, 0 , size);
}

