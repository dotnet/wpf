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
    public class GridSplitterAutomationPeer : ThumbAutomationPeer, ITransformProvider
    {
        ///
        public GridSplitterAutomationPeer(GridSplitter owner): base(owner)
        {}
    
        ///
        override protected string GetClassNameCore()
        {
            return "GridSplitter";
        }

        /// 
        override public object GetPattern(PatternInterface patternInterface)
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
                throw new ArgumentOutOfRangeException("x");

            if (double.IsInfinity(y) || double.IsNaN(y))
                throw new ArgumentOutOfRangeException("y");

            ((GridSplitter)Owner).KeyboardMoveSplitter(x, y);
        }
        void ITransformProvider.Resize(double width, double height)
        {
            throw new InvalidOperationException(SR.Get(SRID.UIA_OperationCannotBePerformed));
        }
        void ITransformProvider.Rotate(double degrees)
        {
            throw new InvalidOperationException(SR.Get(SRID.UIA_OperationCannotBePerformed));
        }

        #endregion
    }
}

