// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//------------------------------------------------------------------------------
//
//
//
//------------------------------------------------------------------------------

#pragma once 
#if !defined(_WPF_OS_VERSIONHELPERS_H_)
#define _WPF_OS_VERSIONHELPERS_H_

// Some useful macros
#pragma region BYTE, HIBYTE and LOBYTE Definitions

#if !defined(BYTE)
#define BYTE UCHAR
#endif

#if !defined(HIBYTE)
#define HIBYTE(w)       ((BYTE)((((DWORD_PTR)(w)) >> 8) & 0xFF))
#endif 

#if !defined(LOBYTE)
#define LOBYTE(w)       ((BYTE)(((DWORD_PTR)(w)) & 0xFF))
#endif 

#pragma endregion // BYTE, HIBYTE and LOBYTE Definitions

namespace WPFUtils
{
    class OSVersionHelper
    {
    private:

        static bool IsWindowsVersionOrGreater(ULONG wMajorVersion, ULONG wMinorVersion, USHORT wServicePackMajor, ULONG wBuildNumber = 0);

    public:

        static bool IsWindowsXPOrGreater();
        static bool IsWindowsXPSP1OrGreater();
        static bool IsWindowsXPSP2OrGreater();
        static bool IsWindowsXPSP3OrGreater();
        static bool IsWindowsVistaOrGreater();
        static bool IsWindowsVistaSP1OrGreater();
        static bool IsWindowsVistaSP2OrGreater();
        static bool IsWindows7OrGreater();
        static bool IsWindows7SP1OrGreater();
        static bool IsWindows8OrGreater();
        static bool IsWindows8Point1OrGreater();
        static bool IsWindows10OrGreater();
        static bool IsWindows10TH1OrGreater();
        static bool IsWindows10TH2OrGreater();
        static bool IsWindows10RS1OrGreater();
        static bool IsWindows10RS2OrGreater();
        static bool IsWindows10RS3OrGreater();
        static bool IsWindows10RS5OrGreater();
        static bool IsWindowsServer();
    };
}

//
// Watson reporting in PresentationHost_v0400.dll uses 
// OS version DWORD's. This is not ideal - we generally 
// do not wish to expose these version numbers directly. 
// Nevertheless, we use the helper class declared below
// to enable correct inference of OS version numbers 
// during watson reporting. 
//
// We should consider eliminating this class and all its 
// uses (there is only one use for it today 
// in host\shimimpl\watsonreporting.cxx) in the future.
//
namespace WatsonReportingHelper
{

    // Declare DWORD locally - we are prevented from including 
    // windows headers that define it globally because this 
    // header is intended for DDK based headers.
    //
    // Put this in a nested namespace so that code that imports
    // WatsonReportingHelper via a using declaration 
    // would not encounter name conflicts if they also 
    // happen to include headers that define DWORD globally.
    // 
    namespace HelperTypes
    {
        typedef unsigned long DWORD;
    }

    class OSVersion
    {

    private:
        HelperTypes::DWORD majorVersion;
        HelperTypes::DWORD minorVersion;
        HelperTypes::DWORD buildNumber;
        USHORT servicePackMajor;
        USHORT servicePackMinor;

        static OSVersion* singleton; 

    private:
        OSVersion();
        static void ensure_singleton();

    public:
        static HelperTypes::DWORD GetMajorVersion();
        static HelperTypes::DWORD GetMinorVersion();
        static HelperTypes::DWORD GetBuildNumber();
        static USHORT GetServicePackMajor();
        static USHORT GetServicePackMinor();
    };
}

#endif // _WPF_OS_VERSIONHELPERS_H_
