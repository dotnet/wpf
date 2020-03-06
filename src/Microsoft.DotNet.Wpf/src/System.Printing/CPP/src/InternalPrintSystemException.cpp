// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++                                                                         
    Abstract:

        Print System exception objects declaration.
--*/
#include "win32inc.hpp"

#ifndef  __INTEROPNAMESPACEUSAGE_HPP__
#include <InteropNamespaceUsage.hpp>
#endif


#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

using namespace MS::Internal::PrintWin32Thunk::Win32ApiThunk;
using namespace System;
using namespace System::Text;
using namespace System::Collections;
using namespace System::Runtime::Serialization;
using namespace System::Resources;

#ifndef  __INTERNALPRINTSYSTEMEXCEPTION_HPP__
#include <InternalPrintSystemException.hpp>
#endif

using namespace System::Printing;
using namespace System::Printing::IndexedProperties;

namespace System
{
namespace Printing
{

/*++

Routine Name:   

    InternalPrintSystemException

Routine Description:

    Constructor

Arguments:
    
    lastWin32Error - last Win32 error 
    
Return Value:

    N/A

--*/
InternalPrintSystemException::
InternalPrintSystemException(
    int lastWin32Error
    ) : hresult(HRESULT_FROM_WIN32(lastWin32Error))
{    
}

/*++

Routine Name:   

    get_HResult

Routine Description:

    property

Arguments:
    
    none

Return Value:

    HRESULT associated with error code

--*/
int
InternalPrintSystemException::HResult::
get(
    void
    )
{
    return hresult;
}


/*++

Routine Name:   

    ThrowIfLastErrorIsNot

Routine Description:

    Utility method that throws a InternalPrintSystemException
    object if the last Win32 error is different than the expected
    value.

Arguments:
    
    expectedLastWin32Error - expect Win32 error

Return Value:

    void

--*/
void
InternalPrintSystemException::
ThrowIfLastErrorIsNot(
    int expectedLastWin32Error
    )
{
    int lastWin32Error = Marshal::GetLastWin32Error();

    if (lastWin32Error != expectedLastWin32Error)
    {
        throw gcnew InternalPrintSystemException(lastWin32Error);
    }
}

/*++

Routine Name:   

    ThrowIfErrorIsNot

Routine Description:

    Utility method that throws a InternalPrintSystemException
    object if the last Win32 error is different than the expected
    value. The last Win32 error is passed in as an input parameter.

Arguments:
    
    expectedLastWin32Error - expect Win32 error

Return Value:

    void

--*/
void
InternalPrintSystemException::
ThrowIfErrorIsNot(
    int lastWin32Error,
    int expectedLastWin32Error
    )
{
    if (lastWin32Error != expectedLastWin32Error)
    {
        throw gcnew InternalPrintSystemException(lastWin32Error);
    }
}

void
/*++

Routine Name:   

    ThrowLastError

Routine Description:

    Utility method that throws a InternalPrintSystemException
    object that packs the last Win32 error.    

Arguments:
    
    expectedLastWin32Error - expect Win32 error

Return Value:

    void

--*/
InternalPrintSystemException::
ThrowLastError(
    void
    )
{
    throw gcnew InternalPrintSystemException(Marshal::GetLastWin32Error());
}

/*++

Routine Name:   

    ThrowIfNotSuccess

Routine Description:

    Utility method that throws a InternalPrintSystemException
    object if the last Win32 error is not success.    

Arguments:
    
    lastWin32Error - last Win32 error

Return Value:

    void

--*/
void
InternalPrintSystemException::
ThrowIfNotSuccess(
    int lastWin32Error
    )
{
    if (lastWin32Error != ERROR_SUCCESS)
    {
        throw gcnew InternalPrintSystemException(lastWin32Error);
    }
}


/*++

Routine Name:   

    ThrowIfNotCOMSuccess

Routine Description:

    Utility method that throws a InternalPrintSystemException
    object if the hresultCode argument is not a COM success code.

Arguments:
    
    hresultCode - COM result

Return Value:

    void

--*/
void
InternalPrintSystemException::
ThrowIfNotCOMSuccess(
    HRESULT  hresultCode
    )
{
    if(!SUCCEEDED(hresultCode))
    {
        InternalPrintSystemException^ exception = gcnew InternalPrintSystemException(0);
        exception->hresult = hresultCode;
        throw exception;
    }
}

}
}



