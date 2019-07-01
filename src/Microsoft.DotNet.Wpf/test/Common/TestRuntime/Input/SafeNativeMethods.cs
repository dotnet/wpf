// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Safe P/Invokes used by UIAutomation

using System.Runtime.InteropServices;
using System;
using System.Security;
using System.Security.Permissions;
using System.Collections;
using System.IO;
using System.Text;

namespace Microsoft.Test.Input.Win32
{
    // This class *MUST* be internal for security purposes
    //CASRemoval:[SuppressUnmanagedCodeSecurity]
    internal class SafeNativeMethods
    {
        #region Public Members

        public const int SM_CXMAXTRACK = 59;
        public const int SM_CYMAXTRACK = 60;
        public const int SM_XVIRTUALSCREEN = 76;
        public const int SM_YVIRTUALSCREEN = 77;
        public const int SM_CXVIRTUALSCREEN = 78;
        public const int SM_CYVIRTUALSCREEN = 79;
        public const int SM_SWAPBUTTON = 23;

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int metric);

        public const int GWL_STYLE      = -16;
        public const int GWL_EXSTYLE    = -20;

        public const int WS_VSCROLL         =  0x00200000;
        public const int WS_HSCROLL         =  0x00100000;
        public const int ES_MULTILINE       =  0x0004;
        public const int ES_AUTOVSCROLL     =  0x0040;
        public const int ES_AUTOHSCROLL     =  0x0080;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowLong(NativeMethods.HWND hWnd, int nIndex);

        #endregion

    }
}

