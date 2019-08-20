// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace MS.Win32.Penimc
{
    [
    ComImport,
    Guid("75C6AAEE-2BA4-4008-B523-4F1E033FF049"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
    ]
    interface IPimcContext3
    {
        void ShutdownComm();
        void GetPacketDescriptionInfo(out int cProps, out int cButtons);
        void GetPacketPropertyInfo(int iProp, out Guid guid, out int iMin, out int iMax, out int iUnits, out float flResolution);
        void GetPacketButtonInfo(int iButton, out Guid guid);
        void GetLastSystemEventData(out int evt, out int modifier, out int character, out int x, out int y, out int stylusMode, out int buttonState);
    }

    [
    ComImport,
    Guid("CEB1EF24-BB4E-498B-9DF7-12887ED0EB24"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
    ]
    interface IPimcTablet3
    {
        void GetKey(out Int32 key);
        void GetName([MarshalAs(UnmanagedType.LPWStr)] out string name);
        void GetPlugAndPlayId([MarshalAs(UnmanagedType.LPWStr)] out string plugAndPlayId);
        void GetTabletAndDisplaySize(out int tabletWidth, out int tabletHeight, out int displayWidth, out int displayHeight);
        void GetHardwareCaps(out int caps);
        void GetDeviceType(out int devType);
        void RefreshCursorInfo();
        void GetCursorCount(out int cCursors);
        void GetCursorInfo(int iCursor, [MarshalAs(UnmanagedType.LPWStr)] out string sName, out int id, [MarshalAs(UnmanagedType.Bool)] out bool fInverted);
        void GetCursorButtonCount(int iCursor, out int cButtons);
        void GetCursorButtonInfo (int iCursor, int iButton, [MarshalAs(UnmanagedType.LPWStr)] out string sName, out Guid guid);
        void IsPropertySupported(Guid guid, [MarshalAs(UnmanagedType.Bool)] out bool fSupported);
        void GetPropertyInfo(Guid guid, out int min, out int max, out int units, out float resolution);
        void CreateContext(IntPtr handle, [MarshalAs(UnmanagedType.Bool)] bool fEnable, uint timeout,
                                out IPimcContext3 IPimcContext, out Int32 key, out Int64 commHandle);
        void GetPacketDescriptionInfo(out int cProps, out int cButtons);
        void GetPacketPropertyInfo(int iProp, out Guid guid, out int iMin, out int iMax, out int iUnits, out float flResolution);
        void GetPacketButtonInfo(int iButton, out Guid guid);
    }

    [
    ComImport,
    Guid(PimcConstants.IPimcManager3IID),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
    ]
    interface IPimcManager3
    {
        void GetTabletCount(out UInt32 count);
        void GetTablet(UInt32 tablet, out IPimcTablet3 IPimcTablet);
    }

    internal static class PimcConstants
    {
        internal const string PimcManager3CLSID = "DB88ADFD-BEC7-47B8-A6B5-58CA3DA2B8D6";
        internal const string IPimcManager3IID = "BD2C38C2-E064-41D0-A999-940F526219C2";
    }
}

