// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: AutomationPeer associated with DocumentPageView.
//

using System.Collections.Generic;           // List<T>
using System.Globalization;                 // CultureInfo
using System.Windows.Controls;              // DocumentViewer
using System.Windows.Controls.Primitives;   // DocumentPageView

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// AutomationPeer associated with DocumentPageView.
    /// </summary>
    public class DocumentPageViewAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">Owner of the AutomationPeer.</param>
        public DocumentPageViewAutomationPeer(DocumentPageView owner)
            : base(owner)
        {}

        /// <summary>
        /// <see cref="AutomationPeer.GetChildrenCore"/>
        /// </summary>
        /// <remarks>
        /// AutomationPeer associated with DocumentPageView blocks any exposure 
        /// of the currently hosted page. So it returns empty collection of children.
        /// </remarks>
        protected override List<AutomationPeer> GetChildrenCore()
        {
            return null;
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetAutomationIdCore()"/>
        /// </summary>
        /// <returns>A string representing the current DocumentPageView.</returns>
        protected override string GetAutomationIdCore() 
        {
            // Initialize the result to Empty, so that if Name is not set on the
            // DocumentPageView, and there is no valid PageNumber set, then the
            // AutomationId will remain blank to avoid duplicate entries.
            string result = string.Empty;
            DocumentPageView owner = (DocumentPageView)Owner;

            // Check if a Name is already set on the DocumentPageView, otherwise attempt
            // to construct one.
            if (!string.IsNullOrEmpty(owner.Name))
            {
                result = owner.Name;
            }
            else if ((owner.PageNumber >= 0) && (owner.PageNumber < int.MaxValue))
            {
                // This will set the AutomationId to a string that represents the current
                // page number, i.e. "DocumentPage1" will represent the first page.  These numbers
                // will be kept in a 1-indexed format.  InvariantCulture is used to ensure
                // that these AutomationIds will not change with the language, so that they
                // can be trusted to always work in automation.
                result = String.Format(CultureInfo.InvariantCulture, "DocumentPage{0}", owner.PageNumber + 1);
            }

            return result;
        }
    }
}

