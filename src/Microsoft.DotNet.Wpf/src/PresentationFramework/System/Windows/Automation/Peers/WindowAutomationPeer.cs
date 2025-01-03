// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;
using System.ComponentModel;
using MS.Win32;

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
                    try
                    {
                        StringBuilder sb = new StringBuilder(512);
                        UnsafeNativeMethods.GetWindowText(new HandleRef(null, window.Handle), sb, sb.Capacity);
                        name = sb.ToString();
                    }
                    catch (Win32Exception)
                    {
                        name = window.Title;
                    }

                    name ??= "";
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
                IntPtr windowHandle = window.Handle;
                if(windowHandle != IntPtr.Zero) //it is Zero on a window that was just closed
                {
                    try { SafeNativeMethods.GetWindowRect(new HandleRef(null, windowHandle), ref rc); }
                    catch(Win32Exception) {}
                }        
                bounds = new Rect(rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);
            }

            return bounds;
        }

        protected override bool IsDialogCore()
        {
            Window window = (Window)Owner;
            if (MS.Internal.Helper.IsDefaultValue(AutomationProperties.IsDialogProperty, window))
            {
                return window.IsShowingAsDialog;
            }
            else
            {
                return AutomationProperties.GetIsDialog(window);
            }
        }
    }
}

