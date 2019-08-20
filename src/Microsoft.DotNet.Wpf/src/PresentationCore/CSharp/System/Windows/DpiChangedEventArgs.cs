// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//
//
// 
// Description: This class is used to pass necessary information to any listener
//              of DpiChanged Event - when a window is moved to a monitor with
//              different DPI, or the dpi of the current monitor changes.

using System.ComponentModel;
using System.Security;
using System.Threading;
using System.Windows.Interop;
using MS.Internal;
using MS.Internal.PresentationCore;
using MS.Win32;

namespace System.Windows
{
    /// <summary>
    /// The HwndDpiChangedEventArgs class represents a type of HandledEventArgs that
    /// is relevant to a DpiChanged event.
    /// </summary>
    public sealed class HwndDpiChangedEventArgs : HandledEventArgs
    {
        /// <summary>
        /// Creates an instance of HwndDpiChangedEventArgs
        /// </summary>
        /// <param name="oldDpiX">
        /// The X-Axis DPI value before DPI changed.
        /// </param>    
        /// <param name="oldDpiY">
        /// The Y-Axis DPI value before DPI changed.
        /// </param>
        /// <param name="newDpiX">
        /// The X-Axis DPI value after DPI changed.
        /// </param>    
        /// <param name="newDpiY">
        /// The Y-Axis DPI value after DPI changed.
        /// </param>    
        /// <param name="lParam">
        /// A pointer to a RECT structure that provides the size and position of the suggested window, scaled for the new DPI.
        /// </param>    
        [Obsolete]
        internal HwndDpiChangedEventArgs(double oldDpiX, double oldDpiY, double newDpiX, double newDpiY, IntPtr lParam) : base(false)
        {
            OldDpi = new DpiScale(oldDpiX / DpiUtil.DefaultPixelsPerInch, oldDpiY / DpiUtil.DefaultPixelsPerInch);
            NewDpi = new DpiScale(newDpiX / DpiUtil.DefaultPixelsPerInch, newDpiY / DpiUtil.DefaultPixelsPerInch);
            NativeMethods.RECT suggestedRect = (NativeMethods.RECT)UnsafeNativeMethods.PtrToStructure(lParam, typeof(NativeMethods.RECT));
            this.SuggestedRect = new Rect((double)suggestedRect.left, (double)suggestedRect.top, (double)suggestedRect.Width, (double)suggestedRect.Height);
        }

        /// <summary>
        /// Creates an instance of HwndDpiChangedEventArgs
        /// </summary>
        /// <param name="oldDpi">X-Axis DPI value before DPI changed</param>
        /// <param name="newDpi">Y-Axis DPI value before DPI changed</param>
        /// <param name="suggestedRect">Suggested client rectangle, scaled for the new DPI</param>
        /// <remarks>
        /// If <paramref name="suggestedRect"/> is <see cref="Rect.Empty"/>, this constructor will act as if
        /// no suggestion for the new client rectangle has been provided. 
        /// When this happens, DPI-change processing logic in <see cref="HwndTarget.OnMonitorDPIChanged"/> will 
        /// not attempt to resize the window. It will assume that the top-level WM_DPICHANGED resulted
        /// in a cascaded set of changes that adjusted the client areas for all child-windows appropriately
        /// in response to DPI changes. <see cref="HwndTarget.OnMonitorDPIChanged"/> will simply 
        /// invalidate and repaint the window if this happens.
        /// </remarks>
        internal HwndDpiChangedEventArgs(DpiScale oldDpi, DpiScale newDpi, Rect suggestedRect) :
            base(false)
        {
            OldDpi = oldDpi;
            NewDpi = newDpi;
            SuggestedRect = suggestedRect;
        }

        /// <summary>
        /// DPI Scale information before change.
        /// </summary>
        public DpiScale OldDpi { get; private set; }

        /// <summary>
        /// DPI Scale information after change.
        /// </summary>
        public DpiScale NewDpi { get; private set; }

        /// <summary>
        /// Provides the size and position of the suggested window, scaled for the new DPI.
        /// </summary>
        public Rect SuggestedRect { get; private set; }
    }

    /// <summary>
    /// Represents information relevant for handling WM_DPICHANGED_AFTERPARENT
    /// Window Message. 
    /// </summary>
    /// <remarks>
    /// This message is relevant only for child-windows. Top-level windows get 
    /// WM_DPICHANGED, and the corresponding data is represented by 
    /// <see cref="HwndDpiChangedEventArgs"/>. 
    /// </remarks>
    internal class HwndDpiChangedAfterParentEventArgs : HandledEventArgs
    {
        /// <summary>
        /// Creates an instances of <see cref="HwndDpiChangedAfterParentEventArgs"/>
        /// </summary>
        /// <param name="oldDpi">DPI scale before the change</param>
        /// <param name="newDpi">DPI scale after the change</param>
        /// ><param name="suggestedRect">Suggested client rectangle, scaled for the new DPI</param>
        internal HwndDpiChangedAfterParentEventArgs(DpiScale oldDpi, DpiScale newDpi, Rect suggestedRect)
            : base(false)
        {
            OldDpi = oldDpi;
            NewDpi = newDpi;
            SuggestedRect = suggestedRect;
        }

        /// <summary>
        /// Creates an instance of <see cref="HwndDpiChangedAfterParentEventArgs"/>
        /// </summary>
        internal HwndDpiChangedAfterParentEventArgs(HwndDpiChangedEventArgs e)
            : this(e.OldDpi, e.NewDpi, e.SuggestedRect)
        {}

        /// <summary>
        /// Dpi scale information before change
        /// </summary>
        internal DpiScale OldDpi { get; }

        /// <summary>
        /// Dpi scale information after change
        /// </summary>
        internal DpiScale NewDpi { get; }

        /// <summary>
        /// Provides the size and position of the suggested window, scaled for the new DPI.
        /// </summary>
        internal Rect SuggestedRect { get; }


        /// <summary>
        /// Conversion operator from <see cref="HwndDpiChangedAfterParentEventArgs"/> to <see cref="HwndDpiChangedEventArgs"/>
        /// </summary>
        public static explicit operator HwndDpiChangedEventArgs(HwndDpiChangedAfterParentEventArgs e)
        {
            return new HwndDpiChangedEventArgs(e.OldDpi, e.NewDpi, e.SuggestedRect);
        }
    }

    /// <summary>
    /// The delegate to use for handlers that receive DpiChangedEventArgs.
    /// </summary>
    public delegate void DpiChangedEventHandler(object sender, DpiChangedEventArgs e);

    /// <summary>
    /// The delegate to use for handlers that receive DPI change notification.
    /// </summary>
    public delegate void HwndDpiChangedEventHandler(object sender, HwndDpiChangedEventArgs e);

    /// <summary>
    /// The DpiChangedventArgs class represents a type of RoutedEventArgs that
    /// is relevant to a DpiChanged event.
    /// </summary>
    public sealed class DpiChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Constructs a DpiChangedEventArgs instance.
        /// </summary>
        internal DpiChangedEventArgs(DpiScale oldDpi, DpiScale newDpi, RoutedEvent routedEvent, object source) : base(routedEvent, source )
        {
            OldDpi = oldDpi;
            NewDpi = newDpi;
        }

        /// <summary>
        /// DPI Scale information before change.
        /// </summary>
        public DpiScale OldDpi { get; private set; }

        /// <summary>
        /// DPI Scale information after change.
        /// </summary>
        public DpiScale NewDpi { get; private set; }
    }

    /// <summary>
    /// Wrapper class to pass all the DPI related information down the Visual tree on DPI change.
    /// </summary>
    internal class DpiRecursiveChangeArgs
    {
        internal DpiRecursiveChangeArgs(DpiFlags dpiFlags, DpiScale oldDpiScale,
            DpiScale newDpiScale)
        {
            DpiScaleFlag1 = dpiFlags.DpiScaleFlag1;
            DpiScaleFlag2 = dpiFlags.DpiScaleFlag2;
            Index = dpiFlags.Index;
            OldDpiScale = oldDpiScale;
            NewDpiScale = newDpiScale;
        }

        internal bool DpiScaleFlag1 { get; set; }
        internal bool DpiScaleFlag2 { get; set; }
        internal int Index { get; set; }
        internal DpiScale OldDpiScale { get; set; }
        internal DpiScale NewDpiScale { get; set; }
    }

    /// <summary>
    /// Wrapper class to pass DPI flags and index of the UIElement.DpiScale array
    /// </summary>
    internal class DpiFlags
    {
        internal DpiFlags(bool dpiScaleFlag1, bool dpiScaleFlag2, int index)
        {
            DpiScaleFlag1 = dpiScaleFlag1;
            DpiScaleFlag2 = dpiScaleFlag2;
            Index = index;
        }
        internal bool DpiScaleFlag1 { get; set; }
        internal bool DpiScaleFlag2 { get; set; }
        internal int Index { get; set; }
    }
}
