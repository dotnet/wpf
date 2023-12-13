// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
    Abstract:
        This file implements PrintQueue::GetLegacyDevice
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

using namespace System::Printing::Interop;

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#ifndef  __GENERICTHUNKINGINC_HPP__
#include <GenericThunkingInc.hpp>
#endif

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif

#ifndef  __PRINTSYSTEMPATHRESOLVER_HPP__
#include <PrintSystemPathResolver.hpp>
#endif

using namespace System::Printing;
using namespace System::Printing::IndexedProperties;

#ifndef  __PRINTSYSTEMATTRIBUTEVALUEFACTORY_HPP__
#include <PrintSystemAttributeValueFactory.hpp>
#endif

#ifndef  __PRINTSYSTEMOBJECTFACTORY_HPP__
#include <PrintSystemObjectFactory.hpp>
#endif

using namespace System::Printing::Activation;

#ifndef  __GETDATATHUNKOBJECT_HPP__
#include <GetDataThunkObject.hpp>
#endif

#ifndef  __ENUMDATATHUNKOBJECT_HPP__
#include <EnumDataThunkObject.hpp>
#endif

#ifndef  __SETDATATHUNKOBJECT_HPP__
#include <SetDataThunkObject.hpp>
#endif

using namespace Microsoft::Internal;

using namespace System::Windows;
using namespace System::Windows::Media;

#define METRODEVICE

#include "..\inc\gdiexporter\precomp.hpp"


ILegacyDevice ^ PrintQueue::
GetLegacyDevice()
{
    return gcnew GDIExporter::CGDIRenderTarget();
}

unsigned PrintQueue::
GetDpiX(ILegacyDevice ^legacyDevice)
{
    unsigned dpiX = 96;

    if (GDIExporter::CGDIRenderTarget ^cGDIRenderTarget = dynamic_cast<GDIExporter::CGDIRenderTarget ^>(legacyDevice))
    {
        dpiX = cGDIRenderTarget->GetDpiX();
    }

    return dpiX;
}

unsigned PrintQueue::
GetDpiY(ILegacyDevice ^legacyDevice)
{
    unsigned dpiY = 96;

    if (GDIExporter::CGDIRenderTarget ^cGDIRenderTarget = dynamic_cast<GDIExporter::CGDIRenderTarget ^>(legacyDevice))
    {
        dpiY = cGDIRenderTarget->GetDpiY();
    }

    return dpiY;
}
