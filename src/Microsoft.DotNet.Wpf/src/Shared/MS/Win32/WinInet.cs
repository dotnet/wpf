// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Runtime.InteropServices;

namespace MS.Win32
{
internal static class WinInet
{
    /// <summary>
    /// Will return the location of the internet cache folder.
    /// </summary>
    /// <returns>The location of the internet cache folder.</returns>
    internal static Uri InternetCacheFolder
    {
        get
        {
            // copied value 260 from orginal implementation in BitmapDownload.cs 
            const int maxPathSize = 260;
            const UInt32 fieldControl = (UInt32)maxPathSize;

            NativeMethods.InternetCacheConfigInfo icci =
                new NativeMethods.InternetCacheConfigInfo();

            icci.CachePath = new string(new char[maxPathSize]);

            UInt32 size = (UInt32)Marshal.SizeOf(icci);
            icci.dwStructSize = size;
            
            bool passed = UnsafeNativeMethods.GetUrlCacheConfigInfo(
                ref icci,
                ref size,
                fieldControl);

            if (!passed)
            {
                int hr = Marshal.GetHRForLastWin32Error();

                if (hr != 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }

            return new Uri(icci.CachePath);
        }
    }
}
}
