// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++                                                                              
    Abstract:

        PrinterDefaults class is the managed class corresponding 
        to PRINTER_DEFAULTS Win32 structure. It has the same
        memory layout as PRINTER_DEFAULTS structure.
--*/

#include "win32inc.hpp"

#ifndef  __INTEROPNAMESPACEUSAGE_HPP__
#include <InteropNamespaceUsage.hpp>
#endif

#ifndef  __PRINTSYSTEMSECURITY_HPP__
#include <PrintSystemSecurity.hpp>
#endif

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif



using namespace MS::Internal;


    /*++

    Routine Name:   

        PrinterDefaults

    Routine Description:

        Constructor 

    Arguments:

        dataType      -   printing datatype. RAW by default
        devmode       -   managed devmode class used for initialization
        desiredAccess -   desired access for the printer
        
    Return Value:

        N\A

    --*/
    PrinterDefaults::
    PrinterDefaults(
        String^                       dataType,
        PrintWin32Thunk::DeviceMode^  devMode,
        PrintSystemDesiredAccess      desiredAccess
        ) : defaultDataType(dataType),
            defaultDeviceMode(NULL),
            defaultDesiredAccess(desiredAccess)
    {
        if (devMode && devMode->Data != nullptr)
        {
            defaultDeviceMode = Marshal::AllocHGlobal(devMode->Data->Length);

            Marshal::Copy((array<Byte>^)devMode->Data, 
                          0, 
                          defaultDeviceMode, 
                          devMode->Data->Length);        

        }
    }

    /*++

    Routine Name:   

        ~PrinterDefaults

    Routine Description:

        Destructor 

    Arguments:

        None
        
    Return Value:

        N\A

    --*/
    PrinterDefaults::
    ~PrinterDefaults(
        void
        )
    {
        InternalDispose(true);
    }

    /*++

    Routine Name:   

        !PrinterDefaults

    Routine Description:

        Finalizer

    Arguments:

        None
        
    Return Value:

        N\A

    --*/
    PrinterDefaults::
    !PrinterDefaults(
        void
        )
    {
        InternalDispose(false);
    }

    void
    PrinterDefaults::
    InternalDispose(
        bool disposing
        )
    {
        if (defaultDeviceMode != IntPtr::Zero)
        {
            Marshal::FreeHGlobal(defaultDeviceMode);
            defaultDeviceMode = IntPtr::Zero;
        }
    }

    /*++

    Routine Name:   

        get_DesiredAccess

    Routine Description:

        property     

    Arguments:

        None
        
    Return Value:

        N\A

    --*/
    PrintSystemDesiredAccess
    PrinterDefaults::DesiredAccess::
    get(
        void
        )
    {
        return defaultDesiredAccess;
    }
