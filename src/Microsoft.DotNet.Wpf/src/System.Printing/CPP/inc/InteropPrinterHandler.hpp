// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __INTEROPPRINTERHANDLER_HPP__
#define __INTEROPPRINTERHANDLER_HPP__
/*++
    Abstract:

        Managed wrapper for Win32 print APIs. This object wraps a printer handle
        and does gets, sets and enum operations. Implements IDisposable. The caller
        must call Dispose when done using the object.
--*/
namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
    using namespace System::IO;
    using namespace System::Security;
    using namespace System::Runtime::InteropServices;

    namespace DirectInteropForPrintQueue
    {   
        ref class PrinterInfoTwoSetter;
    }

    ///<remarks>
    ///  SafeHandle that wraps native memory.
    ///</remarks>
    private ref class  SafeMemoryHandle : public System::Runtime::InteropServices::SafeHandle 
    {
        public:

        ///<remarks>
        /// Allocates and initializes native memory takes ownership (will free memory in Dispose).
        ///</remarks>
        static 
        bool
        TryCreate(
            Int32   byteCount,
            __out SafeMemoryHandle^ % result
            );

        ///<remarks>
        /// Allocates and initializes native memory takes ownership (will free memory in Dispose).
        ///</remarks>
        static 
        SafeMemoryHandle^
        Create(
            Int32   byteCount
            );

        SafeMemoryHandle(
            IntPtr   Win32Pointer
            );

        ///<remarks>
        /// Wraps an IntPtr but does not take ownership and does not free handle in Dispose.
        ///</remarks>
        static 
        SafeMemoryHandle^ 
        Wrap(
            IntPtr   Win32Pointer
            );

        property
        Boolean
        IsInvalid
        {
            Boolean virtual get() override;
        }

        Boolean 
        virtual ReleaseHandle(
            void
            ) override;

        property
        Int32
        Size
        {
            Int32 virtual get();
        }

        void 
        CopyFromArray(
            array<Byte>^ source,
            Int32 startIndex, 
            Int32 length
            );
        
        void 
        CopyToArray(
            array<Byte>^ destination, 
            Int32 startIndex, 
            Int32 length
            );

        static 
        property
        SafeMemoryHandle^ Null 
        {
            SafeMemoryHandle^ get();
        }

        private:
        
        SafeMemoryHandle(
            IntPtr   Win32Pointer,
            Boolean  ownsHandle
            );

        static 
        Exception^ 
        VerifyBufferArguments(
            String^ bufferName,
            array<Byte>^ buffer,
            Int32 startIndex, 
            Int32 length
            );
    };



    ///<remarks>
    ///  SafeHandle that wraps a pointer to a PRINTER_INFO_1.
    ///</remarks>
    private ref class  PrinterInfoOneSafeMemoryHandle sealed : public SafeMemoryHandle 
    {
        public:
            
        PrinterInfoOneSafeMemoryHandle(
            void
            );

        Boolean 
        virtual ReleaseHandle(
            void
            ) override;
    };

    ///<remarks>
    ///  SafeHandle that wraps a pointer to a PRINTER_INFO_3
    ///</remarks>
    private ref class  PrinterInfoThreeSafeMemoryHandle sealed : public SafeMemoryHandle 
    {
        public:
            
        PrinterInfoThreeSafeMemoryHandle(
            void
            );

        Boolean 
        virtual ReleaseHandle(
            void
            ) override;
    };

    ///<remarks>
    ///  SafeHandle that wraps a pointer to a PRINTER_INFO_6
    ///</remarks>
    private ref class  PrinterInfoSixSafeMemoryHandle sealed : public SafeMemoryHandle 
    {
        public:
            
        PrinterInfoSixSafeMemoryHandle(
            void
            );

        Boolean 
        virtual ReleaseHandle(
            void
            ) override;
    };

    ///<remarks>
    ///  SafeHandle that wraps a pointer to a PRINTER_INFO_7
    ///</remarks>
    private ref class  PrinterInfoSevenSafeMemoryHandle sealed : public SafeMemoryHandle 
    {
        public:
            
        PrinterInfoSevenSafeMemoryHandle(
            void
            );

        Boolean 
        virtual ReleaseHandle(
            void
            ) override;
    };


    ///<remarks>
    ///  SafeHandle that wraps a pointer to a PRINTER_INFO_8
    ///</remarks>
    private ref class  PrinterInfoEightSafeMemoryHandle sealed : public SafeMemoryHandle 
    {
        public:
            
        PrinterInfoEightSafeMemoryHandle(
            void
            );

        Boolean 
        virtual ReleaseHandle(
            void
            ) override;
    };


    ///<remarks>
    ///  SafeHandle that wraps a pointer to a PRINTER_INFO_9
    ///</remarks>
    private ref class  PrinterInfoNineSafeMemoryHandle sealed : public SafeMemoryHandle 
    {
        public:
            
        PrinterInfoNineSafeMemoryHandle(
            void
            );

        Boolean 
        virtual ReleaseHandle(
            void
            ) override;
    };


    private ref class  PropertyCollectionMemorySafeHandle sealed : public System::Runtime::InteropServices::SafeHandle 
    {
        public:

        static
        PropertyCollectionMemorySafeHandle^
        AllocPropertyCollectionMemorySafeHandle(
            UInt32   propertyCount
            );

        property
        Boolean
        IsInvalid
        {
            Boolean virtual get() override;
        }

        Boolean 
        virtual ReleaseHandle(
            void
            ) override;        

        void
        SetValue(
            String^         propertyName,
            UInt32          index,
            System::Type^   value
            );

        void
        SetValue(
            String^     propertyName,
            UInt32      index,
            Object^     value
            );

        private:

        PropertyCollectionMemorySafeHandle(
            IntPtr   Win32Pointer
            );

    };

    private ref class  DocEventFilter
    {
        public:

        DocEventFilter(
            void
            );

        Boolean
        IsXpsDocumentEventSupported(
            XpsDocumentEventType    escape
            );

        void
        SetUnsupportedXpsDocumentEvent(
            XpsDocumentEventType    escape
            );

        private:

        array<XpsDocumentEventType>^         eventsfilter;
        static
        Int32                                supportedEventsCount = Int32(XpsDocumentEventType::AddFixedDocumentSequencePost) + 1;
    };

    private ref class PrinterThunkHandler  sealed : public PrinterThunkHandlerBase 
    {
        public:

        PrinterThunkHandler(
            IntPtr              win32PrintHandle
            );

        PrinterThunkHandler(
            String^             printName
            );

        PrinterThunkHandler(
            String^             printName,
            PrinterDefaults^    printerDefaults
            );

        property
        virtual 
        Boolean
        IsInvalid
        {
            Boolean virtual get() override;
        }

        virtual
        Boolean 
        ReleaseHandle(
            void
            ) override;

        PrinterThunkHandler^
        DuplicateHandler(
            void
            );

        IPrinterInfo^
        ThunkGetPrinter(
            UInt32              level
            );

        IPrinterInfo^
        ThunkGetDriver(
            UInt32              level,
            String^             environment
            );

        IPrinterInfo^
        ThunkEnumDrivers(
            UInt32              level,
            String^             environment
            );

        IPrinterInfo^
        ThunkGetJob(
            UInt32              level,
            UInt32              jobID
            );

        IPrinterInfo^
        ThunkEnumJobs(
            UInt32              level,
            UInt32              firstJob,
            UInt32              numberOfJobs
            );

        Boolean
        ThunkSetJob(
            UInt32              jobID,
            UInt32              command
            );

        virtual
        Int32
        ThunkStartDocPrinter(
            DocInfoThree^         docInfo,
            PrintTicket^ printTicket
            ) override;

        virtual
        Boolean
        ThunkEndDocPrinter(
            void
            ) override;

        Boolean
        ThunkStartPagePrinter(
            void
            );

        Boolean
        ThunkEndPagePrinter(
            void
            );

        virtual
        Boolean
        ThunkAbortPrinter(
            void
            ) override;

        virtual
        void
        ThunkOpenSpoolStream(
            void
            ) override;

        virtual
        void
        ThunkCommitSpoolData(
            Int32                   bytes
            ) override;

        virtual
        Boolean
        ThunkCloseSpoolStream(
            void
            ) override;

        property
        virtual 
        int
        JobIdentifier
        {
            int get() override;
        }    

        property
        virtual 
        Stream^
        SpoolStream
        {
            virtual Stream^ get() override;
        }  

        Int32
        ThunkDocumentEvent(
            XpsDocumentEventType    escape,
            UInt32                  inBufferSize,
            SafeHandle^             inBuffer,
            UInt32                  outBufferSize,
            SafeMemoryHandle^       outBuffer
            );

        Int32
        ThunkDocumentEvent(
            XpsDocumentEventType    escape
            );

        Int32
        ThunkDocumentEvent(
            XpsDocumentEventType    escape,
            SafeHandle^             inputBufferSafeHandle
            );

        Boolean
        PrinterThunkHandler::
        ThunkDocumentEventPrintTicket(
            XpsDocumentEventType                escapePre,
            XpsDocumentEventType                escapePost,
            SafeHandle^                         inputBufferSafeHandle,
            System::IO::MemoryStream^%          printTicketStream
            );

        Int32
        ThunkDocumentEventPrintTicketPost(
            XpsDocumentEventType    escape,            
            SafeMemoryHandle^       xpsDocEventOutputBuffer,
            UInt32                  xpsDocEventOutputBufferSize
            );

        Boolean
        IsXpsDocumentEventSupported(
            XpsDocumentEventType    escape,
            Boolean                 reset
            );

        void
        SetUnsupportedXpsDocumentEvent(
            XpsDocumentEventType    escape
            );

        #ifdef XPSJOBNOTIFY

        IPrinterInfo^
        ThunkAddJob(
            UInt32              level
            );

        Boolean
        ThunkScheduleJob(
            UInt32              jobID
            );

        #endif // XPSJOBNOTIFY

        virtual
        Int32
        ThunkReportJobProgress(
            Int32                                                           jobId,
            JobOperation                                                    jobOperation,
            System::Windows::Xps::Packaging::PackagingAction                packagingAction
            ) override;

        

        Boolean
        ThunkDeletePrinter(
            void
            );

        static
        Boolean
        ThunkSetPrinterDataString(
            PrinterThunkHandler^    printerHandle,
            String^                 valueName,
            Object^                 value
            );

        static
        Boolean
        ThunkSetPrinterDataInt32(
            PrinterThunkHandler^    printerHandle,
            String^                 valueName,
            Object^                 value
            );

        static
        Boolean
        ThunkSetPrinterDataBoolean(
            PrinterThunkHandler^    printerHandle,
            String^                 valueName,
            Object^                 value
            );

        static
        Boolean
        ThunkSetPrinterDataServerEventLogging(
            PrinterThunkHandler^    printerHandle,
            String^                 valueName,
            Object^                 value
            );

        static
        Boolean
        ThunkSetPrinterDataThreadPriority(
            PrinterThunkHandler^    printerHandle,
            String^                 valueName,
            Object^                 value
            );

        static
        Object^
        ThunkGetPrinterDataString(
            PrinterThunkHandler^    printerHandle,
            String^                 valueName
            );

        static
        Object^
        ThunkGetPrinterDataInt32(
            PrinterThunkHandler^    printerHandle,
            String^                 valueName
            );

        static
        Object^
        ThunkGetPrinterDataBoolean(
            PrinterThunkHandler^    printerHandle,
            String^                 valueName
            );

        static
        Object^
        ThunkGetPrinterDataThreadPriority(
            PrinterThunkHandler^    printerHandle,
            String^                 valueName
            );

        static
        Object^
        ThunkGetPrinterDataServerEventLogging(
            PrinterThunkHandler^    printerHandle,
            String^                 valueName
            );

        Boolean
        ThunkSetPrinter(
            UInt32              command
            );

        Boolean
        ThunkSetPrinter(
            UInt32              level,
            SafeMemoryHandle^   win32PrinterInfo
            );

        static
        PrinterThunkHandler^
        ThunkAddPrinter(
            String^             serverName,
            String^             printerName,
            String^             driverName,
            String^             portName,
            String^             printProcessorName,
            String^             comment,
            String^             location,
            String^             shareName,
            String^             separatorFile,
            Int32               attributes,
            Int32               priority,
            Int32               defaultPriority
            );

        static
        PrinterThunkHandler^
        ThunkAddPrinter(
            String^                                              serverName,
            DirectInteropForPrintQueue::PrinterInfoTwoSetter^    printInfoTwoLeveThunk
            );

        static
        IPrinterInfo^
        ThunkEnumPrinters(
            String^             serverName,
            UInt32              level,
            UInt32              flags
            );

        static
        Boolean
        ThunkAddPrinterConnection(
            String^             path
            );

        static
        Boolean
        ThunkDeletePrinterConnection(
            String^             path
            );

        static
        String^
        ThunkGetDefaultPrinter(
            void
            );

        static
        Boolean
        ThunkSetDefaultPrinter(
            String^         path
            );

        static
        String^
        GetLocalMachineName(
            void
            );

        #ifdef XPSJOBNOTIFY

        static
        Int32
        ThunkWritePrinter(
            PrinterThunkHandler^    printerHandle,
            array<Byte>^            array,
            Int32                   offset,
            Int32                   count,
            Int32&                  writtenDataCount
            );

        static
        Int32
        ThunkFlushPrinter(
            PrinterThunkHandler^    printerHandle,
            array<Byte>^            array,
            Int32                   offset,
            Int32                   count,
            Int32&                  flushedByteCount,
            Int32                   portIdleTime
            );

        #endif //XPSJOBNOTIFY

        Boolean
        ThunkIsMetroDriverEnabled(
            void
            );

        private:

        FileStream^ 
        PrinterThunkHandler::
        CreateSpoolStream(
            IntPtr fileHandle
            );

        static
        IPrinterInfo^
        GetManagedPrinterInfoObject(
            UInt32              level,
            SafeMemoryHandle^   win32HeapBuffer,
            UInt32              count
            );

        static
        IPrinterInfo^
        GetManagedDriverInfoObject(
            UInt32              level,
            SafeMemoryHandle^   win32HeapBuffer,
            UInt32              count
            );

        static
        IPrinterInfo^
        GetManagedJobInfoObject(
            UInt32              level,
            SafeMemoryHandle^   win32HeapBuffer,
            UInt32              count
            );

        Boolean
        ThunkOpenPrinter(
            String^             name,
            PrinterDefaults^    openPrinterDefaults
            );

        [System::Runtime::ConstrainedExecution::ReliabilityContract(System::Runtime::ConstrainedExecution::Consistency::WillNotCorruptState,
                                                           System::Runtime::ConstrainedExecution::Cer::Success)]
        Boolean
        ThunkClosePrinter(
            void
            );

        Boolean
        ThunkSetPrinterDataStringInternal(
            String^                 valueName,
            Object^                 value
            );

        Boolean
        ThunkSetPrinterDataInt32Internal(
            String^                 valueName,
            Object^                 value
            );

        Boolean
        ThunkSetPrinterDataBooleanInternal(
            String^                 valueName,
            Object^                 value
            );

        Boolean
        ThunkSetPrinterDataServerEventLoggingInternal(
            String^                 valueName,
            Object^                 value
            );

        Boolean
        ThunkSetPrinterDataThreadPriorityInternal(
            String^                 valueName,
            Object^                 value
            );

        #ifdef XPSJOBNOTIFY

        Int32
        ThunkWritePrinterInternal(
            array<Byte>^            array,
            Int32                   offset,
            Int32                   count,
            Int32&                  writtenDataCount
            );  

        Int32
        ThunkFlushPrinterInternal(
            array<Byte>^            array,
            Int32                   offset,
            Int32                   count,
            Int32&                  flushedByteCount,
            Int32                   portIdleTime
            );

        #endif //XPSJOBNOTIFY

        Object^
        ThunkGetPrinterDataStringInternal(
            String^                 valueName
            );

        Object^
        ThunkGetPrinterDataInt32Internal(
            String^                 valueName
            );

        Object^
        ThunkGetPrinterDataBooleanInternal(
            String^                 valueName
            );

        Object^
        ThunkGetPrinterDataThreadPriorityInternal(
            String^                 valueName
            );
        
        Object^
        ThunkGetPrinterDataServerEventLoggingInternal(
            String^                 valueName
            );
        
        PrinterThunkHandler(
            void
            );

        String^                             printerName;

        PrinterDefaults^                    printerDefaults;
        UInt32                              printersCount;
        Boolean                             isDisposed;
        Boolean                             isRunningDownLevel;

        FileStream^                         spoolStream;

        Boolean                             isInPartialTrust;

        int                                 jobIdentifier;

        XpsDocumentEventType                previousXpsDocEventEscape;
        
        DocEventFilter^                     docEventFilter;
        static
        const 
        int                                 MaxPath = MAX_PATH;
    };

}
}
}
#endif

