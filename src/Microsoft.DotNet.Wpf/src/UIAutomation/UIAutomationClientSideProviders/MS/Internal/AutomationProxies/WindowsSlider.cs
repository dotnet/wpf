// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Slider Proxy
//

using System;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    class WindowsSlider: ProxyHwnd, IRangeValueProvider
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors 

        WindowsSlider (IntPtr hwnd, ProxyFragment parent, int item)
            : base (hwnd, parent, item )
        {
            _fHorizontal = IsHorizontalSlider ();
            _fIsKeyboardFocusable = true;

            // Set the strings to return properly the properties.
            _cControlType = ControlType.Slider;

            // support for events
            _createOnEvent = new WinEventTracker.ProxyRaiseEvents (RaiseEvents);
        }

        #endregion

        #region Proxy Create

        // Static Create method called by UIAutomation to create this proxy.
        // returns null if unsuccessful
        internal static IRawElementProviderSimple Create(IntPtr hwnd, int idChild, int idObject)
        {
            return Create(hwnd, idChild);
        }

        private static IRawElementProviderSimple Create(IntPtr hwnd, int idChild)
        {
            // Something is wrong if idChild is not zero 
            if (idChild != 0)
            {
                System.Diagnostics.Debug.Assert (idChild == 0, "Invalid Child Id, idChild != 0");
                throw new ArgumentOutOfRangeException("idChild", idChild, SR.Get(SRID.ShouldBeZero));
            }

            return new WindowsSlider(hwnd, null, idChild);
        }

        // Static Create method called by the event tracker system
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                WindowsSlider wtv = new WindowsSlider (hwnd, null, 0);

                wtv.DispatchEvents (eventId, idProp, idObject, idChild);
            }
        }

        #endregion

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        // Returns a pattern interface if supported.
        internal override object GetPatternProvider (AutomationPattern iid)
        {
            return iid == RangeValuePattern.Pattern ? this : null;
        }

        // Process all the Logical and Raw Element Properties
        internal override object GetElementProperty(AutomationProperty idProp)
        {
            if (idProp == AutomationElement.OrientationProperty)
            {
                return IsVerticalSlider() ? OrientationType.Vertical : OrientationType.Horizontal;
            }

            return base.GetElementProperty(idProp);
        }

        #endregion

        #region ProxyFragment Interface

        // Returns the next sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null if no next child
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            SItem item = (SItem)(child._item + 1);

            return item <= SItem.LargeIncrement ? CreateSliderItem (item) : null;
        }

        // Returns the previous sibling element in the raw hierarchy.
        // Peripheral controls have always negative values.
        // Returns null is no previous
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            SItem item = (SItem)(child._item - 1);

            return item >= SItem.LargeDecrement ? CreateSliderItem (item) : null;
        }

        // Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            return CreateSliderItem (SItem.LargeDecrement);
        }

        // Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            return CreateSliderItem (SItem.LargeIncrement);
        }

        // Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            for (SItem item = SItem.LargeDecrement; (int) item <= (int) SItem.LargeIncrement; item = (SItem) ((int) item + 1))
            {
                NativeMethods.Win32Rect rc = new NativeMethods.Win32Rect (SliderItem.GetBoundingRectangle (_hwnd, item, _fHorizontal));

                if (Misc.PtInRect(ref rc, x, y))
                {
                    return CreateSliderItem (item);
                }
            }

            return null;
        }
        
        #endregion

        #region RangeValue Pattern

        // Sets a new position for the Slider.
        void IRangeValueProvider.SetValue (double val)
        {
            // Make sure that the control is enabled
            if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
            {
                throw new ElementNotEnabledException();
            }

            // Set value does not accept float
            int newVal = Convert.ToInt32 (val);

            SetSliderValue (newVal);

            // Set value of slider control needs to notify parent of change
            IntPtr parent = Misc.GetParent(_hwnd);

            if (IntPtr.Zero != parent)
            {
                // Horizontal sliders update parent with WM_HSCROLL, vertical with WM_VSCROLL
                int msg = IsHorizontalSlider () ? NativeMethods.WM_HSCROLL : NativeMethods.WM_VSCROLL;

                // New position
                int wParam = (newVal << 16) | NativeMethods.SB_THUMBPOSITION;

                // Notify parent of new position
                Misc.ProxySendMessage(parent, msg, new IntPtr(wParam), IntPtr.Zero);
            }
        }

        // Request to get the value that this UI element is representing in a native format
        double IRangeValueProvider.Value
        {
            get
            {
                return (double)GetSliderValue ();
            }
        }

        bool IRangeValueProvider.IsReadOnly
        {
            get
            {
                return false;
            }
        }


        double IRangeValueProvider.Maximum
        {
            get
            {
                return (double)Max;
            }
        }

        double IRangeValueProvider.Minimum
        {
            get
            {
                return (double)Min;
            }
        }

        double IRangeValueProvider.SmallChange
        {
            get
            {
                return (double)LineSize;
            }
        }

        double IRangeValueProvider.LargeChange
        {
            get
            {
                double pageSize = (double)PageSize;
                return PageSize != 0.0 ? pageSize : (double)LineSize;
            }
        }
        
        #endregion

        // ------------------------------------------------------
        //
        // Private methods                                             
        //
        // ------------------------------------------------------

        #region Private Methods

        private ProxySimple CreateSliderItem (SItem item)
        {
            return new SliderItem (_hwnd, this, (int) item, _fHorizontal);
        }

        private bool IsHorizontalSlider ()
        {
            return (!IsVerticalSlider ());
        }

        private bool IsVerticalSlider ()
        {
            return (Misc.IsBitSet(WindowStyle, NativeMethods.TBS_VERT));
        }

        private int GetSliderValue ()
        {
            int value = Misc.ProxySendMessageInt(_hwnd, NativeMethods.TBM_GETPOS, IntPtr.Zero, IntPtr.Zero);

            // adding support for if WindowsStyle has reversed bit set for the slider.
            if ((this.WindowStyle & NativeMethods.TBS_REVERSED) != 0)
            {
                int maxValue = Misc.ProxySendMessageInt(_hwnd, NativeMethods.TBM_GETRANGEMAX, IntPtr.Zero, IntPtr.Zero);
                value = maxValue - value;
            }
            return value;
        }

        private void SetSliderValue (int val)
        {
            // check for the range
            if (val > Max)
            {
                throw new ArgumentOutOfRangeException("value", val, SR.Get(SRID.RangeValueMax));
            }
            else if (val < Min)
            {
                throw new ArgumentOutOfRangeException("value", val, SR.Get(SRID.RangeValueMin));
            }

            Misc.ProxySendMessage(_hwnd, NativeMethods.TBM_SETPOS, new IntPtr(1), new IntPtr(val));
        }

        private int LineSize
        {
            get
            {
                return Misc.ProxySendMessageInt(_hwnd, NativeMethods.TBM_GETLINESIZE, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private int Min
        {
            get
            {
                return Misc.ProxySendMessageInt(_hwnd, NativeMethods.TBM_GETRANGEMIN, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private int Max
        {
            get
            {
                return Misc.ProxySendMessageInt(_hwnd, NativeMethods.TBM_GETRANGEMAX, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private int PageSize
        {
            get
            {
                return Misc.ProxySendMessageInt(_hwnd, NativeMethods.TBM_GETPAGESIZE, IntPtr.Zero, IntPtr.Zero);
            }
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Fields                                             
        //
        // ------------------------------------------------------

        #region Private Fields

        private bool _fHorizontal;

        // Index value used to represent the container itself.
        private enum SItem
        {
            LargeDecrement = 0,
            Thumb = 1,
            LargeIncrement = 2
        }

        #endregion

        // ------------------------------------------------------
        //
        //  SliderItem Private Class
        //
        //------------------------------------------------------

        #region SliderItem 

        class SliderItem: ProxySimple, IInvokeProvider
        {
            // ------------------------------------------------------
            //
            // Constructors
            //
            // ------------------------------------------------------

            #region Constructors

            internal SliderItem (IntPtr hwnd, ProxyFragment parent, int item, bool fHorizontal) 
            : base (hwnd, parent, item )
            {
                _fHorizontal = fHorizontal;

                // Set the strings to return properly the properties.
                _fIsContent = false;

                _sAutomationId = _asAutomationId[item];

                if ((WindowsSlider.SItem)item == WindowsSlider.SItem.Thumb)
                {
                    _cControlType = ControlType.Thumb;
                }
                else
                {
                    _cControlType = ControlType.Button;
                }
            }

            #endregion

            //------------------------------------------------------
            //
            //  Patterns Implementation
            //
            //------------------------------------------------------

            #region ProxySimple Interface

            // Returns a pattern interface if supported.
            internal override object GetPatternProvider (AutomationPattern iid)
            {
                return _item != (int) WindowsSlider.SItem.Thumb && iid == InvokePattern.Pattern ? this : null;
            }

            // Gets the bounding rectangle for this element
            internal override Rect BoundingRectangle
            {
                get
                {
                    return GetBoundingRectangle (_hwnd, (WindowsSlider.SItem) _item, _fHorizontal);
                }
            }

            //Gets the controls help text
            internal override string HelpText
            {
                get
                {
                    IntPtr hwndToolTip = Misc.ProxySendMessage(_hwnd, NativeMethods.TBM_GETTOOLTIPS, IntPtr.Zero, IntPtr.Zero);
                    return Misc.GetItemToolTipText(_hwnd, hwndToolTip, 0);
                }
            }

            //Gets the localized name
            internal override string LocalizedName
            {
                get
                {
                    return SR.Get(_asNames[_item]);
                }
            }

            #endregion

            #region Invoke Pattern

            // Same as a click on the large or smale increment
            void IInvokeProvider.Invoke ()
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                // Check that button can be clicked (button enabled and not hidden)
                // This state could change anytime so success is not guaranteed
                IntPtr wParam = (IntPtr) (_item == (int) WindowsSlider.SItem.LargeDecrement ? NativeMethods.VK_PRIOR : NativeMethods.VK_NEXT);

                Misc.ProxySendMessage(_hwnd, NativeMethods.WM_KEYDOWN, wParam, IntPtr.Zero);
            }

            #endregion

            // ------------------------------------------------------
            //
            // Internal Methods
            //
            // ------------------------------------------------------

            #region Internal Methods

            // Returns the bounding rectangle of the control.
            internal static Rect GetBoundingRectangle (IntPtr hwnd, WindowsSlider.SItem item, bool fHorizontal)
            {
                NativeMethods.Win32Rect rcChannel = new NativeMethods.Win32Rect ();
                rcChannel.left = rcChannel.right = rcChannel.top = rcChannel.bottom = 1000;

                unsafe
                {
                    XSendMessage.XSend(hwnd, NativeMethods.TBM_GETCHANNELRECT, IntPtr.Zero, new IntPtr(&rcChannel), Marshal.SizeOf(rcChannel.GetType()), XSendMessage.ErrorValue.NoCheck);
                }
                if (!Misc.MapWindowPoints(hwnd, IntPtr.Zero, ref rcChannel, 2))
                {
                    return Rect.Empty;
                }

                NativeMethods.Win32Rect rcThumb = new NativeMethods.Win32Rect();
                rcThumb.left = rcThumb.right = rcThumb.top = rcThumb.bottom = 1000;

                unsafe
                {
                    XSendMessage.XSend(hwnd, NativeMethods.TBM_GETTHUMBRECT, IntPtr.Zero, new IntPtr(&rcThumb), Marshal.SizeOf(rcThumb.GetType()), XSendMessage.ErrorValue.NoCheck);
                }
                if (!Misc.MapWindowPoints(hwnd, IntPtr.Zero, ref rcThumb, 2))
                {
                    return Rect.Empty;
                }

                if (fHorizontal)
                {
                    // When WS_EX_RTLREADING is set swap the increment and decrement bars.
                    if (Misc.IsLayoutRTL(hwnd))
                    {
                        if (item == SItem.LargeDecrement)
                        {
                            item = SItem.LargeIncrement;
                        }
                        else if (item == SItem.LargeIncrement)
                        {
                            item = SItem.LargeDecrement;
                        }
                    }

                    switch (item)
                    {
                        case WindowsSlider.SItem.LargeDecrement :
                            return new Rect (rcChannel.left, rcChannel.top, rcThumb.left - rcChannel.left, rcChannel.bottom - rcChannel.top);

                        case WindowsSlider.SItem.Thumb :
                            return new Rect (rcThumb.left, rcThumb.top, rcThumb.right - rcThumb.left, rcThumb.bottom - rcThumb.top);

                        case WindowsSlider.SItem.LargeIncrement :
                            return new Rect (rcThumb.right, rcChannel.top, rcChannel.right - rcThumb.right, rcChannel.bottom - rcChannel.top);
                    }
                }
                else
                {
                    int dx = rcChannel.bottom - rcChannel.top;
                    int dy = rcChannel.right - rcChannel.left;

                    switch (item)
                    {
                        case WindowsSlider.SItem.LargeDecrement :
                            return new Rect (rcChannel.left, rcChannel.top, dx, rcThumb.top - rcChannel.top);

                        case WindowsSlider.SItem.Thumb :
                            return new Rect (rcThumb.left, rcThumb.top, rcThumb.right - rcThumb.left, rcThumb.bottom - rcThumb.top);

                        case WindowsSlider.SItem.LargeIncrement :
                            return new Rect (rcChannel.left, rcThumb.bottom, dx, dy);
                    }
                }

                return Rect.Empty;
            }

            #endregion

            // ------------------------------------------------------
            //
            // Private Fields
            //
            // ------------------------------------------------------

            #region Private Fields

            private bool _fHorizontal;

            private string [] _asNames = {
                SRID.LocalizedNameWindowsSliderItemBackByLargeAmount,
                SRID.LocalizedNameWindowsSliderItemThumb,
                SRID.LocalizedNameWindowsSliderItemForwardByLargeAmount
            };

            private static string[] _asAutomationId = new string[] {
                "LargeDecrement", "Thumb", "LargeIncrement"  // This string is a non-localizable string
            };
            #endregion

        }
        #endregion

    }
}

