// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace System.Windows.Automation.Peers
{
    ///
    public class ScrollBarAutomationPeer : RangeBaseAutomationPeer
    {
        ///
        public ScrollBarAutomationPeer(ScrollBar owner): base(owner)
        {
        }

        ///
        protected override string GetClassNameCore()
        {
            return "ScrollBar";
        }

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ScrollBar;
        }

        // AutomationControlType.ScrollBar must return IsContentElement false.
        // See http://msdn.microsoft.com/en-us/library/ms743712.aspx
        protected override bool IsContentElementCore()
        {
            return false;
        }

        ///
        protected override Point GetClickablePointCore()
        {
            return new Point(double.NaN, double.NaN);
        }

        ///
        protected override AutomationOrientation GetOrientationCore()
        {
            return ((ScrollBar)Owner).Orientation == Orientation.Horizontal ?
                AutomationOrientation.Horizontal :
                AutomationOrientation.Vertical;
        }

        ///
        internal override void SetValueCore(double val)
        {
            double horizontalPercent = -1;
            double verticalPercent = -1;
            ScrollBar sb = Owner as ScrollBar;
            ScrollViewer sv = sb.TemplatedParent as ScrollViewer;
            if (sv == null)
            {
                base.SetValueCore(val);
            }
            else
            {
                if (sb.Orientation == Orientation.Horizontal)
                {
                    horizontalPercent = (val / (sv.ExtentWidth - sv.ViewportWidth)) * 100;
                }
                else
                {
                    verticalPercent = (val / (sv.ExtentHeight - sv.ViewportHeight)) * 100;
                }

                ScrollViewerAutomationPeer svAP = UIElementAutomationPeer.FromElement(sv) as ScrollViewerAutomationPeer;
                IScrollProvider scrollProvider = svAP as IScrollProvider;
                scrollProvider.SetScrollPercent(horizontalPercent, verticalPercent);
            }
        }
    }
}

