// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace System.Windows.Automation.Peers
{
    ///
    public class GridSplitterAutomationPeer : ThumbAutomationPeer, ITransformProvider
    {
        ///
        public GridSplitterAutomationPeer(GridSplitter owner): base(owner)
        {}
    
        ///
        protected override string GetClassNameCore()
        {
            return "GridSplitter";
        }

        /// 
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Transform)
                return this;
            else
                return base.GetPattern(patternInterface); 
        }

        #region ITransformProvider

        bool ITransformProvider.CanMove { get { return true; } }
        bool ITransformProvider.CanResize { get { return false; } }
        bool ITransformProvider.CanRotate { get { return false; } }

        void ITransformProvider.Move(double x, double y)
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            if (double.IsInfinity(x) || double.IsNaN(x))
                throw new ArgumentOutOfRangeException(nameof(x));

            if (double.IsInfinity(y) || double.IsNaN(y))
                throw new ArgumentOutOfRangeException(nameof(y));

            ((GridSplitter)Owner).KeyboardMoveSplitter(x, y);
        }
        void ITransformProvider.Resize(double width, double height)
        {
            throw new InvalidOperationException(SR.UIA_OperationCannotBePerformed);
        }
        void ITransformProvider.Rotate(double degrees)
        {
            throw new InvalidOperationException(SR.UIA_OperationCannotBePerformed);
        }

        #endregion
    }
}

