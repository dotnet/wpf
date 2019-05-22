// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

using MS.Internal;
using MS.Win32;

namespace System.Windows.Automation.Peers
{
    ///
    public class ProgressBarAutomationPeer : RangeBaseAutomationPeer, IRangeValueProvider
    {
        ///
        public ProgressBarAutomationPeer(ProgressBar owner): base(owner)
        {
        }

        ///
        override protected string GetClassNameCore()
        {
            return "ProgressBar";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ProgressBar;
        }

        ///
        override public object GetPattern(PatternInterface patternInterface)
        {
            // Indeterminate ProgressBar should not support RangeValue pattern
            if (patternInterface == PatternInterface.RangeValue && ((ProgressBar)Owner).IsIndeterminate)
                return null;

            return base.GetPattern(patternInterface);
        }

        /// <summary>
        /// Request to set the value that this UI element is representing
        /// </summary>
        /// <param name="val">Value to set the UI to, as an object</param>
        /// <returns>true if the UI element was successfully set to the specified value</returns>
        //[CodeAnalysis("AptcaMethodsShouldOnlyCallAptcaMethods")] //Tracking Bug: 29647
        void IRangeValueProvider.SetValue(double val)
        {
            throw new InvalidOperationException(SR.Get(SRID.ProgressBarReadOnly));
        }

        ///<summary>Indicates that the value can only be read, not modified.
        ///returns True if the control is read-only</summary>
        bool IRangeValueProvider.IsReadOnly
        {
            get
            {
                return true;
            }
        }

        ///<summary>Value of a Large Change</summary>
        double IRangeValueProvider.LargeChange
        {
            get
            {
                return double.NaN;
            }
        }

        ///<summary>Value of a Small Change</summary>
        double IRangeValueProvider.SmallChange
        {
            get
            {
                return double.NaN;
            }
        }
    }
}

