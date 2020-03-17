// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTSYSTEMNOTIFICATIONS_HPP__
#define __PRINTSYSTEMNOTIFICATIONS_HPP__


namespace System
{
namespace Printing
{
    public ref class PrintSystemObjectPropertyChangedEventArgs: 
    public EventArgs
    {
        public:

        PrintSystemObjectPropertyChangedEventArgs(
            String^ eventName
            );

        ~PrintSystemObjectPropertyChangedEventArgs(
            void
            );

        property
        String^
        PropertyName
        {
            String^ get();
        }
    
        protected:

        private:

        String^     propertyName;
    };

    public ref class PrintSystemObjectPropertiesChangedEventArgs: 
    public EventArgs
    {
        public:

        PrintSystemObjectPropertiesChangedEventArgs(
            StringCollection^   events
            );

        ~PrintSystemObjectPropertiesChangedEventArgs(
            void
            );

        property
        StringCollection^
        PropertiesNames
        {
            StringCollection^ get();
        }
    
        protected:

        private:

        StringCollection^     propertiesNames;
    };
}
}

#endif
