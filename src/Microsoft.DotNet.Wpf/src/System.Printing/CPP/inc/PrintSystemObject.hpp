// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTSYSTEMOBJECT_HPP__
#define __PRINTSYSTEMOBJECT_HPP__

namespace System
{
namespace Printing
{
    using namespace System::Printing::IndexedProperties;

    public enum class PrintSystemObjectLoadMode
    {
        None              = 0,
        LoadUninitialized = 1,
        LoadInitialized   = 2
    };

    /// <summary>
    /// Abstract base class for all object in the Print system.
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintSystemObject abstract : 
    public Object, public IDisposable
    {
        public:

        /// <summary>
        /// PrintSystemObject destructor.
        /// </summary>        
        ~PrintSystemObject(
            void
            );
        
        /// <summary>
        /// Collection of attribute-value objects that represent the properties of this object.
        /// </summary>
        property
        PrintPropertyDictionary^
        PropertiesCollection
        {
            public:
                PrintPropertyDictionary^ get();
            internal:
                void set(PrintPropertyDictionary^ collection);
        }

        /// <summary>
        /// Commits the attribute values to the Spooler service.
        /// </summary>        
        virtual 
        void
        Commit(
            void
            ) = 0;

        /// <summary>
        /// Refreshes the attribute values with data from the Spooler service.
        /// </summary>        
        virtual 
        void
        Refresh(
            void
            ) = 0;

        /// <summary>
        /// Name identifier of this object.
        /// </summary>
        property
        virtual
        String^
        Name
        {
            public:
                String^ get();
            internal:
                void set(String^ objName);
        }
        
        /// <summary>
        /// Parent of this object.
        /// </summary>        
        property
        virtual
        PrintSystemObject^
        Parent
        {
            PrintSystemObject^ get();
        }

        /*AsynchronousNotificationsSubscription^
        SubscribeForAsynchronousNotifications(
            ConversationStyle        conversationStyle,
            System::Guid             notificationDataType,
            UserNotificationFilter   perUserNotificationFilter
            );*/

        internal:

        /// <summary>
        /// Synchronization root for this object.
        /// </summary>
        property
        Object^
        SyncRoot
        {
            Object^ get();
        }

        delegate
        void
        PropertyChanged(
            Object^                                    sender,
            PrintSystemObjectPropertyChangedEventArgs^ e
            );

        delegate 
        void
        PropertiesChanged(
            PrintSystemObject^                           sender,
            PrintSystemObjectPropertiesChangedEventArgs^ e
            );

        delegate
        PrintSystemObject^
        Instantiate(
            array<String^>^ propertiesFilter
            );

        delegate
        PrintSystemObject^
        InstantiateOptimized(
            Object^ object,
            array<String^>^ propertiesFilter
            );

        delegate
        PrintProperty^
        CreateWithNoValue(
            String^ attributeName
            );

        delegate
        PrintProperty^
        CreateWithValue(
            String^ attributeName,
            Object^ attributeValue
            );

        delegate
        PrintProperty^
        CreateWithNoValueLinked(
            String^             attributeName,
            MulticastDelegate^  delegate
            );

        delegate
        PrintProperty^
        CreateWithValueLinked(
            String^             attributeName,
            Object^             attributeValue,
            MulticastDelegate^  delegate
            );

        virtual
        PrintPropertyDictionary^
        get_InternalPropertiesCollection(
            String^ attributeName
            ) = 0;
      
        protected:

        /// <summary>
        /// PrintSystemObject constructor.
        /// </summary> 
        PrintSystemObject(
            void
            );

        PrintSystemObject(
            PrintSystemObjectLoadMode mode
            );

        /// <summary>
        /// PrintSystemObject Finalizer.
        /// </summary>     
        !PrintSystemObject(
            void
            );

        internal:
        /// <summary>
        /// Implements the PropertyChanged delegate that is being fired when 
        /// one property in the attribute value collection change.
        /// </summary>    
        /// <param name="sender">Name of the printer to be installed.</param>
        /// <param name="e">Event data <see cref="PrintSystemObjectPropertyChangedEventArgs"/>.</param>
        virtual
        void
        OnPropertyChanged(
            PrintSystemObject^                          sender,
            PrintSystemObjectPropertyChangedEventArgs^  e
            );

        /// <summary>
        /// Implements the PropertiesChanged delegate that is being fired when one 
        /// or more properties in the attribute value collection change.
        /// </summary>
        /// <param name="sender">Name of the printer to be installed.</param>
        /// <param name="e">Event data <see cref="PrintSystemObjectPropertyChangedEventArgs"/>.</param>
        virtual 
        void
        OnPropertiesChanged(
            PrintSystemObject^                            sender,
            PrintSystemObjectPropertiesChangedEventArgs^  e
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            );

        property
        virtual
        bool
        IsDisposed
        {
            bool get() sealed;
            void set(bool disposingStatus) sealed;
        }

        /// <summary>
        /// Initializes the attribute value collection of properties covered by this type.
        /// </summary>        
        void
        Initialize(
            void
            );

        private:

        array<MulticastDelegate^>^
        CreatePropertiesDelegates(
            void
            );

        bool                                    isDisposed;
        PrintPropertyDictionary^                propertiesCollection;
        PrintSystemObject^                      parent; 
        String^                                 name;
        Object^                                 syncRoot;        

        //
        // The following is the necessary data members to link the 
        // compile time properties with the named properties in the
        // associated collection
        //

        protected:
        
        /// <summary>
        /// Returns the names of attributes covered by this class.
        /// </summary>        	
        static
        array<String^>^
        BaseAttributeNames(
            void)
        {
            return baseAttributeNames;
        }

        internal:

        static
        void
        RegisterAttributesNamesTypes(
            Hashtable^ attributeNamesTypes
            );

        private:

        static 
        PrintSystemObject(
            void
            )
        {
        }

        static array<String^>^ baseAttributeNames = 
        {
            L"Name"
        };

        static array<Type^>^ baseAttributeTypes = 
        {
            String::typeid
        };

        internal:

        static
        const  Int32  MaxPath = MAX_PATH;
    };

    public ref class PrintSystemObjects abstract
    {
        public:

        ~PrintSystemObjects(
            void
            );

        internal:

        /*virtual
        void
        Add(
            PrintSystemObject^ printObject
            ) = 0;*/

        protected:

        PrintSystemObjects(
            void
            );
    };

    private ref class PrintSystemDispatcherObject:System::Windows::Threading::DispatcherObject
    {
        public:

        PrintSystemDispatcherObject(
            void
            ):
            System::Windows::Threading::DispatcherObject()
        {

        }

        void
        VerifyThreadLocality(
            void
            )
        {
            VerifyAccess();
        }
    };
}
}

#endif
