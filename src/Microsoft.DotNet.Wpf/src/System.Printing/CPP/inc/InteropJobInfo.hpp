// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __INTEROPJOBINFO_HPP__
#define __INTEROPJOBINFO_HPP__
/*++
    Abstract:

        The file contains the definition for the managed classes that 
        hold the pointers to the JOB_INFO_ unmanaged structures and 
        know how to retrieve a property based on it's name. 

--*/
namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
namespace DirectInteropForJob
{
    using namespace System::Security;
    using namespace System::Drawing::Printing;

    private ref class JobInfoOne  : public IPrinterInfo
    {
	    public :

        JobInfoOne(
		    SafeMemoryHandle^       unmanagedPrinterInfo,
            UInt32                  count
            );

        virtual
        void
        Release(
            void
            );

        property
        SafeMemoryHandle^
        Win32SafeHandle
        {
            virtual SafeMemoryHandle^ get();
        }

        Object^
        GetValueFromName(
            String^         name
            );

        virtual Object^
        GetValueFromName(
            String^         name,
            UInt32          index
            );

        virtual bool
        SetValueFromName(
            String^         name,
            Object^         value
            );

        property
        virtual
        UInt32
        Count
        {
            UInt32 get();
        }

        private:

        static
        JobInfoOne(
            void
            )
        {
            getAttributeMap = gcnew Hashtable();

            RegisterAttributeMaps();
        }

        static
        void
        RegisterAttributeMaps(
            void
            );

        static
        Object^
        GetJobId(
            JOB_INFO_1W*        unmanagedPrinterInfo
            );

        static
        Object^
        GetServerName(
            JOB_INFO_1W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetPrinterName(
            JOB_INFO_1W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetUserName(
            JOB_INFO_1W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetDocumentName(
            JOB_INFO_1W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetDatatype(
            JOB_INFO_1W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetStatusString(
            JOB_INFO_1W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetStatus(
            JOB_INFO_1W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetPriority(
            JOB_INFO_1W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetPosition(
            JOB_INFO_1W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetTotalPages(
            JOB_INFO_1W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetPagesPrinted(
            JOB_INFO_1W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetTimeSubmitted(
            JOB_INFO_1W*        unmanagedPrinterInfo
            );

        delegate
        Object^
        GetValue(
            JOB_INFO_1W*        unmanagedPrinterInfo
            );

        static
        Hashtable^                  getAttributeMap;

        SafeMemoryHandle^           jobInfoOneSafeHandle;

        bool                        isDisposed;
        UInt32                      jobsCount;

    };


    private ref class JobInfoTwo  : public IPrinterInfo
    {
	    public :

        JobInfoTwo(
		    SafeMemoryHandle^       unmanagedPrinterInfo,
            UInt32                  count
            );

        virtual
        void
        Release(
            void
            );

        property
        SafeMemoryHandle^
        Win32SafeHandle
        {
            virtual SafeMemoryHandle^ get();
        }

        Object^
        GetValueFromName(
            String^         name
            );

        virtual Object^
        GetValueFromName(
            String^         name,
            UInt32          index
            );

        virtual bool
        SetValueFromName(
            String^         name,
            Object^         value
            );

        property
        virtual
        UInt32
        Count
        {
            UInt32 get();
        }

        private:
        
        static
        JobInfoTwo(
            void
            )
        {
            getAttributeMap = gcnew Hashtable();

            RegisterAttributeMaps();
        }

        static
        void
        RegisterAttributeMaps(
            void
            );

        static
        Object^
        GetJobId(
            JOB_INFO_2W*        unmanagedPrinterInfo
            );

        static
        Object^
        GetServerName(
            JOB_INFO_2W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetPrinterName(
            JOB_INFO_2W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetUserName(
            JOB_INFO_2W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetDocumentName(
            JOB_INFO_2W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetDatatype(
            JOB_INFO_2W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetStatusString(
            JOB_INFO_2W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetStatus(
            JOB_INFO_2W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetPriority(
            JOB_INFO_2W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetPosition(
            JOB_INFO_2W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetTotalPages(
            JOB_INFO_2W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetPagesPrinted(
            JOB_INFO_2W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetTimeSubmitted(
            JOB_INFO_2W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetSecurityDescriptor(
            JOB_INFO_2W*        unmanagedPrinterInfo
            );

        static
        Object^
        GetNotifyName(
            JOB_INFO_2W* unmanagedJobInfo
            );

        static
        Object^
        GetQueueDriverName(
            JOB_INFO_2W* unmanagedJobInfo
            );

        static
        Object^
        GetPrintProcessor(
            JOB_INFO_2W* unmanagedJobInfo
            );

        static
        Object^
        GetPrintProcessorParameters(
            JOB_INFO_2W* unmanagedJobInfo
            );

        static
        Object^
        GetStartTime(
            JOB_INFO_2W* unmanagedJobInfo
            );

        static
        Object^
        GetUntilTime(
            JOB_INFO_2W* unmanagedJobInfo
            );

        static
        Object^
        GetTimeSinceSubmitted(
            JOB_INFO_2W* unmanagedJobInfo
            );

        static
        Object^
        GetSize(
            JOB_INFO_2W* unmanagedJobInfo
            );

        static
        Object^
        GetDevMode(
            JOB_INFO_2W* unmanagedJobInfo
            );

        delegate
        Object^
        GetValue(
            JOB_INFO_2W*        unmanagedPrinterInfo
            );


        static
        Hashtable^                  getAttributeMap;

        SafeMemoryHandle^           jobInfoTwoSafeHandle;

        bool                        isDisposed;
        UInt32                      jobsCount;

    };

}
}
}
}
#endif
