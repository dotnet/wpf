// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTSYSTEMATTRIBUTEVALUE_HPP__
#define __PRINTSYSTEMATTRIBUTEVALUE_HPP__

namespace System
{
namespace Printing
{
namespace IndexedProperties
{
    /// <summary>
    /// PrintProperty object.
    /// This is the class that abstracts a property-value pair and allows representing 
    //  any type that the Print System supports through a common interface.
    /// </summary>
    /// <ExternalAPI/>    
    [System::Serializable]
    public ref class PrintProperty abstract : 
    public IDisposable, 
    public System::Runtime::Serialization::IDeserializationCallback
    {
        public:

        /// <summary>
        /// PrintProperty destructor.
        /// </summary>            
        ~PrintProperty(
            );

        /// <value>
        /// Name identifier of this object.
        /// </value>    
        /// <remarks>
        /// Inherited from PrintSystemObject.
        /// </remarks>
        property 
        virtual
        String^
        Name
        {
            String^ get();
        }

        /// <value>
        /// Object representing the value of the property-value pair  represented by this object.
        /// </value>
        property
        virtual
        Object^
        Value
        {
            Object^ get() = 0;
            void set(Object^ objValue) = 0;
        }

        property
        bool
        IsInitialized
        {
            public protected:
                bool get();

            protected:
                void set(bool setInitialized);
        }

        virtual void
        OnDeserialization(
            Object^ sender
            );

        internal:

        delegate
        PrintProperty^
        CreateWithNoValue(
            String^  attributeName
            );

        delegate
        PrintProperty^
        CreateWithValue(
            String^  attributeName,
            Object^  attributeValue
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

        // FIX: remove pragma. done to fix compiler error which will be fixed later.
        #pragma warning ( disable:4376 )

        property
        bool
        IsInternallyInitialized
        {
            bool get();
            void set(bool);
        }

        property
        bool
        IsDirty
        {
            bool get();
            void set(bool setDirty);
        }

        property
        bool
        IsLinked
        {
            bool get();
            void set(bool setLinked);
        }

        #pragma warning ( default:4376 )

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            );

        /// <summary>
        /// PrintProperty finalizer.
        /// </summary>            
        !PrintProperty(
            );

        property
        bool
        IsDisposed
        {
            bool get();
            void set(bool disposing);
        }

        /// <summary>
        /// Initialize the instance of the base PrintProperty class. 
        /// </summary>
        /// <param name="attributeName"> Attribute name. </param>
        PrintProperty(
            String^ attributeName
            );

        private:

        String^ propertyName;
        Object^ syncRoot;
        bool    isDirty;
        bool    isDisposed;
        bool    isInitialized;
        bool    isInternallyInitialized;
        bool    isLinked;
    };


    /// <summary>
    /// PrintInt32Property object.
    /// This is the class that represents a property-numeric value pair.
    /// </summary>
    /// <ExternalAPI/>    
    public ref class PrintInt32Property sealed : 
    public PrintProperty
    {
        public:

        PrintInt32Property(
            String^ attributeName
            );

        PrintInt32Property(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintInt32Property(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintInt32Property(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;

            virtual void set(Object^ objValue) override;
        }

        internal:

        // FIX: remove pragma. done to fix compiler error which will be fixed later.
        #pragma warning ( disable:4376 )
        
        property
        PrintSystemDelegates::Int32ValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::Int32ValueChanged^ get();
            void set(PrintSystemDelegates::Int32ValueChanged^ newHandler);
        }
        
        public:


        static
        operator
        Int32(
            PrintInt32Property % attribRef
            );


        static
        operator
        Int32(
            PrintInt32Property ^ attribRef
            );

        internal:

        static
        Int32^
        ToInt32(
            PrintInt32Property % attribRef
            );
        
        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        PrintSystemDelegates::Int32ValueChanged^     changeHandler;       
        Int32                                        value;
    };

    
    /// <summary>
    /// PrintStringProperty object.
    /// This is the class that represents a property-string value pair.
    /// </summary>
    /// <ExternalAPI/>
    [System::Serializable]
    public ref class PrintStringProperty sealed : 
    public PrintProperty
    {
        public:

        PrintStringProperty(
            String^ attributeName
            );

        PrintStringProperty(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintStringProperty(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintStringProperty(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;
            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::StringValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::StringValueChanged^ get();
            void set(PrintSystemDelegates::StringValueChanged^ newHandler);
        }
        
        public:

        static
        operator
        String^(
            PrintStringProperty^ attributeRef
            );

        static
        operator
        String^(
            PrintStringProperty% attributeRef
            );


        internal: 

        static
        String^
        ToString(
            PrintStringProperty % attributeRef
            );
        
        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;
        
        private:

        [NonSerialized]
        PrintSystemDelegates::StringValueChanged^    changeHandler;       
        String^                                      value;
    };


    /// <summary>
    /// PrintStreamProperty object.
    /// This is the class that represents a property-stream value pair.
    /// </summary>
    /// <ExternalAPI/>
    [System::Serializable]
    public ref class PrintStreamProperty sealed :
    public PrintProperty
    {
        public:

        PrintStreamProperty(
            String^ attributeName
            );

        PrintStreamProperty(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintStreamProperty(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintStreamProperty(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;
            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::StreamValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::StreamValueChanged^ get();
            void set(PrintSystemDelegates::StreamValueChanged^ newHandler);
        }
        
        public:

        static
        operator
        Stream^(
            PrintStreamProperty % attributeRef
            );

        static
        operator
        Stream^(
            PrintStreamProperty^ attributeRef
            );

        internal:

        static
        Stream^
        ToStream(
            PrintStreamProperty % attributeRef
            );
        
        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        [NonSerialized]
        PrintSystemDelegates::StreamValueChanged^    changeHandler;       
        Stream^                                      value;
    };

    /// <summary>
    /// PrintQueueAttributeProperty object.
    /// This is the class that represents a property - <c>PrintQueueAttributes</c> value pair.
    /// <see cref="PrintQueueAttributes"/>
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintQueueAttributeProperty sealed : 
    public PrintProperty
    {
        public:

        PrintQueueAttributeProperty(
            String^ attributeName
            );

        PrintQueueAttributeProperty(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintQueueAttributeProperty(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintQueueAttributeProperty(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;
            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::PrintQueueAttributePropertyChanged^
        ChangeHandler
        {
            PrintSystemDelegates::PrintQueueAttributePropertyChanged^ get();
            void set(PrintSystemDelegates::PrintQueueAttributePropertyChanged^ newHandler);
        }

        public:


        static
        operator
        PrintQueueAttributes(
            PrintQueueAttributeProperty % attributeRef
            );


        static
        operator
        PrintQueueAttributes(
            PrintQueueAttributeProperty^ attributeRef
            );

        internal:

        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        PrintSystemDelegates::PrintQueueAttributePropertyChanged^   changeHandler;       
        PrintQueueAttributes                                        value;
    };

    /// <summary>
    /// PrintQueueStatusProperty object.
    /// This is the class that represents a property-<c>PrintQueueStatus</c> value pair.
    /// <see cref="PrintQueueStatus"/>
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintQueueStatusProperty sealed : 
    public PrintProperty
    {
        public:

        PrintQueueStatusProperty(
            String^ attributeName
            );

        PrintQueueStatusProperty(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintQueueStatusProperty(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintQueueStatusProperty(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;
            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::PrintQueueStatusValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::PrintQueueStatusValueChanged^ get();
            void set(PrintSystemDelegates::PrintQueueStatusValueChanged^ newHandler);
        }

        public:

        static
        operator
        PrintQueueStatus(
            PrintQueueStatusProperty^ attributeRef
            );


        static
        operator
        PrintQueueStatus(
            PrintQueueStatusProperty % attributeRef
            );


        internal:

        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        PrintSystemDelegates::PrintQueueStatusValueChanged^     changeHandler;       
        PrintQueueStatus                                        value;
    };

    /// <summary>
    /// PrintBooleanProperty object.
    /// This is the class that represents a property-<c>Boolean</c> value pair.
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintBooleanProperty sealed : 
    public PrintProperty
    {
        public:

        PrintBooleanProperty(
            String^ attributeName
            );

        PrintBooleanProperty(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintBooleanProperty(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintBooleanProperty(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;

            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::BooleanValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::BooleanValueChanged^ get();
            void set(PrintSystemDelegates::BooleanValueChanged^ newHandler);
        }

        public:


        static
        operator
        Boolean(
            PrintBooleanProperty % attribRef
            );


        static
        operator
        Boolean(
            PrintBooleanProperty^ attribRef
            );

        internal:

        static
        Boolean^
        ToBoolean(
            PrintBooleanProperty % attribRef
            );
        
        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        PrintSystemDelegates::BooleanValueChanged^     changeHandler;       
        Boolean                                        value;
    };

    /// <summary>
    /// PrintThreadPriorityProperty object.
    /// This is the class that represents a property-<c>ThreadPriority</c> value pair.
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintThreadPriorityProperty sealed : 
    public PrintProperty
    {
        public:

        PrintThreadPriorityProperty(
            String^ attributeName
            );

        PrintThreadPriorityProperty(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintThreadPriorityProperty(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintThreadPriorityProperty(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;

            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::ThreadPriorityValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::ThreadPriorityValueChanged^ get();
            void set(PrintSystemDelegates::ThreadPriorityValueChanged^ newHandler);
        }

        public:


        static
        operator
        System::Threading::ThreadPriority(
            PrintThreadPriorityProperty % attribRef
            );


        static
        operator
        System::Threading::ThreadPriority(
            PrintThreadPriorityProperty^ attribRef
            );

        internal:

        static
        System::Threading::ThreadPriority^
        ToThreadPriority(
            PrintThreadPriorityProperty % attribRef
            );

        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        PrintSystemDelegates::ThreadPriorityValueChanged^     changeHandler;       
        System::Threading::ThreadPriority                     value;
    };

    /// <summary>
    /// PrintServerLoggingProperty object.
    /// This is the class that represents a property-<c>PrintServerEventLoggingTypes</c> value pair.
    /// <see cref="PrintServerEventLoggingTypes"/>    
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintServerLoggingProperty sealed : 
    public PrintProperty
    {
        public:

        PrintServerLoggingProperty(
            String^ attributeName
            );

        PrintServerLoggingProperty(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintServerLoggingProperty(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintServerLoggingProperty(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;

            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::PrintServerEventLoggingValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::PrintServerEventLoggingValueChanged^ get();
            void set(PrintSystemDelegates::PrintServerEventLoggingValueChanged^ newHandler);
        }

        public:


        static
        operator
        PrintServerEventLoggingTypes(
            PrintServerLoggingProperty % attribRef
            );


        static
        operator
        PrintServerEventLoggingTypes(
            PrintServerLoggingProperty^ attribRef
            );

        internal:

        static
        PrintServerEventLoggingTypes^
        ToPrintServerEventLoggingTypes(
            PrintServerLoggingProperty % attribRef
            );

        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        PrintSystemDelegates::PrintServerEventLoggingValueChanged^     changeHandler;       
        PrintServerEventLoggingTypes                                   value;
    };


    /// <summary>
    /// PrintDriverProperty object.
    /// This is the class that represents a property-<c>Driver</c> value pair.
    /// <see cref="Driver"/>    
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintDriverProperty sealed : 
    public PrintProperty
    {
        public:

        PrintDriverProperty(
            String^ attributeName
            );

        PrintDriverProperty(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintDriverProperty(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintDriverProperty(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;
            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::DriverValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::DriverValueChanged^ get();
            void set(PrintSystemDelegates::DriverValueChanged^ newHandler);
        }

        public:


        static
        operator
        PrintDriver^(
            PrintDriverProperty % attribRef
            );


        static
        operator
        PrintDriver^(
            PrintDriverProperty^ attribRef
            );

        internal:

        static
        PrintDriver^
        ToPrintDriver(
            PrintDriverProperty % attribRef
            );
        
        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        PrintSystemDelegates::DriverValueChanged^     changeHandler;       
        PrintDriver^                                  value;
    };

    /// <summary>
    /// PrintPortProperty object.
    /// This is the class that represents a property-<c>Port</c> value pair.
    /// <see cref="Port"/>    
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintPortProperty sealed : 
    public PrintProperty
    {
        public:

        PrintPortProperty(
            String^ attributeName
            );

        PrintPortProperty(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintPortProperty(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintPortProperty(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;
            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::PortValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::PortValueChanged^ get();
            void set(PrintSystemDelegates::PortValueChanged^ newHandler);
        }

        public:


        static
        operator
        PrintPort^(
            PrintPortProperty % attribRef
            );


        static
        operator
        PrintPort^(
            PrintPortProperty^ attribRef
            );

        internal:

        static
        PrintPort^
        ToPrintPort(
            PrintPortProperty % attribRef
            );
        
        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        PrintSystemDelegates::PortValueChanged^     changeHandler;       
        PrintPort^                                value;
    };

    /// <summary>
    /// PrintServerProperty object.
    /// This is the class that represents a property-<c>PrintServer</c> value pair.
    /// <see cref="PrintServer"/>    
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintServerProperty sealed : 
    public PrintProperty
    {
        public:

        PrintServerProperty(
            String^ attributeName
            );

        PrintServerProperty(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintServerProperty(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintServerProperty(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;
            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::PrintServerValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::PrintServerValueChanged^ get();
            void set(PrintSystemDelegates::PrintServerValueChanged^ newHandler);
        }

        public:


        static
        operator
        PrintServer^(
            PrintServerProperty % attribRef
            );


        static
        operator
        PrintServer^(
            PrintServerProperty^ attribRef
            );

        internal:

        static
        PrintServer^
        ToPrintServer(
            PrintServerProperty % attribRef
            );
        
        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        PrintSystemDelegates::PrintServerValueChanged^     changeHandler;       
        PrintServer^                                       value;
    };

    /// <summary>
    /// PrintTicketProperty object.
    /// This is the class that represents a property-<c>PrintTicket</c> value pair.
    /// <see cref="PrintTicket"/>    
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintTicketProperty sealed : 
    public PrintProperty
    {
        public:

        PrintTicketProperty(
            String^ attributeName
            );

        PrintTicketProperty(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintTicketProperty(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintTicketProperty(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;

            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::PrintTicketValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::PrintTicketValueChanged^ get();

            void set(PrintSystemDelegates::PrintTicketValueChanged^ newHandler);
        }

        public:


        static
        operator
        PrintTicket^(
            PrintTicketProperty % attribRef
            );


        static
        operator
        PrintTicket^(
            PrintTicketProperty^ attribRef
            );

        internal:

        static
        PrintTicket^
        ToPrintTicket(
            PrintTicketProperty % attribRef
            );
        
        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        PrintSystemDelegates::PrintTicketValueChanged^     changeHandler;       
        
        PrintTicket^                                       value;
    };

    /// <summary>
    /// PrintByteArrayProperty object.
    /// This is the class that represents a property-<c>Byte[]</c> value pair.
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintByteArrayProperty sealed : 
    public PrintProperty
    {
        public:

        PrintByteArrayProperty(
            String^ attributeName
            );

        PrintByteArrayProperty(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintByteArrayProperty(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintByteArrayProperty(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;
            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::ByteArrayValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::ByteArrayValueChanged^ get();
            void set(PrintSystemDelegates::ByteArrayValueChanged^ newHandler);
        }

        public:


        static
        operator
        array<Byte>^(
            PrintByteArrayProperty % attribRef
            );


        static
        operator
        array<Byte>^(
            PrintByteArrayProperty^ attribRef
            );

        internal:

        static
        array<Byte>^
        ToByteArray(
            PrintByteArrayProperty % attribRef
            );
        
        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        PrintSystemDelegates::ByteArrayValueChanged^   changeHandler;       
        array<Byte>^                                   value;
    };

    /// <summary>
    /// PrintProcessorProperty object.
    /// This is the class that represents a property-<c>PrintProcessor</c> value pair.
    /// <see cref="PrintProcessor"/>    
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintProcessorProperty sealed : 
    public PrintProperty
    {
        public:

        PrintProcessorProperty(
            String^ attributeName
            );

        PrintProcessorProperty(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintProcessorProperty(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintProcessorProperty(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;
            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::PrintProcessorValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::PrintProcessorValueChanged^ get();
            void set(PrintSystemDelegates::PrintProcessorValueChanged^ newHandler);
        }

        public:


        static
        operator
        PrintProcessor^(
            PrintProcessorProperty % attribRef
            );


        static
        operator
        PrintProcessor^(
            PrintProcessorProperty^ attribRef
            );

        internal:

        static
        PrintProcessor^
        ToPrintProcessor(
            PrintProcessorProperty % attribRef
            );
        
        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        PrintSystemDelegates::PrintProcessorValueChanged^     changeHandler;       
        PrintProcessor^                                       value;
    };

    /// <summary>
    /// PrintQueueProperty object.
    /// This is the class that represents a property-<c>PrintQueue</c> value pair.
    /// <see cref="PrintQueue"/>    
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintQueueProperty sealed : 
    public PrintProperty
    {
        public:

        PrintQueueProperty(
            String^ attributeName
            );

        PrintQueueProperty(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintQueueProperty(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintQueueProperty(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;
            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::PrintQueueValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::PrintQueueValueChanged^ get();
            void set(PrintSystemDelegates::PrintQueueValueChanged^ newHandler);
        }

        public:


        static
        operator
        PrintQueue^(
            PrintQueueProperty % attribRef
            );


        static
        operator
        PrintQueue^(
            PrintQueueProperty^ attribRef
            );

        internal:

        static
        PrintQueue^
        ToPrintQueue(
            PrintQueueProperty % attribRef
            );
        
        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        PrintSystemDelegates::PrintQueueValueChanged^     changeHandler;       
        PrintQueue^                                       value;
    };

    /// <summary>
    /// PrintJobPriorityProperty object.
    /// This is the class that represents a property-<c>PrintJobPriority</c> value pair.
    /// <see cref="PrintQueue"/>    
    /// </summary>
    /// <ExternalAPI/>
    [System::Serializable]
    public ref class PrintJobPriorityProperty sealed : 
    public PrintProperty
    {
        public:

        PrintJobPriorityProperty(
            String^ attributeName
            );

        PrintJobPriorityProperty(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintJobPriorityProperty(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintJobPriorityProperty(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;
            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::JobPriorityValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::JobPriorityValueChanged^ get();
            void set(PrintSystemDelegates::JobPriorityValueChanged^ newHandler);
        }

        public:


        static
        operator
        PrintJobPriority(
            PrintJobPriorityProperty % attribRef
            );


        static
        operator
        PrintJobPriority(
            PrintJobPriorityProperty^ attribRef
            );

        internal:

        static
        PrintJobPriority^
        ToPrintJobPriority(
            PrintJobPriorityProperty % attribRef
            );
        
        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        [NonSerialized]
        PrintSystemDelegates::JobPriorityValueChanged^     changeHandler;       
        PrintJobPriority                                   value;
    };

    /// <summary>
    /// PrintSystemJobTypeAttributeValue object.
    /// PrintJobStatusProperty object.
    /// This is the class that represents a property-<c>PrintJobType</c> value pair.
    /// <see cref="PrintQueue"/>    
    /// </summary>
    /// <ExternalAPI/>
    [System::Serializable]
    private ref class PrintSystemJobTypeAttributeValue sealed : 
    public PrintProperty
    {
        public:

        PrintSystemJobTypeAttributeValue(
            String^ attributeName
            );

        PrintSystemJobTypeAttributeValue(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintSystemJobTypeAttributeValue(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintSystemJobTypeAttributeValue(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;
            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::JobTypeValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::JobTypeValueChanged^ get();
            void set(PrintSystemDelegates::JobTypeValueChanged^ newHandler);
        }

        public:


        static
        operator
        PrintJobType(
            PrintSystemJobTypeAttributeValue % attribRef
            );


        static
        operator
        PrintJobType(
            PrintSystemJobTypeAttributeValue^ attribRef
            );

        internal:

        static
        PrintJobType^
        ToPrintJobType(
            PrintSystemJobTypeAttributeValue % attribRef
            );
        
        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        [NonSerialized]
        PrintSystemDelegates::JobTypeValueChanged^          changeHandler;       
        PrintJobType                                        value;
    };


    /// <summary>
    /// PrintJobStatusProperty object.
    /// This is the class that represents a property-<c>PrintJobStatus</c> value pair.
    /// <see cref="PrintQueue"/>    
    /// </summary>
    /// <ExternalAPI/>
    [System::Serializable]
    public ref class PrintJobStatusProperty sealed : 
    public PrintProperty
    {
        public:

        PrintJobStatusProperty(
            String^ attributeName
            );

        PrintJobStatusProperty(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintJobStatusProperty(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintJobStatusProperty(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;
            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::JobStatusValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::JobStatusValueChanged^ get();
            void set(PrintSystemDelegates::JobStatusValueChanged^ newHandler);
        }

        public:


        static
        operator
        PrintJobStatus(
            PrintJobStatusProperty % attribRef
            );


        static
        operator
        PrintJobStatus(
            PrintJobStatusProperty^ attribRef
            );

        internal:

        static
        PrintJobStatus^
        ToPrintJobStatus(
            PrintJobStatusProperty % attribRef
            );

        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        [NonSerialized]
        PrintSystemDelegates::JobStatusValueChanged^       changeHandler;       
        PrintJobStatus                                      value;
    };

    /// <summary>
    /// PrintJobPriorityProperty object.
    /// This is the class that represents a property-<c>PrintJobPriority</c> value pair.
    /// <see cref="PrintQueue"/>    
    /// </summary>
    /// <ExternalAPI/>
    [System::Serializable]
    public ref class PrintDateTimeProperty sealed : 
    public PrintProperty
    {
        public:

        PrintDateTimeProperty(
            String^ attributeName
            );

        PrintDateTimeProperty(
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintDateTimeProperty(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintDateTimeProperty(
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;
            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::SystemDateTimeValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::SystemDateTimeValueChanged^ get();
            void set(PrintSystemDelegates::SystemDateTimeValueChanged^ newHandler);
        }

        public:


        static
        operator
        System::DateTime^(
            PrintDateTimeProperty % attribRef
            );


        static
        operator
        System::DateTime^(
            PrintDateTimeProperty^ attribRef
            );

        internal:

        static
        System::DateTime^
        ToDateTime(
            PrintDateTimeProperty % attribRef
            );

        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        [NonSerialized]
        PrintSystemDelegates::SystemDateTimeValueChanged^     changeHandler;       
        System::DateTime                                      value;
    };



    [System::Serializable]
    public ref class PrintSystemTypeProperty sealed : 
    public PrintProperty
    {
        public:

        PrintSystemTypeProperty (
            String^ attributeName
            );

        PrintSystemTypeProperty (
            String^ attributeName,
            Object^ attributeValue
            );

        internal:

        PrintSystemTypeProperty (
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        PrintSystemTypeProperty (
            String^            attributeName,
            Object^            attributeValue,
            MulticastDelegate^ delegate
            );

        public:

        property
        Object^
        Value
        {
            virtual Object^ get() override;
            virtual void set(Object^ objValue) override;
        }

        internal:

        property
        PrintSystemDelegates::SystemTypeValueChanged^
        ChangeHandler
        {
            PrintSystemDelegates::SystemTypeValueChanged^ get();
            void set(PrintSystemDelegates::SystemTypeValueChanged^ newHandler);
        }

        #pragma warning ( default:4376 )

        public:


        static
        operator
        System::Type^(
            PrintSystemTypeProperty % attribRef
            );


        static
        operator
        System::Type^(
            PrintSystemTypeProperty^ attribRef
            );

        internal:

        static
        System::Type^
        ToType(
            PrintSystemTypeProperty % attribRef
            );
        
        static
        PrintProperty^
        Create(
            String^ attributeName
            );

        static
        PrintProperty^
        Create(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            MulticastDelegate^ delegate
            );

        static
        PrintProperty^
        Create(
            String^            attributeName,
            Object^            attribValue,
            MulticastDelegate^ delegate
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        private:

        [NonSerialized]
        PrintSystemDelegates::SystemTypeValueChanged^     changeHandler;       
        System::Type^                                     value;
    };



    /// <summary>
    /// This is the class that abstracts a collection of properties 
    /// associated with an object in the Printing namespace.
    /// </summary>
    /// <ExternalAPI/>
    [DefaultMember("Property")]
    [System::Serializable]
    public ref class PrintPropertyDictionary :
    public Hashtable, 
    public System::Runtime::Serialization::ISerializable , 
    public System::Runtime::Serialization::IDeserializationCallback
    {
        public:

        /// <summary>
        /// PrintPropertyDictionary constructor.
        /// </summary>
        PrintPropertyDictionary(
            void
            );

        /// <summary>
        /// PrintPropertyDictionary destructor.
        /// </summary>
        ~PrintPropertyDictionary(
            );

        /// <summary>
        /// Adds a PrintProperty to the collection.
        /// </summary>
        void
        Add(
            PrintProperty^ attributeValue
            );

        virtual void
        OnDeserialization(
            Object^ sender
            ) override;

        virtual void
        GetObjectData(
            System::
            Runtime::
            Serialization::
            SerializationInfo^ info,
            System::
            Runtime::
            Serialization::
            StreamingContext   context
            ) override;

        /// <summary>
        /// Returns the PrintProperty object identified by the attribName.
        /// </summary>
        PrintProperty^
        GetProperty(
            String^ attribName
            );

        void
        SetProperty(
            String^ attribName,
            PrintProperty^ attribValue
            );

        protected:

        PrintPropertyDictionary(
            System::
            Runtime::
            Serialization::
            SerializationInfo^ info,
            System::
            Runtime::
            Serialization::
            StreamingContext   context
            );
        
    };
}
}
}

#endif
