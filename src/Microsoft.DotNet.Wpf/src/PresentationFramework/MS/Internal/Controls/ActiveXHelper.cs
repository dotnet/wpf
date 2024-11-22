// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:  
//      Helper functions for ActiveX hosting
//
//      Source copied from axhosthelper.cs
//     

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Security;
using MS.Win32;

namespace MS.Internal.Controls
{
    //
    // This class contains static properties/methods that are internal.
    // It also has types that make sense only for ActiveX hosting classes.
    // In other words, this is a helper class for the ActiveX hosting classes.
    //
    internal class ActiveXHelper
    {
        //
        // Types:
        //

        //
        // Enumeration of the different states of the ActiveX control
        public enum ActiveXState
        {
            Passive = 0,    // Not loaded
            Loaded = 1,    // Loaded, but no server   [ocx created]
            Running = 2,    // Server running, invisible [depersisted]
            InPlaceActive = 4,    // Server in-place active [visible]
            UIActive = 8,    // Server is UI active [ready to accept input]
            Open = 16    // Server is opened for editing [not used]
        }

        //
        // Static members:
        //

        //
        // BitVector32 masks for various internal state flags.
        public static readonly int sinkAttached = BitVector32.CreateMask();
        public static readonly int inTransition = BitVector32.CreateMask(sinkAttached);
        public static readonly int processingKeyUp = BitVector32.CreateMask(inTransition);

        //[kusumav]These are from Winforms code. These transforms were not working correctly
        //I used Avalon sizes but leaving these in here since they are used in TransformCoordinates
        //in ActiveXSite. It doesn't seem to be invoked by WebBrowser control
        //
        // Gets the LOGPIXELSX of the screen DC.
        private static int logPixelsX = -1;
        private static int logPixelsY = -1;
        private const int HMperInch = 2540;

        // Prevent compiler from generating public CTOR
        private ActiveXHelper()
        {
        }

        //
        // Static helper methods:
        //

        public static int Pix2HM(int pix, int logP)
        {
            return (HMperInch * pix + (logP >> 1)) / logP;
        }

        public static int HM2Pix(int hm, int logP)
        {
            return (logP * hm + HMperInch / 2) / HMperInch;
        }

        //
        // We cache LOGPIXELSX for optimization
        public static int LogPixelsX
        {
            get
            {
                if (logPixelsX == -1)
                {
                    IntPtr hDC = UnsafeNativeMethods.GetDC(NativeMethods.NullHandleRef);
                    if (hDC != IntPtr.Zero)
                    {
                        logPixelsX = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(null, hDC), NativeMethods.LOGPIXELSX);
                        UnsafeNativeMethods.ReleaseDC(NativeMethods.NullHandleRef, new HandleRef(null, hDC));
                    }
                }
                return logPixelsX;
            }
        }

        //
        // We cache LOGPIXELSY for optimization
        public static int LogPixelsY
        {
            get
            {
                if (logPixelsY == -1)
                {
                    IntPtr hDC = UnsafeNativeMethods.GetDC(NativeMethods.NullHandleRef);
                    if (hDC != IntPtr.Zero)
                    {
                        logPixelsY = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(null, hDC), NativeMethods.LOGPIXELSY);
                        UnsafeNativeMethods.ReleaseDC(NativeMethods.NullHandleRef, new HandleRef(null, hDC));
                    }
                }
                return logPixelsY;
            }
        }
    }
}
