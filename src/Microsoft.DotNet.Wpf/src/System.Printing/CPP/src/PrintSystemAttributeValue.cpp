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

using namespace System::Runtime::Serialization;

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif


using namespace System::Printing;

PrintProperty::
PrintProperty(
    String^ name
    )
{
    propertyName = gcnew String(name->ToCharArray());
    syncRoot     = gcnew Object();
}

PrintProperty::
~PrintProperty(
    void
    )
{
    InternalDispose(true);
}

PrintProperty::
!PrintProperty(
    void
    )
{
    InternalDispose(false);
}

String^
PrintProperty::Name::
get(
    void
    )
{
    return propertyName;
}

void
PrintProperty::
InternalDispose(
    bool disposing
    )
{
    if(!this->isDisposed)
    {
        propertyName = nullptr;
        syncRoot = nullptr;
        
        this->isDisposed = true;
    }
}

bool
PrintProperty::IsDisposed::
get(
    void
    )
{
    return isDisposed;
}

void
PrintProperty::IsDisposed::
set(
    bool isDisposed
    )
{
    this->isDisposed = isDisposed;
}

bool
PrintProperty::IsDirty::
get(
    void
    )
{
    return isDirty;
}

void
PrintProperty::IsDirty::
set(
    bool isDirty
    )
{
    this->isDirty = isDirty;
}

bool
PrintProperty::IsInitialized::
get(
    void
    )
{
    return isInitialized;
}

void
PrintProperty::IsInitialized::
set(
    bool isInitialized
    )
{
    this->isInitialized = isInitialized;
}

bool
PrintProperty::IsInternallyInitialized::
get(
    void
    )
{
    return isInternallyInitialized;
}

void
PrintProperty::IsInternallyInitialized::
set(
    bool isInternallyInitialized
    )
{
    this->isInternallyInitialized = isInternallyInitialized;
}

bool
PrintProperty::IsLinked::
get(
    void
    )
{
    return isLinked;
}

void
PrintProperty::IsLinked::
set(
    bool isLinked
    )
{
    this->isLinked = isLinked;
}

void
PrintProperty::
OnDeserialization(
    Object^ sender
    )
{
    isDirty                 = false;
    isInitialized           = false;
    isInternallyInitialized = false;
    isDisposed              = false;
    isLinked                = false;
}

/*--------------------------------------------------------------------------------------*/
/*                            PrintStringProperty Implementation                        */
/*--------------------------------------------------------------------------------------*/

PrintStringProperty::
PrintStringProperty(
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintStringProperty::
PrintStringProperty(
    String^ name,
    Object^ value
    ):
PrintProperty(name),
changeHandler(nullptr)
{
    this->Value = value;
}

PrintStringProperty::
PrintStringProperty(
    String^             name,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::StringValueChanged^)delegate)
{
    IsLinked = true;
}

PrintStringProperty::
PrintStringProperty(
    String^             name,
    Object^             value,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::StringValueChanged^)delegate)
{
    this->Value = value;
    IsLinked = true;
}

Object^
PrintStringProperty::Value::
get(
    void
    )
{
    return value;
}

void
PrintStringProperty::Value::
set(
    Object^ inValue
    )
{
    Type^ inType = inValue ? inValue->GetType() : nullptr;

    if((inType == String::typeid) ||
       (inType == nullptr))
    {
        if((value != inValue) &&
           (!value || !value->Equals(inValue)))
        {
            if(IsInternallyInitialized)
            {
                if(inValue)
                {
                    value = gcnew String(((String^)inValue)->ToCharArray());
                }
                else
                {
                    value = nullptr;
                }
            }
            else
            {
                value = (String^)inValue;
            }

            if(ChangeHandler)
            {
                ChangeHandler->Invoke(value);
            }

            if(IsInternallyInitialized)
            {
                IsInternallyInitialized = false;
                IsInitialized           = true;
                IsDirty                 = false;
            }
            else
            {
                IsDirty = true;
            }
        }
    }
}

PrintProperty^
PrintStringProperty::
Create(
    String^ name
    )
{
    return gcnew PrintStringProperty(name);
}

PrintProperty^
PrintStringProperty::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintStringProperty(name,value);
}

PrintProperty^
PrintStringProperty::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintStringProperty(name,delegate);
}

PrintProperty^
PrintStringProperty::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintStringProperty(name,value,delegate);
}


PrintStringProperty::
operator
String^(
    PrintStringProperty% attribValue
    )
{
    return (String^)attribValue.Value;
}


PrintStringProperty::
operator
String^(
    PrintStringProperty^ attribValue
    )
{
    return (String^)attribValue->Value;
}

String^
PrintStringProperty::
ToString(
    PrintStringProperty% attribValue
    )
{
    return (String^)attribValue.Value;
}

PrintSystemDelegates::
StringValueChanged^
PrintStringProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintStringProperty::ChangeHandler::
set(
    PrintSystemDelegates::StringValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintStringProperty::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        value         = nullptr;
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*--------------------------------------------------------------------------------------*/
/*                         PrintInt32Property Implementation                            */
/*--------------------------------------------------------------------------------------*/

PrintInt32Property::
PrintInt32Property(
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintInt32Property::
PrintInt32Property(
    String^ attributeName,
    Object^ attributeValue
    ):
PrintProperty(attributeName),
changeHandler(nullptr)
{
    this->Value = attributeValue;
}

PrintInt32Property::
PrintInt32Property(
    String^            attributeName,
    MulticastDelegate^ delegate
    ):
PrintProperty(attributeName),
changeHandler((PrintSystemDelegates::Int32ValueChanged^)delegate)
{
    IsLinked = true;
}

PrintInt32Property::
PrintInt32Property(
    String^            attributeName,
    Object^            attributeValue,
    MulticastDelegate^ delegate
    ):
PrintProperty(attributeName),
changeHandler((PrintSystemDelegates::Int32ValueChanged^)delegate)
{
    this->Value = attributeValue;
    IsLinked = true;
}

Object^
PrintInt32Property::Value::
get(
    void
    )
{
    return value;
}

void
PrintInt32Property::Value::
set(
    Object^ inValue
    )
{
    if(Int32^ intValue = (Int32^)inValue)
    {
        if(value != *intValue)
        {
            value = *intValue;

            if(ChangeHandler)
            {
                ChangeHandler->Invoke(value);
            }
            else if(!ChangeHandler && IsLinked && !IsInternallyInitialized)
            {
                //
                // Throw an exception of an invalid operation
                //
                throw gcnew InvalidOperationException();
            }

            if(IsInternallyInitialized)
            {
                IsInternallyInitialized = false;
                IsInitialized           = true;
                IsDirty                 = false;
            }
            else
            {
                IsDirty = true;
            }
        }
    }
}

PrintProperty^
PrintInt32Property::
Create(
    String^ name
    )
{
    return gcnew PrintInt32Property(name);
}

PrintProperty^
PrintInt32Property::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintInt32Property(name,value);
}

PrintProperty^
PrintInt32Property::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintInt32Property(name,delegate);
}

PrintProperty^
PrintInt32Property::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintInt32Property(name,value,delegate);
}

PrintInt32Property::
operator
Int32(
    PrintInt32Property^ attribValue
    )
{
    return (Int32)attribValue->Value;
}


PrintInt32Property::
operator
Int32(
    PrintInt32Property% attribValue
    )
{
    return (Int32)attribValue.Value;
}


Int32^
PrintInt32Property::
ToInt32(
    PrintInt32Property% attribValue
    )
{
    return (Int32^)attribValue.Value;
}

PrintSystemDelegates::
Int32ValueChanged^
PrintInt32Property::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintInt32Property::ChangeHandler::
set(
    PrintSystemDelegates::Int32ValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintInt32Property::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        value         = NULL;
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*--------------------------------------------------------------------------------------*/
/*                           PrintBooleanProperty Implementation                        */
/*--------------------------------------------------------------------------------------*/

PrintBooleanProperty::
PrintBooleanProperty(
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintBooleanProperty::
PrintBooleanProperty(
    String^ attributeName,
    Object^ attributeValue
    ):
PrintProperty(attributeName),
changeHandler(nullptr)
{
    this->Value = attributeValue;
}

PrintBooleanProperty::
PrintBooleanProperty(
    String^            attributeName,
    MulticastDelegate^ delegate
    ):
PrintProperty(attributeName),
changeHandler((PrintSystemDelegates::BooleanValueChanged^)delegate)
{
    IsLinked = true;
}

PrintBooleanProperty::
PrintBooleanProperty(
    String^            attributeName,
    Object^            attributeValue,
    MulticastDelegate^ delegate
    ):
PrintProperty(attributeName),
changeHandler((PrintSystemDelegates::BooleanValueChanged^)delegate)
{
    this->Value = attributeValue;
    IsLinked = true;
}

Object^
PrintBooleanProperty::Value::
get(
    void
    )
{
    return value;
}

void
PrintBooleanProperty::Value::
set(
    Object^ inValue
    )
{
    if(Boolean^ intValue = (Boolean^)inValue)
    {
        if (value != *intValue)
        {
            value = *intValue;

            if(ChangeHandler)
            {
                ChangeHandler->Invoke(value);
            }

            if(IsInternallyInitialized)
            {
                IsInternallyInitialized = false;
                IsInitialized           = true;
                IsDirty                 = false;
            }
            else
            {
                IsDirty = true;
            }
        }
    }
}

PrintProperty^
PrintBooleanProperty::
Create(
    String^ name
    )
{
    return gcnew PrintBooleanProperty(name);
}

PrintProperty^
PrintBooleanProperty::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintBooleanProperty(name,value);
}

PrintProperty^
PrintBooleanProperty::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintBooleanProperty(name,delegate);
}

PrintProperty^
PrintBooleanProperty::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintBooleanProperty(name,value,delegate);
}


PrintBooleanProperty::
operator
Boolean(
    PrintBooleanProperty% attribValue
    )
{
    return (Boolean)attribValue.Value;
}


PrintBooleanProperty::
operator
Boolean(
    PrintBooleanProperty^ attribValue
    )
{
    return (Boolean)attribValue->Value;
}

Boolean^
PrintBooleanProperty::
ToBoolean(
    PrintBooleanProperty% attribValue
    )
{
    return (Boolean^)attribValue.Value;
}

PrintSystemDelegates::
BooleanValueChanged^
PrintBooleanProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintBooleanProperty::ChangeHandler::
set(
    PrintSystemDelegates::BooleanValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintBooleanProperty::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        value         = NULL;
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*--------------------------------------------------------------------------------------*/
/*                PrintThreadPriorityProperty Implementation                            */
/*--------------------------------------------------------------------------------------*/

PrintThreadPriorityProperty::
PrintThreadPriorityProperty(
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintThreadPriorityProperty::
PrintThreadPriorityProperty(
    String^ attributeName,
    Object^ attributeValue
    ):
PrintProperty(attributeName),
changeHandler(nullptr)
{
    this->Value = attributeValue;
}

PrintThreadPriorityProperty::
PrintThreadPriorityProperty(
    String^            attributeName,
    MulticastDelegate^ delegate
    ):
PrintProperty(attributeName),
changeHandler((PrintSystemDelegates::ThreadPriorityValueChanged^)delegate)
{
    IsLinked = true;
}

PrintThreadPriorityProperty::
PrintThreadPriorityProperty(
    String^            attributeName,
    Object^            attributeValue,
    MulticastDelegate^ delegate
    ):
PrintProperty(attributeName),
changeHandler((PrintSystemDelegates::ThreadPriorityValueChanged^)delegate)
{
    this->Value = attributeValue;
    IsLinked = true;
}

Object^
PrintThreadPriorityProperty::Value::
get(
    void
    )
{
    return value;
}

void
PrintThreadPriorityProperty::Value::
set(
    Object^ inValue
    )
{
    if(System::Threading::ThreadPriority^ intValue =
       (System::Threading::ThreadPriority^)inValue)
    {
        if (value != *intValue)
        {
            value = *intValue;

            if(ChangeHandler)
            {
                ChangeHandler->Invoke(value);
            }

            if(IsInternallyInitialized)
            {
                IsInternallyInitialized = false;
                IsInitialized           = true;
                IsDirty                 = false;
            }
            else
            {
                IsDirty = true;
            }
        }
    }
}

PrintProperty^
PrintThreadPriorityProperty::
Create(
    String^ name
    )
{
    return gcnew PrintThreadPriorityProperty(name);
}

PrintProperty^
PrintThreadPriorityProperty::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintThreadPriorityProperty(name,value);
}

PrintProperty^
PrintThreadPriorityProperty::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintThreadPriorityProperty(name,delegate);
}

PrintProperty^
PrintThreadPriorityProperty::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintThreadPriorityProperty(name,value,delegate);
}


PrintThreadPriorityProperty::
operator
System::Threading::ThreadPriority(
    PrintThreadPriorityProperty% attribValue
    )
{
    return (System::Threading::ThreadPriority)attribValue.Value;
}


PrintThreadPriorityProperty::
operator
System::Threading::ThreadPriority(
    PrintThreadPriorityProperty^ attribValue
    )
{
    return (System::Threading::ThreadPriority)attribValue->Value;
}

System::Threading::ThreadPriority^
PrintThreadPriorityProperty::
ToThreadPriority(
    PrintThreadPriorityProperty% attribValue
    )
{
    return (System::Threading::ThreadPriority^)attribValue.Value;
}

PrintSystemDelegates::
ThreadPriorityValueChanged^
PrintThreadPriorityProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintThreadPriorityProperty::ChangeHandler::
set(
    PrintSystemDelegates::ThreadPriorityValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintThreadPriorityProperty::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*--------------------------------------------------------------------------------------*/
/*                       PrintServerLoggingProperty Implementation                      */
/*--------------------------------------------------------------------------------------*/

PrintServerLoggingProperty::
PrintServerLoggingProperty(
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintServerLoggingProperty::
PrintServerLoggingProperty(
    String^ attributeName,
    Object^ attributeValue
    ):
PrintProperty(attributeName),
changeHandler(nullptr)
{
    this->Value = attributeValue;
}

PrintServerLoggingProperty::
PrintServerLoggingProperty(
    String^            attributeName,
    MulticastDelegate^ delegate
    ):
PrintProperty(attributeName),
changeHandler((PrintSystemDelegates::PrintServerEventLoggingValueChanged^)delegate)
{
    IsLinked = true;
}

PrintServerLoggingProperty::
PrintServerLoggingProperty(
    String^            attributeName,
    Object^            attributeValue,
    MulticastDelegate^ delegate
    ):
PrintProperty(attributeName),
changeHandler((PrintSystemDelegates::PrintServerEventLoggingValueChanged^)delegate)
{
    this->Value = attributeValue;
    IsLinked = true;
}

Object^
PrintServerLoggingProperty::Value::
get(
    void
    )
{
    return value;
}

void
PrintServerLoggingProperty::Value::
set(
    Object^ inValue
    )
{
    if(PrintServerEventLoggingTypes^ intValue =
       (PrintServerEventLoggingTypes)inValue)
    {
        if (value != *intValue)
        {
            value = *intValue;

            if(ChangeHandler)
            {
                ChangeHandler->Invoke(value);
            }

            if(IsInternallyInitialized)
            {
                IsInternallyInitialized = false;
                IsInitialized           = true;
                IsDirty                 = false;
            }
            else
            {
                IsDirty = true;
            }
        }
    }
}

PrintProperty^
PrintServerLoggingProperty::
Create(
    String^ name
    )
{
    return gcnew PrintServerLoggingProperty(name);
}

PrintProperty^
PrintServerLoggingProperty::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintServerLoggingProperty(name,value);
}

PrintProperty^
PrintServerLoggingProperty::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintServerLoggingProperty(name,delegate);
}

PrintProperty^
PrintServerLoggingProperty::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintServerLoggingProperty(name,value,delegate);
}


PrintServerLoggingProperty::
operator
PrintServerEventLoggingTypes(
    PrintServerLoggingProperty % attribValue
    )
{
    return (PrintServerEventLoggingTypes)attribValue.Value;
}


PrintServerLoggingProperty::
operator
PrintServerEventLoggingTypes(
    PrintServerLoggingProperty^ attribValue
    )
{
    return (PrintServerEventLoggingTypes)attribValue->Value;
}

PrintServerEventLoggingTypes^
PrintServerLoggingProperty::
ToPrintServerEventLoggingTypes(
    PrintServerLoggingProperty % attribValue
    )
{
    return (PrintServerEventLoggingTypes^)attribValue.Value;
}

PrintSystemDelegates::
PrintServerEventLoggingValueChanged^
PrintServerLoggingProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintServerLoggingProperty::ChangeHandler::
set(
    PrintSystemDelegates::PrintServerEventLoggingValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintServerLoggingProperty::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*--------------------------------------------------------------------------------------*/
/*                      PrintByteArrayProperty Implementation                           */
/*--------------------------------------------------------------------------------------*/

PrintByteArrayProperty::
PrintByteArrayProperty(
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintByteArrayProperty::
PrintByteArrayProperty(
    String^ attributeName,
    Object^ attributeValue
    ):
PrintProperty(attributeName),
changeHandler(nullptr)
{
    this->Value = attributeValue;
}

PrintByteArrayProperty::
PrintByteArrayProperty(
    String^            attributeName,
    MulticastDelegate^ delegate
    ):
PrintProperty(attributeName),
changeHandler((PrintSystemDelegates::ByteArrayValueChanged^)delegate)
{
    IsLinked = true;
}

PrintByteArrayProperty::
PrintByteArrayProperty(
    String^            attributeName,
    Object^            attributeValue,
    MulticastDelegate^ delegate
    ):
PrintProperty(attributeName),
changeHandler((PrintSystemDelegates::ByteArrayValueChanged^)delegate)
{
    this->Value = attributeValue;
    IsLinked = true;
}

Object^
PrintByteArrayProperty::Value::
get(

          void
    )
{
    return value;
}

void
PrintByteArrayProperty::Value::
set(
    Object^ inValue
    )
{
    Type^ inType = inValue ? inValue->GetType() : nullptr;

    if(inType == array<Byte>::typeid ||
       (inType == nullptr))
    {
        if(value != inValue)
        {
            value = (array<Byte>^)inValue;

            if(ChangeHandler && IsInternallyInitialized)
            {
                ChangeHandler->Invoke(value);
            }

            if(IsInternallyInitialized)
            {
                IsInternallyInitialized = false;
                IsInitialized           = true;
                IsDirty                 = false;
            }
            else
            {
                IsDirty = true;
            }
        }
    }
}

PrintProperty^
PrintByteArrayProperty::
Create(
    String^ name
    )
{
    return gcnew PrintByteArrayProperty(name);
}

PrintProperty^
PrintByteArrayProperty::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintByteArrayProperty(name,value);
}

PrintProperty^
PrintByteArrayProperty::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintByteArrayProperty(name,delegate);
}

PrintProperty^
PrintByteArrayProperty::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintByteArrayProperty(name,value,delegate);
}


PrintByteArrayProperty::
operator
array<Byte>^(
    PrintByteArrayProperty % attribValue
    )
{
    return (array<Byte>^)attribValue.Value;
}


PrintByteArrayProperty::
operator
array<Byte>^(
    PrintByteArrayProperty^ attribValue
    )
{
    return (array<Byte>^)attribValue->Value;
}

array<Byte>^
PrintByteArrayProperty::
ToByteArray(
    PrintByteArrayProperty % attribValue
    )
{
    return (array<Byte>^)attribValue.Value;
}


PrintSystemDelegates::
ByteArrayValueChanged^
PrintByteArrayProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintByteArrayProperty::ChangeHandler::
set(
    PrintSystemDelegates::ByteArrayValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintByteArrayProperty::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        value         = nullptr;
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*--------------------------------------------------------------------------------------*/
/*                      PrintStreamProperty Implementation                              */
/*--------------------------------------------------------------------------------------*/

PrintStreamProperty::
PrintStreamProperty(
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintStreamProperty::
PrintStreamProperty(
    String^ name,
    Object^ value
    ):
PrintProperty(name),
changeHandler(nullptr)
{
    this->Value = value;
}

PrintStreamProperty::
PrintStreamProperty(
    String^             name,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::StreamValueChanged^)delegate)
{
    IsLinked = true;
}

PrintStreamProperty::
PrintStreamProperty(
    String^             name,
    Object^             value,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::StreamValueChanged^)delegate)
{
    this->Value = value;
    IsLinked = true;
}

Object^
PrintStreamProperty::Value::
get(
    void
    )
{
    return value;
}

void
PrintStreamProperty::Value::
set(
    Object^ inValue
    )
{
    //Type^ inType = inValue->GetType();

    //if(inType == Stream::typeid)
    //{
        if(value != inValue)
        {
            value = (Stream^)inValue;

            if(ChangeHandler)
            {
                ChangeHandler->Invoke(value);
            }

            if(IsInternallyInitialized)
            {
                IsInternallyInitialized = false;
                IsInitialized           = true;
                IsDirty                 = false;
            }
            else
            {
                IsDirty = true;
            }
        }
    //}
}

PrintProperty^
PrintStreamProperty::
Create(
    String^ name
    )
{
    return gcnew PrintStreamProperty(name);
}

PrintProperty^
PrintStreamProperty::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintStreamProperty(name,value);
}

PrintProperty^
PrintStreamProperty::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintStreamProperty(name,delegate);
}

PrintProperty^
PrintStreamProperty::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintStreamProperty(name,value,delegate);
}


PrintStreamProperty::
operator
Stream^(
    PrintStreamProperty% attribValue
    )
{
    return (Stream^)attribValue.Value;
}


PrintStreamProperty::
operator
Stream^(
    PrintStreamProperty^ attribValue
    )
{
    return (Stream^)attribValue->Value;
}

Stream^
PrintStreamProperty::
ToStream(
    PrintStreamProperty% attribValue
    )
{
    return (Stream^)attribValue.Value;
}

PrintSystemDelegates::
StreamValueChanged^
PrintStreamProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintStreamProperty::ChangeHandler::
set(
    PrintSystemDelegates::StreamValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintStreamProperty::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        delete value;
                        value         = nullptr;
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*--------------------------------------------------------------------------------------*/
/*                          PrintQueueAttributeProperty Implementation                  */
/*--------------------------------------------------------------------------------------*/

PrintQueueAttributeProperty::
PrintQueueAttributeProperty(
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintQueueAttributeProperty::
PrintQueueAttributeProperty(
    String^ attributeName,
    Object^ attributeValue
    ):
PrintProperty(attributeName),
changeHandler(nullptr)
{
    this->Value = attributeValue;
}

PrintQueueAttributeProperty::
PrintQueueAttributeProperty(
    String^            attributeName,
    MulticastDelegate^ delegate
    ):
PrintProperty(attributeName),
changeHandler((PrintSystemDelegates::PrintQueueAttributePropertyChanged^)delegate)
{
    IsLinked = true;
}

PrintQueueAttributeProperty::
PrintQueueAttributeProperty(
    String^            attributeName,
    Object^            attributeValue,
    MulticastDelegate^ delegate
    ):
PrintProperty(attributeName),
changeHandler((PrintSystemDelegates::PrintQueueAttributePropertyChanged^)delegate)
{
    this->Value = attributeValue;
    IsLinked = true;
}

Object^
PrintQueueAttributeProperty::Value::
get(
    void
    )
{
    return value;
}

void
PrintQueueAttributeProperty::Value::
set(
    Object^ inValue
    )
{
     if ( PrintQueueAttributes::typeid->Equals(inValue->GetType()) )
     {
        PrintQueueAttributes intValue = *(static_cast<PrintQueueAttributes^>(inValue));

        if (value != intValue)
        {
            value = intValue;

            if(ChangeHandler)
            {
                ChangeHandler->Invoke(value);
            }
            else if(!ChangeHandler && !IsInternallyInitialized && IsLinked)
            {
                throw gcnew InvalidOperationException();
            }

            if(IsInternallyInitialized)
            {
                IsInternallyInitialized = false;
                IsInitialized           = true;
                IsDirty                 = false;
            }
            else
            {
                IsDirty = true;
            }
        }
    }
}

PrintProperty^
PrintQueueAttributeProperty::
Create(
    String^ name
    )
{
    return gcnew PrintQueueAttributeProperty(name);
}

PrintProperty^
PrintQueueAttributeProperty::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintQueueAttributeProperty(name,value);
}

PrintProperty^
PrintQueueAttributeProperty::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintQueueAttributeProperty(name,delegate);
}

PrintProperty^
PrintQueueAttributeProperty::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintQueueAttributeProperty(name,value,delegate);
}


PrintQueueAttributeProperty::
operator
PrintQueueAttributes(
    PrintQueueAttributeProperty % attribValue
    )
{
    return static_cast<PrintQueueAttributes>(attribValue.Value);
}


PrintQueueAttributeProperty::
operator
PrintQueueAttributes(
    PrintQueueAttributeProperty^ attribValue
    )
{
    return static_cast<PrintQueueAttributes>(attribValue->Value);
}

PrintSystemDelegates::
PrintQueueAttributePropertyChanged^
PrintQueueAttributeProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintQueueAttributeProperty::ChangeHandler::
set(
    PrintSystemDelegates::PrintQueueAttributePropertyChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintQueueAttributeProperty::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        value         = PrintQueueAttributes::None;
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*--------------------------------------------------------------------------------------*/
/*                            PrintQueueStatusProperty Implementation                   */
/*--------------------------------------------------------------------------------------*/

PrintQueueStatusProperty::
PrintQueueStatusProperty(
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintQueueStatusProperty::
PrintQueueStatusProperty(
    String^ attributeName,
    Object^ attributeValue
    ):
PrintProperty(attributeName),
changeHandler(nullptr)
{
    this->Value = attributeValue;
}

PrintQueueStatusProperty::
PrintQueueStatusProperty(
    String^            attributeName,
    MulticastDelegate^ delegate
    ):
PrintProperty(attributeName),
changeHandler((PrintSystemDelegates::PrintQueueStatusValueChanged^)delegate)
{
    IsLinked = true;
}

PrintQueueStatusProperty::
PrintQueueStatusProperty(
    String^            attributeName,
    Object^            attributeValue,
    MulticastDelegate^ delegate
    ):
PrintProperty(attributeName),
changeHandler((PrintSystemDelegates::PrintQueueStatusValueChanged^)delegate)
{
    this->Value = attributeValue;
    IsLinked = true;
}

Object^
PrintQueueStatusProperty::Value::
get(
    void
    )
{
    return value;
}

void
PrintQueueStatusProperty::Value::
set(
    Object^ inValue
    )
{
     if ( PrintQueueStatus::typeid->Equals(inValue->GetType()) )
     {
        PrintQueueStatus intValue = *(static_cast<PrintQueueStatus^>(inValue));

        if (value != intValue)
        {
            value = intValue;
            if(ChangeHandler)
            {
                ChangeHandler->Invoke(value);
            }
            else if(!ChangeHandler && !IsInternallyInitialized && IsLinked)
            {
                throw gcnew InvalidOperationException();
            }

            if(IsInternallyInitialized)
            {
                IsInternallyInitialized = false;
                IsInitialized           = true;
                IsDirty                 = false;
            }
            else
            {
                IsDirty = true;
            }
        }
    }
}

PrintProperty^
PrintQueueStatusProperty::
Create(
    String^ name
    )
{
    return gcnew PrintQueueStatusProperty(name);
}

PrintProperty^
PrintQueueStatusProperty::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintQueueStatusProperty(name,value);
}

PrintProperty^
PrintQueueStatusProperty::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintQueueStatusProperty(name,delegate);
}

PrintProperty^
PrintQueueStatusProperty::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintQueueStatusProperty(name,value,delegate);
}


PrintQueueStatusProperty::
operator
PrintQueueStatus(
    PrintQueueStatusProperty % attribValue
    )
{
    return static_cast<PrintQueueStatus>(attribValue.Value);
}


PrintQueueStatusProperty::
operator
PrintQueueStatus(
    PrintQueueStatusProperty^ attribValue
    )
{
    return static_cast<PrintQueueStatus>(attribValue->Value);
}

PrintSystemDelegates::
PrintQueueStatusValueChanged^
PrintQueueStatusProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintQueueStatusProperty::ChangeHandler::
set(
    PrintSystemDelegates::PrintQueueStatusValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintQueueStatusProperty::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        value         = PrintQueueStatus::None;
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*--------------------------------------------------------------------------------------*/
/*                            PrintDriverProperty Implementation                        */
/*--------------------------------------------------------------------------------------*/

PrintDriverProperty::
PrintDriverProperty(
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintDriverProperty::
PrintDriverProperty(
    String^ name,
    Object^ value
    ):
PrintProperty(name),
changeHandler(nullptr)
{
    this->Value = value;
}

PrintDriverProperty::
PrintDriverProperty(
    String^             name,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::DriverValueChanged^)delegate)
{
    IsLinked = true;
}

PrintDriverProperty::
PrintDriverProperty(
    String^             name,
    Object^             value,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::DriverValueChanged^)delegate)
{
    this->Value = value;
    IsLinked = true;
}

Object^
PrintDriverProperty::Value::
get(
    void
    )
{
    return value;
}

void
PrintDriverProperty::Value::
set(
    Object^ inValue
    )
{
    Type^ inType = inValue->GetType();

    if(inType == PrintDriver::typeid)
    {
        if(value != inValue)
        {
            value = (PrintDriver^)inValue;
            
            if(ChangeHandler)
            {
                ChangeHandler->Invoke(value);
            }

            if(IsInternallyInitialized)
            {
                IsInternallyInitialized = false;
                IsInitialized           = true;
                IsDirty                 = false;
            }
            else
            {
                IsDirty = true;
            }
        }
    }
}

PrintProperty^
PrintDriverProperty::
Create(
    String^ name
    )
{
    return gcnew PrintDriverProperty(name);
}

PrintProperty^
PrintDriverProperty::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintDriverProperty(name,value);
}

PrintProperty^
PrintDriverProperty::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintDriverProperty(name,delegate);
}

PrintProperty^
PrintDriverProperty::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintDriverProperty(name,value,delegate);
}


PrintDriverProperty::
operator
PrintDriver^(
    PrintDriverProperty % attribValue
    )
{
    return (PrintDriver^)attribValue.Value;
}


PrintDriverProperty::
operator
PrintDriver^(
    PrintDriverProperty^ attribValue
    )
{
    return (PrintDriver^)attribValue->Value;
}

PrintDriver^
PrintDriverProperty::
ToPrintDriver(
    PrintDriverProperty % attribValue
    )
{
    return (PrintDriver^)attribValue.Value;
}

PrintSystemDelegates::
DriverValueChanged^
PrintDriverProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintDriverProperty::ChangeHandler::
set(
    PrintSystemDelegates::DriverValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintDriverProperty::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        delete value;
                        value         = nullptr;
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*--------------------------------------------------------------------------------------*/
/*                              PrintPortProperty Implementation                        */
/*--------------------------------------------------------------------------------------*/

PrintPortProperty::
PrintPortProperty(
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintPortProperty::
PrintPortProperty(
    String^ name,
    Object^ value
    ):
PrintProperty(name),
changeHandler(nullptr)
{
    this->Value = value;
}

PrintPortProperty::
PrintPortProperty(
    String^             name,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::PortValueChanged^)delegate)
{
    IsLinked = true;
}

PrintPortProperty::
PrintPortProperty(
    String^             name,
    Object^             value,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::PortValueChanged^)delegate)
{
    this->Value = value;
    IsLinked = true;
}

Object^
PrintPortProperty::Value::
get(
    void
    )
{
    return value;
}

void
PrintPortProperty::Value::
set(
    Object^ inValue
    )
{
    Type^ inType = inValue->GetType();

    if(inType == PrintPort::typeid)
    {
        if(value != inValue)
        {
            value = (PrintPort^)inValue;

            if(ChangeHandler)
            {
                ChangeHandler->Invoke(value);
            }

            if(IsInternallyInitialized)
            {
                IsInternallyInitialized = false;
                IsInitialized           = true;
                IsDirty                 = false;
            }
            else
            {
                IsDirty = true;
            }
        }
    }
}

PrintProperty^
PrintPortProperty::
Create(
    String^ name
    )
{
    return gcnew PrintPortProperty(name);
}

PrintProperty^
PrintPortProperty::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintPortProperty(name,value);
}

PrintProperty^
PrintPortProperty::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintPortProperty(name,delegate);
}

PrintProperty^
PrintPortProperty::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintPortProperty(name,value,delegate);
}


PrintPortProperty::
operator
PrintPort^(
    PrintPortProperty % attribValue
    )
{
    return (PrintPort^)attribValue.Value;
}


PrintPortProperty::
operator
PrintPort^(
    PrintPortProperty^ attribValue
    )
{
    return (PrintPort^)attribValue->Value;
}

PrintPort^
PrintPortProperty::
ToPrintPort(
    PrintPortProperty % attribValue
    )
{
    return (PrintPort^)attribValue.Value;
}

PrintSystemDelegates::
PortValueChanged^
PrintPortProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintPortProperty::ChangeHandler::
set(
    PrintSystemDelegates::PortValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintPortProperty::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        delete value;
                        value         = nullptr;
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*--------------------------------------------------------------------------------------*/
/*                          PrintTicketProperty Implementation                          */
/*--------------------------------------------------------------------------------------*/

PrintTicketProperty::
PrintTicketProperty(
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintTicketProperty::
PrintTicketProperty(
    String^ name,
    Object^ value
    ):
PrintProperty(name),
changeHandler(nullptr)
{
    Type^ inType = value->GetType();

    if(inType == PrintTicket::typeid)
    {
        this->value = (PrintTicket^)value;
    }
}

PrintTicketProperty::
PrintTicketProperty(
    String^             name,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::PrintTicketValueChanged^)delegate)
{
    IsLinked = true;
}

PrintTicketProperty::
PrintTicketProperty(
    String^             name,
    Object^             value,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::PrintTicketValueChanged^)delegate)
{
    Type^ inType = value->GetType();

    if(inType == PrintTicket::typeid)
    {
        this->value = (PrintTicket^)value;
    }

    IsLinked = true;
}

Object^
PrintTicketProperty::Value::
get(
    void
    )
{
    return value;
}

void
PrintTicketProperty::Value::
set(
    Object^ inValue
    )
{
    Type^ inType = inValue->GetType();

    if(inType == PrintTicket::typeid)
    {
        if(value != inValue)
        {
            value = (PrintTicket^)inValue;

            if(ChangeHandler)
            {
                ChangeHandler->Invoke(value);
            }

            if(IsInternallyInitialized)
            {
                IsInternallyInitialized = false;
                IsInitialized           = true;
                IsDirty                 = false;
            }
            else
            {
                IsDirty = true;
            }
        }
    }
}

PrintProperty^
PrintTicketProperty::
Create(
    String^ name
    )
{
    return gcnew PrintTicketProperty(name);
}

PrintProperty^
PrintTicketProperty::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintTicketProperty(name,value);
}

PrintProperty^
PrintTicketProperty::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintTicketProperty(name,delegate);
}

PrintProperty^
PrintTicketProperty::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintTicketProperty(name,value,delegate);
}


PrintTicketProperty::
operator
PrintTicket^(
    PrintTicketProperty% attribValue
    )
{
    return (PrintTicket^)attribValue.Value;
}


PrintTicketProperty::
operator
PrintTicket^(
    PrintTicketProperty^ attribValue
    )
{
    return (PrintTicket^)attribValue->Value;
}

PrintTicket^
PrintTicketProperty::
ToPrintTicket(
    PrintTicketProperty% attribValue
    )
{
    return (PrintTicket^)attribValue.Value;
}

PrintSystemDelegates::
PrintTicketValueChanged^
PrintTicketProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintTicketProperty::ChangeHandler::
set(
    PrintSystemDelegates::PrintTicketValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintTicketProperty::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        value         = nullptr;
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}


/*--------------------------------------------------------------------------------------*/
/*                          PrintServerProperty Implementation                          */
/*--------------------------------------------------------------------------------------*/

PrintServerProperty::
PrintServerProperty(
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintServerProperty::
PrintServerProperty(
    String^ name,
    Object^ value
    ):
PrintProperty(name),
changeHandler(nullptr)
{
    Type^ inType = value->GetType();

    if(inType == PrintServer::typeid)
    {
        this->value = (PrintServer^)value;
    }
}

PrintServerProperty::
PrintServerProperty(
    String^             name,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::PrintServerValueChanged^)delegate)
{
    IsLinked = true;
}

PrintServerProperty::
PrintServerProperty(
    String^             name,
    Object^             value,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::PrintServerValueChanged^)delegate)
{
    Type^ inType = value->GetType();

    if(inType == PrintServer::typeid)
    {
        this->value = (PrintServer^)value;
    }

    IsLinked = true;
}

Object^
PrintServerProperty::Value::
get(
    void
    )
{
    return value;
}

void
PrintServerProperty::Value::
set(
    Object^ inValue
    )
{
    if(IsInternallyInitialized)
    {
        Type^ inType = inValue->GetType();

        if(inType == PrintServer::typeid)
        {
            value = (PrintServer^)inValue;

            if(ChangeHandler)
            {
                ChangeHandler->Invoke(value);
            }

            IsInternallyInitialized = false;
            IsInitialized           = true;
        }
    }
    else
    {
        throw gcnew InvalidOperationException();
    }
}

PrintProperty^
PrintServerProperty::
Create(
    String^ name
    )
{
    return gcnew PrintServerProperty(name);
}

PrintProperty^
PrintServerProperty::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintServerProperty(name,value);
}

PrintProperty^
PrintServerProperty::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintServerProperty(name,delegate);
}

PrintProperty^
PrintServerProperty::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintServerProperty(name,value,delegate);
}


PrintServerProperty::
operator
PrintServer^(
    PrintServerProperty % attribValue
    )
{
    return (PrintServer^)attribValue.Value;
}


PrintServerProperty::
operator
PrintServer^(
    PrintServerProperty^ attribValue
    )
{
    return (PrintServer^)attribValue->Value;
}

PrintServer^
PrintServerProperty::
ToPrintServer(
    PrintServerProperty % attribValue
    )
{
    return (PrintServer^)attribValue.Value;
}

PrintSystemDelegates::
PrintServerValueChanged^
PrintServerProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintServerProperty::ChangeHandler::
set(
    PrintSystemDelegates::PrintServerValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintServerProperty::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        delete value;
                        value         = nullptr;
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*--------------------------------------------------------------------------------------*/
/*                          PrintProcessorProperty Implementation                       */
/*--------------------------------------------------------------------------------------*/

PrintProcessorProperty::
PrintProcessorProperty(
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintProcessorProperty::
PrintProcessorProperty(
    String^ name,
    Object^ value
    ):
PrintProperty(name),
changeHandler(nullptr)
{
    this->Value = value;
}

PrintProcessorProperty::
PrintProcessorProperty(
    String^             name,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::PrintProcessorValueChanged^)delegate)
{
    IsLinked = true;
}

PrintProcessorProperty::
PrintProcessorProperty(
    String^             name,
    Object^             value,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::PrintProcessorValueChanged^)delegate)
{
    this->Value = value;
    IsLinked = true;
}

Object^
PrintProcessorProperty::Value::
get(
    void
    )
{
    return value;
}

void
PrintProcessorProperty::Value::
set(
    Object^ inValue
    )
{
    Type^ inType = inValue->GetType();

    if(inType == PrintProcessor::typeid)
    {
        if(value != inValue)
        {
            value = (PrintProcessor^)inValue;

            if(ChangeHandler)
            {
                ChangeHandler->Invoke(value);
            }

            if(IsInternallyInitialized)
            {
                IsInternallyInitialized = false;
                IsInitialized           = true;
                IsDirty                 = false;
            }
            else
            {
                IsDirty = true;
            }
        }
    }
}

PrintProperty^
PrintProcessorProperty::
Create(
    String^ name
    )
{
    return gcnew PrintProcessorProperty(name);
}

PrintProperty^
PrintProcessorProperty::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintProcessorProperty(name,value);
}

PrintProperty^
PrintProcessorProperty::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintProcessorProperty(name,delegate);
}

PrintProperty^
PrintProcessorProperty::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintProcessorProperty(name,value,delegate);
}


PrintProcessorProperty::
operator
PrintProcessor^(
    PrintProcessorProperty % attribValue
    )
{
    return (PrintProcessor^)attribValue.Value;
}


PrintProcessorProperty::
operator
PrintProcessor^(
    PrintProcessorProperty^ attribValue
    )
{
    return (PrintProcessor^)attribValue->Value;
}

PrintProcessor^
PrintProcessorProperty::
ToPrintProcessor(
    PrintProcessorProperty % attribValue
    )
{
    return (PrintProcessor^)attribValue.Value;
}

PrintSystemDelegates::
PrintProcessorValueChanged^
PrintProcessorProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintProcessorProperty::ChangeHandler::
set(
    PrintSystemDelegates::PrintProcessorValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintProcessorProperty::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        delete value;
                        value         = nullptr;
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*--------------------------------------------------------------------------------------*/
/*                          PrintQueueProperty Implementation                           */
/*--------------------------------------------------------------------------------------*/

PrintQueueProperty::
PrintQueueProperty(
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintQueueProperty::
PrintQueueProperty(
    String^ name,
    Object^ value
    ):
PrintProperty(name),
changeHandler(nullptr)
{
    this->Value = value;
}

PrintQueueProperty::
PrintQueueProperty(
    String^             name,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::PrintQueueValueChanged^)delegate)
{
    IsLinked = true;
}

PrintQueueProperty::
PrintQueueProperty(
    String^             name,
    Object^             value,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::PrintQueueValueChanged^)delegate)
{
    this->Value = value;
    IsLinked = true;
}

Object^
PrintQueueProperty::Value::
get(
    void
    )
{
    return value;
}

void
PrintQueueProperty::Value::
set(
    Object^ inValue
    )
{
    Type^ inType = inValue->GetType();

    if(inType == PrintQueue::typeid)
    {
        if(value != inValue)
        {
            value = (PrintQueue^)inValue;

            if(ChangeHandler)
            {
                ChangeHandler->Invoke(value);
            }

            if(IsInternallyInitialized)
            {
                IsInternallyInitialized = false;
                IsInitialized           = true;
                IsDirty                 = false;
            }
            else
            {
                IsDirty = true;
            }
        }
    }
}

PrintProperty^
PrintQueueProperty::
Create(
    String^ name
    )
{
    return gcnew PrintQueueProperty(name);
}

PrintProperty^
PrintQueueProperty::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintQueueProperty(name,value);
}

PrintProperty^
PrintQueueProperty::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintQueueProperty(name,delegate);
}

PrintProperty^
PrintQueueProperty::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintQueueProperty(name,value,delegate);
}


PrintQueueProperty::
operator
PrintQueue^(
    PrintQueueProperty% attribValue
    )
{
    return (PrintQueue^)attribValue.Value;
}


PrintQueueProperty::
operator
PrintQueue^(
    PrintQueueProperty^ attribValue
    )
{
    return (PrintQueue^)attribValue->Value;
}

PrintQueue^
PrintQueueProperty::
ToPrintQueue(
    PrintQueueProperty% attribValue
    )
{
    return (PrintQueue^)attribValue.Value;
}

PrintSystemDelegates::
PrintQueueValueChanged^
PrintQueueProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintQueueProperty::ChangeHandler::
set(
    PrintSystemDelegates::PrintQueueValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintQueueProperty::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        delete value;
                        value         = nullptr;
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*-------------------------------------------------------------------------------------*/
/*                PrintSystemTypeProperty Implementation                              */
/*-------------------------------------------------------------------------------------*/

PrintSystemTypeProperty ::
PrintSystemTypeProperty (
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintSystemTypeProperty ::
PrintSystemTypeProperty (
    String^ name,
    Object^ value
    ):
PrintProperty(name),
changeHandler(nullptr)
{
    this->Value = value;
}

PrintSystemTypeProperty ::
PrintSystemTypeProperty (
    String^             name,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::SystemTypeValueChanged^)delegate)
{
    IsLinked = true;
}

PrintSystemTypeProperty ::
PrintSystemTypeProperty (
    String^             name,
    Object^             value,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::SystemTypeValueChanged^)delegate)
{
    this->Value = value;
    IsLinked = true;
}

Object^
PrintSystemTypeProperty ::Value::
get(
    void
    )
{
    return value;
}

void
PrintSystemTypeProperty ::Value::
set(
    Object^ inValue
    )
{
    Type^ inType = (Type^)inValue;

    if(inType)
    {
        if(value != inValue)
        {
            value = (System::Type^)inValue;

            if(ChangeHandler)
            {
                ChangeHandler->Invoke(value);
            }

            if(IsInternallyInitialized)
            {
                IsInternallyInitialized = false;
                IsInitialized           = true;
                IsDirty                 = false;
            }
            else
            {
                IsDirty = true;
            }
        }
    }
}

PrintProperty^
PrintSystemTypeProperty ::
Create(
    String^ name
    )
{
    return gcnew PrintSystemTypeProperty (name);
}

PrintProperty^
PrintSystemTypeProperty ::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintSystemTypeProperty (name,value);
}

PrintProperty^
PrintSystemTypeProperty ::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintSystemTypeProperty (name,delegate);
}

PrintProperty^
PrintSystemTypeProperty ::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintSystemTypeProperty (name,value,delegate);
}


PrintSystemTypeProperty ::
operator
System::Type^(
    PrintSystemTypeProperty % attribValue
    )
{
    return (System::Type^)attribValue.Value;
}


PrintSystemTypeProperty ::
operator
System::Type^(
    PrintSystemTypeProperty^ attribValue
    )
{
    return (System::Type^)attribValue->Value;
}

System::Type^
PrintSystemTypeProperty ::
ToType(
    PrintSystemTypeProperty % attribValue
    )
{
    return (System::Type^)attribValue.Value;
}

PrintSystemDelegates::
SystemTypeValueChanged^
PrintSystemTypeProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintSystemTypeProperty::ChangeHandler::
set(
    PrintSystemDelegates::SystemTypeValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintSystemTypeProperty ::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        value         = nullptr;
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}
/*--------------------------------------------------------------------------------------*/
/*                       PrintJobPriorityProperty Implementation                        */
/*--------------------------------------------------------------------------------------*/

PrintJobPriorityProperty ::
PrintJobPriorityProperty (
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintJobPriorityProperty ::
PrintJobPriorityProperty (
    String^ name,
    Object^ value
    ):
PrintProperty(name),
changeHandler(nullptr)
{
    this->Value = value;
}

PrintJobPriorityProperty ::
PrintJobPriorityProperty (
    String^             name,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::JobPriorityValueChanged^)delegate)
{
    IsLinked = true;
}

PrintJobPriorityProperty ::
PrintJobPriorityProperty (
    String^             name,
    Object^             value,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::JobPriorityValueChanged^)delegate)
{
    this->Value = value;
    IsLinked = true;
}

Object^
PrintJobPriorityProperty ::Value::
get(
    void
    )
{
    return value;
}

void
PrintJobPriorityProperty::Value::
set(
    Object^ inValue
    )
{
    Type^ inType = inValue->GetType();

    if(inType == PrintJobPriority::typeid)
    {
        if(PrintJobPriority^ intValue = (PrintJobPriority^)inValue)
        {
            if (value != *intValue)
            {
                value = *intValue;

                if(ChangeHandler)
                {
                    ChangeHandler->Invoke(value);
                }

                if(IsInternallyInitialized)
                {
                    IsInternallyInitialized = false;
                    IsInitialized           = true;
                    IsDirty                 = false;
                }
                else
                {
                    IsDirty = true;
                }
            }
        }
    }
}

PrintProperty^
PrintJobPriorityProperty ::
Create(
    String^ name
    )
{
    return gcnew PrintJobPriorityProperty (name);
}

PrintProperty^
PrintJobPriorityProperty ::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintJobPriorityProperty (name,value);
}

PrintProperty^
PrintJobPriorityProperty ::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintJobPriorityProperty (name,delegate);
}

PrintProperty^
PrintJobPriorityProperty ::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintJobPriorityProperty (name,value,delegate);
}


PrintJobPriorityProperty ::
operator
PrintJobPriority(
    PrintJobPriorityProperty % attribValue
    )
{
    return (PrintJobPriority)attribValue.Value;
}


PrintJobPriorityProperty ::
operator
PrintJobPriority(
    PrintJobPriorityProperty^ attribValue
    )
{
    return (PrintJobPriority)attribValue->Value;
}

PrintJobPriority^
PrintJobPriorityProperty ::
ToPrintJobPriority(
    PrintJobPriorityProperty % attribValue
    )
{
    return (PrintJobPriority^)attribValue.Value;
}

PrintSystemDelegates::
JobPriorityValueChanged^
PrintJobPriorityProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintJobPriorityProperty::ChangeHandler::
set(
    PrintSystemDelegates::JobPriorityValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintJobPriorityProperty ::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*--------------------------------------------------------------------------------------*/
/*                PrintSystemJobTypeAttributeValue Implementation                   */
/*--------------------------------------------------------------------------------------*/

PrintSystemJobTypeAttributeValue ::
PrintSystemJobTypeAttributeValue (
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintSystemJobTypeAttributeValue ::
PrintSystemJobTypeAttributeValue (
    String^ name,
    Object^ value
    ):
PrintProperty(name),
changeHandler(nullptr)
{
    this->Value = value;
}

PrintSystemJobTypeAttributeValue ::
PrintSystemJobTypeAttributeValue (
    String^             name,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::JobTypeValueChanged^)delegate)
{
    IsLinked = true;
}

PrintSystemJobTypeAttributeValue ::
PrintSystemJobTypeAttributeValue (
    String^             name,
    Object^             value,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::JobTypeValueChanged^)delegate)
{
    this->Value = value;
    IsLinked = true;
}

Object^
PrintSystemJobTypeAttributeValue ::Value::
get(
    void
    )
{
    return value;
}

void
PrintSystemJobTypeAttributeValue::Value::
set(
    Object^ inValue
    )
{
    Type^ inType = inValue->GetType();

    if(inType == PrintJobType::typeid)
    {
        if(PrintJobType^ intValue = (PrintJobType^)inValue)
        {
            if (value != *intValue)
            {
                value = *intValue;

                if(ChangeHandler)
                {
                    ChangeHandler->Invoke(value);
                }

                if(IsInternallyInitialized)
                {
                    IsInternallyInitialized = false;
                    IsInitialized           = true;
                    IsDirty                 = false;
                }
                else
                {
                    IsDirty = true;
                }
            }
        }
    }
}

PrintProperty^
PrintSystemJobTypeAttributeValue ::
Create(
    String^ name
    )
{
    return gcnew PrintSystemJobTypeAttributeValue (name);
}

PrintProperty^
PrintSystemJobTypeAttributeValue ::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintSystemJobTypeAttributeValue (name,value);
}

PrintProperty^
PrintSystemJobTypeAttributeValue ::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintSystemJobTypeAttributeValue (name,delegate);
}

PrintProperty^
PrintSystemJobTypeAttributeValue ::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintSystemJobTypeAttributeValue (name,value,delegate);
}


PrintSystemJobTypeAttributeValue ::
operator
PrintJobType(
    PrintSystemJobTypeAttributeValue % attribValue
    )
{
    return (PrintJobType)attribValue.Value;
}


PrintSystemJobTypeAttributeValue ::
operator
PrintJobType(
    PrintSystemJobTypeAttributeValue^ attribValue
    )
{
    return (PrintJobType)attribValue->Value;
}

PrintJobType^
PrintSystemJobTypeAttributeValue ::
ToPrintJobType(
    PrintSystemJobTypeAttributeValue % attribValue
    )
{
    return (PrintJobType^)attribValue.Value;
}

PrintSystemDelegates::
JobTypeValueChanged^
PrintSystemJobTypeAttributeValue::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintSystemJobTypeAttributeValue::ChangeHandler::
set(
    PrintSystemDelegates::JobTypeValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintSystemJobTypeAttributeValue ::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*-------------------------------------------------------------------------------------*/
/*                          PrintJobStatusProperty Implementation                      */
/*-------------------------------------------------------------------------------------*/

PrintJobStatusProperty ::
PrintJobStatusProperty (
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintJobStatusProperty ::
PrintJobStatusProperty (
    String^ name,
    Object^ value
    ):
PrintProperty(name),
changeHandler(nullptr)
{
    this->Value = value;
}

PrintJobStatusProperty ::
PrintJobStatusProperty (
    String^             name,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::JobStatusValueChanged^)delegate)
{
    IsLinked = true;
}

PrintJobStatusProperty ::
PrintJobStatusProperty (
    String^             name,
    Object^             value,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::JobStatusValueChanged^)delegate)
{
    this->Value = value;
    IsLinked = true;
}

Object^
PrintJobStatusProperty::Value::
get(
    void
    )
{
    return value;
}

void
PrintJobStatusProperty::Value::
set(
    Object^ inValue
    )
{
    Type^ inType = inValue->GetType();

    if(inType == PrintJobStatus::typeid)
    {
        if(PrintJobStatus^ intValue = (PrintJobStatus^)inValue)
        {
            if (value != *intValue)
            {
                value = *intValue;

                if(ChangeHandler)
                {
                    ChangeHandler->Invoke(value);
                }

                if(IsInternallyInitialized)
                {
                    IsInternallyInitialized = false;
                    IsInitialized           = true;
                    IsDirty                 = false;
                }
                else
                {
                    IsDirty = true;
                }
            }
        }
    }
}

PrintProperty^
PrintJobStatusProperty ::
Create(
    String^ name
    )
{
    return gcnew PrintJobStatusProperty (name);
}

PrintProperty^
PrintJobStatusProperty ::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintJobStatusProperty (name,value);
}

PrintProperty^
PrintJobStatusProperty ::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintJobStatusProperty (name,delegate);
}

PrintProperty^
PrintJobStatusProperty ::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintJobStatusProperty (name,value,delegate);
}


PrintJobStatusProperty ::
operator
PrintJobStatus(
    PrintJobStatusProperty % attribValue
    )
{
    return (PrintJobStatus)attribValue.Value;
}


PrintJobStatusProperty ::
operator
PrintJobStatus(
    PrintJobStatusProperty^ attribValue
    )
{
    return (PrintJobStatus)attribValue->Value;
}

PrintJobStatus^
PrintJobStatusProperty ::
ToPrintJobStatus(
    PrintJobStatusProperty % attribValue
    )
{
    return (PrintJobStatus^)attribValue.Value;
}

PrintSystemDelegates::
JobStatusValueChanged^
PrintJobStatusProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintJobStatusProperty::ChangeHandler::
set(
    PrintSystemDelegates::JobStatusValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintJobStatusProperty::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*-------------------------------------------------------------------------------------*/
/*                          PrintDateTimeProperty Implementation                       */
/*-------------------------------------------------------------------------------------*/

PrintDateTimeProperty ::
PrintDateTimeProperty (
    String^ name
    ):
PrintProperty(name),
changeHandler(nullptr)
{
}

PrintDateTimeProperty ::
PrintDateTimeProperty (
    String^ name,
    Object^ value
    ):
PrintProperty(name),
changeHandler(nullptr)
{
    this->Value = value;
}

PrintDateTimeProperty ::
PrintDateTimeProperty (
    String^             name,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::SystemDateTimeValueChanged^)delegate)
{
    IsLinked = true;
}

PrintDateTimeProperty ::
PrintDateTimeProperty (
    String^             name,
    Object^             value,
    MulticastDelegate^  delegate
    ):
PrintProperty(name),
changeHandler((PrintSystemDelegates::SystemDateTimeValueChanged^)delegate)
{
    this->Value = value;
    IsLinked = true;
}

Object^
PrintDateTimeProperty::Value::
get(
    void
    )
{
    return value;
}

void
PrintDateTimeProperty::Value::
set(
    Object^ inValue
    )
{
    Type^ inType = inValue->GetType();

    if(inType == System::DateTime::typeid)
    {
        if(System::DateTime^ intValue = (System::DateTime^)inValue)
        {
            if (value != *intValue)
            {
                value = *intValue;

                if(ChangeHandler)
                {
                    ChangeHandler->Invoke(value);
                }

                if(IsInternallyInitialized)
                {
                    IsInternallyInitialized = false;
                    IsInitialized           = true;
                    IsDirty                 = false;
                }
                else
                {
                    IsDirty = true;
                }
            }
        }
    }
}

PrintProperty^
PrintDateTimeProperty ::
Create(
    String^ name
    )
{
    return gcnew PrintDateTimeProperty (name);
}

PrintProperty^
PrintDateTimeProperty ::
Create(
    String^ name,
    Object^ value
    )
{
    return gcnew PrintDateTimeProperty (name,value);
}

PrintProperty^
PrintDateTimeProperty ::
Create(
    String^            name,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintDateTimeProperty (name,delegate);
}

PrintProperty^
PrintDateTimeProperty ::
Create(
    String^            name,
    Object^            value,
    MulticastDelegate^ delegate
    )
{
    return gcnew PrintDateTimeProperty (name,value,delegate);
}


PrintDateTimeProperty ::
operator
System::DateTime^(
    PrintDateTimeProperty % attribValue
    )
{
    return (System::DateTime^)attribValue.Value;
}


PrintDateTimeProperty ::
operator
System::DateTime^(
    PrintDateTimeProperty^ attribValue
    )
{
    return (System::DateTime^)attribValue->Value;
}

System::DateTime^
PrintDateTimeProperty ::
ToDateTime(
    PrintDateTimeProperty % attribValue
    )
{
    return (System::DateTime^)attribValue.Value;
}

PrintSystemDelegates::
SystemDateTimeValueChanged^
PrintDateTimeProperty::ChangeHandler::
get(
    void
    )
{
    return changeHandler;
}

void
PrintDateTimeProperty::ChangeHandler::
set(
    PrintSystemDelegates::SystemDateTimeValueChanged^ newChangeHandler
    )
{
    changeHandler = newChangeHandler;
}

void
PrintDateTimeProperty ::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        ChangeHandler = nullptr;
                    }
                }
                PrintProperty::InternalDispose(disposing);
            }
            __finally
            {
                this->IsDisposed = true;
                System::Threading::Monitor::Exit(this);
            }
        }
    }
}

/*-------------------------------------------------------------------------------------*/
/*                         PrintPropertyDictionary Implementation                      */
/*-------------------------------------------------------------------------------------*/

PrintPropertyDictionary::
PrintPropertyDictionary(
    void
    )
{
}

PrintPropertyDictionary::
~PrintPropertyDictionary(
    void
    )
{
}

void
PrintPropertyDictionary::
Add(
    PrintProperty^ attributeValue
    )
{
    Hashtable::Add(attributeValue->Name,attributeValue);
}

PrintProperty^
PrintPropertyDictionary::
GetProperty(
    String^ retrievedPropertyName
    )
{
    return (PrintProperty^)Hashtable::default::get(retrievedPropertyName);
}

void
PrintPropertyDictionary::
SetProperty(
    String^ newPropertyName,
    PrintProperty^ newProperty
    )
{
    if(newProperty->Name->Equals(newPropertyName))
    {
        PrintProperty^ printProperty = (PrintProperty^)Hashtable::default::get(newPropertyName);

        printProperty->Value = newProperty->Value;
    }
}

PrintPropertyDictionary::
PrintPropertyDictionary(
    SerializationInfo^ info,
    StreamingContext   context
    ):
Hashtable(info,context)
{
}

void
PrintPropertyDictionary::
GetObjectData(
    SerializationInfo^ info,
    StreamingContext   context
    )
{
    Hashtable::GetObjectData(info,context);
}

void
PrintPropertyDictionary::
OnDeserialization(
    Object^ sender
    )
{
    Hashtable::OnDeserialization(sender);
}
