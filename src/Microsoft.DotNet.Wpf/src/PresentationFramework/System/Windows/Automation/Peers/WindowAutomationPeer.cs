// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.ComponentModel;

using MS.Internal;
using MS.Win32;

// Used to support the warnings disabled below
#pragma warning disable 1634, 1691


namespace System.Windows.Automation.Peers
{
    /// 
    public class WindowAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public WindowAutomationPeer(Window owner): base(owner)
        {}
    
        ///
        override protected string GetClassNameCore()
        {
            return "Window";
        }

        ///
        override protected string GetNameCore()
        {
            string name = base.GetNameCore();

            if(name.Length == 0)
            {
                Window window = (Window)Owner;

                if(!window.IsSourceWindowNull)
                {
                    StringBuilder sb = new StringBuilder(512);
                    UnsafeNativeMethods.GetWindowText(new HandleRef(null, window.CriticalHandle), sb, sb.Capacity);
                    name = sb.ToString();

                    if (name == null)
                        name = string.Empty;
                }
            }

            return name;
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Window;
        }



        ///
        override protected Rect GetBoundingRectangleCore()
        {
            Window window = (Window)Owner;
            Rect bounds = new Rect(0,0,0,0);
            
            if(!window.IsSourceWindowNull)
            {
                NativeMethods.RECT rc = new NativeMethods.RECT(0,0,0,0);
                IntPtr windowHandle = window.CriticalHandle;
                if(windowHandle != IntPtr.Zero) //it is Zero on a window that was just closed
                {
                    try { SafeNativeMethods.GetWindowRect(new HandleRef(null, windowHandle), ref rc); }
// Allow empty catch statements.
#pragma warning disable 56502
                    catch(Win32Exception) {}
// Disallow empty catch statements.
#pragma warning restore 56502
                }        
                bounds = new Rect(rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);
            }

            return bounds;
        }
    }
}


