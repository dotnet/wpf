// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    ///
    public class SliderAutomationPeer : RangeBaseAutomationPeer
    {
        ///
        public SliderAutomationPeer(Slider owner): base(owner)
        {
        }

        ///
        override protected string GetClassNameCore()
        {
            return "Slider";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Slider;
        }

        ///
        protected override Point GetClickablePointCore()
        {
            return new Point(double.NaN, double.NaN);
        }
    }
}

