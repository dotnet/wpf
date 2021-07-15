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

#include "AsyncNotify.hpp"
#include "AsyncNotifyUnmanaged.hpp"
#include "InteropAsyncNotify.hpp"

using namespace MS::Internal::PrintWin32Thunk::Win32ApiThunk;
using namespace System::Printing::AsyncNotify;



AsyncNotifyBidiCallbackUnmanaged::
AsyncNotifyBidiCallbackUnmanaged(
    gcroot<BidirectionalAsynchronousNotificationsSubscription^> registration
    ):
    m_hValid(S_OK),
    m_cRef(1)
{
    this->registration = registration;
}

AsyncNotifyBidiCallbackUnmanaged::
~AsyncNotifyBidiCallbackUnmanaged(
    void
    )
{
}


STDMETHODIMP
AsyncNotifyBidiCallbackUnmanaged::
QueryInterface(
    REFIID      riid,
    VOID**      ppv
    )
{
    HRESULT hResult = E_POINTER;

    if (ppv)
    {
        hResult = E_NOINTERFACE;

        *ppv = NULL;

        if (riid == IID_IPrintAsyncNotifyCallback ||
            riid == IID_IUnknown)
        {
            *ppv = reinterpret_cast<VOID *>(this);
            reinterpret_cast<IUnknown *>(*ppv)->AddRef();
            hResult = S_OK;
        }
    }

    return hResult;
}

STDMETHODIMP_(ULONG)
AsyncNotifyBidiCallbackUnmanaged::
AddRef(
    VOID
    )
{
    return InterlockedIncrement(&m_cRef);
}

STDMETHODIMP_(ULONG)
AsyncNotifyBidiCallbackUnmanaged::
Release(
    VOID
    )
{
    ULONG cRef = InterlockedDecrement(&m_cRef);

    if (cRef == 0)
    {
        delete this;
    }

    return cRef;
}

STDMETHODIMP
AsyncNotifyBidiCallbackUnmanaged::
OnEventNotify(
    IPrintAsyncNotifyChannel*       pIAsynchNotifyChannel,
    IPrintAsyncNotifyDataObject*    pNotificationUnmanaged
    )
{
    HRESULT hResult = E_INVALIDARG;

    AsyncNotifyChannel^ channel             = nullptr;
    IntPtr              unmanagedChannelPtr = (IntPtr) pIAsynchNotifyChannel;

    channel = AsyncNotifyChannel::MapUnmanagedChannel(unmanagedChannelPtr.ToString());

    if (channel == nullptr)
    {
        channel = gcnew AsyncNotifyChannel(pIAsynchNotifyChannel);
    }

    AsyncNotificationData^ notification = gcnew AsyncNotificationData(pNotificationUnmanaged);

    registration->OnEventNotify(channel, notification);

    return hResult;
}

HRESULT
AsyncNotifyBidiCallbackUnmanaged::
ChannelClosed(
    IPrintAsyncNotifyChannel*       pIAsynchNotifyChannel,
    IPrintAsyncNotifyDataObject*    pNotificationUnmanaged
    )
{
    HRESULT hResult = E_INVALIDARG;

    if (pIAsynchNotifyChannel && pNotificationUnmanaged)
    {
        AsyncNotifyChannel^ channel             = nullptr;
        IntPtr              unmanagedChannelPtr = (IntPtr) pIAsynchNotifyChannel;

        //
        // If there is competition and another listener closes the channel before this one got a chance to
        // send the initial parking call, then it will get a ChannelClosed notification.
        //
        channel = AsyncNotifyChannel::MapUnmanagedChannel(unmanagedChannelPtr.ToString());

        AsyncNotificationData^ notification = gcnew AsyncNotificationData(pNotificationUnmanaged);

        registration->OnChannelClosed(channel, notification);
    }

    return hResult;
}

/*--------------------------------------------------------------------------------------*/
/*             AsyncNotifyUnidiCallbackUnmanaged Implementation                         */
/*--------------------------------------------------------------------------------------*/

AsyncNotifyUnidiCallbackUnmanaged::
AsyncNotifyUnidiCallbackUnmanaged(
    gcroot<UnidirectionalAsynchronousNotificationsSubscription^> registration
    ):
    m_hValid(S_OK),
    m_cRef(1)
{
    this->registration = registration;
}

AsyncNotifyUnidiCallbackUnmanaged::
~AsyncNotifyUnidiCallbackUnmanaged(
    void
    )
{
}


STDMETHODIMP
AsyncNotifyUnidiCallbackUnmanaged::
QueryInterface(
    REFIID      riid,
    VOID**      ppv
    )
{
    HRESULT hResult = E_POINTER;

    if (ppv)
    {
        hResult = E_NOINTERFACE;

        *ppv = NULL;

        if (riid == IID_IPrintAsyncNotifyCallback ||
            riid == IID_IUnknown)
        {
            *ppv = reinterpret_cast<VOID *>(this);
            reinterpret_cast<IUnknown *>(*ppv)->AddRef();
            hResult = S_OK;
        }
    }

    return hResult;
}

STDMETHODIMP_(ULONG)
AsyncNotifyUnidiCallbackUnmanaged::
AddRef(
    VOID
    )
{
    return InterlockedIncrement(&m_cRef);
}

STDMETHODIMP_(ULONG)
AsyncNotifyUnidiCallbackUnmanaged::
Release(
    VOID
    )
{
    ULONG cRef = InterlockedDecrement(&m_cRef);

    if (cRef == 0)
    {
        delete this;
    }

    return cRef;
}

STDMETHODIMP
AsyncNotifyUnidiCallbackUnmanaged::
OnEventNotify(
    IPrintAsyncNotifyChannel*       ,
    IPrintAsyncNotifyDataObject*    pNotificationUnmanaged
    )
{
    HRESULT hResult = E_INVALIDARG;

    if (pNotificationUnmanaged)
    {
        AsyncNotificationData^ notification = gcnew AsyncNotificationData(pNotificationUnmanaged);

        registration->OnNewUnidirectionalNotification(notification);
    }

    return hResult;
}

HRESULT
AsyncNotifyUnidiCallbackUnmanaged::
ChannelClosed(
    IPrintAsyncNotifyChannel*       ,
    IPrintAsyncNotifyDataObject*    pNotificationUnmanaged
    )
{
    HRESULT hResult = E_INVALIDARG;

    if (pNotificationUnmanaged)
    {
        AsyncNotificationData^ notification = gcnew AsyncNotificationData(pNotificationUnmanaged);

        registration->OnNewUnidirectionalNotification(notification);
    }

    return hResult;
}



/*--------------------------------------------------------------------------------------*/
/*             AsyncNotifyDataObjectUnmanaged Implementation                            */
/*--------------------------------------------------------------------------------------*/

AsyncNotifyDataObjectUnmanaged::
AsyncNotifyDataObjectUnmanaged(
    AsyncNotificationData^  managedNotification
    ):
    m_Type(NULL),
    m_Size(0),
    m_Data(NULL),
    m_cRef(1)
{
    try
    {
        //
        // Copy the data
        //
        __int64 savePosition =  managedNotification->DataStream->Position;

        managedNotification->DataStream->Position = 0;

        m_Size = static_cast<ULONG>(managedNotification->DataStream->Length);

        array<Byte>^ notificationDataManaged = gcnew array<Byte>(m_Size);

        managedNotification->DataStream->Read(notificationDataManaged, 0, m_Size);

        managedNotification->DataStream->Position = savePosition;

        //
        // Throws OutOfMemoryException if it cannot allocate the requested size.
        //
        IntPtr   notificationDataUnmanaged = Marshal::AllocHGlobal(m_Size);

        m_Data = reinterpret_cast<BYTE*>(notificationDataUnmanaged.ToPointer());

        Marshal::Copy(notificationDataManaged, 0, notificationDataUnmanaged, m_Size);

        //
        // Copy the data type
        //
        array<Byte>^ notificationTypeManaged = managedNotification->DataType.ToByteArray();

        //
        // Throws OutOfMemoryException if it cannot allocate the requested size.
        //
        IntPtr  notificationTypeUnmanaged = Marshal::AllocHGlobal(sizeof(GUID));

        m_Type = reinterpret_cast<GUID*>(notificationTypeUnmanaged.ToPointer());

        Marshal::Copy(notificationTypeManaged, 0, notificationTypeUnmanaged, sizeof(GUID));
    }
    catch (SystemException^ exception)
    {
        if (m_Data)
        {
            Marshal::FreeHGlobal(IntPtr(m_Data));
        }

        if (m_Type)
        {
            Marshal::FreeHGlobal(IntPtr(m_Type));
        }

        throw exception;
    }
}

AsyncNotifyDataObjectUnmanaged::
~AsyncNotifyDataObjectUnmanaged(
    )
{
    if (m_Data)
    {
        Marshal::FreeHGlobal(IntPtr(m_Data));
    }

    if (m_Type)
    {
        Marshal::FreeHGlobal(IntPtr(m_Type));
    }

}


STDMETHODIMP
AsyncNotifyDataObjectUnmanaged::
QueryInterface(
    REFIID  riid,
    VOID    **ppv
    )
{
    HRESULT hResult = E_POINTER;

    if (ppv)
    {
        hResult = E_NOINTERFACE;

        *ppv = NULL;

        if (riid == IID_IPrintAsyncNotifyDataObject ||
            riid == IID_IUnknown)
        {
            *ppv = reinterpret_cast<VOID *>(this);
            reinterpret_cast<IUnknown *>(*ppv)->AddRef();
            hResult = S_OK;
        }
    }

    return hResult;
}

STDMETHODIMP_(ULONG)
AsyncNotifyDataObjectUnmanaged::
AddRef(
    VOID
    )
{
    return InterlockedIncrement(&m_cRef);
}

STDMETHODIMP_(ULONG)
AsyncNotifyDataObjectUnmanaged::
Release(
    VOID
    )
{
    ULONG cRef = InterlockedDecrement(&m_cRef);

    if (cRef == 0)
    {
        delete this;
    }
    return cRef;
}

STDMETHODIMP
AsyncNotifyDataObjectUnmanaged::
AcquireData(
    BYTE**                          ppbData,
    ULONG*                          pSize,
    PrintAsyncNotificationType**    pType
    )
{
    HRESULT hResult = E_FAIL;

    if (!ppbData || !pSize || !pType)
    {
        hResult = E_INVALIDARG;
    }
    else
    {
        *ppbData = m_Data;
        *pSize   = m_Size;
        *pType   = m_Type;
        this->AddRef();
        hResult = S_OK;
    }

    return hResult;
}

STDMETHODIMP
AsyncNotifyDataObjectUnmanaged::
ReleaseData(
    VOID
    )
{
    this->Release();

    return S_OK;
}


/*--------------------------------------------------------------------------------------*/
/*                         ChannelSafeHandle Implementation                             */
/*--------------------------------------------------------------------------------------*/

ChannelSafeHandle::
ChannelSafeHandle(
    IPrintAsyncNotifyChannel*   channel
    ) : SafeHandle(IntPtr(channel), true)
{
}

Boolean
ChannelSafeHandle::IsInvalid::
get(
    void
    )
{
    return (DangerousGetHandle() == IntPtr::Zero);
}

Boolean
ChannelSafeHandle::
ReleaseHandle(
    void
    )
{
    Int32 result = ((reinterpret_cast<IPrintAsyncNotifyChannel*>(DangerousGetHandle().ToPointer()))->Release());

    return true;
}

Boolean
ChannelSafeHandle::
SendNotification(
    AsyncNotificationData^ managedNotification
    )
{
    Boolean returnValue = false;

    if (!IsInvalid)
    {
        IPrintAsyncNotifyChannel* unmanagedChannel = reinterpret_cast<IPrintAsyncNotifyChannel*>(DangerousGetHandle().ToPointer());

        AsyncNotifyDataObjectUnmanaged* unmanagedNotification  = new AsyncNotifyDataObjectUnmanaged(managedNotification);

        if (unmanagedNotification)
        {
            HRESULT hResult = unmanagedChannel->SendNotification(unmanagedNotification);

            returnValue = SUCCEEDED(hResult);

            if (!returnValue)
            {
                throw gcnew PrintSystemException(hResult,
                                                 "PrintSystemException.AsyncNotify.SendNotification");
            }

            unmanagedNotification->Release();
        }
        else
        {
            //
            // We try to allocate an unmanaged object. We have to throw ourselves if the allocation fails.
            //
            throw gcnew OutOfMemoryException;
        }
    }

    return returnValue;
}

Boolean
ChannelSafeHandle::
CloseChannel(
    AsyncNotificationData^ managedNotification
    )
{
    Boolean returnValue = false;

    if (!IsInvalid)
    {
        IPrintAsyncNotifyChannel* unmanagedChannel = reinterpret_cast<IPrintAsyncNotifyChannel*>(DangerousGetHandle().ToPointer());

        AsyncNotifyDataObjectUnmanaged* unmanagedNotification  = new AsyncNotifyDataObjectUnmanaged(managedNotification);

        if (unmanagedNotification)
        {
            HRESULT hResult = unmanagedChannel->CloseChannel(unmanagedNotification);

            returnValue = SUCCEEDED(hResult);

            if (!returnValue)
            {
                throw gcnew PrintSystemException(hResult,
                                                "PrintSystemException.AsyncNotify.CloseChannel");
            }

            unmanagedNotification->Release();
        }
        else
        {
            //
            // We try to allocate an unmanaged object. We have to throw ourselves if the allocation fails.
            //
            throw gcnew OutOfMemoryException;
        }
    }

    return returnValue;
}

/*--------------------------------------------------------------------------------------*/
/*                    RegistrationSafeHandle Implementation                             */
/*--------------------------------------------------------------------------------------*/

RegistrationSafeHandle::
RegistrationSafeHandle(
    System::Printing::PrintSystemObject^                        printObject,
    System::Guid                                                                subscriptionDataType,
    UserNotificationFilter                                                      subscriptionUserFilter,
    PrintAsyncNotifyConversationStyle                                           conversationStyle,
    AsyncCallBackSafeHandle^                                                    callBackHandle
    ) : SafeHandle(IntPtr(RegistrationSafeHandle::CreateUnmanagedRegistration(printObject,
                                                                              subscriptionDataType,
                                                                              subscriptionUserFilter,
                                                                              conversationStyle,
                                                                              callBackHandle)),
                   true)
{

}

HANDLE
RegistrationSafeHandle::
CreateUnmanagedRegistration(
    System::Printing::PrintSystemObject^                        printObject,
    System::Guid                                                                subscriptionDataType,
    UserNotificationFilter                                                      subscriptionUserFilter,
    PrintAsyncNotifyConversationStyle                                           conversationStyle,
    AsyncCallBackSafeHandle^                                                    callBackHandle
    )
{
    IntPtr registrationUnmanaged = (IntPtr)nullptr;

    IPrintAsyncNotifyCallback* callBackInterface = reinterpret_cast<IPrintAsyncNotifyCallback*>(((callBackHandle->DangerousGetHandle()).ToPointer()));

    UInt32 hResult =
    AsyncNotifyNativeMethods::RegisterForPrintAsyncNotifications(printObject->Name,
                                                                 %subscriptionDataType,
                                                                 subscriptionUserFilter,
                                                                 conversationStyle,
                                                                 callBackInterface,
                                                                 %registrationUnmanaged);

    if (FAILED(hResult))
    {
        throw gcnew PrintSystemException(hResult,
                                         "PrintSystemException.AsyncNotify.RegisterForPrintAsyncNotifications");
    }

    return registrationUnmanaged.ToPointer();

}


Boolean
RegistrationSafeHandle::IsInvalid::
get(
    void
    )
{
    IntPtr registration = DangerousGetHandle();

    return (registration == IntPtr::Zero || registration.ToPointer() == INVALID_HANDLE_VALUE);
}

Boolean
RegistrationSafeHandle::
ReleaseHandle(
    void
    )
{
    UInt32 hResult = AsyncNotifyNativeMethods::UnRegisterForPrintAsyncNotifications((IntPtr) DangerousGetHandle().ToPointer());

    if (FAILED(hResult))
    {
        throw gcnew PrintSystemException(hResult,
                                         "PrintSystemException.AsyncNotify.UnRegisterForPrintAsyncNotifications");
    }
    return SUCCEEDED(hResult);
}

/*--------------------------------------------------------------------------------------*/
/*                    AsyncCallBackSafeHandle Implementation                            */
/*--------------------------------------------------------------------------------------*/

AsyncCallBackSafeHandle::
AsyncCallBackSafeHandle(
    IPrintAsyncNotifyCallback*   asyncCallBack
    ) : SafeHandle(IntPtr(asyncCallBack), true)
{
}

Boolean
AsyncCallBackSafeHandle::IsInvalid::
get(
    void
    )
{
    return (DangerousGetHandle() == IntPtr::Zero);
}

Boolean
AsyncCallBackSafeHandle::
ReleaseHandle(
    void
    )
{
    return ((reinterpret_cast<IPrintAsyncNotifyCallback*>(DangerousGetHandle().ToPointer()))->Release() == 0);
}
