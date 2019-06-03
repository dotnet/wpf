// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: AutomationPeer associated with FlowDocumentScrollViewer.
//

using System.Collections.Generic;           // List<T>
using System.Windows.Automation.Provider;   // IScrollProvider
using System.Windows.Controls;              // FlowDocumentScrollViewer
using System.Windows.Documents;             // FlowDocument
using MS.Internal.Documents;                // IFlowDocumentViewer


namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer associated with FlowDocumentScrollViewer.
    /// </summary>
    public class FlowDocumentScrollViewerAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">Owner of the AutomationPeer.</param>
        public FlowDocumentScrollViewerAutomationPeer(FlowDocumentScrollViewer owner)
            : base(owner)
        { }

        /// <summary>
        /// <see cref="AutomationPeer.GetPattern"/>
        /// </summary>
        public override object GetPattern(PatternInterface patternInterface)
        {
            object returnValue = null;

            if (patternInterface == PatternInterface.Scroll)
            {
                FlowDocumentScrollViewer owner = (FlowDocumentScrollViewer)Owner;
                if (owner.ScrollViewer != null)
                {
                    AutomationPeer scrollPeer = UIElementAutomationPeer.CreatePeerForElement(owner.ScrollViewer);
                    if (scrollPeer != null && scrollPeer is IScrollProvider)
                    {
                        scrollPeer.EventsSource = this;
                        returnValue = scrollPeer;
                    }
                }
            }
            else if (patternInterface == PatternInterface.Text)
            {
                // Make sure that Automation children are created.
                this.GetChildren();

                // Re-expose TextPattern from hosted document.
                if (_documentPeer != null)
                {
                    _documentPeer.EventsSource = this;
                    returnValue = _documentPeer.GetPattern(patternInterface);
                }
            }
            else if (patternInterface == PatternInterface.SynchronizedInput)
            {
                returnValue = base.GetPattern(patternInterface);
            }

            return returnValue;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetChildrenCore"/>
        /// </summary>
        /// <remarks>
        /// AutomationPeer associated with FlowDocumentScrollViewer returns an AutomationPeer
        /// for hosted Document and for elements in the style.
        /// </remarks>
        protected override List<AutomationPeer> GetChildrenCore()
        {
            // Get children for all elements in the style.
            List<AutomationPeer> children = base.GetChildrenCore();

            // If the owner is IFlowDocumentViewer, it means that it is embedded inside
            // FlowDocumentReaer. In this case DocumentAutumationPeer is already exposed.
            // Hence there is no need to add it to children collection.
            if (!(Owner is IFlowDocumentViewer))
            {
                // Add AutomationPeer associated with the document.
                // Make it the first child of the collection.
                FlowDocument document = ((FlowDocumentScrollViewer)Owner).Document;
                if (document != null)
                {
                    AutomationPeer documentPeer = ContentElementAutomationPeer.CreatePeerForElement(document);
                    if (_documentPeer != documentPeer)
                    {
                        if (_documentPeer != null)
                        {
                            _documentPeer.OnDisconnected();
                        }
                        _documentPeer = documentPeer as DocumentAutomationPeer;
                    }
                    if (documentPeer != null)
                    {
                        if (children == null)
                        {
                            children = new List<AutomationPeer>();
                        }
                        children.Add(documentPeer);
                    }
                }
            }

            return children;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetAutomationControlTypeCore"/>
        /// </summary>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Document;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetClassNameCore"/>
        /// </summary>
        protected override string GetClassNameCore()
        {
            return "FlowDocumentScrollViewer";
        }

        private DocumentAutomationPeer _documentPeer;
    }
}
