// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __LOCALIZED_ERROR_MSGS_H
#define __LOCALIZED_ERROR_MSGS_H

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <summary>
    /// This class contains some localized exception strings passed 
    /// down to the MC++ from the managed C# layer. 
    /// The problem is that SR class is dynamically generated in the C# project.
    /// MC++ layer cannot reference the C# project or else we will have 
    /// circular dependency.
    /// </summary>    
    private ref class LocalizedErrorMsgs sealed
    {
        private:

            /// <summary>
            /// These are some localized exception strings passed down to the MC++ from the managed C# layer.
            /// </summary>
            static System::String^ _localizedExceptionMsgEnumeratorNotStarted;
            static System::String^ _localizedExceptionMsgEnumeratorReachedEnd;
            static System::Object^ _staticLockForLocalizedExceptionMsgs = gcnew Object();

        internal:

            static property System::String^ EnumeratorNotStarted
            {
                System::String^ get();

                void set(System::String^ msg);
            }

            static property System::String^ EnumeratorReachedEnd
            {
                System::String^ get();

                void set(System::String^ msg);
            }
    };
}}}}//MS::Internal::Text::TextInterface

#endif //__LOCALIZED_ERROR_MSGS_H
