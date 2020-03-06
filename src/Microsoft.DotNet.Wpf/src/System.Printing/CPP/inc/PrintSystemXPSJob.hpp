// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTSYSTEMXPSJOB_HPP__
#define __PRINTSYSTEMXPSJOB_HPP__

namespace System
{
namespace Printing
{
    private ref class PrintSystemXpsJob : 
    public System::Printing::PrintSystemJob
    {
        public:

        property
        System::Windows::Xps::Packaging::
        XpsDocument^
        XpsDocument
        {
            public:
                virtual System::Windows::Xps::Packaging::XpsDocument^ get();
            internal:
                void set(System::Windows::Xps::Packaging::XpsDocument^ reachPackage);        
        }

        property
        String^
        Name
        {
            virtual void set(String^ name) override;
            virtual String^ get() override;
        }

        virtual
        void
        Commit(
            void
            ) override;

        virtual
        void
        Refresh(
            void
            ) override;

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override;

        internal:

        PrintSystemXpsJob(
            PrintSystemJobInfo^     jobInfo
            );

        virtual
        PrintPropertyDictionary^
        get_InternalPropertiesCollection(
            String^ attributeName
            ) override;

        static
        PrintProperty^
        CreateAttributeNoValue(
            String^ attributeName
            );

        static
        PrintProperty^
        CreateAttributeValue(
            String^ attributeName,
            Object^ attributeValue
            );

        static
        PrintProperty^
        CreateAttributeNoValueLinked(
            String^             attributeName,
            MulticastDelegate^  delegate
            );

        static
        PrintProperty^
        CreateAttributeValueLinked(
            String^             attributeName,
            Object^             attributeValue,
            MulticastDelegate^  delegate
            );

        static
        void
        RegisterAttributesNamesTypes(
            void
            );

        private:

        static 
        PrintSystemXpsJob(
            void
            )
        {
            attributeNameTypes = gcnew Hashtable();
        }

        
        array<MulticastDelegate^>^
        CreatePropertiesDelegates(
            void
            );

        void
        Initialize(
            void
            );

        array<String^>^
        GetAllPropertiesFilter(
            void
            );


        void
        HandlePackagingProgressEvent(
            Object^                                                         sender,
            System::Windows::Xps::Packaging::PackagingProgressEventArgs^  e
            );

        static 
        Hashtable^                                 attributeNameTypes;

        static array<String^>^ primaryAttributeNames =
        {            
        };

        static array<Type^>^ primaryAttributeTypes =
        {
        };

        System::Windows::Xps::Packaging::
        XpsDocument^                       metroPackage;        
    };
}
}

#endif


