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
    public class GenericRootAutomationPeer : UIElementAutomationPeer
    {
        ///
        public GenericRootAutomationPeer(UIElement owner): base(owner)
        {}
    
        ///
        override protected string GetClassNameCore()
        {
            return "Pane";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Pane;
        }

        ///
        override protected string GetNameCore()
        {
            string name = base.GetNameCore();

            if(name == string.Empty)
            {
                IntPtr hwnd = this.Hwnd;
                if(hwnd != IntPtr.Zero)
                {
                    try
                    {
                        StringBuilder sb = new StringBuilder(512);

                        //This method elevates via SuppressUnmanadegCodeSecurity and throws Win32Exception on GetLastError
                        UnsafeNativeMethods.GetWindowText(new HandleRef(null, hwnd), sb, sb.Capacity);

                        name = sb.ToString();
                    }
                    catch(Win32Exception) {}
                    
                    if (name == null)
                        name = string.Empty;
                }
            }

            return name;
        }

        ///
        override protected Rect GetBoundingRectangleCore()
        {
            Rect bounds = new Rect(0,0,0,0);
            
            IntPtr hwnd = this.Hwnd;
            if(hwnd != IntPtr.Zero)
            {
                NativeMethods.RECT rc = new NativeMethods.RECT(0,0,0,0);
                try 
                { 
                    //This method elevates via SuppressUnmanadegCodeSecurity and throws Win32Exception on GetLastError
                    SafeNativeMethods.GetWindowRect(new HandleRef(null, hwnd), ref rc); 
                }
                catch(Win32Exception) {}

                bounds = new Rect(rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);
            }

            return bounds;
        }
}
}



