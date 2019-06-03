// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Internal.AppModel;

namespace System.Windows.Automation.Peers
{

    /// 
    internal class RootBrowserWindowAutomationPeer : WindowAutomationPeer
    {
        ///
        public RootBrowserWindowAutomationPeer(RootBrowserWindow owner): base(owner)
        {}
    
        ///
        override protected string GetClassNameCore()
        {
            return "RootBrowserWindow";
        }

    }
}



