// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Win32 ListViewGroupSubsetLink proxy
//
//

using System;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Runtime.InteropServices;
using System.Windows;
using MS.Win32;

namespace MS.Internal.AutomationProxies
{
        //------------------------------------------------------
        //
        //  ListViewGroupSubsetLink 
        //
        //------------------------------------------------------

        // Proxy for List view Group Subset Link
        class ListViewGroupSubsetLink: ProxySimple, IInvokeProvider
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructor

            internal ListViewGroupSubsetLink (IntPtr hwnd, ProxyFragment parent, int item, int groupId)
            : base(hwnd, parent, item)
            {
                _cControlType = ControlType.Button;
                _sAutomationId = "ListviewGroupSubsetLink" + groupId; // This string is a non-localizable string

                _groupId = groupId;
                _fIsKeyboardFocusable = true;
            }

            #endregion Constructor

            //------------------------------------------------------
            //
            //  Pattern Implementation
            //
            //------------------------------------------------------

            #region ProxySimple Interface

            // Returns a pattern interface if supported.
            internal override object GetPatternProvider (AutomationPattern iid)
            {
                if (iid == InvokePattern.Pattern)
                {
                    return this;
                }

                return null;
            }

            // Gets the bounding rectangle for this element
            internal unsafe override Rect BoundingRectangle
            {
                get
                {
                    NativeMethods.Win32Rect rect = new NativeMethods.Win32Rect();
                    rect.top = NativeMethods.LVGGR_SUBSETLINK;
                    XSendMessage.XSend(_hwnd, NativeMethods.LVM_GETGROUPRECT, new IntPtr(0), new IntPtr(&rect), Marshal.SizeOf(rect.GetType()));
                    Misc.MapWindowPoints(_hwnd, IntPtr.Zero, ref rect, 2);
                
                    return rect.ToRect(false);
                }
            }
            
            // Is focus set to the specified item
            protected override bool IsFocused ()
            {
                NativeMethods.LVGROUP_V6 groupInfo = new NativeMethods.LVGROUP_V6();
                groupInfo.Init(Marshal.SizeOf(typeof(NativeMethods.LVGROUP_V6)));
                groupInfo.iGroupID = _groupId;
                groupInfo.mask = NativeMethods.LVGF_STATE;
                groupInfo.stateMask = NativeMethods.LVGS_SUBSETLINKFOCUSED;
                
                // Note: return code of GetGroupInfo() is not reliable.
                XSendMessage.GetGroupInfo(_hwnd, ref groupInfo); // ignore return code.

                return (groupInfo.state & NativeMethods.LVGS_SUBSETLINKFOCUSED) != 0;
            }
            
            //Gets the localized name
            internal override string LocalizedName
            {
                get
                {
                    NativeMethods.LVGROUP_V6 group = new NativeMethods.LVGROUP_V6();
                    group.Init(Marshal.SizeOf(typeof(NativeMethods.LVGROUP_V6)));

                    group.iGroupID = _groupId;
                    group.cchSubsetTitle= Misc.MaxLengthNameProperty;

                    return XSendMessage.GetItemText(_hwnd, group, NativeMethods.LVGF_SUBSET);
                }
            }

            #endregion ProxySimple Interface

            #region Invoke Pattern

            void IInvokeProvider.Invoke ()
            {
                NativeMethods.Win32Point pt;
                if (GetClickablePoint(out pt, false))
                {
                    Misc.MouseClick(pt.x, pt.y);
                }
            }

            #endregion Invoke Pattern

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // The group id this link belongs to
        private int _groupId;

        #endregion Private Fields

        }
}

