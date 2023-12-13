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

using namespace System::Printing::Interop;

#include <vcclr.h>

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#ifndef  __GENERICTHUNKINGINC_HPP__
#include <GenericThunkingInc.hpp>
#endif

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif


using namespace System::Printing;

#ifndef __ASYNCNOTIFY_HPP__
#include "AsyncNotify.hpp"
#endif

#ifndef __ASYNCNOTIFYUNMANAGED_HPP__
#include "AsyncNotifyUnmanaged.hpp"
#endif

#ifndef __INTEROPASYNCNOTIFY_HPP__
#include "InteropAsyncNotify.hpp"
#endif


using namespace MS::Internal::PrintWin32Thunk::Win32ApiThunk;
using namespace System::Printing::AsyncNotify;

AsyncNotifyChannel::
AsyncNotifyChannel(
    IPrintAsyncNotifyChannel*  asynchNotifyChannelUnmanaged
    )
{
    if (asynchNotifyChannelUnmanaged != NULL)
    {
        this->channelHandle = gcnew ChannelSafeHandle(asynchNotifyChannelUnmanaged);

        channelMappingTable->Add(IntPtr(asynchNotifyChannelUnmanaged).ToString(), this);
    }
    else
    {
        throw gcnew PrintSystemException("PrintSystemException.AsyncNotify.NullChannelReference");
    }
}

AsyncNotifyChannel::
~AsyncNotifyChannel(
    void
    )
{
    //Dispose(false);
    Dispose(true);
    GC::SuppressFinalize(this);
}

bool
AsyncNotifyChannel::
Send(
    AsyncNotificationData^ notification
    )
{
    return  channelHandle->SendNotification(notification);
}

bool
AsyncNotifyChannel::
Close(
    AsyncNotificationData^ notification
    )
{

    return  channelHandle->CloseChannel(notification);
}

/*void
AsyncNotifyChannel::
Dispose(
    void
    )
{
    Dispose(true);
    GC::SuppressFinalize(this);
}*/

void
AsyncNotifyChannel::
Dispose(
    bool disposing
    )
{
    if(!isDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if (disposing)
                {
                    channelMappingTable->Remove(channelHandle->DangerousGetHandle().ToString());

                    //channelHandle->Dispose();
                    delete channelHandle;
                }
            }
            __finally
            {
                isDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

AsyncNotifyChannel^
AsyncNotifyChannel::
MapUnmanagedChannel(
    String^ channelGuid
    )
{
    AsyncNotifyChannel^ channel = nullptr;

    channel = (AsyncNotifyChannel^)channelMappingTable[channelGuid];

    return channel;
}

/*--------------------------------------------------------------------------------------*/
/*                AsynchronousNotificationsSubscription Implementation                  */
/*--------------------------------------------------------------------------------------*/

AsynchronousNotificationsSubscription::
AsynchronousNotificationsSubscription(
    System::Printing::PrintSystemObject^                            printObject,
    System::Guid                                                                    subscriptionDataType,
    System::Printing::AsyncNotify::UserNotificationFilter           subscriptionUserFilter) :
    isDisposed(false),
    printSystemObject(printObject),
    notificationDataType(subscriptionDataType),
    perUserNotificationFilter(subscriptionUserFilter),
    registrationHandler(nullptr),
    callBackHandler(nullptr)
{
}

AsynchronousNotificationsSubscription^
AsynchronousNotificationsSubscription::
CreateSubscription(
    System::Printing::PrintSystemObject^                            publisher,
    ConversationStyle                                                               conversationStyle,
    System::Guid                                                                    notificationDataType,
    System::Printing::AsyncNotify::UserNotificationFilter           perUserNotificationFilter
    )
{
    AsynchronousNotificationsSubscription^ subscription = nullptr;

    if (conversationStyle == ConversationStyle::Unidirectional)
    {
        subscription = gcnew UnidirectionalAsynchronousNotificationsSubscription(publisher,
                                                                                 notificationDataType,
                                                                                 perUserNotificationFilter);
    }
    else
    {
        subscription = gcnew BidirectionalAsynchronousNotificationsSubscription(publisher,
                                                                                notificationDataType,
                                                                                perUserNotificationFilter);
    }

    return subscription;
}

PrintSystemObject^
AsynchronousNotificationsSubscription::PublisherPrintSystemObject::
get(
    void
    )
{
    return printSystemObject;
}


UserNotificationFilter
AsynchronousNotificationsSubscription::PerUserNotificationFilter::
get(
    void
    )
{
    return perUserNotificationFilter;
}

System::Guid
AsynchronousNotificationsSubscription::NotificationDataType::
get(
    void
    )
{
    return notificationDataType;
}

bool
AsynchronousNotificationsSubscription::IsDisposed::
get(
    void
    )
{
    return isDisposed;
}

void
AsynchronousNotificationsSubscription::RegistrationHandler::
set(
    RegistrationSafeHandle^ registrationHandler
    )
{
    this->registrationHandler = registrationHandler;
}

AsyncCallBackSafeHandle^
AsynchronousNotificationsSubscription::AsyncCallBackHandler::
get(
    void
    )
{
    return this->callBackHandler;
}

void
AsynchronousNotificationsSubscription::AsyncCallBackHandler::
set(
    AsyncCallBackSafeHandle^     callBackHandler
    )
{
    this->callBackHandler = callBackHandler;
}

void
AsynchronousNotificationsSubscription::
Dispose(
    bool disposing
    )
{
    if(!isDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if (disposing)
                {
                    //registrationHandler->Dispose();
                    delete registrationHandler;
                    //callBackHandler->Dispose();
                    delete callBackHandler;
                }
            }
            __finally
            {
                isDisposed  = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*void
AsynchronousNotificationsSubscription::
Dispose(
    void
    )
{
    Dispose(true);
    GC::SuppressFinalize(this);
}*/

AsynchronousNotificationsSubscription::
~AsynchronousNotificationsSubscription(
    void
    )
{
    //Dispose(false);
    Dispose(true);
    GC::SuppressFinalize(this);
}

/*--------------------------------------------------------------------------------------*/
/*       BidirectionalAsynchronousNotificationsSubscription Implementation              */
/*--------------------------------------------------------------------------------------*/

BidirectionalAsynchronousNotificationsSubscription::
BidirectionalAsynchronousNotificationsSubscription(
    System::Printing::PrintSystemObject^                    printObject,
    System::Guid                                                            subscriptionDataType,
    System::Printing::AsyncNotify::UserNotificationFilter   subscriptionUserFilter) :
    AsynchronousNotificationsSubscription(printObject,
                                          subscriptionDataType,
                                          subscriptionUserFilter)
{
    //this->AsyncCallBackHandler = gcnew AsyncCallBackSafeHandle(new AsyncNotifyBidiCallbackUnmanaged(this));

    this->RegistrationHandler = gcnew RegistrationSafeHandle(printObject,
                                                             subscriptionDataType,
                                                             subscriptionUserFilter,
                                                             kBiDirectional,
                                                             this->AsyncCallBackHandler);
}

BidirectionalAsynchronousNotificationsSubscription::
~BidirectionalAsynchronousNotificationsSubscription(
    void
    )
{
    Dispose(false);
}

void
BidirectionalAsynchronousNotificationsSubscription::
OnEventNotify(
    AsyncNotifyChannel^     channel,
    AsyncNotificationData^  notification
    )
{
    OnBidirectionalNotificationArrived(gcnew BidirectionalNotificationEventArgs(channel, notification, false));
}

void
BidirectionalAsynchronousNotificationsSubscription::
OnChannelClosed(
    AsyncNotifyChannel^     channel,
    AsyncNotificationData^  notification
    )
{
    OnBidirectionalNotificationArrived(gcnew BidirectionalNotificationEventArgs(channel, notification, true));
}

void
BidirectionalAsynchronousNotificationsSubscription::
OnBidirectionalNotificationArrived(
    BidirectionalNotificationEventArgs^ e
    )
{
    BidirectionalNotificationArrived(this, e);
}

void
BidirectionalAsynchronousNotificationsSubscription::
Dispose(
    bool disposing
    )
{
    if(!IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if (disposing)
                {

                }
            }
            __finally
            {
                AsynchronousNotificationsSubscription::Dispose(disposing);
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}


/*--------------------------------------------------------------------------------------*/
/*    UnidirectionalAsynchronousNotificationsSubscription Implementation                */
/*--------------------------------------------------------------------------------------*/

UnidirectionalAsynchronousNotificationsSubscription::
UnidirectionalAsynchronousNotificationsSubscription(
    System::Printing::PrintSystemObject^                    printObject,
    System::Guid                                                            subscriptionDataType,
    System::Printing::AsyncNotify::UserNotificationFilter   subscriptionUserFilter) :
    AsynchronousNotificationsSubscription(printObject,
                                          subscriptionDataType,
                                          subscriptionUserFilter)
{
    //this->AsyncCallBackHandler = gcnew AsyncCallBackSafeHandle(new AsyncNotifyUnidiCallbackUnmanaged(this));

    this->RegistrationHandler = gcnew RegistrationSafeHandle(printObject,
                                                             subscriptionDataType,
                                                             subscriptionUserFilter,
                                                             kUniDirectional,
                                                             this->AsyncCallBackHandler);

}

UnidirectionalAsynchronousNotificationsSubscription::
~UnidirectionalAsynchronousNotificationsSubscription(
    void
    )
{
    Dispose(false);
}

void
UnidirectionalAsynchronousNotificationsSubscription::
OnNewUnidirectionalNotification(
    AsyncNotificationData^      notificationData
    )
{
    OnUnidirectionalNotificationArrived(gcnew UnidirectionalNotificationEventArgs(notificationData));
}

void
UnidirectionalAsynchronousNotificationsSubscription::
OnUnidirectionalNotificationArrived(
    UnidirectionalNotificationEventArgs^ e
    )
{
    UnidirectionalNotificationArrived(this, e);
}

void
UnidirectionalAsynchronousNotificationsSubscription::
Dispose(
    bool disposing
    )
{
    if(!IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if (disposing)
                {
                    //
                    // No managed data
                    //
                }
            }
            __finally
            {
                AsynchronousNotificationsSubscription::Dispose(disposing);
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}




/*--------------------------------------------------------------------------------------*/
/*                          AsyncNotificationData Implementation                        */
/*--------------------------------------------------------------------------------------*/
AsyncNotificationData::
AsyncNotificationData(
    IPrintAsyncNotifyDataObject*    pNotification
    ) : isDisposed(false)
{
    if (pNotification)
    {
        HRESULT hrResult = E_FAIL;

        BYTE*                           notificationData    = NULL;
        ULONG                           notificationLength  = 0;
        PrintAsyncNotificationType*     notificationType    = NULL;

        if (SUCCEEDED(hrResult = pNotification->AcquireData(&notificationData,
                                                            &notificationLength,
                                                            &notificationType)))
        {
            array<Byte>^ notificationDataManaged = gcnew array<Byte>(notificationLength);

            if (notificationLength && notificationData)
            {
                Marshal::Copy((IntPtr)notificationData, notificationDataManaged, 0 , notificationLength);

                this->dataStream = gcnew MemoryStream(notificationDataManaged);

                this->dataStream->Position = 0;
            }

            if (notificationType)
            {
                array<Byte>^ notificationTypeManaged = gcnew array<Byte>(sizeof(PrintAsyncNotificationType));

                Marshal::Copy((IntPtr)notificationType, notificationTypeManaged, 0 , sizeof(PrintAsyncNotificationType));

                this->dataType = Guid(notificationTypeManaged);
            }

            pNotification->ReleaseData();

            pNotification->Release();
        }
    }
    else
    {
        throw gcnew PrintSystemException("PrintSystemException.AsyncNotify.NullNotificationDataReference");
    }

}

AsyncNotificationData::
AsyncNotificationData(
    Stream^                 dataStream,
    System::Guid            dataType
    ) : isDisposed(false)
{
    this->dataType   = dataType;
    this->dataStream = dataStream;
}

System::Guid
AsyncNotificationData::DataType::
get(
    void
    )
{
    return dataType;
}

Stream^
AsyncNotificationData::DataStream::
get(
    void
    )
{
    return dataStream;
}

/*void
AsyncNotificationData::
Dispose(
    void
    )
{
    Dispose(true);
    GC::SuppressFinalize(this);
}*/

void
AsyncNotificationData::
Dispose(
    bool disposing
    )
{
    if(!isDisposed)
    {
        isDisposed = true;

        //dataStream->Dispose();
        delete dataStream;
    }
}

AsyncNotificationData::
~AsyncNotificationData(
    void
    )
{
    //Dispose(false);
    Dispose(true);
    GC::SuppressFinalize(this);
}

/*--------------------------------------------------------------------------------------*/
/*                   UnidirectionalNotificationEventArgs Implementation                 */
/*--------------------------------------------------------------------------------------*/
UnidirectionalNotificationEventArgs::
UnidirectionalNotificationEventArgs(
    AsyncNotificationData^ data
    )
{
    this->notification = data;
}

AsyncNotificationData^
UnidirectionalNotificationEventArgs::Notification::
get(
    void
    )
{
    return notification;
}

/*--------------------------------------------------------------------------------------*/
/*             BidirectionalNotificationEventArgs Implementation                        */
/*--------------------------------------------------------------------------------------*/
BidirectionalNotificationEventArgs::
BidirectionalNotificationEventArgs(
    AsyncNotifyChannel^         channel,
    AsyncNotificationData^      notification,
    Boolean                     isClosed
    )
{
    this->channel       = channel;
    this->notification  = notification;
    this->isClosed      = isClosed;
}

AsyncNotifyChannel^
BidirectionalNotificationEventArgs::Channel::
get(
    )
{
    return channel;
}

AsyncNotificationData^
BidirectionalNotificationEventArgs::Notification::
get(
    )
{
    return notification;
}

bool
BidirectionalNotificationEventArgs::IsChannelClosed::
get(
    void
    )
{
    return isClosed;
}

