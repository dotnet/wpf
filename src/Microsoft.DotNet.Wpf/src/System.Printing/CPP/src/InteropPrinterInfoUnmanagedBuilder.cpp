// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
    Abstract:

        UnmanagedPrinterInfoLevelBuilder - utility class that builds unmanaged buffers
        that follow the layout of printing _INFO_ structures.
        
--*/

#include "win32inc.hpp"

#ifndef  __INTEROPNAMESPACEUSAGE_HPP__
#include <InteropNamespaceUsage.hpp>
#endif

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

using namespace MS::Internal::PrintWin32Thunk::Win32ApiThunk;
using namespace System::IO;

IntPtr
UnmanagedPrinterInfoLevelBuilder::
BuildEmptyUnmanagedPrinterInfoTwo(
    void
    )
{
    IntPtr win32PrinterInfoTwo = Marshal::AllocHGlobal(sizeof(PRINTER_INFO_2W));        
    
    Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pServerName),         IntPtr::Zero);
    Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pPrinterName),        IntPtr::Zero);
    Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pShareName),          IntPtr::Zero);
    Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pPrinterName),        IntPtr::Zero);
    Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pDriverName),         IntPtr::Zero);
    Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pPortName),           IntPtr::Zero);
    Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pComment),            IntPtr::Zero);
    Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pLocation),           IntPtr::Zero);
    Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pDevMode),            IntPtr::Zero);
    Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pSepFile),            IntPtr::Zero);
    Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pPrintProcessor),     IntPtr::Zero);
    Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pDatatype),           IntPtr::Zero);
    Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pParameters),         IntPtr::Zero);
    Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pSecurityDescriptor), IntPtr::Zero);
    Marshal::WriteInt32(win32PrinterInfoTwo,    offsetof(PRINTER_INFO_2W, Attributes),          0);
    Marshal::WriteInt32(win32PrinterInfoTwo,    offsetof(PRINTER_INFO_2W, Priority),            0);
    Marshal::WriteInt32(win32PrinterInfoTwo,    offsetof(PRINTER_INFO_2W, DefaultPriority),     0);
    Marshal::WriteInt32(win32PrinterInfoTwo,    offsetof(PRINTER_INFO_2W, StartTime),           0);
    Marshal::WriteInt32(win32PrinterInfoTwo,    offsetof(PRINTER_INFO_2W, UntilTime),           0);
    Marshal::WriteInt32(win32PrinterInfoTwo,    offsetof(PRINTER_INFO_2W, Status),              0);
    Marshal::WriteInt32(win32PrinterInfoTwo,    offsetof(PRINTER_INFO_2W, cJobs),               0);
    Marshal::WriteInt32(win32PrinterInfoTwo,    offsetof(PRINTER_INFO_2W, AveragePPM),          0);

    return win32PrinterInfoTwo;
}

IntPtr
UnmanagedPrinterInfoLevelBuilder::
BuildUnmanagedPrinterInfoTwo(
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
        IntPtr win32PrinterInfoTwo      = BuildEmptyUnmanagedPrinterInfoTwo();        
        IntPtr win32ServerName          = Marshal::StringToHGlobalUni(serverName);
        IntPtr win32PrinterName         = Marshal::StringToHGlobalUni(printerName);
        IntPtr win32DriverName          = Marshal::StringToHGlobalUni(driverName);
        IntPtr win32PortName            = Marshal::StringToHGlobalUni(portName);
        IntPtr win32PrintProcessorName  = Marshal::StringToHGlobalUni(printProcessorName);
        
        Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pServerName),         win32ServerName);
        Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pPrinterName),        win32PrinterName);
        Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pPrinterName),        win32PrinterName);
        Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pDriverName),         win32DriverName);
        Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pPortName),           win32PortName);
        Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pPrintProcessor),     win32PrintProcessorName);
        Marshal::WriteInt32(win32PrinterInfoTwo,    offsetof(PRINTER_INFO_2W, Attributes),          attributes);
        Marshal::WriteInt32(win32PrinterInfoTwo,    offsetof(PRINTER_INFO_2W, Priority),            priority);
        Marshal::WriteInt32(win32PrinterInfoTwo,    offsetof(PRINTER_INFO_2W, DefaultPriority),     defaultPriority);

        if (comment)
        {
            IntPtr win32Comment  = Marshal::StringToHGlobalUni(comment);
            Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pComment),     win32Comment);
        }

        if (location)
        {
            IntPtr win32Location  = Marshal::StringToHGlobalUni(location);
            Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pLocation),     win32Location);
        }

        if (shareName)
        {
            IntPtr win32ShareName  = Marshal::StringToHGlobalUni(shareName);
            Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pShareName),     win32ShareName);
        }

        if (separatorFile)
        {
            IntPtr win32SeparatorFile  = Marshal::StringToHGlobalUni(separatorFile);
            Marshal::WriteIntPtr(win32PrinterInfoTwo,   offsetof(PRINTER_INFO_2W, pSepFile),       win32SeparatorFile);
        }

        return win32PrinterInfoTwo;
}



IntPtr
UnmanagedPrinterInfoLevelBuilder::
WriteStringInUnmanagedPrinterInfo(
    IntPtr      win32PrinterInfo,
    String^     stringValue,
    int         offset
    )
{
    IntPtr win32String = IntPtr::Zero;

    if (win32PrinterInfo != IntPtr::Zero)
    {
        win32String = Marshal::StringToHGlobalUni(stringValue);
        Marshal::WriteIntPtr(win32PrinterInfo, offset, win32String);
    }

    return win32String;
}


bool
UnmanagedPrinterInfoLevelBuilder::
WriteIntPtrInUnmanagedPrinterInfo(
    IntPtr      win32PrinterInfoTwo,
    IntPtr      pointerValue,
    int         offset
    )
{
    bool    returnValue = false;

    if (win32PrinterInfoTwo != IntPtr::Zero)
    {
        Marshal::WriteIntPtr(win32PrinterInfoTwo, offset, pointerValue);
        returnValue = true;
    }

    return returnValue;
}

bool
UnmanagedPrinterInfoLevelBuilder::
WriteInt32InUnmanagedPrinterInfo(
    IntPtr      win32PrinterInfoTwo,
    Int32       value,
    int         offset
    )
{
    bool    returnValue = false;

    if (win32PrinterInfoTwo != IntPtr::Zero)
    {
        Marshal::WriteInt32(win32PrinterInfoTwo, offset, value);
        returnValue = true;
    }

    return returnValue;
}

void
UnmanagedPrinterInfoLevelBuilder::
FreeUnmanagedPrinterInfoTwo(
    IntPtr  win32PrinterInfoTwo
    )
{
    IntPtr  win32Buffer = IntPtr::Zero;

    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoTwo,  offsetof(PRINTER_INFO_2W, pServerName));    
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }
    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoTwo,  offsetof(PRINTER_INFO_2W, pPrinterName));
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }
    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoTwo,  offsetof(PRINTER_INFO_2W, pShareName));
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }
    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoTwo,  offsetof(PRINTER_INFO_2W, pDriverName));
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }
    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoTwo,  offsetof(PRINTER_INFO_2W, pPortName));
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }
    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoTwo,  offsetof(PRINTER_INFO_2W, pComment));
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }
    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoTwo,  offsetof(PRINTER_INFO_2W, pLocation));
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }
    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoTwo,  offsetof(PRINTER_INFO_2W, pDevMode));
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }
    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoTwo,  offsetof(PRINTER_INFO_2W, pSepFile));
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }
    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoTwo,  offsetof(PRINTER_INFO_2W, pPrintProcessor));
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }
    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoTwo,  offsetof(PRINTER_INFO_2W, pDatatype));
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }
    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoTwo,  offsetof(PRINTER_INFO_2W, pParameters));
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }
    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoTwo,  offsetof(PRINTER_INFO_2W, pSecurityDescriptor));
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }    
}

IntPtr
UnmanagedPrinterInfoLevelBuilder::
BuildEmptyUnmanagedPrinterInfoOne(
    void
    )
{
    IntPtr win32PrinterInfoOne      = Marshal::AllocHGlobal(sizeof(PRINTER_INFO_1W));        
    
    Marshal::WriteIntPtr(win32PrinterInfoOne,   offsetof(PRINTER_INFO_1W, pDescription), IntPtr::Zero);
    Marshal::WriteIntPtr(win32PrinterInfoOne,   offsetof(PRINTER_INFO_1W, pName),        IntPtr::Zero);
    Marshal::WriteIntPtr(win32PrinterInfoOne,   offsetof(PRINTER_INFO_1W, pComment),     IntPtr::Zero);
    Marshal::WriteInt32(win32PrinterInfoOne,    offsetof(PRINTER_INFO_1W, Flags),        0);
    
    return win32PrinterInfoOne;
}

void
UnmanagedPrinterInfoLevelBuilder::
FreeUnmanagedPrinterInfoOne(
    IntPtr win32PrinterInfoOne
    )
{
    IntPtr  win32Buffer = IntPtr::Zero;

    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoOne,  offsetof(PRINTER_INFO_1W, pName));    
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }
    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoOne,  offsetof(PRINTER_INFO_1W, pComment));
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }
    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoOne,  offsetof(PRINTER_INFO_1W, pDescription));
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }    
}

IntPtr
UnmanagedPrinterInfoLevelBuilder::
BuildEmptyUnmanagedPrinterInfoThree(
    void
    )
{
    IntPtr win32PrinterInfoThree = Marshal::AllocHGlobal(sizeof(PRINTER_INFO_3));        
    
    Marshal::WriteIntPtr(win32PrinterInfoThree, offsetof(PRINTER_INFO_3, pSecurityDescriptor), IntPtr::Zero);
    
    return win32PrinterInfoThree;
}

void
UnmanagedPrinterInfoLevelBuilder::
FreeUnmanagedPrinterInfoThree(
    IntPtr win32PrinterInfoThree
    )
{
    IntPtr  win32Buffer = IntPtr::Zero;

    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoThree, offsetof(PRINTER_INFO_3, pSecurityDescriptor));    
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }    
}

IntPtr
UnmanagedPrinterInfoLevelBuilder::
BuildEmptyUnmanagedPrinterInfoSix(
    void
    )
{
    IntPtr win32PrinterInfoSix = Marshal::AllocHGlobal(sizeof(PRINTER_INFO_6));        
    
    Marshal::WriteInt32(win32PrinterInfoSix, offsetof(PRINTER_INFO_6, dwStatus),   0);
    
    return win32PrinterInfoSix;
}

void
UnmanagedPrinterInfoLevelBuilder::
FreeUnmanagedPrinterInfoSix(
    IntPtr  win32PrinterInfoSix
    )
{
    // No internal pointers to free
}

IntPtr
UnmanagedPrinterInfoLevelBuilder::
BuildEmptyUnmanagedPrinterInfoSeven(
    void
    )
{
    IntPtr win32PrinterInfoSeven = Marshal::AllocHGlobal(sizeof(PRINTER_INFO_7W));        
    
    Marshal::WriteIntPtr(win32PrinterInfoSeven, offsetof(PRINTER_INFO_7W, pszObjectGUID), IntPtr::Zero);
    Marshal::WriteInt32(win32PrinterInfoSeven,  offsetof(PRINTER_INFO_7W, dwAction),   0);
    
    return win32PrinterInfoSeven;
}

void
UnmanagedPrinterInfoLevelBuilder::
FreeUnmanagedPrinterInfoSeven(
    IntPtr  win32PrinterInfoSeven
    )
{
    IntPtr  win32Buffer = IntPtr::Zero;
    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoSeven, offsetof(PRINTER_INFO_7W, pszObjectGUID));    
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }
}

IntPtr
UnmanagedPrinterInfoLevelBuilder::
BuildEmptyUnmanagedPrinterInfoEight(
    void
    )
{
    IntPtr win32PrinterInfoEight = Marshal::AllocHGlobal(sizeof(PRINTER_INFO_8W));        
    
    Marshal::WriteIntPtr(win32PrinterInfoEight, offsetof(PRINTER_INFO_8W, pDevMode), IntPtr::Zero);    
    
    return win32PrinterInfoEight;
}

bool
UnmanagedPrinterInfoLevelBuilder::
WriteDevModeInUnmanagedPrinterInfoEight(
    IntPtr  win32PrinterInfoEight,
    IntPtr  pDevMode
    )
{
    bool    returnValue = false;

    if (win32PrinterInfoEight != IntPtr::Zero)
    {
        Marshal::WriteIntPtr(win32PrinterInfoEight, offsetof(PRINTER_INFO_8W, pDevMode), pDevMode);    
        returnValue = true;
    }
    
    return returnValue;
}

bool
UnmanagedPrinterInfoLevelBuilder::
WriteDevModeInUnmanagedPrinterInfoNine(
    IntPtr win32PrinterInfoNine,
    IntPtr pDevMode
    )
{
    bool    returnValue = false;

    if (win32PrinterInfoNine != IntPtr::Zero)
    {
        Marshal::WriteIntPtr(win32PrinterInfoNine,  offsetof(PRINTER_INFO_8W, pDevMode), pDevMode);    
        returnValue = true;
    }
    
    return returnValue;
}

void
UnmanagedPrinterInfoLevelBuilder::
FreeUnmanagedPrinterInfoEight(
    IntPtr  win32PrinterInfoEight
    )
{
    IntPtr  win32Buffer = IntPtr::Zero;

    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoEight, offsetof(PRINTER_INFO_8W, pDevMode));    
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }
}

IntPtr
UnmanagedPrinterInfoLevelBuilder::
BuildEmptyUnmanagedPrinterInfoNine(
    void
    )
{
    IntPtr win32PrinterInfoNine = Marshal::AllocHGlobal(sizeof(PRINTER_INFO_9W));        
    
    Marshal::WriteIntPtr(win32PrinterInfoNine,  offsetof(PRINTER_INFO_9W, pDevMode), IntPtr::Zero);    
    
    return win32PrinterInfoNine;
}

void
UnmanagedPrinterInfoLevelBuilder::
FreeUnmanagedPrinterInfoNine(
    IntPtr  win32PrinterInfoNine
    )
{
    IntPtr  win32Buffer = IntPtr::Zero;
    win32Buffer = Marshal::ReadIntPtr(win32PrinterInfoNine, offsetof(PRINTER_INFO_9W, pDevMode));    
    if (win32Buffer != IntPtr::Zero)
    {
        Marshal::FreeHGlobal(win32Buffer);
    }
}

///////////////////////////////////////////////////////////////////////////////////

SafeHandle^
UnmanagedXpsDocEventBuilder::
XpsDocEventFixedDocSequence(
    XpsDocumentEventType    escape,
    UInt32                  jobIdentifier,
    String^                 jobName,
    Stream^                 printTicketStream,
    Boolean                 mustAddPrintTicket
    )
{   
    PropertyCollectionMemorySafeHandle^ collectionSafeHandle = nullptr;
    Int32   unmanagedPropertyCount = mustAddPrintTicket ? 4 : 3;

    try
    {
        collectionSafeHandle = PropertyCollectionMemorySafeHandle::AllocPropertyCollectionMemorySafeHandle(unmanagedPropertyCount);

        collectionSafeHandle->SetValue("EscapeCode", 0, Int32(escape));
        collectionSafeHandle->SetValue("JobIdentifier", 1, Int32(jobIdentifier));
        collectionSafeHandle->SetValue("JobName", 2, jobName);

        if (mustAddPrintTicket)
        {
            if (printTicketStream != nullptr)
            {
                collectionSafeHandle->SetValue("PrintTicket", 3, printTicketStream);
            }
            else
            {
                collectionSafeHandle->SetValue("PrintTicket", 3, System::IO::MemoryStream::typeid);
            }
        }
    }
    catch (SystemException^   exception)
    {
        if (collectionSafeHandle != nullptr)
        {
            delete collectionSafeHandle;
        }

        throw exception;
    }
    
    return collectionSafeHandle;
}

SafeHandle^
UnmanagedXpsDocEventBuilder::
XpsDocEventFixedDocument(
    XpsDocumentEventType    escape,
    UInt32                  fixedDocumentNumber,
    Stream^                 printTicketStream,
    Boolean                 mustAddPrintTicket
    )
{
    PropertyCollectionMemorySafeHandle^ collectionSafeHandle = nullptr;
    Int32   unmanagedPropertyCount = mustAddPrintTicket ? 3 : 2;

    try
    {
        collectionSafeHandle = PropertyCollectionMemorySafeHandle::AllocPropertyCollectionMemorySafeHandle(unmanagedPropertyCount);

        collectionSafeHandle->SetValue("EscapeCode", 0, Int32(escape));
        collectionSafeHandle->SetValue("DocumentNumber", 1, Int32(fixedDocumentNumber));

        if (mustAddPrintTicket)
        {
            if (printTicketStream != nullptr)
            {
                collectionSafeHandle->SetValue("PrintTicket", 2, printTicketStream);
            }
            else
            {
                collectionSafeHandle->SetValue("PrintTicket", 2, System::IO::MemoryStream::typeid);
            }
        }
    }
    catch (SystemException^   exception)
    {
        if (collectionSafeHandle != nullptr)
        {
            delete collectionSafeHandle;
        }

        throw exception;
    }
    
    return collectionSafeHandle;
}

SafeHandle^
UnmanagedXpsDocEventBuilder::
XpsDocEventFixedPage(
    XpsDocumentEventType    escape,
    UInt32                  fixedPageNumber,
    Stream^                 printTicketStream,
    Boolean                 mustAddPrintTicket
    )
{
    PropertyCollectionMemorySafeHandle^ collectionSafeHandle = nullptr;
    Int32   unmanagedPropertyCount = mustAddPrintTicket ? 3 : 2;

    try
    {
        collectionSafeHandle = PropertyCollectionMemorySafeHandle::AllocPropertyCollectionMemorySafeHandle(unmanagedPropertyCount);

        collectionSafeHandle->SetValue("EscapeCode", 0, Int32(escape));
        collectionSafeHandle->SetValue("PageNumber", 1, Int32(fixedPageNumber));

        if (mustAddPrintTicket)
        {
            if (printTicketStream != nullptr)
            {
                collectionSafeHandle->SetValue("PrintTicket", 2, printTicketStream);
            }
            else
            {
                collectionSafeHandle->SetValue("PrintTicket", 2, System::IO::MemoryStream::typeid);
            }
        }
    }
    catch (SystemException^   exception)
    {
        if (collectionSafeHandle != nullptr)
        {
            delete collectionSafeHandle;
        }

        throw exception;
    }
    
    return collectionSafeHandle;
}
