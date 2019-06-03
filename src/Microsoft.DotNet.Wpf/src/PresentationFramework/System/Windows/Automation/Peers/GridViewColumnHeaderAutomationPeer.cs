// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
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
    public class GridViewColumnHeaderAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider, ITransformProvider
    {
        ///
        public GridViewColumnHeaderAutomationPeer(GridViewColumnHeader owner)
            : base(owner)
        {
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.HeaderItem;
        }

        // AutomationControlType.HeaderItem must return IsContentElement false.
        // See http://msdn.microsoft.com/en-us/library/ms742202.aspx
        override protected bool IsContentElementCore()
        {
            return false;
        }

        ///
        override protected string GetClassNameCore()
        {
            return "GridViewColumnHeader";
        }

        /// 
        override public object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Invoke || patternInterface == PatternInterface.Transform)
            {
                return this;
            }
            else
            {
                return base.GetPattern(patternInterface);
            }
        }

        void IInvokeProvider.Invoke()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            GridViewColumnHeader owner = (GridViewColumnHeader)Owner;
            owner.AutomationClick();
        }

        #region ITransformProvider

        bool ITransformProvider.CanMove { get { return false; } }

        //Note: CanResize can be false if Max/MinWidth,Height has been added on GridViewColumn/ColumnHeader
        bool ITransformProvider.CanResize { get { return true; } }
        bool ITransformProvider.CanRotate { get { return false; } }

        //Note: Don't support Move so far, if users do need this feature to reorder columns, 
        //we can consider to add it later. (One concern is GVCH doesn't support reorder by moving itself)
        void ITransformProvider.Move(double x, double y)
        {
            throw new InvalidOperationException(SR.Get(SRID.UIA_OperationCannotBePerformed));
        }

        void ITransformProvider.Resize(double width, double height)
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            if (width < 0)
            {
                throw new ArgumentOutOfRangeException("width");
            }
            if (height < 0)
            {
                throw new ArgumentOutOfRangeException("height");
            }

            GridViewColumnHeader header = Owner as GridViewColumnHeader;
            if (header != null)
            {
                if (header.Column != null)
                {
                    header.Column.Width = width;
                }

                header.Height = height;
            }
        }

        void ITransformProvider.Rotate(double degrees)
        {
            throw new InvalidOperationException(SR.Get(SRID.UIA_OperationCannotBePerformed));
        }

        #endregion
    }
}
