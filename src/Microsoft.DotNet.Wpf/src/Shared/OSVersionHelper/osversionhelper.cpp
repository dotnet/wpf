// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "wpfntddk.h"
#include "wpfsdkddkver.h"
#include "osversionhelper.h"

// Adding a new OS
//  See <versionhelpers.h> in the Windows SDK and re-use the appropriate code from there.
//  See OperatingSystemVersion.cs for instructions on porting to managed checks


bool WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(ULONG wMajorVersion, ULONG wMinorVersion, USHORT wServicePackMajor, ULONG wBuildNumber)
{
    RTL_OSVERSIONINFOEXW osvi =
    {
        sizeof(osvi),       // dwOSVersionInfoSize
        wMajorVersion,      // dwMajorVersion
        wMinorVersion,      // dwMinorVersion 
        wBuildNumber,       // dwBuildNumber
        0,                  // dwPlatformId
        { 0 },              // szCSDVersion[128]
        wServicePackMajor,  // wServicePackMajor
        0,                  // wServicePackMinor
        0,                  // wSuiteMask
        0,                  // wProductType
        0                   // wReserved
    };

    ULONGLONG dwlConditionMask = 0;
    VER_SET_CONDITION(dwlConditionMask, VER_MAJORVERSION, VER_GREATER_EQUAL);
    VER_SET_CONDITION(dwlConditionMask, VER_MINORVERSION, VER_GREATER_EQUAL);
    VER_SET_CONDITION(dwlConditionMask, VER_SERVICEPACKMAJOR, VER_GREATER_EQUAL);

    auto dwFlags = VER_MAJORVERSION | VER_MINORVERSION | VER_SERVICEPACKMAJOR;

    if (wBuildNumber > 0)
    {
        VER_SET_CONDITION(dwlConditionMask, VER_BUILDNUMBER, VER_GREATER_EQUAL);
        dwFlags |= VER_BUILDNUMBER;
    }

    return RtlVerifyVersionInfo(&osvi, dwFlags, dwlConditionMask) == STATUS_SUCCESS;
}

bool WPFUtils::OSVersionHelper::IsWindowsXPOrGreater()
{
    return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WINXP), LOBYTE(_WIN32_WINNT_WINXP), 0);
}

bool WPFUtils::OSVersionHelper::IsWindowsXPSP1OrGreater()
{
    return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WINXP), LOBYTE(_WIN32_WINNT_WINXP), 1);
}

bool WPFUtils::OSVersionHelper::IsWindowsXPSP2OrGreater()
{
    return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WINXP), LOBYTE(_WIN32_WINNT_WINXP), 2);
}

bool WPFUtils::OSVersionHelper::IsWindowsXPSP3OrGreater()
{
    return IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WINXP), LOBYTE(_WIN32_WINNT_WINXP), 3);
}

bool WPFUtils::OSVersionHelper::IsWindowsVistaOrGreater()
{
    return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_VISTA), LOBYTE(_WIN32_WINNT_VISTA), 0);
}

bool WPFUtils::OSVersionHelper::IsWindowsVistaSP1OrGreater()
{
    return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_VISTA), LOBYTE(_WIN32_WINNT_VISTA), 1);
}

bool WPFUtils::OSVersionHelper::IsWindowsVistaSP2OrGreater()
{
    return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_VISTA), LOBYTE(_WIN32_WINNT_VISTA), 2);
}

bool WPFUtils::OSVersionHelper::IsWindows7OrGreater()
{
    return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WIN7), LOBYTE(_WIN32_WINNT_WIN7), 0);
}

bool WPFUtils::OSVersionHelper::IsWindows7SP1OrGreater()
{
    return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WIN7), LOBYTE(_WIN32_WINNT_WIN7), 1);
}

bool WPFUtils::OSVersionHelper::IsWindows8OrGreater()
{
    return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WIN8), LOBYTE(_WIN32_WINNT_WIN8), 0);
}

bool WPFUtils::OSVersionHelper::IsWindows8Point1OrGreater()
{
    return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WINBLUE), LOBYTE(_WIN32_WINNT_WINBLUE), 0);
}

bool WPFUtils::OSVersionHelper::IsWindows10OrGreater()
{
    return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WIN10), LOBYTE(_WIN32_WINNT_WIN10), 0);
}

bool WPFUtils::OSVersionHelper::IsWindows10TH1OrGreater()
{
    return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WIN10), LOBYTE(_WIN32_WINNT_WIN10), 0, _TH1_BUILD_NUMBER);
}

bool WPFUtils::OSVersionHelper::IsWindows10TH2OrGreater()
{
    return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WIN10), LOBYTE(_WIN32_WINNT_WIN10), 0, _TH2_BUILD_NUMBER);
}

bool WPFUtils::OSVersionHelper::IsWindows10RS1OrGreater()
{
    return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WIN10), LOBYTE(_WIN32_WINNT_WIN10), 0, _RS1_BUILD_NUMBER);
}

bool WPFUtils::OSVersionHelper::IsWindows10RS2OrGreater()
{
    return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WIN10), LOBYTE(_WIN32_WINNT_WIN10), 0, _RS2_BUILD_NUMBER);
}

bool WPFUtils::OSVersionHelper::IsWindows10RS3OrGreater()
{
    return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WIN10), LOBYTE(_WIN32_WINNT_WIN10), 0, _RS3_BUILD_NUMBER);
}

bool WPFUtils::OSVersionHelper::IsWindows10RS5OrGreater()
{
    return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WIN10), LOBYTE(_WIN32_WINNT_WIN10), 0, _RS5_BUILD_NUMBER);
}

// This function is a template for checking the version for an unreleased version of Windows via a strictly greater than comparison from the previous known build.
//bool WPFUtils::OSVersionHelper::IsWindows1019H1OrGreater()
//{
//    //
//    // 19H1 will have a build number strictly greater than _RS5_BUILD_NUMBER. 
//    // Once the precise build number for 19H1 is known at/after RTM, this method should be updated to 
//    // something like this:
//    //
//    // return WPFUtils::OSVersionHelper::IsWindowsVersionOrGreater(HIBYTE(_WIN32_WINNT_WIN10), LOBYTE(_WIN32_WINNT_WIN10), 0, _19H1_BUILD_NUMBER);
//    // 
//
//    ULONG wMajorVersion = HIBYTE(_WIN32_WINNT_WIN10);
//    ULONG wMinorVersion = LOBYTE(_WIN32_WINNT_WIN10);
//    USHORT wServicePackMajor = 0;
//    ULONG wBuildNumber = _RS5_BUILD_NUMBER;
//
//    RTL_OSVERSIONINFOEXW osvi =
//    {
//        sizeof(osvi),       // dwOSVersionInfoSize
//        wMajorVersion,      // dwMajorVersion
//        wMinorVersion,      // dwMinorVersion 
//        wBuildNumber,       // dwBuildNumber
//        0,                  // dwPlatformId
//        { 0 },              // szCSDVersion[128]
//        wServicePackMajor,  // wServicePackMajor
//        0,                  // wServicePackMinor
//        0,                  // wSuiteMask
//        0,                  // wProductType
//        0                   // wReserved
//    };
//
//    ULONGLONG dwlConditionMask = 0;
//    VER_SET_CONDITION(dwlConditionMask, VER_MAJORVERSION, VER_GREATER_EQUAL);
//    VER_SET_CONDITION(dwlConditionMask, VER_MINORVERSION, VER_GREATER_EQUAL);
//    VER_SET_CONDITION(dwlConditionMask, VER_SERVICEPACKMAJOR, VER_GREATER_EQUAL);
//    VER_SET_CONDITION(dwlConditionMask, VER_BUILDNUMBER, VER_GREATER);
//
//    return
//        STATUS_SUCCESS == RtlVerifyVersionInfo(
//            &osvi,
//            VER_MAJORVERSION | VER_MINORVERSION | VER_SERVICEPACKMAJOR | VER_BUILDNUMBER,
//            dwlConditionMask);
//}

bool WPFUtils::OSVersionHelper::IsWindowsServer()
{
    // We'd like to test for any version that does NOT match VER_NT_WORKSTATION
    RTL_OSVERSIONINFOEXW osvi =
    {
        sizeof(osvi),       // dwOSVersionInfoSize
        0,                  // dwMajorVersion
        0,                  // dwMinorVersion 
        0,                  // dwBuildNumber
        0,                  // dwPlatformId
        { 0 },              // szCSDVersion[128]
        0,                  // wServicePackMajor
        0,                  // wServicePackMinor
        0,                  // wSuiteMask
        VER_NT_WORKSTATION, // wProductType
        0                   // wReserved
    };

    ULONGLONG dwlConditionMask = 0;
    VER_SET_CONDITION(dwlConditionMask, VER_PRODUCT_TYPE, VER_EQUAL);

    return RtlVerifyVersionInfo(&osvi, VER_PRODUCT_TYPE, dwlConditionMask) != STATUS_SUCCESS;
}

