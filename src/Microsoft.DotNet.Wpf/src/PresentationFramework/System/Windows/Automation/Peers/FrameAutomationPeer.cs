// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.ComponentModel;

namespace System.Windows.Automation.Peers
{
    /// 
    public class FrameAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public FrameAutomationPeer(Frame owner) : base(owner)
        { }

        ///
        override protected string GetClassNameCore()
        {
            return "Frame";
        }

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Pane;
        }
    }
}


