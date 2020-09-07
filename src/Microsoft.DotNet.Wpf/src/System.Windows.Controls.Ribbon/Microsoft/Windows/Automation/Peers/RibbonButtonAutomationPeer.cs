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
    using System.Windows.Automation.Peers;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    /// <summary>
    ///   An automation peer class which automates RibbonButton control.
    /// </summary>
    public class RibbonButtonAutomationPeer : ButtonAutomationPeer
    {

        #region Constructors

        /// <summary>
        ///   Initialize Automation Peer for RibbonButton
        /// </summary>
        public RibbonButtonAutomationPeer(RibbonButton owner): base(owner)
        {
        }

        #endregion

        #region AutomationPeer overrides

        /// <summary>
        ///   Get KeyTip of the owner control.
        /// </summary>
        protected override string GetAccessKeyCore()
        {
            string accessKey = ((RibbonButton)Owner).KeyTip;
            if (string.IsNullOrEmpty(accessKey))
            {
                accessKey = base.GetAccessKeyCore();
            }
            return accessKey;
        }

        /// <summary>
        ///   Return class name for automation clients to display
        /// </summary> 
        protected override string GetClassNameCore()
        {
            return "RibbonButton";
        }

        /// <summary>
        ///   Returns name for automation clients to display
        /// </summary>
        protected override string GetNameCore()
        {
            string name = base.GetNameCore();
            if (String.IsNullOrEmpty(name))
            {
                name = ((RibbonButton)Owner).Label;
            }

            return name;
        }

        /// <summary>
        ///   Returns help text 
        /// </summary>
        protected override string GetHelpTextCore()
        {
            string helpText = base.GetHelpTextCore();
            if (String.IsNullOrEmpty(helpText))
            {
                RibbonToolTip toolTip = ((RibbonButton)Owner).ToolTip as RibbonToolTip;
                if (toolTip != null)
                {
                    helpText = toolTip.Description;
                }
            }

            return helpText;
        }

        #endregion
    }
}
