// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once 

#ifndef __INTEROPWIN32SPLAPITHUNK_HPP__
#define __INTEROPWIN32SPLAPITHUNK_HPP__
/*++

    Abstract:        
        
        PInvoke methods definition.
--*/

using namespace System::Windows::Xps::Serialization;
using namespace System::Runtime::InteropServices;

namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
namespace Win32ApiThunk
{   
    private ref class UnsafeNativeMethods abstract
    {
        public:

        [DllImportAttribute("winspool.drv",EntryPoint="OpenPrinterW",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeOpenPrinter(String^, IntPtr*, PrinterDefaults^);

        [DllImportAttribute("winspool.drv",EntryPoint="GetPrinterW",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeGetPrinter(IntPtr, UInt32, SafeMemoryHandle^, UInt32, UInt32*);

        [DllImportAttribute("winspool.drv",EntryPoint="GetPrinterDataW",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        UInt32
        InvokeGetPrinterData(IntPtr, String^, UInt32*, SafeMemoryHandle^, UInt32, UInt32*);

        [DllImportAttribute("winspool.drv",EntryPoint="GetPrinterDriverW",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeGetPrinterDriver(IntPtr, String^, UInt32, SafeMemoryHandle^, UInt32, UInt32*);

        [DllImportAttribute("winspool.drv",EntryPoint="EnumPrintersW",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeEnumPrinters(UInt32, String^, UInt32, SafeMemoryHandle^, UInt32, UInt32*, UInt32*);

        [DllImportAttribute("winspool.drv",EntryPoint="ClosePrinter",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        [System::Runtime::ConstrainedExecution::ReliabilityContract(System::Runtime::ConstrainedExecution::Consistency::WillNotCorruptState,
                                                                    System::Runtime::ConstrainedExecution::Cer::Success)]
        static
        bool
        InvokeClosePrinter(IntPtr);

        [DllImportAttribute("winspool.drv",EntryPoint="AddPrinterConnectionW",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeAddPrinterConnection(String^);

        [DllImportAttribute("winspool.drv",EntryPoint="DeletePrinterConnectionW",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeDeletePrinterConnection(String^);

        [DllImportAttribute("winspool.drv",EntryPoint="GetDefaultPrinterW",
                            CharSet=CharSet::Unicode,
                            SetLastError=true, 
                            CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeGetDefaultPrinter(System::Text::StringBuilder^, int*);

        [DllImportAttribute("winspool.drv",EntryPoint="GetJobW",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeGetJob(IntPtr, UInt32, UInt32, SafeMemoryHandle^, UInt32, UInt32*);


        [DllImportAttribute("winspool.drv",EntryPoint="SetJobW",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeSetJob(IntPtr, UInt32, UInt32, IntPtr, UInt32);


        [DllImportAttribute("winspool.drv",EntryPoint="EnumJobsW",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeEnumJobs(IntPtr, UInt32, UInt32, UInt32, SafeMemoryHandle^, UInt32, UInt32*, UInt32*);

        #ifdef XPSJOBNOTIFY

        [DllImportAttribute("winspool.drv", EntryPoint = "AddJob",
                            CharSet = CharSet::Unicode,
                            SetLastError = true,
                            CallingConvention = CallingConvention::Winapi)]

        static
        bool
        InvokeAddJob(IntPtr, UInt32, SafeMemoryHandle^, UInt32, UInt32*);

        [DllImportAttribute("winspool.drv", EntryPoint = "ScheduleJob",
                            CharSet = CharSet::Unicode,
                            SetLastError = true,
                            CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeScheduleJob(IntPtr, UInt32);
        
        [DllImport("winspool.drv", EntryPoint="EDocWritePrinter",
                   CharSet=CharSet::Unicode,
                   SetLastError=true, 
                   CallingConvention = CallingConvention::Winapi)]
        static 
        Boolean
        InvokeEDocWritePrinter(IntPtr, IntPtr, Int32, Int32*);

        [DllImport("winspool.drv", EntryPoint="FlushPrinter",
                   CharSet=CharSet::Unicode,
                   SetLastError=true, 
                   CallingConvention = CallingConvention::Winapi)]
        static 
        Boolean
        InvokeFlushPrinter(IntPtr, IntPtr, Int32, Int32*, Int32);

        #endif //XPSJOBNOTIFY

        [DllImportAttribute("winspool.drv",EntryPoint="ReportJobProcessingProgress",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        Int32
        InvokeReportJobProgress(IntPtr, Int32, Int32, Int32);

        [DllImportAttribute("winspool.drv",EntryPoint="StartPagePrinter",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeStartPagePrinter(IntPtr);

        [DllImportAttribute("winspool.drv",EntryPoint="EndPagePrinter",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeEndPagePrinter(IntPtr);

        [DllImportAttribute("winspool.drv",EntryPoint="SetDefaultPrinterW",
                            CharSet=CharSet::Unicode,
                            SetLastError=true, 
                            CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeSetDefaultPrinter(String^);
        
        [DllImport("winspool.drv", EntryPoint = "StartDocPrinter",
                   CharSet = CharSet::Unicode,
                   SetLastError = true,
                   CallingConvention = CallingConvention::Winapi)]
        static
        Int32
        InvokeStartDocPrinter(IntPtr, Int32, DocInfoThree^);

        [DllImport("winspool.drv", EntryPoint = "EndDocPrinter",
                   CharSet = CharSet::Unicode,
                   SetLastError = true,
                   CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeEndDocPrinter(IntPtr);

        [DllImport("winspool.drv", EntryPoint = "AbortPrinter",
                   CharSet = CharSet::Unicode,
                   SetLastError = true,
                   CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeAbortPrinter(IntPtr);


        [DllImport("winspool.drv", EntryPoint = "GetSpoolFileHandle",
                   CharSet = CharSet::Unicode,
                   SetLastError = true,
                   CallingConvention = CallingConvention::Winapi)]
        static
        IntPtr
        InvokeGetSpoolFileHandle(IntPtr);

        [DllImport("winspool.drv", EntryPoint = "CommitSpoolData",
                   CharSet = CharSet::Unicode,
                   SetLastError = true,
                   CallingConvention = CallingConvention::Winapi)]
        static
        IntPtr
        InvokeCommitSpoolData(IntPtr, Microsoft::Win32::SafeHandles::SafeFileHandle^, Int32);

        [DllImport("winspool.drv", EntryPoint = "CloseSpoolFileHandle",
                   CharSet = CharSet::Unicode,
                   SetLastError = true,
                   CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeCloseSpoolFileHandle(IntPtr, Microsoft::Win32::SafeHandles::SafeFileHandle^);

        [DllImport("winspool.drv", EntryPoint = "DocumentEvent",
                   CharSet = CharSet::Unicode,
                   SetLastError = true,
                   CallingConvention = CallingConvention::Winapi)]
        static
        Int32
        InvokeDocumentEvent(IntPtr, IntPtr, Int32, UInt32, SafeHandle^, UInt32, SafeMemoryHandle^);

        [DllImportAttribute("winspool.drv",EntryPoint="SetPrinterDataW",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        UInt32
        InvokeSetPrinterDataIntPtr(IntPtr, String^, UInt32, IntPtr, UInt32);

        [DllImportAttribute("winspool.drv",EntryPoint="SetPrinterDataW",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        UInt32
        InvokeSetPrinterDataInt32(IntPtr, String^, UInt32, Int32*, UInt32);

        [DllImportAttribute("winspool.drv",EntryPoint="AddPrinterW",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        HANDLE*
        InvokeAddPrinter(String^, UInt32, SafeMemoryHandle^);
        
        [DllImportAttribute("winspool.drv",EntryPoint="SetPrinterW",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeSetPrinter(IntPtr, UInt32, SafeMemoryHandle^, UInt32);

        [DllImportAttribute("winspool.drv",EntryPoint="DeletePrinter",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        bool
        InvokeDeletePrinter(IntPtr);
        
        [DllImport("Kernel32.dll", EntryPoint="GetComputerNameW",
                   CharSet=CharSet::Unicode,
                   SetLastError=true, 
                   CallingConvention = CallingConvention::Winapi)]
        static 
        bool 
        GetComputerName(System::Text::StringBuilder^ nameBuffer, int* bufferSize);
    };
    
    private ref class PresentationNativeUnsafeNativeMethods abstract
    {
    public:
        static
        BOOL
        IsStartXpsPrintJobSupported(VOID) 
        {
            return IsStartXpsPrintJobSupportedImpl();
        }
        
        static
        UInt32
        LateBoundStartXpsPrintJob( 
            String^ printerName,
            String^ jobName,
            String^ outputFileName,
            Microsoft::Win32::SafeHandles::SafeWaitHandle^ progressEvent,
            Microsoft::Win32::SafeHandles::SafeWaitHandle^ completionEvent,
            UINT8  *printablePagesOn,
            UINT32 printablePagesOnCount,
            VOID **xpsPrintJob,
            VOID **documentStream,
            VOID **printTicketStream
        )
        {
            return LateBoundStartXpsPrintJobImpl(
                printerName, 
                jobName, 
                outputFileName, 
                progressEvent, 
                completionEvent, 
                printablePagesOn,
                printablePagesOnCount,
                xpsPrintJob,
                documentStream,
                printTicketStream);
        }
        
        static
        BOOL
        IsPrintPackageTargetSupported(VOID)
        {
            return IsPrintPackageTargetSupportedImpl();
        }

        static
            UInt32
        PrintToPackageTarget(
            String^ printerName,
            String^ jobName,
            ComTypes::IStream^ jobPrintTicketStream,
            [Out] RCW::IPrintDocumentPackageTarget^ %printDocPackageTarget,
            [Out] RCW::IXpsDocumentPackageTarget^ %xpsPackageTarget
        )
        {
            return PrintToPackageTargetImpl(
                printerName,
                jobName,
                jobPrintTicketStream,
                printDocPackageTarget,
                xpsPackageTarget);
        }
    private:
        [DllImportAttribute("PresentationNative_cor3.dll" ,EntryPoint="IsStartXpsPrintJobSupported",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        BOOL
        IsStartXpsPrintJobSupportedImpl(VOID);
        
        [DllImportAttribute("PresentationNative_cor3.dll" ,EntryPoint="LateBoundStartXpsPrintJob",
                             CharSet=CharSet::Unicode,
                             SetLastError=true, 
                             CallingConvention = CallingConvention::Winapi)]
        static
        UInt32
        LateBoundStartXpsPrintJobImpl( 
            String^ printerName,
            String^ jobName,
            String^ outputFileName,
            Microsoft::Win32::SafeHandles::SafeWaitHandle^ progressEvent,
            Microsoft::Win32::SafeHandles::SafeWaitHandle^ completionEvent,
            UINT8  *printablePagesOn,
            UINT32 printablePagesOnCount,
            VOID **xpsPrintJob,
            VOID **documentStream,
            VOID **printTicketStream
        );
        
        [DllImportAttribute("PresentationNative_cor3.dll", EntryPoint = "IsPrintPackageTargetSupported",
            CharSet = CharSet::Unicode,
            SetLastError = true,
            CallingConvention = CallingConvention::Winapi)]
        static
        BOOL
        IsPrintPackageTargetSupportedImpl(VOID);

        [DllImportAttribute("PresentationNative_cor3.dll", EntryPoint = "PrintToPackageTarget",
            CharSet = CharSet::Unicode,
            SetLastError = true,
            CallingConvention = CallingConvention::Winapi)]
        static
        Int32
        PrintToPackageTargetImpl(
            String^ printerName,
            String^ jobName,
            ComTypes::IStream^ jobPrintTicketStream,
            [MarshalAs(UnmanagedType::Interface)]
            [Out] RCW::IPrintDocumentPackageTarget^ %printDocPackageTarget,
            [MarshalAs(UnmanagedType::Interface)]
            [Out] RCW::IXpsDocumentPackageTarget^ %xpsPackageTarget
        );
    };

}
}
}
}
#endif

