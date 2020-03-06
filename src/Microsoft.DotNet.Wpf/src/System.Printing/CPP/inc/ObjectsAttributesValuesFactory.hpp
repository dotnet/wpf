// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __OBJECTSATTRIBUTESVALUESFACTORY_HPP__
#define __OBJECTSATTRIBUTESVALUESFACTORY_HPP__

namespace System
{
namespace Printing
{
namespace Activation
{
    using namespace  System::Printing::IndexedProperties;    

    private ref struct ObjectTypeDelegate
    {
        public:
        ObjectTypeDelegate(
            Type^                                        inType,
            PrintSystemObject::CreateWithValue^          inDelegateValue,
            PrintSystemObject::CreateWithNoValue^        inDelegateNoValue,
            PrintSystemObject::CreateWithValueLinked^    inDelegateValueLinked,
            PrintSystemObject::CreateWithNoValueLinked^  inDelegateNoValueLinked
            )
        {
            type                  = inType;
            delegateValue         = inDelegateValue;
            delegateNoValue       = inDelegateNoValue;
            delegateValueLinked   = inDelegateValueLinked;
            delegateNoValueLinked = inDelegateNoValueLinked;
        }

        Type^                                           type;
        PrintSystemObject::CreateWithValue^             delegateValue;
        PrintSystemObject::CreateWithNoValue^           delegateNoValue;
        PrintSystemObject::CreateWithValueLinked^       delegateValueLinked;
        PrintSystemObject::CreateWithNoValueLinked^     delegateNoValueLinked;
    };

    private ref struct AttributeTypeDelegate
    {
        public:
        AttributeTypeDelegate(
            Type^                                    inType,
            PrintProperty::CreateWithValue^          inDelegateValue,
            PrintProperty::CreateWithNoValue^        inDelegateNoValue,
            PrintProperty::CreateWithValueLinked^    inDelegateValueLinked,
            PrintProperty::CreateWithNoValueLinked^  inDelegateNoValueLinked
            )
        {
            type                  = inType;
            delegateValue         = inDelegateValue;
            delegateNoValue       = inDelegateNoValue;
            delegateValueLinked   = inDelegateValueLinked;
            delegateNoValueLinked = inDelegateNoValueLinked;
        }

        Type^                                    type;
        PrintProperty::CreateWithValue^          delegateValue;
        PrintProperty::CreateWithNoValue^        delegateNoValue;
        PrintProperty::CreateWithValueLinked^    delegateValueLinked;
        PrintProperty::CreateWithNoValueLinked^  delegateNoValueLinked;
    };

    private ref class ObjectsAttributesValuesFactory sealed
    {
        public:

        property 
        static 
        ObjectsAttributesValuesFactory^
        Value
        {
            ObjectsAttributesValuesFactory^ get();
        }

        void
        RegisterObjectAttributeNoValueCreationMethod(
            Type^                                           type,
            PrintSystemObject::CreateWithNoValue^           delegate
            );

        void
        RegisterObjectAttributeNoValueLinkedCreationMethod(
            Type^                                           type,      
            PrintSystemObject::CreateWithNoValueLinked^     delegate
            );

        void
        RegisterObjectAttributeValueCreationMethod(
            Type^                                           type,      
            PrintSystemObject::CreateWithValue^             delegate
            );

        void
        RegisterObjectAttributeValueLinkedCreationMethod(
            Type^                                           type,
            PrintSystemObject::CreateWithValueLinked^       delegate
            );

        PrintProperty^
        Create(
            Type^   type,
            String^ atttributeName
            );

        PrintProperty^
        Create(
            Type^   type,
            String^ attributeName,
            Object^ attributeValue
            );

        PrintProperty^
        Create(
            Type^               type,
            String^             attributeName,
            MulticastDelegate^  delegate
            );

        PrintProperty^
        Create(
            Type^               type,
            String^             attributeName,
            Object^             attributeValue,
            MulticastDelegate^  delegate
            );

        protected:

        !ObjectsAttributesValuesFactory(
            void
            );

        virtual
        void
        InternalDispose(
            bool    disposing
            );


        private:

        static
        ObjectsAttributesValuesFactory(
            void
            )
        {
            ObjectsAttributesValuesFactory::value    = nullptr;
            ObjectsAttributesValuesFactory::syncRoot = gcnew Object();
        }


        ObjectsAttributesValuesFactory(
            void
            );

        ~ObjectsAttributesValuesFactory(
            void
            );

        property 
        static 
        Object^
        SyncRoot
        {
            Object^ get();
        }

        static 
        ObjectsAttributesValuesFactory^ value;

        static
        array<ObjectTypeDelegate^>^   objectTypeDelegate =
        {
            gcnew ObjectTypeDelegate(PrintQueue::typeid,
                                     gcnew PrintSystemObject::CreateWithValue(&PrintQueue::CreateAttributeValue),
                                     gcnew PrintSystemObject::CreateWithNoValue(&PrintQueue::CreateAttributeNoValue),
                                     gcnew PrintSystemObject::CreateWithValueLinked(&PrintQueue::CreateAttributeValueLinked),
                                     gcnew PrintSystemObject::CreateWithNoValueLinked(&PrintQueue::CreateAttributeNoValueLinked)),

            gcnew ObjectTypeDelegate(PrintServer::typeid,
                                     gcnew PrintSystemObject::CreateWithValue(&PrintServer::CreateAttributeValue),
                                     gcnew PrintSystemObject::CreateWithNoValue(&PrintServer::CreateAttributeNoValue),
                                     gcnew PrintSystemObject::CreateWithValueLinked(&PrintServer::CreateAttributeValueLinked),
                                     gcnew PrintSystemObject::CreateWithNoValueLinked(&PrintServer::CreateAttributeNoValueLinked)),

            gcnew ObjectTypeDelegate(LocalPrintServer::typeid,
                                     gcnew PrintSystemObject::CreateWithValue(&LocalPrintServer::CreateAttributeValue),
                                     gcnew PrintSystemObject::CreateWithNoValue(&LocalPrintServer::CreateAttributeNoValue),
                                     gcnew PrintSystemObject::CreateWithValueLinked(&LocalPrintServer::CreateAttributeValueLinked),
                                     gcnew PrintSystemObject::CreateWithNoValueLinked(&LocalPrintServer::CreateAttributeNoValueLinked)),

            gcnew ObjectTypeDelegate(PrintDriver::typeid,
                                     gcnew PrintSystemObject::CreateWithValue(&PrintDriver::CreateAttributeValue),
                                     gcnew PrintSystemObject::CreateWithNoValue(&PrintDriver::CreateAttributeNoValue),
                                     gcnew PrintSystemObject::CreateWithValueLinked(&PrintDriver::CreateAttributeValueLinked),
                                     gcnew PrintSystemObject::CreateWithNoValueLinked(&PrintDriver::CreateAttributeNoValueLinked)),

            gcnew ObjectTypeDelegate(PrintPort::typeid,
                                     gcnew PrintSystemObject::CreateWithValue(&PrintPort::CreateAttributeValue),
                                     gcnew PrintSystemObject::CreateWithNoValue(&PrintPort::CreateAttributeNoValue),
                                     gcnew PrintSystemObject::CreateWithValueLinked(&PrintPort::CreateAttributeValueLinked),
                                     gcnew PrintSystemObject::CreateWithNoValueLinked(&PrintPort::CreateAttributeNoValueLinked)),

            gcnew ObjectTypeDelegate(PrintProcessor::typeid,
                                     gcnew PrintSystemObject::CreateWithValue(&PrintProcessor::CreateAttributeValue),
                                     gcnew PrintSystemObject::CreateWithNoValue(&PrintProcessor::CreateAttributeNoValue),
                                     gcnew PrintSystemObject::CreateWithValueLinked(&PrintProcessor::CreateAttributeValueLinked),
                                     gcnew PrintSystemObject::CreateWithNoValueLinked(&PrintProcessor::CreateAttributeNoValueLinked)),

            gcnew ObjectTypeDelegate(PrintSystemJobInfo::typeid,
                                     gcnew PrintSystemObject::CreateWithValue(&PrintSystemJobInfo::CreateAttributeValue),
                                     gcnew PrintSystemObject::CreateWithNoValue(&PrintSystemJobInfo::CreateAttributeNoValue),
                                     gcnew PrintSystemObject::CreateWithValueLinked(&PrintSystemJobInfo::CreateAttributeValueLinked),
                                     gcnew PrintSystemObject::CreateWithNoValueLinked(&PrintSystemJobInfo::CreateAttributeNoValueLinked))
        };

        static
        array<AttributeTypeDelegate^>^   attributeValueTypeDelegate =
        {
            gcnew AttributeTypeDelegate(String::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintStringProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintStringProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintStringProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintStringProperty::Create)),

            gcnew AttributeTypeDelegate(Int32::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintInt32Property::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintInt32Property::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintInt32Property::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintInt32Property::Create)),

            gcnew AttributeTypeDelegate(Stream::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintStreamProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintStreamProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintStreamProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintStreamProperty::Create)),

            gcnew AttributeTypeDelegate(Boolean::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintBooleanProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintBooleanProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintBooleanProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintBooleanProperty::Create)),

            gcnew AttributeTypeDelegate(PrintPort::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintPortProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintPortProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintPortProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintPortProperty::Create)),

            gcnew AttributeTypeDelegate(PrintDriver::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintDriverProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintDriverProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintDriverProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintDriverProperty::Create)),

            gcnew AttributeTypeDelegate(PrintProcessor::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintProcessorProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintProcessorProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintProcessorProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintProcessorProperty::Create)),

            gcnew AttributeTypeDelegate(PrintQueue::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintQueueProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintQueueProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintQueueProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintQueueProperty::Create)),

            gcnew AttributeTypeDelegate(PrintQueueAttributes::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintQueueAttributeProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintQueueAttributeProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintQueueAttributeProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintQueueAttributeProperty::Create)),

            gcnew AttributeTypeDelegate(PrintQueueStatus::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintQueueStatusProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintQueueStatusProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintQueueStatusProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintQueueStatusProperty::Create)),

            gcnew AttributeTypeDelegate(PrintServer::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintServerProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintServerProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintServerProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintServerProperty::Create)),

            gcnew AttributeTypeDelegate(System::Threading::ThreadPriority::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintThreadPriorityProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintThreadPriorityProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintThreadPriorityProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintThreadPriorityProperty::Create)),

            gcnew AttributeTypeDelegate(array<Byte>::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintByteArrayProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintByteArrayProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintByteArrayProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintByteArrayProperty::Create)),

            gcnew AttributeTypeDelegate(PrintServerEventLoggingTypes::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintServerLoggingProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintServerLoggingProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintServerLoggingProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintServerLoggingProperty::Create)),

            gcnew AttributeTypeDelegate(System::Type::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintSystemTypeProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintSystemTypeProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintSystemTypeProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintSystemTypeProperty::Create)),

            gcnew AttributeTypeDelegate(PrintJobStatus::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintJobStatusProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintJobStatusProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintJobStatusProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintJobStatusProperty::Create)),

            gcnew AttributeTypeDelegate(PrintJobPriority::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintJobPriorityProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintJobPriorityProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintJobPriorityProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintJobPriorityProperty::Create)),

            gcnew AttributeTypeDelegate(PrintJobType::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintSystemJobTypeAttributeValue::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintSystemJobTypeAttributeValue::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintSystemJobTypeAttributeValue::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintSystemJobTypeAttributeValue::Create)),

            gcnew AttributeTypeDelegate(System::DateTime::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintDateTimeProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintDateTimeProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintDateTimeProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintDateTimeProperty::Create)),

            gcnew AttributeTypeDelegate(PrintTicket::typeid,
                                        gcnew PrintProperty::CreateWithValue(&PrintTicketProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValue(&PrintTicketProperty::Create),
                                        gcnew PrintProperty::CreateWithValueLinked(&PrintTicketProperty::Create),
                                        gcnew PrintProperty::CreateWithNoValueLinked(&PrintTicketProperty::Create)),


        };

        static 
        array<MulticastDelegate^>^ registerationDelegate =
        {
            gcnew PrintSystemDelegates::ObjectRegistered(&PrintQueue::RegisterAttributesNamesTypes),
            gcnew PrintSystemDelegates::ObjectRegistered(&PrintServer::RegisterAttributesNamesTypes),
            gcnew PrintSystemDelegates::ObjectRegistered(&LocalPrintServer::RegisterAttributesNamesTypes),
            gcnew PrintSystemDelegates::ObjectRegistered(&PrintDriver::RegisterAttributesNamesTypes),
            gcnew PrintSystemDelegates::ObjectRegistered(&PrintPort::RegisterAttributesNamesTypes),
            gcnew PrintSystemDelegates::ObjectRegistered(&PrintProcessor::RegisterAttributesNamesTypes),
            gcnew PrintSystemDelegates::ObjectRegistered(&PrintSystemJobInfo::RegisterAttributesNamesTypes)
        };

        static 
        volatile 
        Object^                         syncRoot;

        bool                            isDisposed;
        Hashtable^                      valueDelegatesTable;
        Hashtable^                      noValueDelegatesTable;
        Hashtable^                      valueLinkedDelegatesTable;
        Hashtable^                      noValueLinkedDelegatesTable;
    };
}
}
}

#endif

