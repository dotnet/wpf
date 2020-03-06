// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

/*++
    Abstract:
        This file includes the implementation of the PrintDocumentImageableArea which
        represents the imageable dimensions used during printing.

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

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif


/*--------------------------------------------------------------------------------------*/
/*                     PrintDocumentImageableArea Implementation                        */
/*--------------------------------------------------------------------------------------*/

/*++
    Function Name:
        PrintDocumentImageableArea

    Description:
        Constructor of class instance

    Parameters:
        None
    
    Return Value
        None
--*/
PrintDocumentImageableArea::
PrintDocumentImageableArea(
    ) 
{
    _originWidth     = 0;          
    _originHeight    = 0;         
    _extentWidth     = 0;          
    _extentHeight    = 0;         
    _mediaSizeWidth  = 0;   
    _mediaSizeHeight = 0;  

    _accessVerifier = gcnew PrintSystemDispatcherObject();
}

double
PrintDocumentImageableArea::
OriginWidth::
get(
    void
    )
{
    VerifyAccess();

    return _originWidth;
}

void
PrintDocumentImageableArea::
OriginWidth::
set(
    double originWidth
    )
{
    VerifyAccess();

    _originWidth = originWidth;
}

double
PrintDocumentImageableArea::
OriginHeight::
get(
    void
    )
{
    VerifyAccess();

    return _originHeight;
}

void
PrintDocumentImageableArea::
OriginHeight::
set(
    double originHeight
    )
{
   VerifyAccess();

    _originHeight = originHeight;
}

double
PrintDocumentImageableArea::
ExtentWidth::
get(
    void
    )
{
   VerifyAccess();

    return _extentWidth;
}

void
PrintDocumentImageableArea::
ExtentWidth::
set(
    double extentWidth
    )
{
   VerifyAccess();

    _extentWidth = extentWidth;
}

double
PrintDocumentImageableArea::
ExtentHeight::
get(
    void
    )
{
   VerifyAccess();

    return _extentHeight;
}

void
PrintDocumentImageableArea::
ExtentHeight::
set(
    double extentHeight
    )
{
   VerifyAccess();

    _extentHeight = extentHeight;
}

double
PrintDocumentImageableArea::
MediaSizeWidth::
get(
    void
    )
{
   VerifyAccess();

    return _mediaSizeWidth;
}

void
PrintDocumentImageableArea::
MediaSizeWidth::
set(
    double mediaSizeWidth
    )
{
   VerifyAccess();

    _mediaSizeWidth = mediaSizeWidth;
}

double
PrintDocumentImageableArea::
MediaSizeHeight::
get(
    void
    )
{
   VerifyAccess();

    return _mediaSizeHeight;
}

void
PrintDocumentImageableArea::
MediaSizeHeight::
set(
    double mediaSizeHeight
    )
{
   VerifyAccess();

    _mediaSizeHeight = mediaSizeHeight;
}

void
PrintDocumentImageableArea::
VerifyAccess(
    void
    )
{
    if(_accessVerifier==nullptr)
    {
        _accessVerifier = gcnew PrintSystemDispatcherObject();
    }

    _accessVerifier->VerifyThreadLocality();
}



