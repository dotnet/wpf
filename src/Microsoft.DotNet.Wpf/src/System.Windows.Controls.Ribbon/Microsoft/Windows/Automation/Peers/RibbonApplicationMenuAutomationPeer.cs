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

    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Automation.Peers;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    /// <summary>
    ///   An automation peer class which automates RibbonApplicationMenu control.
    /// </summary>
    public class RibbonApplicationMenuAutomationPeer : RibbonMenuButtonAutomationPeer
    {
        #region Constructors

        /// <summary>
        ///   Initialize Automation Peer for RibbonApplicationMenu
        /// </summary>
        public RibbonApplicationMenuAutomationPeer(RibbonApplicationMenu owner)
            : base(owner)
        {
        }

        #endregion

        #region AutomationPeer overrides

        /// <summary>
        ///  Creates peers for AuxiliaryPaneContent and FooterPanecontent and add them to the 
        ///  collection of children peers.
        /// </summary>
        /// <returns></returns>
        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> children = base.GetChildrenCore();
            RibbonApplicationMenu menu = Owner as RibbonApplicationMenu; 
            UIElement element = menu.FooterPaneHost;
            if (element != null)
            {
                if (children == null)
                {
                    children = new List<AutomationPeer>();
                }
                children.Add(RibbonHelper.CreatePeer(element));
            }

            element = menu.AuxiliaryPaneHost;
            if (element != null)
            {
                if (children == null)
                {
                    children = new List<AutomationPeer>();
                }
                children.Add(RibbonHelper.CreatePeer(element));
            }

            return children;
        }

        #endregion

    }
}
