// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

using MS.Internal;
using MS.Win32;

namespace System.Windows.Automation.Peers
{
    ///
    public class ThumbAutomationPeer : FrameworkElementAutomationPeer
    {
        ///
        public ThumbAutomationPeer(Thumb owner): base(owner)
        {}
    
        ///
        override protected string GetClassNameCore()
        {
            return "Thumb";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Thumb;
        }

        // AutomationControlType.Thumb must return IsContentElement false.
        // See http://msdn.microsoft.com/en-us/library/ms742539.aspx
        override protected bool IsContentElementCore()
        {
            return false;
        }
    }
}

