// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//------------------------------------------------------------------------------
//

//
//------------------------------------------------------------------------------

#include "wpfntddk.h"
#include "wpfsdkddkver.h"
#include "osversionhelper.h"

using namespace WatsonReportingHelper; 
using namespace WatsonReportingHelper::HelperTypes;

OSVersion* OSVersion::singleton = new OSVersion();

OSVersion::OSVersion()
{
    RTL_OSVERSIONINFOEXW osvi = {0};
    osvi.dwOSVersionInfoSize = sizeof(osvi);

    if (RtlGetVersion(reinterpret_cast<PRTL_OSVERSIONINFOW>(&osvi)) == STATUS_SUCCESS)
    {
        majorVersion = osvi.dwMajorVersion;
        minorVersion = osvi.dwMinorVersion;
        buildNumber = osvi.dwBuildNumber;
        servicePackMajor = osvi.wServicePackMajor;
        servicePackMinor = osvi.wServicePackMinor;
    }
}

DWORD OSVersion::GetMajorVersion()
{
    return singleton->majorVersion;
}

DWORD OSVersion::GetMinorVersion()
{
    return singleton->minorVersion;
}

DWORD OSVersion::GetBuildNumber()
{
    return singleton->buildNumber;
}

USHORT OSVersion::GetServicePackMajor()
{
    return singleton->servicePackMajor;
}

USHORT OSVersion::GetServicePackMinor()
{
    return singleton->servicePackMinor;
}


