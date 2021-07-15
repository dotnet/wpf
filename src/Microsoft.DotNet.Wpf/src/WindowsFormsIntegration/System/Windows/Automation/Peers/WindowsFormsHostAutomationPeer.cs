// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Forms.Integration;
using System.Security;

namespace System.Windows.Automation.Peers
{

    /// 
    public sealed class WindowsFormsHostAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public WindowsFormsHostAutomationPeer(WindowsFormsHost owner): base(owner)
        {}
    
        ///
        override protected string GetClassNameCore()
        {
            return "WindowsFormsHost";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Pane;
        }

        ///
        override protected internal bool IsHwndHost { get { return true; }}

        override protected HostedWindowWrapper GetHostRawElementProviderCore()
        {
            HostedWindowWrapper host = null;
            
            WindowsFormsHost wfh = (WindowsFormsHost)Owner;
            IntPtr hwnd = wfh.Handle;

            if(hwnd != IntPtr.Zero)
            {
                host = new HostedWindowWrapper(hwnd);
            }

            return host;
        }

        internal IRawElementProviderSimple GetProvider()
         {
            return ProviderFromPeer(this);
        }

    }
}


