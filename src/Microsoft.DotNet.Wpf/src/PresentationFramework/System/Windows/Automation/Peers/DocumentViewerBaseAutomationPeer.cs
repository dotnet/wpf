// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: AutomationPeer associated with DocumentViewerBase.
//

using System.Collections.Generic;           // List<T>
using System.Windows.Controls.Primitives;   // DocumentViewerBase
using System.Windows.Documents;             // IDocumentPaginatorSource

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer associated with DocumentViewerBase.
    /// </summary>
    public class DocumentViewerBaseAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">Owner of the AutomationPeer.</param>
        public DocumentViewerBaseAutomationPeer(DocumentViewerBase owner)
            : base(owner)
        { }

        /// <summary>
        /// <see cref="AutomationPeer.GetPattern"/>
        /// </summary>
        public override object GetPattern(PatternInterface patternInterface)
        {
            object returnValue = null;

            if (patternInterface == PatternInterface.Text)
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
            else
            {
                returnValue = base.GetPattern(patternInterface);
            }

            return returnValue;
        }

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

            // Add AutomationPeer associated with the document.
            // Make it the first child of the collection.
            AutomationPeer documentPeer = GetDocumentAutomationPeer();
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
            return "DocumentViewer";
        }

        /// <summary>
        /// Retrieves AutomationPeer for the document.
        /// </summary>
        /// <returns></returns>
        private AutomationPeer GetDocumentAutomationPeer()
        {
            AutomationPeer documentPeer = null;
            IDocumentPaginatorSource document = ((DocumentViewerBase)Owner).Document;
            if (document != null)
            {
                if (document is UIElement)
                {
                    documentPeer = UIElementAutomationPeer.CreatePeerForElement((UIElement)document);
                }
                else if (document is ContentElement)
                {
                    documentPeer = ContentElementAutomationPeer.CreatePeerForElement((ContentElement)document);
                }
            }
            return documentPeer;
        }

        private DocumentAutomationPeer _documentPeer;
    }
}

