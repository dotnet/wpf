// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: AutomationPeer associated with FrameworkContnetElement.
//

using System.Windows.Markup;                // DefinitionProperties
using System.Windows.Controls;              // Label

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer associated with FrameworkContentElement.
    /// </summary>
    public class FrameworkContentElementAutomationPeer : ContentElementAutomationPeer
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">Owner of the AutomationPeer.</param>
        public FrameworkContentElementAutomationPeer(FrameworkContentElement owner)
            : base(owner)
        { }

        /// <summary>
        /// <see cref="AutomationPeer.GetAutomationIdCore"/>
        /// </summary>
        protected override string GetAutomationIdCore()
        {
            //  1. fetch AutomationProperties.AutomationIdProperty
            string result = base.GetAutomationIdCore();

            if (string.IsNullOrEmpty(result))
            {
                //  2. fetch x:Uid
                // Uid's do not appear on content elements.
                // result = DefinitionProperties.GetUid(Owner);

                if (string.IsNullOrEmpty(result))
                {
                    //  3. fetch FrameworkElement.NameProperty
                    result = ((FrameworkContentElement)Owner).Name;
                }
            }

            return result == null ? string.Empty : result;
        }

        ///
        protected override string GetHelpTextCore()
        {
            string result = base.GetHelpTextCore();
            if (string.IsNullOrEmpty(result))
            {
                object toolTip = ((FrameworkContentElement)Owner).ToolTip;
                if (toolTip != null)
                {
                    result = toolTip as string;
                    if (string.IsNullOrEmpty(result))
                    {
                        FrameworkElement toolTipElement = toolTip as FrameworkElement;
                        if (toolTipElement != null)
                            result = toolTipElement.GetPlainText();
                    }
                }
            }
            return result ?? String.Empty;
        }

        ///
        override protected AutomationPeer GetLabeledByCore()
        {
            AutomationPeer labelPeer = base.GetLabeledByCore();
            if (labelPeer == null)
            {
                Label label = Label.GetLabeledBy(Owner);
                if (label != null)
                    return label.GetAutomationPeer();
            }

            return labelPeer;
        }
    }
}
