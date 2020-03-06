// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __INTEROPPRINTERDEFAULTS_HPP__
#define __INTEROPPRINTERDEFAULTS_HPP__
/*++
    Abstract:

        This file contains the definition of the managed class corresponding 
        to PRINTER_DEFAULTS Win32 structure. PrinterDefaults class has the same
        memory layout as PRINTER_DEFAULTS structure.
--*/
namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
    [StructLayout(LayoutKind::Sequential, CharSet=CharSet::Unicode)]
    private ref class  PrinterDefaults : 
	public IDisposable
    { 
        public:

        PrinterDefaults(
            String^                                     dataType,
            DeviceMode^                                 devMode,
            System::Printing::PrintSystemDesiredAccess  desiredAccess
            );

        ~PrinterDefaults(
            void
            );

        property
        System::Printing::PrintSystemDesiredAccess
        DesiredAccess
        {
            System::Printing::PrintSystemDesiredAccess get();
        }

        protected:

        !PrinterDefaults(
            void
            );

        virtual
        void
        InternalDispose(
            bool    disposing
            );
        
        private:

        [MarshalAs(UnmanagedType::LPWStr)]
        String^             defaultDataType;

        [MarshalAs(UnmanagedType::SysInt)]
        IntPtr              defaultDeviceMode; 

        [MarshalAs(UnmanagedType::U4)]
        System::Printing::PrintSystemDesiredAccess defaultDesiredAccess; 
    };
}
}
}
#endif
