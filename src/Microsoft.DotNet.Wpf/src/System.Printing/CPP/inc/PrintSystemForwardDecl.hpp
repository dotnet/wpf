// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTSYSTEMFORWARDDECL_HPP__
#define __PRINTSYSTEMFORWARDDECL_HPP__

namespace System
{
namespace Printing
{
namespace IndexedProperties
{
    ref class PrintProperty;
    ref class PrintServerProperty;
    ref class PrintPropertyDictionary;
    ref class PrintStringProperty;
    ref class PrintDateTimeProperty;
    ref class PrintInt32Property;
    ref class PrintStreamProperty;
    ref class PrintSystemTypeProperty;
    ref class PrintBooleanProperty;
    ref class PrintServerLoggingProperty;
    ref class PrintThreadPriorityProperty;
}
}
}

namespace System
{
namespace Printing
{
    ref class PrintSystemObject;
    ref struct DriverIdentifier;
    ref class Filter;
    ref class PrintDriver;
    ref class PrintPort;
    ref class Monitor;
    ref class PrintProcessor;
    ref class PrintProcessDataType;
    ref class PrintProcessorDataTypes;
    ref class PrintServer;
    ref class PrintSystemObjects;
    ref class PrintQueue;
    interface class IPrintQueueComponent;
    ref class PrintSystemObjectPropertyChangedEventArgs;
    ref class PrintSystemObjectPropertiesChangedEventArgs;
    ref class PrintSystemException;
    ref class PrintQueueException;
    ref class PrintServerException;
    ref class PrintCommitAttributesException;
    ref class PrintSystemJobInfo;
    ref class PrintJobInfoCollection;
    ref class  PrintQueueStream;
    ref class  PrintJobSettings;
}
}

namespace Microsoft
{
namespace Printing
{
namespace PrintTicket
{
    ref class PrintTicket;
}
}
}

//namespace System
//{
//namespace IO
//{
//namespace CompoundFile
//{
//    __gc public class StorageRoot;
//    __gc public class StorageInfo;
//}
//}
//}

namespace System
{
namespace Printing
{
namespace AsyncNotify
{
    ref class AsyncNotificationData;
    ref class AsyncNotifyChannel;
    ref class UnidirectionalNotificationEventArgs;

    ref class BidirectionalNotificationEventArgs;
    ref class AsynchronousNotificationsSubscription;

    ref class BidirectionalAsynchronousNotificationsSubscription;
    ref class UnidirectionalAsynchronousNotificationsSubscription;

    ref class ChannelSafeHandle;
    ref class RegistrationSafeHandle;
    ref class AsyncCallBackSafeHandle;
    
}
}
}

using namespace System::Printing;
using namespace System::Printing::AsyncNotify;

namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
    ref class PrinterThunkHandler;
    ref class SafeMemoryHandle;
    ref class PropertyCollectionMemorySafeHandle;
    ref class AttributeValueInteropHandler;
}
}
}

#endif
