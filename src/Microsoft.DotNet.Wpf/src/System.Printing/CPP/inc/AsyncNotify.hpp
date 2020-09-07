// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __ASYNCNOTIFY_HPP__
#define __ASYNCNOTIFY_HPP__

/*++
                                                                              
    Copyright (C) 2002 - 2003 Microsoft Corporation                                   
    All rights reserved.                                                        
                                                                              
    Module Name:                                                                
        
        AsyncNotify.hpp
                                                                              
    Abstract:
        
        Managed Asynchronous notificiations object declarations

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
    /// <summary>
    /// Enumeration of per user filters that are supported by the Printing Asynchronous Notifications objects. 
    /// <list type="table">
    /// <item>
    /// <term>PerUserFilter</term>
    /// <description>
    /// A subscription using this value will receive notifications
    /// sent by the Print Spooler service while impersonating the same user account that
    /// was impersonated at the time the subscription was created.
    /// </description>
    /// </item>
    /// <item>
    /// <term>AllUsers</term>
    /// <description>
    /// A subscription using this value will receive notifications
    /// sent by the Print Spooler service regardless of the impersonation at the time
    /// the channel was created. The impersonated user at the time the subscription is created
    /// must have administrative permissions on the targeted print server.
    /// </description>
    /// </item>
    /// </list> 
    /// </summary>    
    /// <ExternalAPI/>    
    public enum class UserNotificationFilter
    {
        PerUserFilter = 1,      	
        AllUsers      = 2
    };

    /// <summary>
    /// Enumeration of the conversation styles that are supported by the Printing Asynchronous Notifications objects. 
    /// <list type="table">
    /// <item>
    /// <term>Unidirectional</term>
    /// <description>
    /// A subscription using this value will receive unidirectional notifications
    /// sent by the Print Spooler service.
    /// </description>
    /// </item>
    /// <item>
    /// <term>Bidirectional</term>
    /// <description>
    /// A subscription using this value will receive bidirectional notifications
    /// sent by the Print Spooler service.
    /// </description>
    /// </item>
    /// </list> 
    /// </summary>    
    /// <ExternalAPI/>
    public enum class ConversationStyle
    {
        Unidirectional = 1,      	
        Bidirectional  = 2
    };


    /// <summary>
    /// GUID that identifies the data schema that a asynchronous notification follows.
    /// </summary>
    /// <ExternalAPI/>    
    //typedef public System::Guid NotificationDataType;

    /// <summary>
    /// This class represents a printing asynchronous notification.
    /// </summary>
    /// <ExternalAPI/>        
    public ref class AsyncNotificationData : public IDisposable
    {
        public:

        /// <summary>
        /// Instantiates a <c>AsyncNotificationData</c> object representing a notification.
        /// </summary>        
        /// <param name="notificationDataStream"><c>Stream</c> object that holds the notification data.</param>
        /// <param name="notificationDataType">a <c>Guid</c> that is associated with the data schema.</param>
        /// <remarks>
        /// There is no validation of the data against the schema identified by the <c>notificationDataType</c>
        /// The publisher associates a Guid with the data schema. The data schema can be kept secret and shared only 
        /// with certain subscribers. A subscriber that knows about the existence of a Guid is assumed to be in possesion
        /// of the data schema that the notification follows. The Print Spooler service guarantees that a notification
        /// is always associted with a Guid.
        /// </remarks>
        AsyncNotificationData(
            Stream^                 notificationDataStream,
            System::Guid            notificationDataType
            );

        /// <value>
        /// Notification data type.
        /// </value>
        property 
        System::Guid
        DataType
        {
            System::Guid get();
        }

        /// <value>
        /// Notification data stream.
        /// </value> 
        property 
        Stream^
        DataStream
        {
            Stream^ get();
        }

        /// <summary>
        /// <c>AsyncNotificationData</c> destructor.
        /// </summary>
        ~AsyncNotificationData(
            void
            );

        /// <summary>
        /// Overloaded. Overridden. Converts the value of this instance to a String.
        /// </summary> 
        String^
        ToString(
            void
            )
        {
            return "AsyncNotifyChannel";
        }

        internal: 

        AsyncNotificationData(
            IPrintAsyncNotifyDataObject*    pNotification
            );

        private:

        void
        Dispose(
            bool disposing
            );

        Stream^                              dataStream;
        System::Guid                         dataType;
        bool                                 isDisposed;

    };

    /// <summary>
    /// This class represents a printing asynchronous channel.
    /// </summary>
    /// <ExternalAPI/> 
    public ref class AsyncNotifyChannel : public IDisposable
    {	
        public:

        /// <summary>
        /// Sends a notification.
        /// </summary>        
        /// <param name="notificationData"><c>AsyncNotificationData</c> object that holds the notification data and type.</param>
        bool
        Send(  
            AsyncNotificationData^      notificationData
            );

        /// <summary>
        /// Closes the channel and sends a notification that represents the reason for the closure.
        /// </summary>        
        /// <param name="notificationData"><c>AsyncNotificationData</c> object that holds the notification data and type.</param>
        bool
        Close(            
            AsyncNotificationData^      notificationData
            );

        /// <summary>
        /// Overloaded. Overridden. Converts the value of this instance to a String.
        /// </summary> 
        String^
        ToString(
            void
            )
        {
            return "AsyncNotifyChannel";
        }
    
        ~AsyncNotifyChannel(
            void
            );

        internal:

        static
        AsyncNotifyChannel^
        MapUnmanagedChannel(
            String^ channelGuid
            );
        
        AsyncNotifyChannel(
            IPrintAsyncNotifyChannel* 
            );
        
        void
        Dispose(
            bool disposing
            );

        private:

        static
        AsyncNotifyChannel(
            void
            )
        {
            channelMappingTable = gcnew Hashtable();
        }

        static
        Hashtable^                                                          channelMappingTable;

        bool                                                                isDisposed;
        ChannelSafeHandle^                                                  channelHandle;

    };

    /// <summary>
    /// This class represents the unidirectional notification event data object.
    /// </summary>
    /// <ExternalAPI/>     
    public ref class UnidirectionalNotificationEventArgs : public EventArgs
    {
        public:

        /// <value>
        /// Notification object.
        /// </value>
        property
        AsyncNotificationData^
        Notification
        {
            AsyncNotificationData^ get();
        }

        /// <summary>
        /// Overloaded. Overridden. Converts the value of this instance to a String.
        /// </summary> 
        String^
        ToString(
            void
            )
        {
            return "UnidirectionalNotificationEventArgs";
        }

        internal:

        UnidirectionalNotificationEventArgs(
            AsyncNotificationData^          notification
            );

        private:

        AsyncNotificationData^              notification;
    };


    /// <summary>
    /// This class represents the bidirectional notification event data object.
    /// </summary>
    /// <ExternalAPI/>  
    public ref class BidirectionalNotificationEventArgs : public EventArgs
    {
        public:

        /// <value>
        /// Notification channel.
        /// </value>
        property
        AsyncNotifyChannel^
        Channel
        {
            AsyncNotifyChannel^ get();
        }

        /// <value>
        /// Notification object.
        /// </value>
        property
        AsyncNotificationData^
        Notification
        {
            AsyncNotificationData^ get();
        }

        /// <value>
        /// Indicates that the channel was closed and this is the last notification.
        /// </value>
        property 
        Boolean
        IsChannelClosed
        {
            Boolean get();
        }

        /// <summary>
        /// Overloaded. Overridden. Converts the value of this instance to a String.
        /// </summary> 
        String^
        ToString(
            void
            )
        {
            return "BidirectionalNotificationEventArgs";
        }

        internal:

        BidirectionalNotificationEventArgs(
            AsyncNotifyChannel^         channel,
            AsyncNotificationData^      notification,
            Boolean                     isClosed                        
            );

    private:

        AsyncNotifyChannel^         channel;
        AsyncNotificationData^      notification;
        Boolean                     isClosed;


    };

    /// <summary>
    /// This class represents the subscription for bidirectional notifications
    /// sent by the Print Spooler object for a given <c>publisher</c>
    /// </summary>
    /// <ExternalAPI/>  
    public ref class AsynchronousNotificationsSubscription abstract : public IDisposable
    {	

        protected:
        
        /// <summary>
        /// Instantiates a subscription for asynchronous notifications.
        /// <param name="publisher"><c>PrintSystemObject</c> object the subscription is made for.</param>
        /// <param name="notificationDataType">Specifies the GUID that identifies the data schema that the subscriber recognizes.</param>
        /// <param name="perUserNotificationFilter">Specifies whether to receive notifications form any user or only the user that is curently impersonated.</param>
        /// </summary> 
        AsynchronousNotificationsSubscription(
            PrintSystemObject^              publisher,          
            System::Guid                    notificationDataType,
            UserNotificationFilter          perUserNotificationFilter
            );

        public:

        ~AsynchronousNotificationsSubscription(
            void
            );

        /// <value>
        /// <c>PrintSystemObject</c> object that the subscription is made for.
        /// </value>
        property 
        PrintSystemObject^
        PublisherPrintSystemObject
        {
            PrintSystemObject^ get();
        }

        /// <value>
        /// <c>UserNotificationFilter</c> object that the subscription is made with.
        /// </value>
        property
        UserNotificationFilter
        PerUserNotificationFilter
        {
            UserNotificationFilter get();
        }

        /// <value>
        /// <c>System::Guid</c> that the subscription is made with.
        /// </value>
        property 
        System::Guid
        NotificationDataType
        {
            System::Guid get();
        }

        /// <summary>
        /// Overloaded. Overridden. Converts the value of this instance to a String.
        /// </summary> 
        String^
        ToString(
            void
            )
        {
            return "AsynchronousNotificationsSubscription";
        }

        internal:

        static
        AsynchronousNotificationsSubscription^
        CreateSubscription(
            System::Printing::PrintSystemObject^                    publisher,
            ConversationStyle                                                       conversationStyle,
            System::Guid                                                            notificationDataType,
            System::Printing::AsyncNotify::UserNotificationFilter   perUserNotificationFilter
            );

        // FIX: remove pragma. done to fix compiler error which will be fixed later.
        #pragma warning ( disable:4376 )
        property
        AsyncCallBackSafeHandle^
        AsyncCallBackHandler
        {
            AsyncCallBackSafeHandle^ get();
            void set(AsyncCallBackSafeHandle^ callBackHandler);
        }

        property
        RegistrationSafeHandle^
        RegistrationHandler
        {
            void set(RegistrationSafeHandle^     registrationHandler);
        }

        #pragma warning ( default:4376 )
        
        protected:

        property 
        bool
        IsDisposed
        {
            bool get();
        }

        virtual
        void
        Dispose(
            bool disposing
            );

        private:

        RegistrationSafeHandle^                                                 registrationHandler;
        AsyncCallBackSafeHandle^                                                callBackHandler;
        
        bool                                                                    isDisposed;
        System::Printing::PrintSystemObject^                    printSystemObject;
        System::Guid     notificationDataType;
        System::Printing::AsyncNotify::UserNotificationFilter   perUserNotificationFilter;

    };

    public ref class BidirectionalAsynchronousNotificationsSubscription : 
    public AsynchronousNotificationsSubscription
    {	

        public:
        
        /// <summary>
        /// Delegate event handler for bidirectional notifications.
        /// </summary> 
        delegate
        void
        NotifyOnBidirectionalNotificationEventHandler(
            Object^                                                sender, // used to be BidirectionalAsynchronousNotificationsSubscription* FxCop
            BidirectionalNotificationEventArgs^                    e
            );

        event 
        NotifyOnBidirectionalNotificationEventHandler^           BidirectionalNotificationArrived;

        /// <summary>
        /// Overloaded. Overridden. Converts the value of this instance to a String.
        /// </summary> 
        String^
        ToString(
            void
            )
        {
            return "BidirectionalAsynchronousNotificationsSubscription";
        }
        

        internal:

        BidirectionalAsynchronousNotificationsSubscription(
            System::Printing::PrintSystemObject^                    publisher,
            System::Guid                                                            notificationDataType,
            System::Printing::AsyncNotify::UserNotificationFilter   perUserNotificationFilter
            );

        void
        OnEventNotify(
            AsyncNotifyChannel^     channel,
            AsyncNotificationData^  notification
            );

        void
        OnChannelClosed(
            AsyncNotifyChannel^     channel,
            AsyncNotificationData^  notification
            );

        protected:

        void
        OnBidirectionalNotificationArrived(
            BidirectionalNotificationEventArgs^ e
            );


        void
        Dispose(
            bool disposing
            );

        ~BidirectionalAsynchronousNotificationsSubscription();
    };

    public ref class UnidirectionalAsynchronousNotificationsSubscription : 
    public AsynchronousNotificationsSubscription
    {	
        public:
        
        /// <summary>
        /// Delegate event handler for unidirectional notifications.
        /// </summary> 
        delegate
        void
        NotifyOnUnidirectionalNotificationEventHandler(
            Object^                                                        sender, 
            UnidirectionalNotificationEventArgs^                           e
            );

        event 
        NotifyOnUnidirectionalNotificationEventHandler^           UnidirectionalNotificationArrived;

        /// <summary>
        /// Overloaded. Overridden. Converts the value of this instance to a String.
        /// </summary> 
        String^
        ToString(
            void
            )
        {
            return "UnidirectionalAsynchronousNotificationsSubscription";
        }
        
        internal:

        UnidirectionalAsynchronousNotificationsSubscription(
            System::Printing::PrintSystemObject^                    publisher,
            System::Guid                                                            notificationDataType,
            System::Printing::AsyncNotify::UserNotificationFilter   perUserNotificationFilter
            );

        internal:

        void
        OnNewUnidirectionalNotification(
            AsyncNotificationData^      notificationData
            );

        protected:

        void
        OnUnidirectionalNotificationArrived(
            UnidirectionalNotificationEventArgs^ e
            );

        void
        Dispose(
            bool disposing
            );

        ~UnidirectionalAsynchronousNotificationsSubscription();

    };
    
}
}
}

#endif
