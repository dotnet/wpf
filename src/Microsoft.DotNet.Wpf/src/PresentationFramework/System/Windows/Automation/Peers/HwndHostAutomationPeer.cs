// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Interop;
using System.Security;
using MS.Internal.Automation;

namespace System.Windows.Automation.Peers
{
    /// 
    internal class HwndHostAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public HwndHostAutomationPeer(HwndHost owner): base(owner)
        {
            IsInteropPeer = true;
        }
    
        ///
        override protected string GetClassNameCore()
        {
            return "HwndHost";
        }
        
        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Pane;
        }

        override internal InteropAutomationProvider GetInteropChild()
        {
            if (_interopProvider == null)
            {
                HostedWindowWrapper wrapper = null;
                
                HwndHost host = (HwndHost)Owner;
                IntPtr hwnd = host.CriticalHandle;
                
                if(hwnd != IntPtr.Zero)
                {
                    wrapper = HostedWindowWrapper.CreateInternal(hwnd);
                }
            
                _interopProvider = new InteropAutomationProvider(wrapper, this);
            }

            return _interopProvider;
        }

        #region Data

        private InteropAutomationProvider _interopProvider;

        #endregion Data
    }
}

