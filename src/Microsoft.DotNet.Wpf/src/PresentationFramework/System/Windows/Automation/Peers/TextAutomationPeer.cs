// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Base class for AutomationPeers associated with TextPattern.
//

using System.Collections.Generic;           // List<T>
using System.Windows.Automation.Provider;   // IRawElementProviderSimple
using System.Windows.Documents;             // ITextPointer

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// Base class for AutomationPeers associated with TextPattern.
    /// </summary>
    public abstract class TextAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        protected TextAutomationPeer(FrameworkElement owner)
            : base(owner)
        {}

        /// <summary>
        /// GetNameCore will return a value matching (in priority order)
        ///
        /// 1. Automation.Name
        /// 2. GetLabeledBy.Name 
        /// 3. String.Empty 
        /// 
        /// This differs from the base implementation in that we must
        /// never return GetPlainText() .
        /// </summary>
        override protected string GetNameCore()
        {
            string result = AutomationProperties.GetName(this.Owner);

            if (string.IsNullOrEmpty(result))
            {
                AutomationPeer labelAutomationPeer = GetLabeledByCore();
                if (labelAutomationPeer != null)
                {
                    result = labelAutomationPeer.GetName();
                }
            }

            return result ?? string.Empty;
        }

        /// <summary>
        /// Maps AutomationPeer to provider object.
        /// </summary>
        internal new IRawElementProviderSimple ProviderFromPeer(AutomationPeer peer)
        {
            return base.ProviderFromPeer(peer);
        }

        /// <summary>
        /// Maps automation provider to DependencyObject.
        /// </summary>
        internal DependencyObject ElementFromProvider(IRawElementProviderSimple provider)
        {
            DependencyObject element = null;
            AutomationPeer peer = PeerFromProvider(provider);
            if (peer is UIElementAutomationPeer)
            {
                element = ((UIElementAutomationPeer)peer).Owner;
            }
            else if (peer is ContentElementAutomationPeer)
            {
                element = ((ContentElementAutomationPeer)peer).Owner;
            }
            return element;
        }

        /// <summary>
        /// Gets collection of AutomationPeers for given text range.
        /// </summary>
        internal abstract List<AutomationPeer> GetAutomationPeersFromRange(ITextPointer start, ITextPointer end);
    }
}


