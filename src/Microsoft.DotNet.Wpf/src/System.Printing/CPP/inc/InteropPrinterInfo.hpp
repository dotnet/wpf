// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __INTEROPPRINTERINFO_HPP__
#define __INTEROPPRINTERINFO_HPP__
/*++

    Abstract:

        The file contains the definition for the managed classes that 
        hold the pointers to the PRINTER_INFO_ unmanaged structures and know how 
        to retrieve a property based on it's name. 
--*/
namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
    ref class PrinterThunkHandler;
    ref class SafeMemoryHandle;

namespace DirectInteropForPrintQueue
{    
    using namespace System::Security;
    using namespace System::Drawing::Printing;

    private ref class PrinterInfoOne : public IPrinterInfo
    {
        public:

        PrinterInfoOne(
            SafeMemoryHandle^   unmanagedPrinterInfo,
            UInt32              count
            );

        PrinterInfoOne(
            void
            );

        virtual
        void
        Release(
            void
            );
        
        property
        UInt32
        Count
        {
            UInt32 virtual get();
        }

        property
        SafeMemoryHandle^
        Win32SafeHandle
        {
            virtual SafeMemoryHandle^ get();
        }

        Object^
        GetValueFromName(
            String^             valueName
            );

        virtual Object^
        GetValueFromName(
            String^             valueName,
            UInt32              index
            );

        virtual bool
        SetValueFromName(
            String^             valueName,
            Object^             value
            );

        private:

        // FIX: remove pragma. done to fix compiler error which will be fixed later.
        #pragma warning ( disable:4567 )
        static
        PrinterInfoOne(
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
        GetComment(
            PRINTER_INFO_1W*    unmanagedPrinterInfo
            );

        static
        Object^
        GetDescription(
            PRINTER_INFO_1W*    unmanagedPrinterInfo
            );

        static
        Object^
        GetFlags(
            PRINTER_INFO_1W*    unmanagedPrinterInfo
            );
        
        delegate
        Object^
        GetValue(
            PRINTER_INFO_1W*    unmanagedPrinterInfo
            );

        static
        Hashtable^              getAttributeMap;

        SafeMemoryHandle^       printerInfoOneSafeHandle;

        UInt32                  printersCount;
    };


    private ref class PrinterInfoTwoGetter : public IPrinterInfo
    {
        public:

        PrinterInfoTwoGetter(
            SafeMemoryHandle^   unmanagedPrinterInfo,
            UInt32              count
            );

        virtual
        void
        Release(
            void
            );

        property
        UInt32
        Count
        {
            virtual UInt32 get();
        }
        
        property
        SafeMemoryHandle^
        Win32SafeHandle
        {
            virtual SafeMemoryHandle^ get();
        }
        
        Object^
        GetValueFromName(
            String^             valueName
            );

        virtual Object^
        GetValueFromName(
            String^             valueName,
            UInt32              index
            );

        virtual bool
        SetValueFromName(
            String^             valueName,
            Object^             value
            );

        private:

        static
        PrinterInfoTwoGetter(
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
        GetServerName(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static        
        Object^
        GetPrinterName(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static 
        Object^
        GetShareName(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static 
        Object^
        GetPortName(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static 
        Object^
        GetDriverName(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static
        Object^
        GetComment(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static
        Object^
        GetLocation(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static
        Object^
        GetDeviceMode(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );
        
        static
        Object^
        GetSeparatorFile(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static
        Object^ 
        GetPrintProcessor(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static
        Object^
        GetPrintProcessorDatatype(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static
        Object^ 
        GetPrintProcessorParameters(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static
        Object^
        GetSecurityDescriptor(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static
        Object^
        GetAttributes(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static
        Object^
        GetPriority(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static
        Object^
        GetDefaultPriority(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static
        Object^
        GetStartTime(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static
        Object^
        GetUntilTime(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );
        
        static
        Object^
        GetStatus(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static
        Object^
        GetAveragePPM(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );

        static
        Object^
        GetJobs(
            PRINTER_INFO_2W*    unmanagedPrinterInfo
            );
        
        private:

        delegate
        Object^
        GetValue(
            PRINTER_INFO_2W*    unmanagedPrinterInfo    
            );

        static
        Hashtable^              getAttributeMap;

        SafeMemoryHandle^       printerInfoTwoSafeHandle;

        UInt32                  printersCount;        
    };


    private ref class PrinterInfoTwoSetter sealed : public IPrinterInfo
    {
        public:

        PrinterInfoTwoSetter(
            void
            );

        PrinterInfoTwoSetter(
            PrinterThunkHandler^    printerHandler
            );

        virtual
        void
        Release(
            void
            );

        property
        UInt32
        Count
        {
            virtual UInt32 get();
        }
        
        property
        SafeMemoryHandle^
        Win32SafeHandle
        {
            virtual SafeMemoryHandle^ get();
        }
        
        virtual Object^
        GetValueFromName(
            String^                 valueName,
            UInt32                  index
            );

        virtual bool
        SetValueFromName(
            String^                 valueName,
            Object^                 value
            );

        private:

        static
        void
        RegisterAttributeMaps(
            void
            );
        
        static
        IntPtr
        SetServerName(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );

        static
        IntPtr
        SetPrinterName(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );

        static
        IntPtr
        SetShareName(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );

        static
        IntPtr
        SetPortName(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );

        static
        IntPtr
        SetDriverName(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );

        static
        IntPtr
        SetComment(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );
        
        static
        IntPtr
        SetLocation(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );

        static
        IntPtr
        SetSeparatorFile(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );

        static
        IntPtr
        SetPrintProcessor(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );

        static
        IntPtr
        SetPrintProcessorDatatype(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );

        static
        IntPtr
        SetPrintProcessorParameters(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );
        
        static
        IntPtr
        SetSecurityDescriptor(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );

        static
        IntPtr
        SetAttributes(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );

        static
        IntPtr
        SetPriority(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );

        static
        IntPtr
        SetDefaultPriority(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );

        static
        IntPtr
        SetStartTime(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );
        
        static
        IntPtr
        SetUntilTime(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );

        static
        IntPtr
        SetStatus(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );
        
        static
        IntPtr
        SetAveragePPM(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );

        static
        IntPtr
        SetJobs(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );
                
        private:

        static
        PrinterInfoTwoSetter(
            void
            )
        {
            setAttributeMap = gcnew Hashtable();

            RegisterAttributeMaps();
        }

        delegate
        IntPtr
        SetValue(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );
        
        static
        Hashtable^                  setAttributeMap;

        SafeMemoryHandle^           win32PrinterInfoSafeHandle;

        array<SafeMemoryHandle^>^   internalMembersList;
        int                         internalMembersIndex;
    };

    private ref class PrinterInfoThree sealed : public IPrinterInfo
    {
        public :

        PrinterInfoThree(
            SafeMemoryHandle^       unmanagedPrinterInfo,
            UInt32                  count
            );

        PrinterInfoThree(
            void
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

        property
        UInt32
        Count
        {
            virtual UInt32 get();
        }

        Object^
        GetValueFromName(
            String^                 valueName
            );

        virtual Object^
        GetValueFromName(
            String^                 valueName,
            UInt32                  index
            );

        virtual bool
        SetValueFromName(
            String^                 valueName,
            Object^                 value
            );

        private:

        SafeMemoryHandle^           printerInfoThreeSafeHandle;

        UInt32                      printersCount;
    };

    private ref class PrinterInfoFourGetter sealed : public IPrinterInfo
    {
        public:

        PrinterInfoFourGetter(
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

        property
        UInt32
        Count
        {
            virtual UInt32 get();
        }
        
        Object^
        GetValueFromName(
            String^                 valueName
            );

        virtual Object^
        GetValueFromName(
            String^                 valueName,
            UInt32                  index
            );

        virtual bool
        SetValueFromName(
            String^                 valueName,
            Object^                 value
            );

        private:

        static
        PrinterInfoFourGetter(
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
        GetAttributes(
            PRINTER_INFO_4W*        unmanagedPrinterInfo
            );

        static
        Object^
        GetServerName(
            PRINTER_INFO_4W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetPrinterName(
            PRINTER_INFO_4W*        unmanagedPrinterInfo
            );

        delegate
        Object^
        GetValue(
            PRINTER_INFO_4W*        unmanagedPrinterInfo
            );

        static
        Hashtable^                  getAttributeMap;

        SafeMemoryHandle^           printerInfoFourSafeHandle;

        UInt32                      printersCount;

    };

    private ref class PrinterInfoFourSetter sealed : public IPrinterInfo
    {
        public:

        PrinterInfoFourSetter(
            PrinterThunkHandler^        printerThunkHandle 
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

        property
        UInt32
        Count
        {
            virtual UInt32 get();
        }
        
        virtual Object^
        GetValueFromName(
            String^                     valueName,
            UInt32                      index
            );

        virtual bool
        SetValueFromName(
            String^                     valueName,
            Object^                     value
            );

        private:

        static
        PrinterInfoFourSetter(
            void
            )
        {
            setAttributeMap = gcnew Hashtable();

            RegisterAttributeMaps();
        }

        static
        void
        RegisterAttributeMaps(
            void
            );

        delegate
        IntPtr
        SetValue(
            IntPtr                      valueName,
            Object^                     value
            );

        static
        IntPtr
        SetServerName(
            IntPtr                      valueName,
            Object^                     value
            );

        static
        IntPtr
        SetPrinterName(
            IntPtr                      printerInfoTwoBuffer,
            Object^                     value
            );

        static
        IntPtr
        SetAttributes(
            IntPtr                      printerInfoTwoBuffer,
            Object^                     value
            );

        static
        Hashtable^                      setAttributeMap;
        IPrinterInfo^                   printerInfo;
        
        array<SafeMemoryHandle^>^       internalMembersList;
        int                             internalMembersIndex;
    };

    private ref class PrinterInfoFiveGetter sealed : public IPrinterInfo
    {
        public:

        PrinterInfoFiveGetter(
            SafeMemoryHandle^       unmanagedPrinterInfo,
            UInt32                  count
            );

        virtual
        void
        Release(
            void
            );

        property
        UInt32
        Count
        {
            virtual UInt32 get();
        }

        property
        SafeMemoryHandle^
        Win32SafeHandle
        {
            virtual SafeMemoryHandle^ get();
        }

        virtual
        Object^
        GetValueFromName(
            String^                 valueName,
            UInt32                  index
            );

        virtual bool
        SetValueFromName(
            String^                 valueName,
            Object^                 value
            );

        private:

        static
        PrinterInfoFiveGetter(
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
        GetAttributes(
            PRINTER_INFO_5W*        unmanagedPrinterInfo
            );

        static
        Object^
        GetPortName(
            PRINTER_INFO_5W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetPrinterName(
            PRINTER_INFO_5W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetDeviceNotSelectedTimeout(
            PRINTER_INFO_5W*        unmanagedPrinterInfo
            );

        static        
        Object^
        GetTransmissionRetryTimeout(
            PRINTER_INFO_5W*        unmanagedPrinterInfo
            );

        delegate
        Object^
        GetValue(
            PRINTER_INFO_5W*        unmanagedPrinterInfo
            );
       
        static
        Hashtable^                  getAttributeMap;

        SafeMemoryHandle^           printerInfoFiveSafeHandle;

        UInt32                      printersCount;
    };

    private ref class PrinterInfoFiveSetter sealed  : public IPrinterInfo
    {
        public:

        PrinterInfoFiveSetter(
            PrinterThunkHandler^    printThunkHandle
            );

        virtual
        void
        Release(
            void
            );

        property
        UInt32
        Count
        {
            virtual UInt32 get();
        }

        property
        SafeMemoryHandle^
        Win32SafeHandle
        {
            virtual SafeMemoryHandle^ get();
        }

        virtual Object^
        GetValueFromName(
            String^                 valueName,
            UInt32                  index
            );

        virtual bool
        SetValueFromName(
            String^                 valueName,
            Object^                 value
            );

        private:

        static
        PrinterInfoFiveSetter(
            void
            )
        {
            setAttributeMap = gcnew Hashtable();

            RegisterAttributeMaps();
        }

        static
        void
        RegisterAttributeMaps(
            void
            );

        delegate
        IntPtr
        SetValue(
            IntPtr                  unmanagedValue,
            Object^                 value
            );

        static
        IntPtr
        SetPrinterName(
            IntPtr                  valueName,
            Object^                 value
            );

        static
        IntPtr
        SetPortName(
            IntPtr                  valueName,
            Object^                 value
            );

        static
        IntPtr
        SetAttributes(
            IntPtr                  valueName,
            Object^                 value
            );

        static
        IntPtr
        SetTransmissionRetryTimeout(
            IntPtr                  valueName,
            Object^                 value
            );

        static
        IntPtr
        SetDeviceNotSelectedTimeout(
            IntPtr                  printerInfoTwoBuffer,
            Object^                 value
            );

        static
        Hashtable^                  setAttributeMap;
        IPrinterInfo^               printerInfo;
        
        array<SafeMemoryHandle^>^   internalMembersList;
        int                         internalMembersIndex;
    };

    private ref class PrinterInfoSix sealed : public IPrinterInfo
    {
        public:

        PrinterInfoSix(
            SafeMemoryHandle^           unmanagedPrinterInfo,
            UInt32                      count
            );

        PrinterInfoSix(
            void
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
            String^                     valueName
            );

        virtual Object^
        GetValueFromName(
            String^                     valueName,
            UInt32                      index
            );

        virtual bool
        SetValueFromName(
            String^                     valueName,
            Object^                     value
            );

        property
        UInt32
        Count
        {
            virtual UInt32 get();
        }

        private:

        SafeMemoryHandle^               printerInfoSixSafeHandle;

        UInt32                          printersCount;
    };

    private ref class PrinterInfoSeven sealed : public IPrinterInfo
    {
        public:

        PrinterInfoSeven(
            SafeMemoryHandle^       unmanagedPrinterInfo,
            UInt32                  count
            );

        PrinterInfoSeven(
            void
            );

        virtual
        void
        Release(
            void
            );

        property
        UInt32
        Count
        {
            virtual UInt32 get();
        }

        property
        SafeMemoryHandle^
        Win32SafeHandle
        {
            virtual SafeMemoryHandle^ get();
        }

        virtual
        Object^
        GetValueFromName(
            String^                 valueName
            );

        virtual Object^
        GetValueFromName(
            String^                 valueName,
            UInt32                  index
            );

        virtual bool
        SetValueFromName(
            String^                 valueName,
            Object^                 value
            );

        private:

        static
        PrinterInfoSeven(
            void
            )
        {
            getAttributeMap = gcnew Hashtable();
            setAttributeMap = gcnew Hashtable();

            RegisterAttributeMaps();            
        }

        static
        void
        RegisterAttributeMaps(
            void
            );

        static
        Object^
        GetObjectGUID(
            PRINTER_INFO_7W*        unmanagedPrinterInfo
            );

        static
        Object^
        GetAction(
            PRINTER_INFO_7W*        unmanagedPrinterInfo
            );

        delegate
        Object^
        GetValue(
            PRINTER_INFO_7W*        unmanagedPrinterInfo
            );
       
        delegate
        bool
        SetValue(
            IntPtr                  printerInfoSevenBuffer,
            Object^                 value
            );

        static
        bool
        SetObjectGUID(
            IntPtr                  printerInfoSevenBuffer,
            Object^                 value
            );

        static
        bool
        SetAction(
            IntPtr                  printerInfoSevenBuffer,
            Object^                 value
            );
        

        static
        Hashtable^                  getAttributeMap;

        static
        Hashtable^                  setAttributeMap;

        SafeMemoryHandle^           printerInfoSevenSafeHandle;

        bool                        objectOwnsInternalUnmanagedMembers;
        UInt32                      printersCount;

    };

    private ref class PrinterInfoEight sealed : public IPrinterInfo
    {
        public:

        PrinterInfoEight(
            SafeMemoryHandle^   unmanagedPrinterInfo,
            UInt32              count
            );

        PrinterInfoEight(
            void
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

        property
        UInt32
        Count
        {
            virtual UInt32 get();
        }

        virtual
        Object^
        GetValueFromName(
            String^                 valueName
            );

        virtual
        Object^
        GetValueFromName(
            String^                 valueName,
            UInt32                  index
            );

        virtual bool
        SetValueFromName(
            String^                 valueName,
            Object^                 value
            );

        private:

        SafeMemoryHandle^           printerInfoEightSafeHandle;

        bool                        objectOwnsInternalUnmanagedMembers;
        UInt32                      printersCount;

    };

    private ref class PrinterInfoNine sealed : public IPrinterInfo
    {
        public:

        PrinterInfoNine(
            SafeMemoryHandle^       unmanagedPrinterInfo,
            UInt32                  count
            );

        PrinterInfoNine(
            void
            );

        virtual
        void
        Release(
            void
            );

        property
        UInt32
        Count
        {
            virtual UInt32 get();
        }

        property
        SafeMemoryHandle^
        Win32SafeHandle
        {
            virtual SafeMemoryHandle^ get();
        }

        Object^
        GetValueFromName(
            String^                 valueName
            );

        virtual Object^
        GetValueFromName(
            String^                 valueName,
            UInt32                  index
            );

        virtual bool
        SetValueFromName(
            String^                 valueName,
            Object^                 value
            );

        private:

        SafeMemoryHandle^           printerInfoNineSafeHandle;

        bool                        objectOwnsInternalUnmanagedMembers;
        UInt32                      printersCount;

    };
}
}
}
}
#endif  
