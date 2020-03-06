// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __ASYNCNOTIFYUNMANAGED_HPP__
#define __ASYNCNOTIFYUNMANAGED_HPP__

/*++
                                                                              
    Copyright (C) 2002 - 2003 Microsoft Corporation                                   
    All rights reserved.                                                        
                                                                              
    Module Name:                                                                
        
        AsyncNotifyUnmanaged.hpp
                                                                              
    Abstract:
        
        Implements the managed/unmanaged interop for Async Notify LAPI
        
    Author:                                                                     
        
        Adina Trufinescu (adinatru) Septempber 04, 2003
                                                                             
    Revision History:                                                           
--*/

namespace System
{
namespace Printing
{
namespace AsyncNotify
{
    class AsyncNotifyBidiCallbackUnmanaged : public IPrintAsyncNotifyCallback
    {
        public:

        AsyncNotifyBidiCallbackUnmanaged(
            gcroot<BidirectionalAsynchronousNotificationsSubscription^> registration
            );

        //
        // IUnknown methods
        //
        STDMETHODIMP
        QueryInterface(
            REFIID  riid,
            VOID**  ppv
            );

        STDMETHODIMP_(ULONG)
        AddRef(
            VOID
            );

        STDMETHODIMP_(ULONG)
        Release(
            VOID
            );

        //
        // IAsyncNotifications methods
        //
        STDMETHODIMP
        OnEventNotify(
            IPrintAsyncNotifyChannel*       pIAsyncNotification,
            IPrintAsyncNotifyDataObject*    pNotification
            );

        STDMETHODIMP
        ChannelClosed(
            IPrintAsyncNotifyChannel*       pIAsyncNotification,
            IPrintAsyncNotifyDataObject*    pNotification
            ); 
        
        ~AsyncNotifyBidiCallbackUnmanaged(
            void
            );

        private:

        HRESULT                                                         m_hValid;
        LONG                                                            m_cRef;
        gcroot<BidirectionalAsynchronousNotificationsSubscription^>     registration;

    };


    class AsyncNotifyUnidiCallbackUnmanaged : public IPrintAsyncNotifyCallback
    {
        public:

        AsyncNotifyUnidiCallbackUnmanaged(
            gcroot<UnidirectionalAsynchronousNotificationsSubscription^>    registration
            );

        //
        // IUnknown methods
        //
        STDMETHODIMP
        QueryInterface(
            REFIID  riid,
            VOID**  ppv
            );

        STDMETHODIMP_(ULONG)
        AddRef(
            VOID
            );

        STDMETHODIMP_(ULONG)
        Release(
            VOID
            );

        //
        // IAsyncNotifications methods
        //
        STDMETHODIMP
        OnEventNotify(
            IPrintAsyncNotifyChannel*       pIAsyncNotification,
            IPrintAsyncNotifyDataObject*    pNotification
            );

        STDMETHODIMP
        ChannelClosed(
            IPrintAsyncNotifyChannel*       pIAsyncNotification,
            IPrintAsyncNotifyDataObject*    pNotification
            ); 
        
        private:

        ~AsyncNotifyUnidiCallbackUnmanaged(
            );  

        HRESULT                                                         m_hValid;
        LONG                                                            m_cRef;
        gcroot<UnidirectionalAsynchronousNotificationsSubscription^>    registration;

    };

    class AsyncNotifyDataObjectUnmanaged : public IPrintAsyncNotifyDataObject
    {
        public:

        AsyncNotifyDataObjectUnmanaged(
            AsyncNotificationData^          notification
            );

        //
        // IUnknown methods
        //
        STDMETHODIMP
        QueryInterface(
            REFIID  riid,
            VOID**  ppv
            );

        STDMETHODIMP_(ULONG)
        AddRef(
            VOID
            );

        STDMETHODIMP_(ULONG)
        Release(
            VOID
            );

        //
        // INotifyDataObject methods
        //
        STDMETHODIMP
        AcquireData(
            BYTE**                              ppData,
            ULONG*                              pDataSize,
            PrintAsyncNotificationType**        ppDataType
            );

        STDMETHODIMP
        ReleaseData(
            VOID
            );
        

        ~AsyncNotifyDataObjectUnmanaged(
            void
            );  

        private:

        BYTE*                           m_Data;
        ULONG                           m_Size;    
        PrintAsyncNotificationType*     m_Type;
        LONG                            m_cRef;        
    };




    private ref class ChannelSafeHandle : public System::Runtime::InteropServices::SafeHandle
    {
        public:

        ChannelSafeHandle(
            IPrintAsyncNotifyChannel*   channel
            );

        property
        Boolean
        IsInvalid
        {
            Boolean get();
        }

        Boolean
        ReleaseHandle(
            void
            );

        Boolean
        SendNotification(
            AsyncNotificationData^ managedNotification
            );

        Boolean
        CloseChannel(
            AsyncNotificationData^ managedNotification
            );
    
    };

    private ref class AsyncCallBackSafeHandle : public System::Runtime::InteropServices::SafeHandle
    {
        public:

        AsyncCallBackSafeHandle(
            IPrintAsyncNotifyCallback*   asyncCallBack
            );

        property
        Boolean
        IsInvalid
        {
            Boolean get();
        }

        Boolean
        ReleaseHandle(
            void
            );
    
    };




    private ref class RegistrationSafeHandle : public System::Runtime::InteropServices::SafeHandle
    {
        public:

        static
        HANDLE
        CreateUnmanagedRegistration(
            System::Printing::PrintSystemObject^                        printObject,
            System::Guid                                                                subscriptionDataType,
            System::Printing::AsyncNotify::UserNotificationFilter       subscriptionUserFilter,
            PrintAsyncNotifyConversationStyle                                           conversationStyle,
            AsyncCallBackSafeHandle^                                                    callBackHandle
            );

        RegistrationSafeHandle(
            System::Printing::PrintSystemObject^                        printObject,
            System::Guid                                                                subscriptionDataType,
            System::Printing::AsyncNotify::UserNotificationFilter       subscriptionUserFilter,
            PrintAsyncNotifyConversationStyle                                           conversationStyle,
            AsyncCallBackSafeHandle^                                                    callBackHandle
            );

        property
        Boolean
        IsInvalid
        {
            Boolean get();
        }

        Boolean
        ReleaseHandle(
            void
            );        
        
    };


}
}
}

#endif
