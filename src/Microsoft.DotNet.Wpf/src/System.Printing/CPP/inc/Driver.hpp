// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __DRIVER_HPP__
#define __DRIVER_HPP__

/*++                                                          
    Abstract:
        This is the header file including the declaration of the 
        first class citizen component of the Filter ("The Driver").
        The declarations in this file are subject to change as our
        design evolves and as requirements from the new spooler
        architecture comes in
--*/

namespace System
{
namespace Printing
{
    /// <summary>
    ///    This class abstracts the functionality of a printer driver.
    ///    This object is returned by the Print System and cannot be instantiated by the end user.
    ///    The object has minimal functionality as it stands today. 
    ///    It is considered a management object.
    /// </summary>
    /// <ExternalAPI/>
    public ref class PrintDriver sealed :
    public PrintFilter
    {
        public:

        void
        virtual Commit(
            void
            ) override;

        void
        virtual Refresh(
            void
            ) override;

        internal: 

        PrintDriver(
            String^    driverName
            );

        virtual PrintPropertyDictionary^
        get_InternalPropertiesCollection(
            String^ attributeName
            ) override;

        static
        void
        RegisterAttributesNamesTypes(
            void
            );

        static
        PrintProperty^
        CreateAttributeNoValue(
            String^
            );

        static
        PrintProperty^
        CreateAttributeValue(
            String^,
            Object^
            );

        static
        PrintProperty^
        CreateAttributeNoValueLinked(
            String^,
            MulticastDelegate^
            );

        static
        PrintProperty^
        CreateAttributeValueLinked(
            String^,
            Object^,
            MulticastDelegate^
            );

        protected:

        virtual
        void
        InternalDispose(
            bool disposing
            ) override sealed;

        private:

        static 
        PrintDriver(
            void
            )
        {
            attributeNameTypes = gcnew Hashtable();
        }

        void
        VerifyAccess(
            void
        );

        PrintSystemDispatcherObject^    accessVerifier;
        static Hashtable^ attributeNameTypes;
    };
}
}

#endif
