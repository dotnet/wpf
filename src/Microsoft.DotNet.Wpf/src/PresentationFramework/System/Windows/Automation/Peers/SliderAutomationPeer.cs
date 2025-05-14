// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        protected override string GetClassNameCore()
        {
            return "Slider";
        }

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
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

