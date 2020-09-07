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
        public static void ResetLogPixelsX()
        {
            logPixelsX = -1;
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
        public static void ResetLogPixelsY()
        {
            logPixelsY = -1;
        }

        /// <summary>
        /// Wraps a given managed object, expected to be enabled for IDispatch interop, in a native one that 
        /// trivially delegates the IDispatch calls. This ensures that cross-context calls on the managed object
        /// occur on the same thread (assumed to be in an STA), not on some random RPC worker thread.
        /// </summary>
        /// <remarks>
        /// CLR objects are thread-agile. But nearly all COM objects we use are STA-bound, and we also need 
        /// our implementations of COM interfaces to be called on the thread on which their object was created,
        /// often because of interacting with core WPF objects that also have thread affinity.
        /// Getting a CLR object to "stick to the right thread" when called from another context (like cross-process)
        /// turns out to be incredibly hard. WinForms has solved the problem with StandardOleMarshalObject.
        /// Unfortunately, it wasn't designed to work in partial trust. And apart from that, it just doesn't 
        /// seem to work cross-process, which is what we actually need for the WebOC hosting.
        /// There's also the ContextBoundObject family. A derived object could be made thread-bound. But this 
        /// doesn't work for us either, because we get deadlocks. COM allows re-entrancy when "blocked" on an
        /// outgoing call from an STA, but ContextBoundObject apparently doesn't--it really blocks.
        /// </remarks>
        [DllImport(ExternDll.PresentationHostDll, PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.IDispatch)]
        internal static extern object CreateIDispatchSTAForwarder([MarshalAs(UnmanagedType.IDispatch)] object pDispatchDelegate);
    }
}
