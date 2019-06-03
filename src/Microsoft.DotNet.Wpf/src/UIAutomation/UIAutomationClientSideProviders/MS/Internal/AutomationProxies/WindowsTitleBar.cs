// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Spinner Proxy

using System;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.ComponentModel;
using MS.Win32;


namespace MS.Internal.AutomationProxies
{
    // the title bar contains the system menu, the context help, minimize, maximize and close buttons.
    // there is a win32 title bar contant for the ime button.  There really is no such thing as an ims button
    // it's bogus.  So when this code apears to by using 1 for the item for the system menu it will never
    // conflict because the ime button does not exist.
    class WindowsTitleBar: ProxyFragment
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        public WindowsTitleBar (IntPtr hwnd, ProxyFragment parent, int item)
            : base( hwnd, parent, item )
        {
            // Set the strings to return properly the properties.
            _cControlType = ControlType.TitleBar;

            _sAutomationId = "TitleBar"; // This string is a non-localizable string
            // _cControlType = ControlType.TitleBar;

            _fNonClientAreaElement = true;
            _fIsContent = false;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Patterns Implementation
        //
        //------------------------------------------------------

        #region ProxySimple Interface

        /// Gets the bounding rectangle for this element
        internal override Rect BoundingRectangle
        {
            get
            {
                return Misc.GetTitleBarRect(_hwnd);
            }
        }

        /// Returns the Run Time Id.
        /// returns an array of ints as the concatenation of IDs
        internal override int [] GetRuntimeId ()
        {
            return new int [] { 4, unchecked((int)(long)_hwnd), _item };
        }

        //Gets the localized name
        internal override string LocalizedName
        {
            get
            {
                return Misc.ProxyGetText(_hwnd);
            }
        }

        #endregion

        #region ProxyFragment Interface

        /// Returns the next sibling element in the raw hierarchy.
        /// Peripheral controls have always negative values.
        /// Returns null if no next child.
        internal override ProxySimple GetNextSibling (ProxySimple child)
        {
            return ReturnNextTitleBarChild (true, child._item + 1);
        }

        /// Returns the previous sibling element in the raw hierarchy.
        /// Peripheral controls have always negative values.
        /// Returns null is no previous.
        internal override ProxySimple GetPreviousSibling (ProxySimple child)
        {
            return ReturnNextTitleBarChild (false, child._item - 1);
        }

        /// Returns the first child element in the raw hierarchy.
        internal override ProxySimple GetFirstChild ()
        {
            return ReturnNextTitleBarChild (true, NativeMethods.INDEX_TITLEBAR_MIC);
        }

        /// Returns the last child element in the raw hierarchy.
        internal override ProxySimple GetLastChild ()
        {
            return ReturnNextTitleBarChild (true, NativeMethods.INDEX_TITLEBAR_MAC);
        }
        /// Returns a Proxy element corresponding to the specified screen coordinates.
        internal override ProxySimple ElementProviderFromPoint (int x, int y)
        {
            int hit = Misc.ProxySendMessageInt(_hwnd, NativeMethods.WM_NCHITTEST, IntPtr.Zero, (IntPtr)NativeMethods.Util.MAKELONG(x, y));
            switch (hit)
            {
                case NativeMethods.HTCAPTION:     
                    return this;
                
                case NativeMethods.HTMINBUTTON:   
                    return CreateTitleBarChild(NativeMethods.INDEX_TITLEBAR_MINBUTTON);
                
                case NativeMethods.HTMAXBUTTON :  
                    return CreateTitleBarChild(NativeMethods.INDEX_TITLEBAR_MAXBUTTON);
                
                case NativeMethods.HTHELP :       
                    return CreateTitleBarChild(NativeMethods.INDEX_TITLEBAR_HELPBUTTON);

                case NativeMethods.HTCLOSE :      
                    return CreateTitleBarChild(NativeMethods.INDEX_TITLEBAR_CLOSEBUTTON);

                case NativeMethods.HTSYSMENU :
                {
                    // this gets us the system menu bar...
                    WindowsMenu sysmenu = WindowsMenu.CreateSystemMenu(_hwnd, this);
                    // now drill into the menu item...
                    return sysmenu.CreateMenuItem(0);
                }
            }
            return null;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal static bool HasTitleBar (IntPtr hwnd)
        {
            return IsTitleBarVisible (hwnd);
        }

        internal ProxySimple CreateTitleBarChild (int item)
        {
            switch (item)
            {
                case _systemMenu:
                    return WindowsMenu.CreateSystemMenu (_hwnd, this);

                case NativeMethods.INDEX_TITLEBAR_HELPBUTTON :
                case NativeMethods.INDEX_TITLEBAR_MINBUTTON :
                case NativeMethods.INDEX_TITLEBAR_MAXBUTTON :
                case NativeMethods.INDEX_TITLEBAR_CLOSEBUTTON :
                    return new TitleBarButton (_hwnd, this, item);
            }

            return null;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        internal const int _systemMenu = 1;

        #endregion

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Not all the titlebar children exist for every titlebar so this get the next one that exists
        private ProxySimple ReturnNextTitleBarChild (bool next, int start)
        {
            UnsafeNativeMethods.TITLEBARINFO ti;
            Misc.ProxyGetTitleBarInfo(_hwnd, out ti);
            ProxySimple el;

            for (int i = start; i >= NativeMethods.INDEX_TITLEBAR_MIC && i <= NativeMethods.INDEX_TITLEBAR_MAC; i += next ? 1 : -1)
            {
                // the system menu is taking the slot in the bogus IME button so it will allway be invisible
                // therefore make an exception for the system menu.
                if (!Misc.IsBitSet(ti.rgstate[i], NativeMethods.STATE_SYSTEM_INVISIBLE) || i == _systemMenu)
                {
                    el = CreateTitleBarChild (i);
                    if (el != null)
                        return el;
                }
            }

            return null;
        }

        private static bool IsTitleBarVisible (IntPtr hwnd)
        {
            UnsafeNativeMethods.TITLEBARINFO ti;
            if (Misc.ProxyGetTitleBarInfo(hwnd, out ti))
            {
                return !Misc.IsBitSet(ti.rgstate[0], NativeMethods.STATE_SYSTEM_INVISIBLE);
            }
            return false;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  TitleBarButton Private Class
        //
        //------------------------------------------------------

        #region TitleBarButton

        class TitleBarButton: ProxySimple, IInvokeProvider
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            public TitleBarButton (IntPtr hwnd, ProxyFragment parent, int item)
            : base( hwnd, parent, item)
            {
                _fNonClientAreaElement = true;
                _cControlType = ControlType.Button;
                _fIsContent = false;
            }

            #endregion

            //------------------------------------------------------
            //
            //  Patterns Implementation
            //
            //------------------------------------------------------

            #region ProxySimple Interface

            /// Returns a pattern interface if supported.
            internal override object GetPatternProvider (AutomationPattern iid)
            {
                if (iid == InvokePattern.Pattern)
                {
                    return this;
                }

                return null;
            }

            /// Process all the Logical and Raw Element Properties
            internal override object GetElementProperty (AutomationProperty idProp)
            {
                if (idProp == AutomationElement.AutomationIdProperty)
                {
                    switch (_item)
                    {
                        case NativeMethods.INDEX_TITLEBAR_HELPBUTTON:
                            return "Help";  // This string is a non-localizable string

                        case NativeMethods.INDEX_TITLEBAR_MINBUTTON:
                            if (Misc.IsBitSet(WindowStyle, NativeMethods.WS_MINIMIZE))
                            {
                                return "Restore";  // This string is a non-localizable string
                            }
                            else
                            {
                                return "Minimize";  // This string is a non-localizable string
                            }

                        case NativeMethods.INDEX_TITLEBAR_MAXBUTTON:
                            if (Misc.IsBitSet(WindowStyle, NativeMethods.WS_MAXIMIZE))
                            {
                                return "Restore";  // This string is a non-localizable string
                            }
                            else
                            {
                                return "Maximize";  // This string is a non-localizable string
                            }

                        case NativeMethods.INDEX_TITLEBAR_CLOSEBUTTON:
                            return "Close";  // This string is a non-localizable string

                        default:
                            break;
                    }
                }
                else if (idProp == AutomationElement.IsEnabledProperty)
                {
                    switch (_item)
                    {
                        case NativeMethods.INDEX_TITLEBAR_HELPBUTTON:
                            if (Misc.IsBitSet(WindowStyle, NativeMethods.WS_DISABLED))
                            {
                                return false;
                            }

                            return Misc.IsBitSet(WindowExStyle, NativeMethods.WS_EX_CONTEXTHELP);

                        case NativeMethods.INDEX_TITLEBAR_MINBUTTON:
                        {
                            int style = WindowStyle;

                            if (Misc.IsBitSet(style, NativeMethods.WS_DISABLED))
                            {
                                return false;
                            }

                            return Misc.IsBitSet(style, NativeMethods.WS_MINIMIZEBOX);
                        }

                        case NativeMethods.INDEX_TITLEBAR_MAXBUTTON:
                        {
                            int style = WindowStyle;

                            if (Misc.IsBitSet(style, NativeMethods.WS_DISABLED))
                            {
                                return false;
                            }

                            return Misc.IsBitSet(style, NativeMethods.WS_MAXIMIZEBOX);
                        }
                    }
                }

                return base.GetElementProperty (idProp);
            }

            // Gets the bounding rectangle for this element
            internal override Rect BoundingRectangle
            {
                get
                {
                    Rect[] rects = Misc.GetTitlebarRects(_hwnd);
                    return rects[_item];
                }
            }

            //Gets the localized name
            internal override string LocalizedName
            {
                get
                {
                    switch (_item)
                    {
                        case NativeMethods.INDEX_TITLEBAR_MINBUTTON:
                            if (Misc.IsBitSet(WindowStyle, NativeMethods.WS_MINIMIZE))
                                return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonRestore);
                            else
                                return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonMinimize);

                        case NativeMethods.INDEX_TITLEBAR_HELPBUTTON:
                            return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonContextHelp);

                        case NativeMethods.INDEX_TITLEBAR_MAXBUTTON:
                            if (Misc.IsBitSet(WindowStyle, NativeMethods.WS_MAXIMIZE))
                                return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonRestore);
                            else
                                return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonMaximize);

                        case NativeMethods.INDEX_TITLEBAR_CLOSEBUTTON:
                            return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonClose);

                        default:
                            return SR.Get(SRID.LocalizedNameWindowsTitleBarButtonUnknown);
                    }
                }
            }

            #endregion

            #region Invoke Pattern

            /// Same effect as a click on one of the title bar button
            void IInvokeProvider.Invoke ()
            {
                // Make sure that the control is enabled
                if (!SafeNativeMethods.IsWindowEnabled(_hwnd))
                {
                    throw new ElementNotEnabledException();
                }

                int command;
                switch (_item)
                {
                    case NativeMethods.INDEX_TITLEBAR_MINBUTTON :
                        if ((WindowStyle & NativeMethods.WS_MINIMIZE) != 0)
                            command = NativeMethods.SC_RESTORE;
                        else
                            command = NativeMethods.SC_MINIMIZE;
                        break;

                    case NativeMethods.INDEX_TITLEBAR_HELPBUTTON :
                        command = NativeMethods.SC_CONTEXTHELP;
                        break;

                    case NativeMethods.INDEX_TITLEBAR_MAXBUTTON :
                        if ((WindowStyle & NativeMethods.WS_MAXIMIZE) != 0)
                            command = NativeMethods.SC_RESTORE;
                        else
                            command = NativeMethods.SC_MAXIMIZE;
                        break;

                    case NativeMethods.INDEX_TITLEBAR_CLOSEBUTTON :
                        if ((WindowStyle & NativeMethods.WS_MINIMIZE) != 0)
                        {
                            Misc.PostMessage(_hwnd, NativeMethods.WM_SYSCOMMAND, (IntPtr)NativeMethods.SC_RESTORE, IntPtr.Zero);
                        }

                        command = NativeMethods.SC_CLOSE;
                        break;

                    default :
                        return;
                }

                // leave menu-mode if we're in it
                Misc.ClearMenuMode();

                // push the right button
                Misc.PostMessage(_hwnd, NativeMethods.WM_SYSCOMMAND, (IntPtr)command, IntPtr.Zero);
            }

            #endregion
        }
        #endregion
    }
}

