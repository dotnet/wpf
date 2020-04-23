// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __INTEROPPRINTERINFOUNMANAGEDBUILDER_HPP__
#define __INTEROPPRINTERINFOUNMANAGEDBUILDER_HPP__
/*++

    Abstract:

        Utility classes that allocate and free the unmanaged printer info
        buffers that are going to be sent to the Win32 APIs.
--*/
namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
    namespace Win32ApiThunk
    {
        ref class UnmanagedPrinterInfoLevelBuilder abstract
        {
            public:

            static
            IntPtr
            BuildEmptyUnmanagedPrinterInfoOne(
                void
                );        

            static
            void
            FreeUnmanagedPrinterInfoOne(
                IntPtr                  win32PrinterInfoOne
                );

            
            static
            IntPtr
            BuildEmptyUnmanagedPrinterInfoTwo(
                void
                );

            static
            IntPtr
            BuildUnmanagedPrinterInfoTwo(
                String^                 serverName,
                String^                 printerName,
                String^                 driverName,
                String^                 portName,
                String^                 printProcessorName,
                String^                 comment,
                String^                 location,
                String^                 shareName, 
                String^                 separatorFile,
                Int32                   attributes,        
                Int32                   priority,
                Int32                   defaultPriority
                );

            static
            IntPtr
            WriteStringInUnmanagedPrinterInfo(
                IntPtr                  win32PrinterInfo,
                String^                 stringValue,
                int                     offset
                );

            static
            bool
            WriteIntPtrInUnmanagedPrinterInfo(
                IntPtr                  win32PrinterInfo,
                IntPtr                  pointerValue,
                int                     offset
                );

            static
            bool
            WriteInt32InUnmanagedPrinterInfo(
                IntPtr                  win32PrinterInfo,
                Int32                   value,
                int                     offset
                );

            static
            void
            FreeUnmanagedPrinterInfoTwo(
                IntPtr                  win32PrinterInfoTwo
                );

            static
            IntPtr
            BuildEmptyUnmanagedPrinterInfoThree(
                void
                );

            static
            void
            FreeUnmanagedPrinterInfoThree(
                IntPtr                  win32PrinterInfoThree
                );

            static
            IntPtr
            BuildEmptyUnmanagedPrinterInfoSix(
                void
                );

            static
            void
            FreeUnmanagedPrinterInfoSix(
                IntPtr                  win32PrinterInfoSix
                );

            static
            IntPtr
            BuildEmptyUnmanagedPrinterInfoSeven(
                void
                );

            static
            void
            FreeUnmanagedPrinterInfoSeven(
                IntPtr                  win32PrinterInfoSeven
                );

            static
            IntPtr
            BuildEmptyUnmanagedPrinterInfoEight(
                void
                );

            static
            bool
            WriteDevModeInUnmanagedPrinterInfoEight(
                IntPtr                  win32PrinterInfoEight,
                IntPtr                  pDevMode
                );

            static
            bool
            WriteDevModeInUnmanagedPrinterInfoNine(
                IntPtr                  win32PrinterInfoNine,
                IntPtr                  pDevMode
                );

            static
            void
            FreeUnmanagedPrinterInfoEight(
                IntPtr                  win32PrinterInfoEight 
                );

            static
            IntPtr
            BuildEmptyUnmanagedPrinterInfoNine(
                void
                );

            static
            void
            FreeUnmanagedPrinterInfoNine(
                IntPtr                  win32PrinterInfoNine 
                );
        };

        private ref class  UnmanagedXpsDocEventBuilder abstract
        { 
            public:

            static
            SafeHandle^
            XpsDocEventFixedDocSequence(
                XpsDocumentEventType    escape,
                UInt32                  jobId,
                String^                 jobName,
                System::IO::Stream^     printTicket,
                Boolean                 mustAddPrintTicket
                );
            
            static
            SafeHandle^
            XpsDocEventFixedDocument(
                XpsDocumentEventType    escape,
                UInt32                  fixedDocumentNumber,
                System::IO::Stream^     printTicket,
                Boolean                 mustAddPrintTicket
                );

            static
            SafeHandle^
            XpsDocEventFixedPage(
                XpsDocumentEventType    escape,
                UInt32                  fixedPageNumber,
                System::IO::Stream^     printTicket,
                Boolean                 mustAddPrintTicket
                );
            
            
        };
    }
}
}
}
#endif
