// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Windows Static Proxy

using System;
using System.Text;
using System.Collections;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
    class WindowsStatic: ProxyHwnd
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        WindowsStatic (IntPtr hwnd, ProxyFragment parent, StaticType type, int style)
            : base( hwnd, parent, 0)
        {
            _type = type;
            _style = style;
            if (type == StaticType.Text)
            {
                _cControlType = ControlType.Text;
                _fIsContent = false;
                _fControlHasLabel = false;
            }
            else
            {
                _cControlType = ControlType.Image;
            }

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
            // This proxy should not be created with idChild != 0,
            // unless it is a link label.
            if (idChild != 0 && !IsLinkLabel(hwnd))
            {
                System.Diagnostics.Debug.Assert(idChild == 0, "Invalid Child Id, idChild != 0");
                throw new ArgumentOutOfRangeException("idChild", idChild, SR.Get(SRID.ShouldBeZero));
            }

            StaticType type;
            int style;

            try
            {
                string className = Misc.GetClassName(hwnd).ToLower(System.Globalization.CultureInfo.InvariantCulture);

                // Both labels and linklabels have "STATIC" class names
                if (WindowsFormsHelper.IsWindowsFormsControl(className))
                {
                    if (IsLinkLabel(hwnd))
                    {
                        // Use a different proxy for LinkLabel.
                        return FormsLink.Create(hwnd, 0);
                    }
                }
                else 
                {
                    // if it's not a Windows Forms control, we didn't want substring matching
                    if (className != "static")
                    {
                        return null;
                    }
                }
                
                style = Misc.GetWindowStyle(hwnd) & NativeMethods.SS_TYPEMASK;
                type = GetStaticTypeFromStyle(style);
                if (type == StaticType.Unsupported)
                {
                    return null;
                }
            }
            catch (ElementNotAvailableException)
            {
                return null;
            }

            return new WindowsStatic(hwnd, null, type, style);
        }

        // Static Create method called by the event tracker system
        // WinEvents are raised because items exist. So it makes sense to create the item and
        // check for details afterward.
        internal static void RaiseEvents (IntPtr hwnd, int eventId, object idProp, int idObject, int idChild)
        {
            if (idObject != NativeMethods.OBJID_VSCROLL && idObject != NativeMethods.OBJID_HSCROLL)
            {
                WindowsStatic wtv = (WindowsStatic) Create (hwnd, 0);
                // If wtv is null the window handle is invalid or no longer available (or something,
                // Create eats the problem).
                if (wtv != null)
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

        // Process all the Logical and Raw Element Properties
        internal override object GetElementProperty (AutomationProperty idProp)
        {
            if (idProp == AutomationElement.AccessKeyProperty)
            {
                return Misc.AccessKey(Misc.ProxyGetText(_hwnd));
            }

            return base.GetElementProperty (idProp);
        }

        internal override bool IsKeyboardFocusable()
        {
            // A static control is never focusable via the keyboard.
            return false;
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                if (_type == StaticType.Text)
                {
                    return Misc.StripMnemonic(Misc.ProxyGetText(_hwnd));
                }
                return null;
            }
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------

        #region Private Methods

        private static bool IsLinkLabel(IntPtr hwnd)
        {
            // [Microsoft]:
            // could be a label or a linklabel
            // we differentiate based on whether the item has children or not
            Accessible acc = null;
            return Accessible.AccessibleObjectFromWindow(hwnd, NativeMethods.OBJID_CLIENT, ref acc) == NativeMethods.S_OK && acc != null && acc.ChildCount > 0;
        }

        private static StaticType GetStaticTypeFromStyle(int style)
        {
            StaticType staticType = StaticType.Unsupported;
            switch (style)
            {
                case NativeMethods.SS_ICON:
                    staticType = StaticType.Icon;
                    break;

                case NativeMethods.SS_BITMAP:
                    staticType = StaticType.Bitmap;
                    break;

                case NativeMethods.SS_LEFT:
                case NativeMethods.SS_CENTER:
                case NativeMethods.SS_RIGHT:
                case NativeMethods.SS_BLACKRECT:
                case NativeMethods.SS_GRAYRECT:
                case NativeMethods.SS_WHITERECT:
                case NativeMethods.SS_BLACKFRAME:
                case NativeMethods.SS_GRAYFRAME:
                case NativeMethods.SS_WHITEFRAME:
                case NativeMethods.SS_SIMPLE:
                case NativeMethods.SS_LEFTNOWORDWRAP:
                case NativeMethods.SS_ETCHEDHORZ:
                case NativeMethods.SS_ETCHEDVERT:
                case NativeMethods.SS_ETCHEDFRAME:
                case NativeMethods.SS_OWNERDRAW:
                    staticType = StaticType.Text;
                    break;

                case NativeMethods.SS_ENHMETAFILE:
                case NativeMethods.SS_USERITEM:
                default:
                    // current patterns do not account for images
                    break;
            }
            return staticType;
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Fields and Types Declaration
        //
        // ------------------------------------------------------

        #region Private Fields

        StaticType _type;

        int _style;

        // Static control types based on style constants
        enum StaticType
        {
            Bitmap,
            Icon,
            Text,
            Unsupported
        };
    }
        #endregion
}

