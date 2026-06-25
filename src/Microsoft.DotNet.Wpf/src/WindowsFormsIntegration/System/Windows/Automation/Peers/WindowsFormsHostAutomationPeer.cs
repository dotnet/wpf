// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Automation.Provider;
using System.Windows.Forms.Integration;

namespace System.Windows.Automation.Peers
{

    /// 
    public sealed class WindowsFormsHostAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public WindowsFormsHostAutomationPeer(WindowsFormsHost owner): base(owner)
        {}
    
        ///
        protected override string GetClassNameCore()
        {
            return "WindowsFormsHost";
        }

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Pane;
        }

        ///
        protected internal override bool IsHwndHost { get { return true; }}

        protected override HostedWindowWrapper GetHostRawElementProviderCore()
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


