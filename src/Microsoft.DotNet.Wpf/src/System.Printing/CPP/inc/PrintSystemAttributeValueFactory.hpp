// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTSYSTEMATTRIBUTEVALUEFACTORY_HPP__
#define __PRINTSYSTEMATTRIBUTEVALUEFACTORY_HPP__

namespace System
{
namespace Printing
{
namespace Activation
{
    private ref class PrintPropertyFactory sealed : 
    public IDisposable, public IEnumerable
    {
        public:
        
        void
        RegisterValueCreationDelegate(
            Type^                              type,
            System::Printing::
            IndexedProperties::
            PrintProperty::CreateWithValue^    creationDelegate
            );

        void
        RegisterNoValueCreationDelegate(
            Type^                              type,
            System::Printing::
            IndexedProperties::
            PrintProperty::CreateWithNoValue^  creationDelegate
            );

        void
        RegisterValueLinkedCreationDelegate(
            Type^                                  type,
            System::Printing::
            IndexedProperties::
            PrintProperty::CreateWithValueLinked^  creationDelegate
            );

        void
        RegisterNoValueLinkedCreationDelegate(
            Type^                                    type,
            System::Printing::
            IndexedProperties::
            PrintProperty::CreateWithNoValueLinked^  creationDelegate
            );

        void
        UnRegisterValueCreationDelegate(
            Type^                               type
            );

        void
        UnRegisterNoValueCreationDelegate(
            Type^                               type
            );

        void
        UnRegisterValueLinkedCreationDelegate(
            Type^                               type
            );

        void
        UnRegisterNoValueLinkedCreationDelegate(
            Type^                               type
            );

        System::Printing::
        IndexedProperties::
        PrintProperty^
        Create(
            Type^                               type,
            String^                             attributeName
            );

        System::Printing::
        IndexedProperties::
        PrintProperty^
        Create(
            Type^                               type,
            String^                             attributeName,
            Object^                             attributeValue
            );

        System::Printing::
        IndexedProperties::
        PrintProperty^
        Create(
            Type^                               type,
            String^                             attributeName,
            MulticastDelegate^                  delegate
            );

        System::Printing::
        IndexedProperties::
        PrintProperty^
        Create(
            Type^                               type,
            String^                             attributeName,
            Object^                             attributeValue,
            MulticastDelegate^                  delegate
            );

        virtual IEnumerator^
        GetEnumerator(
            void
            );

        property
        static
        PrintPropertyFactory^
        Value
        {
            PrintPropertyFactory^ get();
        }

        protected:

        virtual
        void
        InternalDispose(
            bool    disposing
            );

        !PrintPropertyFactory(
            void
            );

        private:

        static 
        PrintPropertyFactory(
            void
            )
        {
            PrintPropertyFactory::value    = nullptr;
            PrintPropertyFactory::syncRoot = gcnew Object();
        }

        PrintPropertyFactory(
            void
            );

        ~PrintPropertyFactory(
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
        volatile 
        PrintPropertyFactory^               value;

        static
        volatile
        Object^                             syncRoot; 

        bool                                isDisposed;
        Hashtable^                          valueDelegatesTable;
        Hashtable^                          noValueDelegatesTable;
        Hashtable^                          valueLinkedDelegatesTable;
        Hashtable^                          noValueLinkedDelegatesTable;
    };

}
}
}

#endif
