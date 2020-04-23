// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
    Abstract:

        Managed wrapper for Win32 print APIs. This object wraps a printer handle
        and does gets, sets and enum operations.It also provides static methods
        for adding and deleting a printer and enumerating printers on a print server.

--*/

#include "win32inc.hpp"

using namespace System;
using namespace System::IO;
using namespace System::Collections;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;
using namespace System::Collections::Specialized;
using namespace System::Xml;
using namespace System::Xml::XPath;
using namespace System::Drawing::Printing;

using namespace System::Windows::Xps::Packaging;

#ifndef  __INTEROPNAMESPACEUSAGE_HPP__
#include <InteropNamespaceUsage.hpp>
#endif

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#ifndef  __PRINTERDATATYPES_HPP__
#include <PrinterDataTypes.hpp>
#endif

#ifndef  __GENERICTHUNKINGINC_HPP__
#include <GenericThunkingInc.hpp>
#endif

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif


using namespace System;
using namespace MS::Internal::PrintWin32Thunk;
using namespace MS::Internal::PrintWin32Thunk::DirectInteropForPrintQueue;
using namespace MS::Internal::PrintWin32Thunk::DirectInteropForJob;
using namespace MS::Internal::PrintWin32Thunk::Win32ApiThunk;
using namespace System::Threading;

using namespace System::Printing;
using namespace System::Printing::IndexedProperties;

using namespace MS::Internal::PrintWin32Thunk::Win32ApiThunk;
/*++

Routine Name:

    PrinterThunkHandler

Routine Description:

    Constructor. The object will be initialized with a nullptr string
    and nullptr defaults. A local server handle will be opened.

Arguments:

    None

Return Value:

    N\A

--*/
PrinterThunkHandler::
PrinterThunkHandler(
    void
    ) : printerName(nullptr),
        printerDefaults(nullptr),
        isRunningDownLevel(false)
{
}
/*++

Routine Name:

    PrinterThunkHandler

Routine Description:

    Constructor. The object will be initialized with nullptr defaults.

Arguments:

    name - name of the printer to open

Return Value:

    N\A

--*/
PrinterThunkHandler::
PrinterThunkHandler(
    String^ name
    ) : printerName(name),
        printerDefaults(nullptr),
        isRunningDownLevel(false)
{
    ThunkOpenPrinter(printerName, nullptr);
}

/*++

Routine Name:

    PrinterThunkHandler

Routine Description:

    The object will be initialized with a Win32 handle
    that will be closed when the object is disposed.

Arguments:

    win32PrintHandle - Win32 printer handle.

Return Value:

    N\A

--*/
PrinterThunkHandler::
PrinterThunkHandler(
    IntPtr  win32PrintHandle
    ) : printerName(nullptr),
        printerDefaults(nullptr),
        isRunningDownLevel(false)
{
    if (win32PrintHandle == IntPtr::Zero)
    {
        InternalExceptionResourceManager^ manager = gcnew InternalExceptionResourceManager();

        throw gcnew ArgumentException(manager->GetString("ArgumentException.InvalidHandle",
                                                         Thread::CurrentThread->CurrentUICulture),
                                      "win32PrintHandle");
    }

    SetHandle(handle);
}



/*++

Routine Name:

    PrinterThunkHandler

Routine Description:

    The object will be initialized with a Win32 handle
    that will be closed when the object is disposed.

Arguments:

    name     - name of the printer or server
    defaults - printer defaults used when calling OpenPrinter API

Return Value:

    N\A

--*/
PrinterThunkHandler::
PrinterThunkHandler(
    String^             name,
    PrinterDefaults^    defaults
    ) : printerName(name),
        printerDefaults(defaults),
        isRunningDownLevel(false)
{
    ThunkOpenPrinter(printerName, printerDefaults);
}

/*++

Routine Name:

    get_IsInvalid

Routine Description:

    Checks the object validity

Arguments:

    None

Return Value:

    true if the object contains a valid Win3e2 printer handle

--*/
Boolean
PrinterThunkHandler::IsInvalid::
get(
    void
    )
{
    return handle == IntPtr::Zero;
}

Boolean
PrinterThunkHandler::
ReleaseHandle(
    void
    )
{
    try
    {
        if (!IsInvalid)
        {
            ThunkClosePrinter();
        }
    }
    __finally
    {
        if (printerDefaults)
        {
            //printerDefaults->Dispose();
            delete printerDefaults;
        }
    }

    return true;
}


PrinterThunkHandler^
PrinterThunkHandler::
DuplicateHandler(
    void
    )
{
    PrinterThunkHandler^ result = gcnew PrinterThunkHandler(printerName, printerDefaults);
    result->isInPartialTrust = isInPartialTrust;

    return result;
}

/*++

Routine Name:

    ThunkOpenPrinter

Routine Description:

    The routine opens a Win32 print handle given a
    name and defaults parameters. It calls OpenPrinter API using PInvoke.

Arguments:

    printerName         - printer name to be opened
    openPrinterDefaults - default parameters to open the printer with

Return Value:

    N\A

--*/
Boolean
PrinterThunkHandler::
ThunkOpenPrinter(
    String^                 printerName,
    PrinterDefaults^        openPrinterDefaults
    )
{
    Boolean     returnValue    = false;

    IntPtr hOpenPrinter;

    returnValue = UnsafeNativeMethods::InvokeOpenPrinter(printerName,
                                                       &hOpenPrinter,
                                                       openPrinterDefaults);

    if (returnValue)
    {
        SetHandle(hOpenPrinter);
    }
    else
    {
        InternalPrintSystemException::ThrowLastError();
    }

    return returnValue;
}

/*++

Routine Name:

    ThunkClosePrinter

Routine Description:

    The routine closes a Win32 print handle by
    directly calling the ClosePrinter API in IJW mode.

Arguments:

    none

Return Value:

    N\A

--*/
Boolean
PrinterThunkHandler::
ThunkClosePrinter(
    void
    )
{
    Boolean returnValue = false;

    try
    {
        //
        // If ClosePrinter fails, there isn't something we can do. We won't throw in this case.
        //
        returnValue = UnsafeNativeMethods::InvokeClosePrinter(handle);
    }
    __finally
    {
        handle = (IntPtr)nullptr;
    }

    return returnValue;
}


/*++

Routine Name:

    ThunkDeletePrinter

Routine Description:

    The routine deletes the printer bound to the intarnal
    handle by calling the DeletePrinter API in IJW mode.

Arguments:

    none

Return Value:

    N\A

--*/
Boolean
PrinterThunkHandler::
ThunkDeletePrinter(
    void
    )
{
    Boolean    returnValue = false;

    returnValue = UnsafeNativeMethods::InvokeDeletePrinter(handle);

    if (!returnValue)
    {
        InternalPrintSystemException::ThrowLastError();
    }
    else
    {
        handle = (IntPtr)nullptr;
    }

    return returnValue;
}

/*++

Routine Name:

    ThunkSetPrinter

Routine Description:

    The routine sets a command on the printer bound to the internal handle
    by calling the SetPrinter API in IJW mode.

Arguments:

    command - command to be set on the printer

Return Value:

    true if succeeded

--*/
Boolean
PrinterThunkHandler::
ThunkSetPrinter(
    UInt32      command
    )
{
    Boolean    returnValue     = false;


    returnValue = UnsafeNativeMethods::InvokeSetPrinter(handle,
                                                      0,
                                                      SafeMemoryHandle::Null,
                                                      command) == TRUE;

    if (!returnValue)
    {
        InternalPrintSystemException::ThrowLastError();
    }

    return returnValue;
}

/*++

Routine Name:

    ThunkSetPrinter

Routine Description:

    The routine sets a command on the printer bound to the internal handle
    by calling the SetPrinter API in IJW mode.

Arguments:

    level               - Win32 level to be set
    win32PrinterInfo    - pointer to Win32 unmanaged buffer to be set

Return Value:

    true if succeeded

--*/
Boolean
PrinterThunkHandler::
ThunkSetPrinter(
    UInt32            level,
    SafeMemoryHandle^ win32PrinterInfo
    )
{
    Boolean    returnValue     = false;

    if (!win32PrinterInfo->IsInvalid)
    {

        returnValue = UnsafeNativeMethods::InvokeSetPrinter(handle,
                                                          level,
                                                          win32PrinterInfo,
                                                          0) == TRUE;

        if (!returnValue)
        {
            InternalPrintSystemException::ThrowLastError();
        }
    }


    return returnValue;
}

/*++

Routine Name:

    ThunkGetPrinter

Routine Description:

    The routine gets printer data from the server by calling the GetPrinter API in IJW mode.
    a command on the printer bound to the internal handle
    by calling the SetPrinter API in IJW mode.

Arguments:

    level               - Win32 level to be set
    win32PrinterInfo    - pointer to Win32 unmanaged buffer to be set

Return Value:

    Pointer to the managed object that wraps the unmanaged data and exposes it as managed objects.

--*/
IPrinterInfo^
PrinterThunkHandler::
ThunkGetPrinter(
    UInt32  level
    )
{
    IPrinterInfo^       printerInfo     = nullptr;
    SafeMemoryHandle^   win32HeapBuffer = nullptr;

    try
    {
        UInt32   bytesNeeded     = 0;
        UInt32   byteCount       = 0;
        Boolean  returnValue     = false;

        returnValue = UnsafeNativeMethods::InvokeGetPrinter(handle,
                                                          level,
                                                          SafeMemoryHandle::Null,
                                                          0,
                                                          &bytesNeeded);


        InternalPrintSystemException::ThrowIfLastErrorIsNot(ERROR_INSUFFICIENT_BUFFER);

        if (byteCount = bytesNeeded)
        {
            if (SafeMemoryHandle::TryCreate(byteCount, win32HeapBuffer))
            {
                returnValue = UnsafeNativeMethods::InvokeGetPrinter(handle,
                                                                  level,
                                                                  win32HeapBuffer,
                                                                  byteCount,
                                                                  &bytesNeeded);

                if (returnValue)
                {
                    //this method takes ownership win32HeapBuffer
                    printerInfo = GetManagedPrinterInfoObject(level, win32HeapBuffer, 1);
                }
                else
                {
                    InternalPrintSystemException::ThrowLastError();
                }
            }
            else
            {
                InternalPrintSystemException::ThrowIfNotSuccess(ERROR_OUTOFMEMORY);
            }
        }
    }
    __finally
    {
    }

    return printerInfo;
}

/*++

Routine Name:

    ThunkGetPrinterDataString

Routine Description:

    The routine gets the printer data associated with a value for the printer that the object is bound to.
    The printer data must be of type string.It calls GetPrinterDataW API in IJW mode.

Arguments:

    handle   - printer handle
    valueName       - value name identifier string

Return Value:

    Pointer to printer data string
--*/
Object^
PrinterThunkHandler::
ThunkGetPrinterDataStringInternal(
    String^                 valueName
    )
{
    String^            printerData      = nullptr;
    SafeMemoryHandle^  win32HeapBuffer  = nullptr;

    try
    {
        if (valueName)
        {
            UInt32  bytesNeeded     = 0;
            UInt32  byteCount       = 0;
            UInt32  returnValue     = 0;
            UInt32  registryType    = 0;

            returnValue = UnsafeNativeMethods::InvokeGetPrinterData(handle,
                                                                  valueName,
                                                                  &registryType,
                                                                  SafeMemoryHandle::Null,
                                                                  0,
                                                                  &bytesNeeded);

            if (byteCount = bytesNeeded)
            {
                if (SafeMemoryHandle::TryCreate(byteCount, win32HeapBuffer))
                {
                    returnValue = UnsafeNativeMethods::InvokeGetPrinterData(handle,
                                                                          valueName,
                                                                          &registryType,
                                                                          win32HeapBuffer,
                                                                          byteCount,
                                                                          &bytesNeeded);

                    if (returnValue == ERROR_SUCCESS && registryType == REG_SZ)
                    {
                        printerData = gcnew String(reinterpret_cast<WCHAR*>(win32HeapBuffer->DangerousGetHandle().ToPointer()));
                    }

                    InternalPrintSystemException::ThrowIfNotSuccess(returnValue);
                    win32HeapBuffer->ReleaseHandle();
                }
                else
                {
                    InternalPrintSystemException::ThrowIfNotSuccess(ERROR_OUTOFMEMORY);
                }
            }
            else
            {
                if (returnValue != ERROR_SUCCESS)
                {
                    InternalPrintSystemException::ThrowIfErrorIsNot(returnValue, ERROR_MORE_DATA);
                }
            }
        }
    }
    __finally
    {
    }

    return printerData;
}

Object^
PrinterThunkHandler::
ThunkGetPrinterDataString(
    PrinterThunkHandler^    printerThunkHandler,
    String^                 valueName
    )
{
    return printerThunkHandler->ThunkGetPrinterDataStringInternal(valueName);
}

/*++

Routine Name:

    ThunkGetPrinterDataInt32

Routine Description:

    The routine gets the printer data associated with a value for the printer that the object is bound to.
    The printer data must be of type Int32.It calls GetPrinterDataW API in IJW mode.

Arguments:

    handle   - printer handle
    valueName       - value name identifier string

Return Value:

    Printer data
--*/
Object^
PrinterThunkHandler::
ThunkGetPrinterDataInt32Internal(
    String^                 valueName
    )
{
    Int32              printerData      = 0;
    SafeMemoryHandle^  win32HeapBuffer  = nullptr;

    try
    {
        if (valueName)
        {
            UInt32  bytesNeeded     = 0;
            UInt32  byteCount       = 0;
            UInt32  returnValue     = 0;
            UInt32  registryType    = 0;

            returnValue = UnsafeNativeMethods::InvokeGetPrinterData(handle,
                                                                  valueName,
                                                                  &registryType,
                                                                  SafeMemoryHandle::Null,
                                                                  0,
                                                                  &bytesNeeded);

            if (byteCount = bytesNeeded)
            {
                if (SafeMemoryHandle::TryCreate(byteCount, win32HeapBuffer))
                {
                    returnValue = UnsafeNativeMethods::InvokeGetPrinterData(handle,
                                                                          valueName,
                                                                          &registryType,
                                                                          win32HeapBuffer,
                                                                          byteCount,
                                                                          &bytesNeeded);

                    if (returnValue == ERROR_SUCCESS && registryType == REG_DWORD)
                    {
                        printerData =*(reinterpret_cast<DWORD*>(win32HeapBuffer->DangerousGetHandle().ToPointer()));
                    }

                    InternalPrintSystemException::ThrowIfNotSuccess(returnValue);
                    win32HeapBuffer->ReleaseHandle();
                }
                else
                {
                    InternalPrintSystemException::ThrowIfNotSuccess(ERROR_OUTOFMEMORY);
                }
            }
            else
            {
                if (returnValue != ERROR_SUCCESS)
                {
                    InternalPrintSystemException::ThrowIfErrorIsNot(returnValue, ERROR_MORE_DATA);
                }
            }
        }
    }
    __finally
    {
    }

    return printerData;
}

Object^
PrinterThunkHandler::
ThunkGetPrinterDataInt32(
    PrinterThunkHandler^    printerThunkHandler,
    String^                 valueName
    )
{
    return printerThunkHandler->ThunkGetPrinterDataInt32Internal(valueName);
}

/*++

Routine Name:

    ThunkGetPrinterDataBoolean

Routine Description:

    The routine gets the printer data associated with a value for the printer that the object is bound to.
    The printer data must be of type Boolean. It calls GetPrinterDataW API in IJW mode.

Arguments:

    handle   - printer handle
    valueName       - value name identifier string

Return Value:

    Printer data

--*/
Object^
PrinterThunkHandler::
ThunkGetPrinterDataBooleanInternal(
    String^                 valueName
    )
{
    Int32 value = *((Int32^)ThunkGetPrinterDataInt32Internal(valueName));
    return !!(value);
}

Object^
PrinterThunkHandler::
ThunkGetPrinterDataBoolean(
    PrinterThunkHandler^    printerThunkHandler,
    String^                 valueName
    )
{
    return printerThunkHandler->ThunkGetPrinterDataBooleanInternal(valueName);
}

/*++

Routine Name:

    ThunkGetPrinterDataThreadPriority

Routine Description:

    The routine gets the printer data associated with a value for the printer that the object is bound to.
    The printer data must be of type ThreadPriority. It calls GetPrinterDataW API in IJW mode.

Arguments:

    valueName - value name identifier string

Return Value:

    Printer data
--*/
Object^
PrinterThunkHandler::
ThunkGetPrinterDataThreadPriorityInternal(
    String^                 valueName
    )
{
    System::Threading::ThreadPriority threadPriority;

    Int32 value = *((Int32^)ThunkGetPrinterDataInt32Internal(valueName));

    switch (value)
    {
        case THREAD_PRIORITY_LOWEST:
        case THREAD_PRIORITY_IDLE:
        {
            threadPriority = System::Threading::ThreadPriority::Lowest;
            break;
        }
        case THREAD_PRIORITY_BELOW_NORMAL:
        {
            threadPriority = System::Threading::ThreadPriority::BelowNormal;
            break;
        }
        case THREAD_PRIORITY_NORMAL:
        {
            threadPriority = System::Threading::ThreadPriority::Normal;
            break;
        }
        case THREAD_PRIORITY_ABOVE_NORMAL:
        {
            threadPriority = System::Threading::ThreadPriority::AboveNormal;
            break;
        }
        case THREAD_PRIORITY_HIGHEST:
        case THREAD_PRIORITY_TIME_CRITICAL:
        {
            threadPriority = System::Threading::ThreadPriority::Highest;
            break;
        }
        case THREAD_PRIORITY_ERROR_RETURN:
        default:
        {
            //
            // We should assert here.
            //
            threadPriority = System::Threading::ThreadPriority::Normal;
            break;
        }
    }

    return threadPriority;
}

Object^
PrinterThunkHandler::
ThunkGetPrinterDataThreadPriority(
    PrinterThunkHandler^    printerThunkHandler,
    String^                 valueName
    )
{
    return printerThunkHandler->ThunkGetPrinterDataThreadPriorityInternal(valueName);
}

/*++

Routine Name:

    ThunkSetPrinterDataThreadPriority

Routine Description:

    The routine Sets the printer data associated with a value for the printer that the object is bound to.
    The printer data must be of type ThreadPriority.

Arguments:

    valueName - value name to be set
    value     - value

Return Value:

    true if succeeded
--*/
Boolean
PrinterThunkHandler::
ThunkSetPrinterDataThreadPriorityInternal(
    String^                 valueName,
    Object^                 value
    )
{
    ThreadPriority  threadPriority = *((ThreadPriority^)value);
    Int32          priority        = 0;

    switch (threadPriority)
    {
        case System::Threading::ThreadPriority::Lowest:
        {
            priority = THREAD_PRIORITY_LOWEST;
            break;
        }
        case System::Threading::ThreadPriority::BelowNormal:
        {
            priority = THREAD_PRIORITY_BELOW_NORMAL;
            break;
        }
        case System::Threading::ThreadPriority::Normal:
        {
            priority = THREAD_PRIORITY_NORMAL;
            break;
        }
        case System::Threading::ThreadPriority::AboveNormal:
        {
            priority = THREAD_PRIORITY_ABOVE_NORMAL;
            break;
        }
        case System::Threading::ThreadPriority::Highest:
        {
            priority = THREAD_PRIORITY_HIGHEST;
            break;
        }
        default:
        {
            break;
        }
    }

    return ThunkSetPrinterDataInt32Internal(valueName, priority);
}

Boolean
PrinterThunkHandler::
ThunkSetPrinterDataThreadPriority(
    PrinterThunkHandler^    printerThunkHandler,
    String^                 valueName,
    Object^                 value
    )
{
    return printerThunkHandler->ThunkSetPrinterDataThreadPriorityInternal(valueName, value);
}

/*++

Routine Name:

    ThunkGetPrinterDataServerEventLogging

Routine Description:

    The routine gets the printer data associated with a value for the printer that the object is bound to.

Arguments:

    valueName - value name identifier string

Return Value:

    Printer data
--*/
Object^
PrinterThunkHandler::
ThunkGetPrinterDataServerEventLoggingInternal(
    String^                 valueName
    )
{
    PrintServerEventLoggingTypes eventLoggingFlags = PrintServerEventLoggingTypes::LogPrintingErrorEvents | PrintServerEventLoggingTypes::LogPrintingWarningEvents;

    Int32 value = *((Int32^)ThunkGetPrinterDataInt32Internal(valueName));

    if ((value & EVENTLOG_ERROR_TYPE) ||
        (value & EVENTLOG_AUDIT_FAILURE))
    {
        eventLoggingFlags = PrintServerEventLoggingTypes::LogPrintingErrorEvents;
    }
    else if (value & EVENTLOG_AUDIT_SUCCESS)
    {
        eventLoggingFlags = PrintServerEventLoggingTypes::LogPrintingSuccessEvents;
    }
    else if (value & EVENTLOG_INFORMATION_TYPE)
    {
        eventLoggingFlags = PrintServerEventLoggingTypes::LogPrintingInformationEvents;
    }
    else if (value & EVENTLOG_WARNING_TYPE)
    {
        eventLoggingFlags = PrintServerEventLoggingTypes::LogPrintingWarningEvents;
    }

    return eventLoggingFlags;
}

Object^
PrinterThunkHandler::
ThunkGetPrinterDataServerEventLogging(
    PrinterThunkHandler^    printerThunkHandler,
    String^                 valueName
    )
{
    return printerThunkHandler->ThunkGetPrinterDataServerEventLoggingInternal(valueName);
}

/*++

Routine Name:

    ThunkSetPrinterDataServerEventLogging

Routine Description:

    The routine sets the printer data associated with a value for the printer that the object is bound to.

Arguments:

    handle   -   printer handle
    valueName       -   value name identifier string
    value           -   value object

Return Value:

    Printer data
--*/
Boolean
PrinterThunkHandler::
ThunkSetPrinterDataServerEventLoggingInternal(
    String^                 valueName,
    Object^                 value
    )
{
    PrintServerEventLoggingTypes eventLogValue = *((PrintServerEventLoggingTypes^)value);

    return ThunkSetPrinterDataInt32Internal(valueName, static_cast<Int32>(eventLogValue));
}

Boolean
PrinterThunkHandler::
ThunkSetPrinterDataServerEventLogging(
    PrinterThunkHandler^    printerThunkHandler,
    String^                 valueName,
    Object^                 value
    )
{
    return printerThunkHandler->ThunkSetPrinterDataServerEventLoggingInternal(valueName, value);
}

/*++

Routine Name:

    ThunkSetPrinterDataBoolean

Routine Description:

    The routine sets the printer data associated with a value for the printer that the object is bound to.

Arguments:

    handle   -   printer handle
    valueName       -   value name identifier string
    value           -   value object

Return Value:

    Printer data
--*/
Boolean
PrinterThunkHandler::
ThunkSetPrinterDataBooleanInternal(
    String^                 valueName,
    Object^                 value
    )
{
    Boolean booleanValue = *((Boolean^)value);

    return ThunkSetPrinterDataInt32Internal(valueName, static_cast<Int32>(booleanValue));
}

Boolean
PrinterThunkHandler::
ThunkSetPrinterDataBoolean(
    PrinterThunkHandler^    printerThunkHandler,
    String^                 valueName,
    Object^                 value
    )
{
    return printerThunkHandler->ThunkSetPrinterDataBooleanInternal(valueName, value);
}

/*++

Routine Name:

    ThunkSetPrinterData

Routine Description:

    The routine sets the printer data of type string.
    It pInvokes SetPrinterDataW API.

Arguments:

    valueName - value name to be set
    value     - value

Return Value:

    Printer data
--*/
Boolean
PrinterThunkHandler::
ThunkSetPrinterDataString(
    PrinterThunkHandler^    printerThunkHandler,
    String^                 valueName,
    Object^                 value
    )
{
    return printerThunkHandler->ThunkSetPrinterDataStringInternal(valueName, value);
}

/*++

Routine Name:

    ThunkSetPrinterData

Routine Description:

    The routine sets the printer data of type string.
    It pInvokes SetPrinterDataW API.

Arguments:

    valueName - value name to be set
    value     - value

Return Value:

    Printer data
--*/
Boolean
PrinterThunkHandler::
ThunkSetPrinterDataStringInternal(
    String^                 valueName,
    Object^                 value
    )
{
    UInt32    returnValue    = ERROR_SUCCESS;
    IntPtr    valueUnmanaged = (IntPtr)nullptr;
    String^   stringValue    = (String^)value;

    try
    {
        if (value && valueName)
        {
            valueUnmanaged  = Marshal::StringToHGlobalUni(stringValue);

            if (valueUnmanaged != IntPtr::Zero)
            {
                UInt32  byteCount = (stringValue->Length + 1) * sizeof(WCHAR);

                returnValue = UnsafeNativeMethods::InvokeSetPrinterDataIntPtr(handle,
                                                                            valueName,
                                                                            REG_SZ,
                                                                            (IntPtr)valueUnmanaged.ToPointer(),
                                                                             byteCount);

                InternalPrintSystemException::ThrowIfNotSuccess(returnValue);
            }
        }
    }
    __finally
    {
        if (valueUnmanaged != IntPtr::Zero)
        {
            Marshal::FreeHGlobal(valueUnmanaged);
        }
    }

    return returnValue == ERROR_SUCCESS;
}

/*++

Routine Name:

    ThunkSetPrinterDataInt32

Routine Description:

    The routine sets the printer data of type integer.
    It pInvokes SetPrinterDataW API.

Arguments:

    valueName - value name to be set
    value     - value

Return Value:

    Printer data
--*/
Boolean
PrinterThunkHandler::
ThunkSetPrinterDataInt32Internal(
    String^                 valueName,
    Object^                 value
    )
{
    UInt32 returnValue = ERROR_SUCCESS;
    Int32  intValue     = *((Int32^)value);


    returnValue = UnsafeNativeMethods::InvokeSetPrinterDataInt32(handle,
                                                               valueName,
                                                               REG_DWORD,
                                                               &intValue,
                                                               sizeof(DWORD));

    InternalPrintSystemException::ThrowIfNotSuccess(returnValue);


    return (returnValue == ERROR_SUCCESS);
}

/*++

Routine Name:

    ThunkSetPrinterDataInt32

Routine Description:

    The routine sets the printer data of type integer.
    It pInvokes SetPrinterDataW API.

Arguments:

    valueName - value name to be set
    value     - value

Return Value:

    Printer data
--*/
Boolean
PrinterThunkHandler::
ThunkSetPrinterDataInt32(
    PrinterThunkHandler^    printerThunkHandler,
    String^                 valueName,
    Object^                 value
    )
{
    return printerThunkHandler->ThunkSetPrinterDataInt32Internal(valueName, value);
}

/*++

Routine Name:

    ThunkGetDriver

Routine Description:

    The routine gets the driver associated with the the printer that the object is bound to.
    It calls GetPrinterDriver in IJW mode and build the wrapper object around the unmanaged buffer

Arguments:

    level           - level to call
    environment     - string identifying the architecture

Return Value:

    Pointer to the managed object that wraps the unmanaged data and exposes it as managed objects.
--*/
IPrinterInfo^
PrinterThunkHandler::
ThunkGetDriver(
    UInt32  level,
    String^ environment
    )
{
    IPrinterInfo^       driverInfo              = nullptr;
    SafeMemoryHandle^   win32HeapBuffer         = nullptr;

    try
    {
        Boolean returnValue = false;
        UInt32  bytesNeeded = 0;
        UInt32  byteCount   = 0;

        UnsafeNativeMethods::InvokeGetPrinterDriver(handle,
                                                  environment,
                                                  level,
                                                  SafeMemoryHandle::Null,
                                                  0,
                                                  &bytesNeeded);

        InternalPrintSystemException::ThrowIfLastErrorIsNot(ERROR_INSUFFICIENT_BUFFER);

        if (byteCount = bytesNeeded)
        {
            if (SafeMemoryHandle::TryCreate(byteCount, win32HeapBuffer))
            {
                returnValue = UnsafeNativeMethods::InvokeGetPrinterDriver(handle,
                                                                        environment,
                                                                        level,
                                                                        win32HeapBuffer,
                                                                        byteCount,
                                                                        &bytesNeeded);

                if (returnValue)
                {
                    //This method takes ownership of win32HeapBuffer
                    driverInfo = GetManagedDriverInfoObject(level, win32HeapBuffer, 1);
                }
                else
                {
                    InternalPrintSystemException::ThrowLastError();
                }
            }
            else
            {
                InternalPrintSystemException::ThrowIfNotSuccess(ERROR_OUTOFMEMORY);
            }
        }
    }
    __finally
    {
    }

    return driverInfo;
}
/*++

Routine Name:

    ThunkEnumDrivers

Routine Description:

    Not implemented

Arguments:

    level       - Win32 level to make the enumeration upon
    environment - environment string

Return Value:

   nullptr

--*/
IPrinterInfo^
PrinterThunkHandler::
ThunkEnumDrivers(
    UInt32  level,
    String^ environment
    )
{
    return nullptr;
}

/*++

Routine Name:

    GetManagedDriverInfoObject

Routine Description:

    Depending on the level, it build the managed type associated with the unmanaged structred
    that the win32HeapBuffer points to.

Arguments:

    level           - Win32 level
    win32HeapBuffer - unmanaged buffer that contains an array of structures
    count           - number of structures in array

Return Value:

   Pointer to the managed object that wraps the unmanaged data and exposes it as managed objects.

--*/
IPrinterInfo^
PrinterThunkHandler::
GetManagedDriverInfoObject(
    UInt32              level,
    SafeMemoryHandle^   win32HeapBuffer,
    UInt32              count
    )
{
    return nullptr;
}

/*++

Routine Name:

    ThunkGetJob

Routine Description:

    The routine gets the Job associated with the the printer that the object is bound to.
    It calls GetJob in IJW mode and build the wrapper object around the unmanaged buffer

Arguments:

    level           - level to call
    JobID           - Job ID

Return Value:

    Pointer to the managed object that wraps the unmanaged data and exposes it as managed objects.
--*/
IPrinterInfo^
PrinterThunkHandler::
ThunkGetJob(
    UInt32  level,
    UInt32  jobID
    )
{
    IPrinterInfo^       jobInfo         = nullptr;
    SafeMemoryHandle^   win32HeapBuffer = nullptr;

    try
    {
        Boolean returnValue = false;
        UInt32  bytesNeeded = 0;
        UInt32  byteCount   = 0;

        UnsafeNativeMethods::InvokeGetJob(handle,
                                        jobID,
                                        level,
                                        SafeMemoryHandle::Null,
                                        0,
                                        &bytesNeeded);

        InternalPrintSystemException::ThrowIfLastErrorIsNot(ERROR_INSUFFICIENT_BUFFER);

        if (byteCount = bytesNeeded)
        {
            if (SafeMemoryHandle::TryCreate(byteCount, win32HeapBuffer))
            {
                returnValue = UnsafeNativeMethods::InvokeGetJob(handle,
                                                              jobID,
                                                              level,
                                                              win32HeapBuffer,
                                                              byteCount,
                                                              &bytesNeeded);

                if (returnValue)
                {
                    // This method takes ownership handle
                    jobInfo = GetManagedJobInfoObject(level, win32HeapBuffer, 1);
                }
                else
                {
                    InternalPrintSystemException::ThrowLastError();
                }
            }
            else
            {
                InternalPrintSystemException::ThrowIfNotSuccess(ERROR_OUTOFMEMORY);
            }
        }
    }
    __finally
    {
    }

    return jobInfo;
}

/*++

Routine Name:

    ThunkEnumJobs

Routine Description:

    The routine enumerates print jobs on the printer,
    by calling the EnumJobs API in IJW mode.

Arguments:

    serverName - server name
    level      - Win32 level to make the enumeration upon
    flags      - enumeration flags

Return Value:

    Pointer to the managed object that wraps the unmanaged data
    and exposes it as managed objects. This object is frees the unmanaged memory when disposed.

--*/
IPrinterInfo^
PrinterThunkHandler::
ThunkEnumJobs(
    UInt32    level,
    UInt32  firstJob,
    UInt32  numberOfJobs
    )
{
    IPrinterInfo^   printerInfoArray    = nullptr;

    try
    {
        UInt32                bytesNeeded     = 0;
        UInt32                byteCount       = 0;
        UInt32                jobCount        = 0;
        Boolean               returnValue     = false;
        SafeMemoryHandle^     win32HeapBuffer = nullptr;

        returnValue = UnsafeNativeMethods::InvokeEnumJobs(handle,
                                                        firstJob,
                                                        numberOfJobs,
                                                        level,
                                                        SafeMemoryHandle::Null,
                                                        0,
                                                        &bytesNeeded,
                                                        &jobCount);

        if (!returnValue)
        {
            InternalPrintSystemException::ThrowIfLastErrorIsNot(ERROR_INSUFFICIENT_BUFFER);
        }

        if (byteCount = bytesNeeded)
        {
            if (SafeMemoryHandle::TryCreate(byteCount, win32HeapBuffer))
            {
                returnValue = UnsafeNativeMethods::InvokeEnumJobs(handle,
                                                                firstJob,
                                                                numberOfJobs,
                                                                level,
                                                                win32HeapBuffer,
                                                                byteCount,
                                                                &bytesNeeded,
                                                                &jobCount);
                if (returnValue)
                {
                   // This method takes ownership handle
                    printerInfoArray = GetManagedJobInfoObject(level,
                                                               win32HeapBuffer,
                                                               jobCount);
                }
                else
                {
                    InternalPrintSystemException::ThrowLastError();
                }
            }
            else
            {
                InternalPrintSystemException::ThrowIfNotSuccess(ERROR_OUTOFMEMORY);
            }
        }
    }
    __finally
    {
    }

    return printerInfoArray;
}

/*++

Routine Name:

    ThunkSetJob

Routine Description:

    Pauses, Resumes a print job

Arguments:

    command - command to be set on the print job

Return Value:

    true if succeeded

--*/
Boolean
PrinterThunkHandler::
ThunkSetJob(
    UInt32              jobId,
    UInt32              command
    )
{
    Boolean    returnValue     = false;


    returnValue = UnsafeNativeMethods::InvokeSetJob(handle,
                                                  jobId,
                                                  0,
                                                  (IntPtr)nullptr,
                                                   command) == TRUE;

    if (!returnValue)
    {
        InternalPrintSystemException::ThrowLastError();
    }

    return returnValue;
}

/*++

Routine Name:

    GetManagedJobInfoObject

Routine Description:

    Depending on the level, it build the managed type associated with the unmanaged structred
    that the win32HeapBuffer points to.

Arguments:

    level           - Win32 level
    win32HeapBuffer - unmanaged buffer that contains an array of structures
    count           - number of structures in array

Return Value:

   Pointer to the managed object that wraps the unmanaged data and exposes it as managed objects.

--*/
IPrinterInfo^
PrinterThunkHandler::
GetManagedJobInfoObject(
    UInt32              level,
    SafeMemoryHandle^   win32HeapBuffer,
    UInt32              count
    )
{
    IPrinterInfo^  jobInfo = nullptr;

    switch (level)
    {
        case 1:
        {
            jobInfo = gcnew JobInfoOne(win32HeapBuffer, count);

            break;
        }
        case 2:
        {
            jobInfo = gcnew JobInfoTwo(win32HeapBuffer, count);

            break;
        }
        default:
        {
            break;
        }
    }

    return jobInfo;
}

/*++

Routine Name:

    GetManagedPrinterInfoObject

Routine Description:

    Depending on the level, it build the managed type associated with the unmanaged structred
    that the win32HeapBuffer points to.

Arguments:

    level           - Win32 level
    win32HeapBuffer - unmanaged buffer that contains an array of structures
    count           - number of structures in array

Return Value:

   Pointer to the managed object that wraps the unmanaged data and exposes it as managed objects.

--*/
IPrinterInfo^
PrinterThunkHandler::
GetManagedPrinterInfoObject(
    UInt32            level,
    SafeMemoryHandle^ win32HeapBuffer,
    UInt32            count
    )
{
    IPrinterInfo^  printerInfo = nullptr;

    if (!win32HeapBuffer->IsInvalid)
    {
        switch (level)
        {
            case 1:
            {
                printerInfo = gcnew PrinterInfoOne(win32HeapBuffer, count);
                break;
            }
            case 2:
            {
                printerInfo = gcnew PrinterInfoTwoGetter(win32HeapBuffer, count);
                break;
            }
            case 3:
            {
                printerInfo = gcnew PrinterInfoThree(win32HeapBuffer, count);
                break;
            }
            case 4:
            {
                printerInfo = gcnew PrinterInfoFourGetter(win32HeapBuffer, count);
                break;
            }
            case 5:
            {
                printerInfo = gcnew PrinterInfoFiveGetter(win32HeapBuffer, count);
                break;
            }
            case 6:
            {
                printerInfo = gcnew PrinterInfoSix(win32HeapBuffer, count);
                break;
            }
            case 7:
            {
                printerInfo = gcnew PrinterInfoSeven(win32HeapBuffer, count);
                break;
            }
            case 8:
            {
                printerInfo = gcnew PrinterInfoEight(win32HeapBuffer, count);
                break;
            }
            case 9:
            {
                printerInfo = gcnew PrinterInfoNine(win32HeapBuffer, count);
                break;
            }
            default:
            {
                break;
            }
        }
    }

    return printerInfo;
}

/*++

Routine Name:

    ThunkAddPrinter

Routine Description:

    The routine installs a print queue giving a server name, a printer name,
    a driver, port and printprocesssor. It builds a PRINTER_INFO_2 unmanaged buffer and
    it pInvokes AddPrinterW API.

Arguments:

    serverName          -   server name
    printerName         -   printer name
    driverName          -   driver name
    portName            -   port name
    printProcessorName  -   print processor name

Return Value:

    PrinterThunkHandler object associated with the installed printer.

--*/
PrinterThunkHandler^
PrinterThunkHandler::
ThunkAddPrinter(
    String^     serverName,
    String^     printerName,
    String^     driverName,
    String^     portName,
    String^     printProcessorName,
    String^     comment,
    String^     location,
    String^     shareName,
    String^     separatorFile,
    Int32       attributes,
    Int32       priority,
    Int32       defaultPriority
    )
{
    IntPtr                  handle       = (IntPtr)nullptr;
    PrinterThunkHandler^    printerThunkHandler = nullptr;
    IntPtr                  win32PrinterInfoTwo = (IntPtr)nullptr;

    try
    {
        if (printerName && driverName && portName && printProcessorName)
        {
            win32PrinterInfoTwo = UnmanagedPrinterInfoLevelBuilder::BuildUnmanagedPrinterInfoTwo(serverName,
                                                                                                 printerName,
                                                                                                 driverName,
                                                                                                 portName,
                                                                                                 printProcessorName,
                                                                                                 comment,
                                                                                                 location,
                                                                                                 shareName,
                                                                                                 separatorFile,
                                                                                                 attributes,
                                                                                                 priority,
                                                                                                 defaultPriority);

            if (win32PrinterInfoTwo != IntPtr::Zero)
            {
                handle = (IntPtr) UnsafeNativeMethods::InvokeAddPrinter(serverName,
                                                                      2,
                                                                      SafeMemoryHandle::Wrap(win32PrinterInfoTwo));

                if (handle != (IntPtr)nullptr)
                {
                    printerThunkHandler = gcnew PrinterThunkHandler(handle);
                }
                else
                {
                    InternalPrintSystemException::ThrowLastError();
                }
            }
        }
        else
        {
            throw gcnew InternalPrintSystemException(ERROR_INVALID_PARAMETER);
        }
    }
    __finally
    {
        if (win32PrinterInfoTwo != IntPtr::Zero)
        {
            UnmanagedPrinterInfoLevelBuilder::FreeUnmanagedPrinterInfoTwo(win32PrinterInfoTwo);
        }
    }

    return printerThunkHandler;
}

/*++

Routine Name:

    ThunkAddPrinter

Routine Description:

    The routine installs a print queue giving a server name, a printer name,
    a driver, port and printprocesssor. It builds a PRINTER_INFO_2 unmanaged buffer and
    it pInvokes AddPrinterW API.

Arguments:

    serverName          -   server name
    printerName         -   printer name
    driverName          -   driver name
    portName            -   port name
    printProcessorName  -   print processor name
    attributes          -   print queue attributes

Return Value:

    PrinterThunkHandler object associated with the installed printer.

--*/
PrinterThunkHandler^
PrinterThunkHandler::
ThunkAddPrinter(
    String^                     serverName,
    PrinterInfoTwoSetter^       printInfoTwoLeveThunk
    )
{
    IntPtr                  handle                        = (IntPtr)nullptr;
    PrinterThunkHandler^    printerThunkHandler           = nullptr;
    SafeMemoryHandle^       win32PrinterInfoTwoSafeHandle = nullptr;

    if (printInfoTwoLeveThunk)
    {
        win32PrinterInfoTwoSafeHandle = printInfoTwoLeveThunk->Win32SafeHandle;

        handle = (IntPtr) UnsafeNativeMethods::InvokeAddPrinter(serverName,
                                                              2,
                                                              win32PrinterInfoTwoSafeHandle);

        if (handle != (IntPtr)nullptr)
        {
            printerThunkHandler = gcnew PrinterThunkHandler(handle);
        }
        else
        {
            InternalPrintSystemException::ThrowLastError();
        }
    }
    else
    {
        throw gcnew InternalPrintSystemException(ERROR_INVALID_PARAMETER);
    }

    return printerThunkHandler;
}

/*++

Routine Name:

    ThunkEnumPrinters

Routine Description:

    The routine enumarates printers on the printer name that the object is bound to,
    by calling the EnumPrinters API in IJW mode.

Arguments:

    serverName - server name
    level      - Win32 level to make the enumeration upon
    flags      - enumeration flags

Return Value:

    Pointer to the managed object that wraps the unmanaged data
    and exposes it as managed objects. This object is frees the unmanaged memory when disposed.

--*/
IPrinterInfo^
PrinterThunkHandler::
ThunkEnumPrinters(
    String^ serverName,
    UInt32    level,
    UInt32    flags
    )
{
    IPrinterInfo^   printerInfoArray    = nullptr;
    IntPtr          unmanagedServerName = (IntPtr)nullptr;

    try
    {
        UInt32                bytesNeeded     = 0;
        UInt32                byteCount       = 0;
        UInt32                printerCount    = 0;
        Boolean               returnValue     = false;
        SafeMemoryHandle^     win32HeapBuffer = nullptr;

        returnValue = UnsafeNativeMethods::InvokeEnumPrinters(flags,
                                                            serverName,
                                                            level,
                                                            SafeMemoryHandle::Null,
                                                            0,
                                                            &bytesNeeded,
                                                            &printerCount);

        if (!returnValue)
        {
            InternalPrintSystemException::ThrowIfLastErrorIsNot(ERROR_INSUFFICIENT_BUFFER);
        }

        if (byteCount = bytesNeeded)
        {
            if (SafeMemoryHandle::TryCreate(byteCount, win32HeapBuffer))
            {
                returnValue = UnsafeNativeMethods::InvokeEnumPrinters(flags,
                                                                    serverName,
                                                                    level,
                                                                    win32HeapBuffer,
                                                                    byteCount,
                                                                    &bytesNeeded,
                                                                    &printerCount);

                if (returnValue)
                {
                    //This method takes ownership win32HeapBuffer
                    printerInfoArray = GetManagedPrinterInfoObject(level,
                                                                   win32HeapBuffer,
                                                                   printerCount);
                }
                else
                {
                    InternalPrintSystemException::ThrowLastError();
                }
            }
            else
            {
                InternalPrintSystemException::ThrowIfNotSuccess(ERROR_OUTOFMEMORY);
            }
        }

    }
    __finally
    {
        if (unmanagedServerName != IntPtr::Zero)
        {
            Marshal::FreeHGlobal(unmanagedServerName);
        }
    }

    return printerInfoArray;
}

String^
PrinterThunkHandler::
GetLocalMachineName(
    void
    )
{
    int             length      = MaxPath;
    StringBuilder^  netBiosName = gcnew StringBuilder(MaxPath);
    String^         machineName = nullptr;

    if (UnsafeNativeMethods::GetComputerName(netBiosName, &length))
    {
        String^ wackWack = gcnew String("\\\\");
        machineName = String::Concat(wackWack, netBiosName->ToString());
    }
    else
    {
        InternalPrintSystemException::ThrowLastError();
    }

    return machineName;
}

/*++

Routine Name:

    ThunkAddPrinterConnection

Routine Description:

    The routine adds a printer connection a given printer.

Arguments:

    path    - \\server\printer path

Return Value:

    true if succeeded

--*/
Boolean
PrinterThunkHandler::
ThunkAddPrinterConnection(
    String^     path
    )
{
    Boolean    returnValue = false;

    if (path)
    {
        returnValue = UnsafeNativeMethods::InvokeAddPrinterConnection(path);

        if (!returnValue)
        {
            InternalPrintSystemException::ThrowLastError();
        }
    }
    else
    {
        throw gcnew InternalPrintSystemException(ERROR_INVALID_PARAMETER);
    }

    return returnValue;
}


/*++

Routine Name:

    ThunkDeletePrinterConnection

Routine Description:

    The routine adds a printer connection a given printer.

Arguments:

    path    - \\server\printer path

Return Value:

    true if succeeded

--*/
Boolean
PrinterThunkHandler::
ThunkDeletePrinterConnection(
    String^     path
    )
{
    Boolean    returnValue = false;

    if (path)
    {
        returnValue = UnsafeNativeMethods::InvokeDeletePrinterConnection(path);

        if (!returnValue)
        {
            InternalPrintSystemException::ThrowLastError();
        }
    }
    else
    {
        throw gcnew InternalPrintSystemException(ERROR_INVALID_PARAMETER);
    }

    return returnValue;
}

/*++

Routine Name:

    ThunkSetDefaultPrinter

Routine Description:

    Sets the default printer for the calling user

Arguments:

    printerName - name of default printer

Return Value:

    true if succeeded

--*/
Boolean
PrinterThunkHandler::
ThunkSetDefaultPrinter(
    String^     printerName
    )
{
    Boolean    returnValue = false;

    if (printerName)
    {
        returnValue = UnsafeNativeMethods::InvokeSetDefaultPrinter(printerName);

        if (!returnValue)
        {
            InternalPrintSystemException::ThrowLastError();
        }
    }
    else
    {
        throw gcnew InternalPrintSystemException(ERROR_INVALID_PARAMETER);
    }

    return returnValue;
}

/*++

Routine Name:

    ThunkGetDefaultPrinter

Routine Description:

    Gets the default printer for the calling user.

Arguments:

    none

Return Value:

    String that represents the default printer for the
    user that impersonates the calling thread.

--*/
String^
PrinterThunkHandler::
ThunkGetDefaultPrinter(
    void
    )
{
    String^ defaultPrinterName = nullptr;

    int             length                    = MaxPath;
    StringBuilder^  defaultPrinterNameBuilder = gcnew StringBuilder(length);
    Boolean         returnValue               = false;

    returnValue = UnsafeNativeMethods::InvokeGetDefaultPrinter(defaultPrinterNameBuilder,
                                                             &length);

    if (!returnValue)
    {
        InternalPrintSystemException::ThrowLastError();
    }
    else
    {
        defaultPrinterName = defaultPrinterNameBuilder->ToString();
    }

    return defaultPrinterName;
}

#ifdef XPSJOBNOTIFY

/*++

Routine Name:

    ThunkWritePrinter

Routine Description:

    Writes a stream of bytes to a print queue.

Arguments:

    handle   - printer handle
    array           - array of bytes
    offset          - offset in the array of bytes
    count           - number of bytes to write
    writtenDataCount - number of bytes succesfully written

Return Value:

    Win32 error

--*/
Int32
PrinterThunkHandler::
ThunkWritePrinterInternal(
    array<Byte>^            array,
    Int32                   offset,
    Int32                   count,
    Int32&                  writtenDataCount
    )
{
    Int32   lastWin32Error   = 0;
    IntPtr  rawDataUnmanaged = (IntPtr)nullptr;
    writtenDataCount         = 0;

    if (array == nullptr)
    {
        throw gcnew ArgumentNullException("array");
    }

    if (offset + count > array->Length)
    {
        InternalExceptionResourceManager^ manager = gcnew InternalExceptionResourceManager();

        throw gcnew ArgumentException(manager->GetString("ArgumentException.InvalidValue",
                                                         Thread::CurrentThread->CurrentUICulture),
                                                         "array");
    }

    if (offset < 0 || count < 0)
    {
        throw gcnew ArgumentOutOfRangeException("offset");
    }

    try
    {
        if (count)
        {
            Int32    perWriteOperationCount = 0;

            rawDataUnmanaged    = Marshal::AllocHGlobal(count);

            Marshal::Copy(array, offset, rawDataUnmanaged, count);

            for ( IntPtr dataUnmanaged = rawDataUnmanaged;
                  writtenDataCount < count;
                  writtenDataCount += perWriteOperationCount )
            {
                dataUnmanaged = IntPtr((reinterpret_cast<BYTE*>(rawDataUnmanaged.ToPointer())) + writtenDataCount);

                if (!UnsafeNativeMethods::InvokeEDocWritePrinter(handle,
                                                               dataUnmanaged,
                                                               count,
                                                               &perWriteOperationCount))
                {
                    lastWin32Error = Marshal::GetLastWin32Error();
                    break;
                }
            }
        }
    }
    __finally
    {
        Marshal::FreeHGlobal(rawDataUnmanaged);
    }

    return lastWin32Error;
}

Int32
PrinterThunkHandler::
ThunkWritePrinter(
    PrinterThunkHandler^    printerThunkHandler,
    array<Byte>^            array,
    Int32                   offset,
    Int32                   count,
    Int32&                  writtenDataCount
    )
{
    return printerThunkHandler->ThunkWritePrinterInternal(array, offset, count, writtenDataCount);
}

/*++

Routine Name:

    ThunkFlushPrinter

Routine Description:

    Flushes a stream of bytes to a print queue.

Arguments:

    handle   - printer handle
    array           - array of bytes
    offset          - offset in the array of bytes
    count           - number of bytes to flush
    flushedByteCount - number of bytes succesfully written

Return Value:

    Win32 error

--*/
Int32
PrinterThunkHandler::
ThunkFlushPrinterInternal(
    array<Byte>^            array,
    Int32                   offset,
    Int32                   count,
    Int32&                  flushedByteCount,
    Int32                   portIdleTime
    )
{
    Int32   lastWin32Error   = 0;
    IntPtr  rawDataUnmanaged = (IntPtr)nullptr;

    if (array && (offset + count > array->Length))
    {
        throw gcnew ArgumentException();
    }

    if (offset < 0 || count < 0 || portIdleTime < 0)
    {
        throw gcnew ArgumentOutOfRangeException();
    }

    try
    {
        if (count)
        {
            rawDataUnmanaged = Marshal::AllocHGlobal(count);

            Marshal::Copy(array, offset, rawDataUnmanaged, count);

            if (!UnsafeNativeMethods::InvokeFlushPrinter(handle,
                                                       rawDataUnmanaged,
                                                       count,
                                                       &flushedByteCount,
                                                       portIdleTime))
            {
                lastWin32Error = Marshal::GetLastWin32Error();
            }
        }
    }
    __finally
    {
        Marshal::FreeHGlobal(rawDataUnmanaged);
    }

    return lastWin32Error;
}

Int32
PrinterThunkHandler::
ThunkFlushPrinter(
    PrinterThunkHandler^    printerThunkHandler,
    array<Byte>^            array,
    Int32                   offset,
    Int32                   count,
    Int32&                  flushedByteCount,
    Int32                   portIdleTime
    )
{
    return printerThunkHandler->ThunkFlushPrinterInternal(array, offset, count, flushedByteCount, portIdleTime);
}


/*++

Routine Name:

    ThunkAddJob

Routine Description:

    Creates a print job.

Arguments:

    inputCollection   - collection of job name and job type attributes
    outputCollection  - collection that contains attributes for the job id and
                        eDoc spool file name

Return Value:

    Win32 error

--*/
IPrinterInfo^
PrinterThunkHandler::
ThunkAddJob(
    UInt32  level
    )
{
    int                 lastWin32Error  = ERROR_SUCCESS;
    IPrinterInfo^       addJobInfo      = nullptr;
    SafeMemoryHandle^   win32HeapBuffer = nullptr;

    try
    {
        Boolean    returnValue = false;
        UInt32  bytesNeeded = 0;
        UInt32  byteCount   = 0;

        UnsafeNativeMethods::InvokeAddJob(handle,
                                        level,
                                        (IntPtr)nullptr,
                                        0,
                                        &bytesNeeded);

        InternalPrintSystemException::ThrowIfLastErrorIsNot(ERROR_INSUFFICIENT_BUFFER);

        if (byteCount = bytesNeeded)
        {
            if (SafeMemoryHandle::TryCreate(byteCount, win32HeapBuffer))
            {
                returnValue = UnsafeNativeMethods::InvokeAddJob(handle,
                                                              level,
                                                              win32HeapBuffer,
                                                              byteCount,
                                                              &bytesNeeded);

                if (returnValue)
                {
                    //This method takes ownership of win32HeapBuffer
                    addJobInfo = GetManagedAddJobInfoObject(level, win32HeapBuffer, 1);
                }
                else
                {
                    InternalPrintSystemException::ThrowLastError();
                }
            }
            else
            {
                InternalPrintSystemException::ThrowIfNotSuccess(ERROR_OUTOFMEMORY);
            }
        }
    }
    __finally
    {
    }

    return addJobInfo;
}

/*++

Routine Name:

    ThunkScheduleJob

Routine Description:

    Schedules the print job.

Arguments:

    jobID   -   ID of the job we want to schedule.

Return Value:

    Boolean

--*/
Boolean
PrinterThunkHandler::
ThunkScheduleJob(
    UInt32   jobID
    )
{
    Boolean    returnValue = false;

    returnValue = UnsafeNativeMethods::InvokeScheduleJob(handle, jobID);

    if (!returnValue)
    {
        InternalPrintSystemException::ThrowLastError();
    }

    return returnValue;
}

#endif // XPSJOBNOTIFY

/*++

Routine Name:

    ThunkReportJobProgress

Routine Description:

    Reports the job creation/consumption progress to Spooler.

Arguments:

    jobIndentifier      -   job ID
    jobOperation        -   production vs consumption
    jobStatus           -   job status

Return Value:

    Win32 error

--*/
Int32
PrinterThunkHandler::
ThunkReportJobProgress(
    Int32                   jobId,
    JobOperation            jobOperation,
    PackagingAction         packagingAction
    )
{
    UInt32  returnHResultValue = 0;

    switch (packagingAction)
    {
        case PackagingAction::AddingDocumentSequence:
        case PackagingAction::AddingFixedDocument:
        case PackagingAction::XpsDocumentCommitted:
        case PackagingAction::FixedDocumentCompleted:
        case PackagingAction::DocumentSequenceCompleted:
        {
            if (!isRunningDownLevel)
            {
                try
                {
                    returnHResultValue = UnsafeNativeMethods::InvokeReportJobProgress(handle,
                                                                                    jobId,
                                                                                    (Int32)jobOperation,
                                                                                    (Int32)packagingAction);
                }
                catch (EntryPointNotFoundException^)
                {
                    //
                    // PLACEHOLDER
                    // See what exception you get when running downlevel and catch that here
                    // Check to see if there's a smarter way to check for downlevel platform
                    //
                    isRunningDownLevel = true;
                }
            }
            break;
        }

        case PackagingAction::AddingFixedPage:
        {
            if (isRunningDownLevel)
            {
                if (!ThunkStartPagePrinter())
                {
                    InternalPrintSystemException::ThrowLastError();
                }
            }
            else
            {
                returnHResultValue = UnsafeNativeMethods::InvokeReportJobProgress(handle,
                                                                                jobId,
                                                                                (Int32)jobOperation,
                                                                                (Int32)packagingAction);
            }
            break;
        }

        case PackagingAction::FixedPageCompleted:
        {
            if (isRunningDownLevel)
            {
                if (!ThunkEndPagePrinter())
                {
                    InternalPrintSystemException::ThrowLastError();
                }
            }
            else
            {
                returnHResultValue = UnsafeNativeMethods::InvokeReportJobProgress(handle,
                                                                                jobId,
                                                                                (Int32)jobOperation,
                                                                                (Int32)packagingAction);
            }
            break;
        }

        case PackagingAction::FontAdded:
        case PackagingAction::ImageAdded:
        case PackagingAction::ResourceAdded:
        case PackagingAction::None:
        default:
        {
            break;
        }
    }

    InternalPrintSystemException::ThrowIfNotSuccess(returnHResultValue);

    return returnHResultValue;
}




Int32
PrinterThunkHandler::
ThunkStartDocPrinter(
    DocInfoThree^         docInfo,
    PrintTicket^ printTicket
    )
{
    // Clear the 'fast copy' flag;  see remarks in PrintQueueStream::InitializePrintStream
    docInfo->docFlags = docInfo->docFlags & ~0x40000000;

    // Note: the print ticket is ignored in this implementation for
    // compatibility.  Note that the jobID will be available once this
    // call returns, unlike the StartXpsPrintJob API.
    jobIdentifier = UnsafeNativeMethods::InvokeStartDocPrinter(handle,
                                                           3,
                                                           docInfo);

    if(jobIdentifier == 0)
    {
        InternalPrintSystemException::ThrowLastError();
    }

    return jobIdentifier;
}

Boolean
PrinterThunkHandler::
ThunkEndDocPrinter(
    void
    )
{
    if(spoolStream != nullptr)
    {
        spoolStream->Close();
        spoolStream = nullptr;
    }

    return UnsafeNativeMethods::InvokeEndDocPrinter(handle);
}

Boolean
PrinterThunkHandler::
ThunkAbortPrinter(
    void
    )
{
    if(UnsafeNativeMethods::InvokeAbortPrinter(handle))
    {
        if(spoolStream != nullptr)
        {
            spoolStream->Close();
            spoolStream = nullptr;
        }

        return true;
    }
    else
    {
        return false;
    }
}

Boolean
PrinterThunkHandler::
ThunkStartPagePrinter(
    void
    )
{
    return UnsafeNativeMethods::InvokeStartPagePrinter(handle);
}

Boolean
PrinterThunkHandler::
ThunkEndPagePrinter(
    void
    )
{
    return UnsafeNativeMethods::InvokeEndPagePrinter(handle);
}

FileStream^
PrinterThunkHandler::
CreateSpoolStream(
    IntPtr fileHandle
    )
{
    return gcnew FileStream(gcnew Microsoft::Win32::SafeHandles::SafeFileHandle(fileHandle, false), FileAccess::ReadWrite);
}

void
PrinterThunkHandler::
ThunkOpenSpoolStream(
    void
    )
{
    IntPtr  returnHandle    = (IntPtr)nullptr;

    returnHandle = UnsafeNativeMethods::InvokeGetSpoolFileHandle(handle);

    if(returnHandle == (IntPtr)INVALID_HANDLE_VALUE)
    {
        InternalPrintSystemException::ThrowLastError();
    }

    spoolStream = CreateSpoolStream(returnHandle);
}

void
PrinterThunkHandler::
ThunkCommitSpoolData(
    Int32                   bytes
    )
{
    Microsoft::Win32::SafeHandles::SafeFileHandle^ spoolFileHandle = nullptr;
    if(spoolStream != nullptr)
    {
        spoolFileHandle = spoolStream->SafeFileHandle;
    }

    IntPtr  returnHandle    = IntPtr::Zero;
    static const IntPtr CommitSpoolDataError = (IntPtr)INVALID_HANDLE_VALUE ;

    if(spoolFileHandle != nullptr && bytes > 0)
    {
        returnHandle = UnsafeNativeMethods::InvokeCommitSpoolData(handle,
                                                                spoolFileHandle,
                                                                bytes);

        if(returnHandle == (IntPtr)nullptr|| returnHandle == CommitSpoolDataError)
        {
            InternalPrintSystemException::ThrowLastError();
        }

        bool success = false;
        spoolFileHandle->DangerousAddRef(success);
        if(success)
        {
            try
            {
                if(returnHandle != spoolFileHandle->DangerousGetHandle())
                {
                    spoolStream = CreateSpoolStream(returnHandle);
                }
            }
            finally
            {
                spoolFileHandle->DangerousRelease();
            }
        }
    }
    else
    {
        throw gcnew InternalPrintSystemException(ERROR_INVALID_PARAMETER);
    }
}

Boolean
PrinterThunkHandler::
ThunkCloseSpoolStream(
    void
    )
{
    Boolean  returnValue = false;

    Microsoft::Win32::SafeHandles::SafeFileHandle^ spoolFileHandle = nullptr;
    if(spoolStream != nullptr)
    {
        spoolFileHandle = spoolStream->SafeFileHandle;
    }

    if(spoolFileHandle != nullptr)
    {
        returnValue = UnsafeNativeMethods::InvokeCloseSpoolFileHandle(handle,
                                                                    spoolFileHandle);

        if(!returnValue)
        {
            InternalPrintSystemException::ThrowLastError();
        }

        spoolStream->Close();
        spoolStream = nullptr;
    }
    else
    {
        throw gcnew InternalPrintSystemException(ERROR_INVALID_PARAMETER);
    }

    return returnValue;
}

int
PrinterThunkHandler::JobIdentifier::
get(
    void
    )
{
    return jobIdentifier;
}

Stream^
PrinterThunkHandler::SpoolStream::
get(
    void
    )
{
    return spoolStream;
}

Boolean
PrinterThunkHandler::
IsXpsDocumentEventSupported(
    XpsDocumentEventType    escape,
    Boolean                 reset
    )
{
    Boolean supported = false;

    if (reset)
    {
        if (docEventFilter)
        {
            delete docEventFilter;
        }

        docEventFilter = gcnew DocEventFilter();

    }

    if (docEventFilter != nullptr)
    {
        supported = docEventFilter->IsXpsDocumentEventSupported(escape);
    }

    return supported;
}

void
PrinterThunkHandler::
SetUnsupportedXpsDocumentEvent(
    XpsDocumentEventType    escape
    )
{
    if (docEventFilter != nullptr)
    {
        docEventFilter->SetUnsupportedXpsDocumentEvent(escape);
    }

}

Int32
PrinterThunkHandler::
ThunkDocumentEvent(
    XpsDocumentEventType    escape,
    UInt32                  inBufferSize,
    SafeHandle^             inBuffer,
    UInt32                  outputBufferSize,
    SafeMemoryHandle^       outputBuffer
    )
{
    Int32   returnValue = DOCUMENTEVENT_UNSUPPORTED;

    IntPtr dummy;
    if (outputBuffer == nullptr)
    {
        //Work around in order to resolve problem with pre-vista x64 spooler code
        //Windows OS Bug: 1818440
        outputBuffer = SafeMemoryHandle::Wrap((IntPtr)&dummy);
        outputBufferSize = IntPtr::Size;
    }
#if defined(DEBUG)
    else
    {
        assert(outputBuffer->DangerousGetHandle() != IntPtr::Zero);
    }
#endif // defined(DEBUG)

    returnValue =  UnsafeNativeMethods::InvokeDocumentEvent(handle,
                                                           (IntPtr)INVALID_HANDLE_VALUE,
                                                           (Int32)escape,
                                                           inBufferSize,
                                                           inBuffer,
                                                           outputBufferSize,
                                                           outputBuffer);


    if (returnValue == DOCUMENTEVENT_UNSUPPORTED)
    {
        this->SetUnsupportedXpsDocumentEvent(escape);
    }
    else if (returnValue == DOCUMENTEVENT_FAILURE)
    {
        throw gcnew InternalPrintSystemException(DOCUMENTEVENT_FAILURE);
    }
    return returnValue;
}

Int32
PrinterThunkHandler::
ThunkDocumentEvent(
    XpsDocumentEventType    escape,
    SafeHandle^             inputBufferSafeHandle
    )
{
    return ThunkDocumentEvent(escape,
                                  0,
                                  inputBufferSafeHandle,
                                  0,
                                  nullptr);
}

Int32
PrinterThunkHandler::
ThunkDocumentEvent(
    XpsDocumentEventType    escape
    )
{
    return ThunkDocumentEvent(escape,
                              0,
                              nullptr,
                              0,
                              nullptr);
}

Boolean
PrinterThunkHandler::
ThunkDocumentEventPrintTicket(
    XpsDocumentEventType    escapePre,
    XpsDocumentEventType    escapePost,
    SafeHandle^             inputBufferSafeHandle,
    MemoryStream^%          driverXpsDocEventPrintTicketStream
    )
{
    Boolean             collectionReturned          = false;
    Boolean             printTicketPropertyPresent  = false;
    Int32               docEventReturnValue         = DOCUMENTEVENT_UNSUPPORTED;
    UInt32              xpsDocEventOutputBufferSize = (UInt32)sizeof(IntPtr^);
    SafeMemoryHandle^   win32HeapBuffer             = nullptr;

    try
    {
        if(SafeMemoryHandle::TryCreate(xpsDocEventOutputBufferSize, win32HeapBuffer))
        {
            docEventReturnValue =  UnsafeNativeMethods::InvokeDocumentEvent(handle,
                                                                            (IntPtr)INVALID_HANDLE_VALUE,
                                                                            (Int32)escapePre,
                                                                            0,
                                                                            inputBufferSafeHandle,
                                                                            xpsDocEventOutputBufferSize,
                                                                            win32HeapBuffer);

            if (docEventReturnValue == DOCUMENTEVENT_SUCCESS)
            {
                IntPtr  unmanagedCollectionPtr = IntPtr(*(reinterpret_cast<PVOID*>((win32HeapBuffer->DangerousGetHandle()).ToPointer())));

                //
                // If the XPS driver returns a NULL collection, that means that it doesn't mean to change the PrintTicket.
                //
                if (unmanagedCollectionPtr.ToPointer() != nullptr)
                {
                    collectionReturned  = true;

                    driverXpsDocEventPrintTicketStream = (MemoryStream^)AttributeValueInteropHandler::GetValue(unmanagedCollectionPtr,
                                                                                                               "PrintTicket",
                                                                                                               System::IO::MemoryStream::typeid,
                                                                                                               printTicketPropertyPresent);

                    if (!printTicketPropertyPresent)
                    {
                        throw gcnew InternalPrintSystemException(ERROR_INVALID_PARAMETER);
                    }

                    ThunkDocumentEventPrintTicketPost(escapePost,
                                                      win32HeapBuffer,
                                                      xpsDocEventOutputBufferSize);
                }

            }
            else if (docEventReturnValue == DOCUMENTEVENT_UNSUPPORTED)
            {
                this->SetUnsupportedXpsDocumentEvent(escapePre);
                this->SetUnsupportedXpsDocumentEvent(escapePost);
            }
            else if (docEventReturnValue == DOCUMENTEVENT_FAILURE)
            {
                throw gcnew InternalPrintSystemException(DOCUMENTEVENT_FAILURE);
            }
            win32HeapBuffer->ReleaseHandle();
        }
    }
    __finally
    {
        delete win32HeapBuffer;
        delete inputBufferSafeHandle;
    }
    return collectionReturned;
}


Int32
PrinterThunkHandler::
ThunkDocumentEventPrintTicketPost(
    XpsDocumentEventType    escape,
    SafeMemoryHandle^       xpsDocEventOutputBuffer,
    UInt32                  xpsDocEventOutputBufferSize
    )
{
    return ThunkDocumentEvent(escape,
                            xpsDocEventOutputBufferSize,
                            xpsDocEventOutputBuffer,
                            0,
                            nullptr);
    }





Boolean
PrinterThunkHandler::
ThunkIsMetroDriverEnabled()
{
    SafeMemoryHandle^   win32HeapBuffer    = nullptr;
    Boolean             metroDriverEnabled = false;

    Boolean returnValue = false;
    UInt32  bytesNeeded = 0;
    UInt32  byteCount   = 0;

    UnsafeNativeMethods::InvokeGetPrinterDriver(handle,
                                              nullptr,
                                              6,
                                              SafeMemoryHandle::Null,
                                              0,
                                              &bytesNeeded);

    InternalPrintSystemException::ThrowIfLastErrorIsNot(ERROR_INSUFFICIENT_BUFFER);

    byteCount = bytesNeeded;
    if (byteCount > 0)
    {
        if (SafeMemoryHandle::TryCreate(byteCount, win32HeapBuffer))
        {
            returnValue = UnsafeNativeMethods::InvokeGetPrinterDriver(handle,
                                                                    nullptr,
                                                                    6,
                                                                    win32HeapBuffer,
                                                                    byteCount,
                                                                    &bytesNeeded);

            if (returnValue)
            {
                size_t length = 0;
                DRIVER_INFO_6W*     pDriverInfo6 = reinterpret_cast<DRIVER_INFO_6W*>(win32HeapBuffer->DangerousGetHandle().ToPointer());

                String^ szPipelineConfig = gcnew String("PipelineConfig.xml");

                for (PWSTR psz = pDriverInfo6->pDependentFiles; psz && *psz; psz += length + 1)
                {
                    length = wcslen(psz);
                    size_t tailLength = length - szPipelineConfig->Length;

                    if (tailLength > 0)
                    {
                        if (0 == String::Compare(szPipelineConfig, gcnew String(psz + tailLength), StringComparison::OrdinalIgnoreCase))
                        {
                            metroDriverEnabled = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                InternalPrintSystemException::ThrowLastError();
            }

            win32HeapBuffer->ReleaseHandle();
        }
        else
        {
            InternalPrintSystemException::ThrowIfNotSuccess(ERROR_OUTOFMEMORY);
        }
    }
    return metroDriverEnabled;
}

/*--------------------------------------------------------------------------------------*/
/*                    SafeMemoryHandle Implementation                                   */
/*--------------------------------------------------------------------------------------*/

///<remarks>
/// Allocates and initializes native memory takes ownership and frees handle in Dispose.
///</remarks>
bool
SafeMemoryHandle::
TryCreate(
    Int32   byteCount,
    __out SafeMemoryHandle^ % result
    )
{
    result = nullptr;

    if (byteCount < 0)
    {
        throw gcnew ArgumentOutOfRangeException("byteCount", byteCount, String::Empty);
    }

    if (byteCount > 0)
    {
        IntPtr tempHandle = Marshal::AllocHGlobal(byteCount);

        if (tempHandle != IntPtr::Zero)
        {
            ZeroMemory((void*)tempHandle, byteCount);
            result = gcnew SafeMemoryHandle(tempHandle, true);

            return true;
        }
    }

    return false;
}

///<remarks>
/// Allocates and initializes native memory takes ownership and frees handle in Dispose.
///</remarks>
SafeMemoryHandle^
SafeMemoryHandle::
Create(
    Int32   byteCount
    )
{
    SafeMemoryHandle^ result;
    if(TryCreate(byteCount, result))
    {
        return result;
    }

    throw gcnew OutOfMemoryException();
}


///<remarks>
/// Wraps an IntPtr but does not take ownership and does not free handle in Dispose.
///</remarks>
SafeMemoryHandle^
SafeMemoryHandle::
Wrap(
    IntPtr   Win32Pointer
    )
{
    return gcnew SafeMemoryHandle(Win32Pointer, false);
}


SafeMemoryHandle::
SafeMemoryHandle(
    IntPtr   Win32Pointer
    ) : SafeHandle(IntPtr::Zero, true)
{
    SetHandle(Win32Pointer);
}

SafeMemoryHandle::
SafeMemoryHandle(
    IntPtr   Win32Pointer,
    Boolean  ownsHandle
    ) : SafeHandle(IntPtr::Zero, ownsHandle)
{
    SetHandle(Win32Pointer);
}

Boolean
SafeMemoryHandle::IsInvalid::
get(
    void
    )
{
    return handle == IntPtr::Zero;
}

Int32
SafeMemoryHandle::Size::
get(
    void
    )
{
    return handle.Size;
}

SafeMemoryHandle^
SafeMemoryHandle::Null::
get(
    void
    )
{
    return SafeMemoryHandle::Wrap(IntPtr::Zero);
}

Boolean
SafeMemoryHandle::
ReleaseHandle(
    void
    )
{
    if (handle != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(handle);
        handle = IntPtr::Zero;
    }

    return true;
}

void
SafeMemoryHandle::
CopyFromArray(
     array<Byte>^ source,
     Int32 startIndex,
     Int32 length
     )
{
    Exception^ exception = VerifyBufferArguments("source", source, startIndex, length);
    if(exception != nullptr)
    {
        throw exception;
    }

    Boolean shouldRelease = false;
    DangerousAddRef(shouldRelease);
    __try
    {
        Marshal::Copy(source, startIndex, DangerousGetHandle(), length);
    }
    __finally
    {
        if(shouldRelease)
        {
            DangerousRelease();
        }
    }
}

void
SafeMemoryHandle::
CopyToArray(
     array<Byte>^ destination,
     Int32 startIndex,
     Int32 length
     )
{
    Exception^ exception = VerifyBufferArguments("destination", destination, startIndex, length);
    if(exception != nullptr)
    {
        throw exception;
    }

    Boolean shouldRelease = false;
    DangerousAddRef(shouldRelease);
    __try
    {
        Marshal::Copy(DangerousGetHandle(), destination, startIndex, length);
    }
    __finally
    {
        if(shouldRelease)
        {
            DangerousRelease();
        }
    }
}

Exception^
SafeMemoryHandle::
VerifyBufferArguments(
    String^ bufferName,
    array<Byte>^ buffer,
    Int32 startIndex,
    Int32 length
    )
{
    if(buffer == nullptr)
    {
        return gcnew ArgumentNullException(bufferName);
    }

    if (startIndex < 0 || startIndex >= buffer->Length)
    {
        return gcnew ArgumentOutOfRangeException("startIndex", startIndex, String::Empty);
    }

    Int32 end = startIndex + length;
    if (end < startIndex || end > buffer->Length)
    {
        return gcnew ArgumentOutOfRangeException("length", length, String::Empty);
    }

    return nullptr;
}

/*--------------------------------------------------------------------------------------*/
/*                    PrinterInfoOneSafeMemoryHandle Implementation                     */
/*--------------------------------------------------------------------------------------*/


PrinterInfoOneSafeMemoryHandle::
PrinterInfoOneSafeMemoryHandle(
    void
    ) : SafeMemoryHandle(UnmanagedPrinterInfoLevelBuilder::BuildEmptyUnmanagedPrinterInfoOne())
{
}


Boolean
PrinterInfoOneSafeMemoryHandle::
ReleaseHandle(
    void
    )
{
    if (handle != IntPtr::Zero)
    {
        UnmanagedPrinterInfoLevelBuilder::FreeUnmanagedPrinterInfoOne(handle);
    }

    return SafeMemoryHandle::ReleaseHandle();
}


/*--------------------------------------------------------------------------------------*/
/*                    PrinterInfoThreeSafeMemoryHandle Implementation                     */
/*--------------------------------------------------------------------------------------*/


PrinterInfoThreeSafeMemoryHandle::
PrinterInfoThreeSafeMemoryHandle(
    void
    ) : SafeMemoryHandle(UnmanagedPrinterInfoLevelBuilder::BuildEmptyUnmanagedPrinterInfoThree())
{
}


Boolean
PrinterInfoThreeSafeMemoryHandle::
ReleaseHandle(
    void
    )
{
    if (handle != IntPtr::Zero)
    {
        UnmanagedPrinterInfoLevelBuilder::FreeUnmanagedPrinterInfoThree(handle);
    }

    return SafeMemoryHandle::ReleaseHandle();
}

/*--------------------------------------------------------------------------------------*/
/*                    PrinterInfoSixSafeMemoryHandle Implementation                     */
/*--------------------------------------------------------------------------------------*/


PrinterInfoSixSafeMemoryHandle::
PrinterInfoSixSafeMemoryHandle(
    void
    ) : SafeMemoryHandle(UnmanagedPrinterInfoLevelBuilder::BuildEmptyUnmanagedPrinterInfoSix())
{
}


Boolean
PrinterInfoSixSafeMemoryHandle::
ReleaseHandle(
    void
    )
{
    if (handle != IntPtr::Zero)
    {
        UnmanagedPrinterInfoLevelBuilder::FreeUnmanagedPrinterInfoSix(handle);
    }

    return SafeMemoryHandle::ReleaseHandle();
}

/*--------------------------------------------------------------------------------------*/
/*                    PrinterInfoSevenSafeMemoryHandle Implementation                     */
/*--------------------------------------------------------------------------------------*/


PrinterInfoSevenSafeMemoryHandle::
PrinterInfoSevenSafeMemoryHandle(
    void
    ) : SafeMemoryHandle(UnmanagedPrinterInfoLevelBuilder::BuildEmptyUnmanagedPrinterInfoSeven())
{
}


Boolean
PrinterInfoSevenSafeMemoryHandle::
ReleaseHandle(
    void
    )
{
    if (handle != IntPtr::Zero)
    {
        UnmanagedPrinterInfoLevelBuilder::FreeUnmanagedPrinterInfoSeven(handle);
    }

    return SafeMemoryHandle::ReleaseHandle();
}


/*--------------------------------------------------------------------------------------*/
/*                    PrinterInfoEightSafeMemoryHandle Implementation                     */
/*--------------------------------------------------------------------------------------*/


PrinterInfoEightSafeMemoryHandle::
PrinterInfoEightSafeMemoryHandle(
    void
    ) : SafeMemoryHandle(UnmanagedPrinterInfoLevelBuilder::BuildEmptyUnmanagedPrinterInfoEight())
{
}


Boolean
PrinterInfoEightSafeMemoryHandle::
ReleaseHandle(
    void
    )
{
    if (handle != IntPtr::Zero)
    {
        UnmanagedPrinterInfoLevelBuilder::FreeUnmanagedPrinterInfoEight(handle);
    }

    return SafeMemoryHandle::ReleaseHandle();
}


/*--------------------------------------------------------------------------------------*/
/*                    PrinterInfoNineSafeMemoryHandle Implementation                     */
/*--------------------------------------------------------------------------------------*/


PrinterInfoNineSafeMemoryHandle::
PrinterInfoNineSafeMemoryHandle(
    void
    ) : SafeMemoryHandle(UnmanagedPrinterInfoLevelBuilder::BuildEmptyUnmanagedPrinterInfoNine())
{
}


Boolean
PrinterInfoNineSafeMemoryHandle::
ReleaseHandle(
    void
    )
{
    if (handle != IntPtr::Zero)
    {
        UnmanagedPrinterInfoLevelBuilder::FreeUnmanagedPrinterInfoNine(handle);
    }

    return SafeMemoryHandle::ReleaseHandle();
}

/*--------------------------------------------------------------------------------------*/
/*                    PropertyCollectionMemorySafeHandle Implementation                                   */
/*--------------------------------------------------------------------------------------*/

PropertyCollectionMemorySafeHandle^
PropertyCollectionMemorySafeHandle::
AllocPropertyCollectionMemorySafeHandle(
    UInt32   propertyCount
    )
{
    IntPtr unmanagedPropertiesCollection = IntPtr::Zero;

    try
    {
        unmanagedPropertiesCollection = AttributeValueInteropHandler::
                                        AllocateUnmanagedPrintPropertiesCollection(propertyCount);

    }
    catch (System::Exception ^ exception)
    {
        if (unmanagedPropertiesCollection != IntPtr::Zero)
        {
            AttributeValueInteropHandler::FreeUnmanagedPrintPropertiesCollection(unmanagedPropertiesCollection);
        }
        throw exception;
    }

    return gcnew PropertyCollectionMemorySafeHandle(unmanagedPropertiesCollection);
}

PropertyCollectionMemorySafeHandle::
PropertyCollectionMemorySafeHandle(
    IntPtr   Win32Pointer
    ) : SafeHandle(IntPtr::Zero, true)
{
    SetHandle(Win32Pointer);
}

Boolean
PropertyCollectionMemorySafeHandle::IsInvalid::
get(
    void
    )
{
    return handle == IntPtr::Zero;
}

Boolean
PropertyCollectionMemorySafeHandle::
ReleaseHandle(
    void
    )
{
    if (handle != IntPtr::Zero)
    {
        AttributeValueInteropHandler::FreeUnmanagedPrintPropertiesCollection(handle);
    }

    return true;
}

void
PropertyCollectionMemorySafeHandle::
SetValue(
    String^     propertyName,
    UInt32      index,
    Object^     value
    )
{
    bool shouldRelease;
    DangerousAddRef(shouldRelease);

    AttributeValueInteropHandler::SetValue(DangerousGetHandle(), propertyName, index, value);

    if(shouldRelease)
    {
        DangerousRelease();
    }
}

void
PropertyCollectionMemorySafeHandle::
SetValue(
    String^         propertyName,
    UInt32          index,
    System::Type^   value
    )
{
    bool shouldRelease;
    DangerousAddRef(shouldRelease);

    AttributeValueInteropHandler::SetValue(handle, propertyName, index, value);

    if(shouldRelease)
    {
        DangerousRelease();
    }
}

/*--------------------------------------------------------------------------------------*/
/*                    DocEventFilter Implementation                                     */
/*--------------------------------------------------------------------------------------*/
DocEventFilter::
DocEventFilter(
    void
    )
{
    eventsfilter = gcnew array<XpsDocumentEventType>(supportedEventsCount);
    //
    // We start with the assumption that all events are supported.
    //
    for (Int32 index = 0; index < supportedEventsCount; index++)
    {
        eventsfilter[index] = (XpsDocumentEventType)(index);
    }
}

Boolean
DocEventFilter::
IsXpsDocumentEventSupported(
    XpsDocumentEventType    escape
    )
{
    return (eventsfilter != nullptr)? (eventsfilter[Int32(escape)] == escape) : false;
}

void
DocEventFilter::
SetUnsupportedXpsDocumentEvent(
    XpsDocumentEventType    escape
    )
{
    if (eventsfilter != nullptr)
    {
        eventsfilter[Int32(escape)] = XpsDocumentEventType::None;
    }
}
