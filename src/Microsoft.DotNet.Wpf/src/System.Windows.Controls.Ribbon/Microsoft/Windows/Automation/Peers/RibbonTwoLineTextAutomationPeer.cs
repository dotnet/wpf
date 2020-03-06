// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Automation.Peers
#else
namespace Microsoft.Windows.Automation.Peers
#endif
{

    #region Using declarations

    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Automation.Peers;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    /// <summary>
    ///   An automation peer class which automates the RibbonTwoLineText control.
    /// </summary>
    public class RibbonTwoLineTextAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>
        ///   Initialize automation peer for RibbonTwoLineText.
        /// </summary>
        public RibbonTwoLineTextAutomationPeer(RibbonTwoLineText owner) : base(owner)
        { }

        /// <summary>
        ///   Return class name for automation clients to display
        /// </summary> 
        protected override string GetClassNameCore()
        {
            return Owner.GetType().Name;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Text;
        }

        protected override bool IsControlElementCore()
        {
            // Return false if RibbonTwoLineText is part of a ControlTemplate, otherwise return the base method
            RibbonTwoLineText tlt = (RibbonTwoLineText)Owner;
            DependencyObject templatedParent = tlt.TemplatedParent;
            // If the templatedParent is a ContentPresenter, this RibbonTwoLineText is generated from a DataTemplate
            if (templatedParent == null || templatedParent is ContentPresenter)
            {
                return base.IsControlElementCore();
            }

            return false;
        }

        /// <summary>
        ///   Returns name for automation clients to display
        /// </summary>
        protected override string GetNameCore()
        {
            string name = base.GetNameCore();
            if (String.IsNullOrEmpty(name))
            {
                name = ((RibbonTwoLineText)Owner).Text;
            }

            return name;
        }

    }
}
