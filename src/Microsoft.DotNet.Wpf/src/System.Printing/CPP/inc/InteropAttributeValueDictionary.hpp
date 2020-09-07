// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __INTEROPATTRIBUTEVALUEDICTIONARY_HPP__
#define __INTEROPATTRIBUTEVALUEDICTIONARY_HPP__
/*++
    Abstract:

        Utility classes that allocate and free the unmanaged printer info
        buffers that are going to be sent to the Win32 APIs. 
--*/
namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
    using namespace  System::Printing::IndexedProperties;

    private ref class AttributeValueInteropHandler
    {
        public:
        
        enum class PrintPropertyTypeInterop
        {
            StringPrintType     = kPropertyTypeString, //1
            Int32PrintType      = kPropertyTypeInt32, //2            
            DataTimePrintType   = kPropertyTypeTime,//5
            ByteBufferPrintType = 10,//kPropertyTypeBuffer
        };

        IntPtr
        BuildUnmanagedPrintPropertiesCollection(
            PrintPropertyDictionary^    collection
            );

        PrintPropertyDictionary^
        BuildManagedPrintPropertiesCollection(
            IntPtr                      unmanagedCollection
            );

        static
        void
        FreeUnmanagedPrintPropertiesCollection(
            IntPtr  unmanagedCollection
            );

        void
        CopyManagedPrintPropertiesCollection(
            IntPtr                    unmanagedCollection,
            PrintSystemObject^        printSystemObject
            );

        static
        IntPtr
        AllocateUnmanagedPrintPropertiesCollection(
            Int32     propertyCount
            );

        static
        void
        SetValue(
            IntPtr          unmanagedCollectionPtr,
            String^         propertyName,
            UInt32          index,
            System::Type^   type
            );

        static
        void
        SetValue(
            IntPtr      unmanagedCollectionPtr,
            String^     propertyName,
            UInt32      index,
            Object^     value
            );

        static
        Object^
        GetValue(
            IntPtr                      unmanagedCollectionPtr,
            String^                     propertyName,
            Type^                       type,
            Boolean%                    isPropertyPresent
            );

        property
        static
        AttributeValueInteropHandler^
        Value
        {
            AttributeValueInteropHandler^ get();
        }

        private:

        static
        Hashtable^                      unmanagedToManagedTypeMap;

        static
        Hashtable^                      managedToUnmanagedTypeMap;

        static
        Hashtable^                      attributeValueToUnmanagedTypeMap;

        static
        Hashtable^                      unmanagedPropertyToObjectDelegateMap;

        static 
        volatile 
        AttributeValueInteropHandler^      value;

        static
        volatile
        Object^                            syncRoot; 

        property
        static
        Object^
        SyncRoot
        {
            Object^ get();
        }

        delegate
        Object^
        GetValueFromUnmanagedValue(
            PrintPropertyValue                 unmanagedPropertyValue
            );

        static
        Object^
        GetString(
            PrintPropertyValue                 unmanagedPropertyValue
            );

        static
        Object^
        GetInt32(
            PrintPropertyValue                 unmanagedPropertyValue
            );

        static
        Object^
        GetStream(
            PrintPropertyValue                 unmanagedPropertyValue
            );

        static
        Object^
        GetDateTime(
            PrintPropertyValue                 unmanagedPropertyValue
            );

        static array<Type^>^ PrintSystemAttributePrimitiveTypes = 
        {
            String::typeid,
            Int32::typeid,            
            DateTime::typeid,
            System::IO::MemoryStream::typeid
        };

        static array<Type^>^ PrintSystemAttributeValueTypes = 
        {
            PrintStringProperty::typeid,
            PrintInt32Property::typeid,
            PrintDateTimeProperty::typeid,
            PrintStreamProperty::typeid
        };

        static array<GetValueFromUnmanagedValue^>^ GetValueFromUnmanagedValueDelegateTable = 
        {
            gcnew GetValueFromUnmanagedValue(&GetString),
            gcnew GetValueFromUnmanagedValue(&GetInt32),
            gcnew GetValueFromUnmanagedValue(&GetDateTime),
            gcnew GetValueFromUnmanagedValue(&GetStream)
        };


        AttributeValueInteropHandler(
            void
            );

        static 
        AttributeValueInteropHandler(
            void
            )
        {
            
            AttributeValueInteropHandler::value    = nullptr;
            AttributeValueInteropHandler::syncRoot = gcnew Object();
        
            managedToUnmanagedTypeMap            = gcnew Hashtable;
            unmanagedToManagedTypeMap            = gcnew Hashtable;
            attributeValueToUnmanagedTypeMap     = gcnew Hashtable;
            unmanagedPropertyToObjectDelegateMap = gcnew Hashtable;
            

            RegisterStaticMaps();
        }

        static 
        void
        RegisterStaticMaps(
            void
            );
                
        IntPtr
        AllocateUnmanagedPrintPropertiesCollection(
            PrintPropertyDictionary^    managedCollection
            );

        void
        AssignUnmanagedPrintPropertyValue(
            PrintNamedProperty*  unmanagedPropertyValue,
            PrintProperty^       managedAttributeValue
            );
          
    };

}
}
}
#endif

