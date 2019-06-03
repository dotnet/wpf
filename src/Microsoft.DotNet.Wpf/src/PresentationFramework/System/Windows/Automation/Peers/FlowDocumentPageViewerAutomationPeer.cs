// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: AutomationPeer associated with FlowDocumentPageViewer.
//

using System.Collections.Generic;           // List<T>
using System.Windows.Controls;              // FlowDocumentPageViewer
using MS.Internal.Documents;                // IFlowDocumentView

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer associated with FlowDocumentPageViewer.
    /// </summary>
    public class FlowDocumentPageViewerAutomationPeer : DocumentViewerBaseAutomationPeer
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">Owner of the AutomationPeer.</param>
        public FlowDocumentPageViewerAutomationPeer(FlowDocumentPageViewer owner)
            : base(owner)
        { }

        /// <summary>
        /// <see cref="AutomationPeer.GetChildrenCore"/>
        /// </summary>
        /// <remarks>
        /// AutomationPeer associated with DocumentViewerBase returns an AutomationPeer
        /// for hosted Document and for elements in the style.
        /// </remarks>
        protected override List<AutomationPeer> GetChildrenCore()
        {
            // Get children for all elements in the style.
            List<AutomationPeer> children = base.GetChildrenCore();

            // If the owner is IFlowDocumentViewer, it means that it is embedded inside
            // FlowDocumentReaer. In this case DocumentAutumationPeer is already exposed.
            // Hence need to remove it from children collection.
            if (Owner is IFlowDocumentViewer && children != null && children.Count > 0)
            {
                if (children[children.Count-1] is DocumentAutomationPeer)
                {
                    children.RemoveAt(children.Count - 1);
                    if (children.Count == 0)
                    {
                        children = null;
                    }
                }
            }

            return children;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetClassNameCore"/>
        /// </summary>
        protected override string GetClassNameCore()
        {
            return "FlowDocumentPageViewer";
        }
    }
}
