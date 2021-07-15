// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Base class for AutomationPeers associated with TextPattern.
//

using System.Collections.Generic;           // List<T>
using System.Windows.Automation.Provider;   // IRawElementProviderSimple
using System.Windows.Documents;             // ITextPointer
using MS.Internal.Automation;               // EventMap, TextAdaptor

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
        /// This method is called by implementation of the peer to raise the automation "ActiveTextPositionChanged" event
        /// </summary>
        // Never inline, as we don't want to unnecessarily link the automation DLL.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public virtual void RaiseActiveTextPositionChangedEvent(TextPointer rangeStart, TextPointer rangeEnd)
        {
            if (EventMap.HasRegisteredEvent(AutomationEvents.ActiveTextPositionChanged))
            {
                IRawElementProviderSimple provider = ProviderFromPeer(this);
                if (provider != null)
                {
                    ActiveTextPositionChangedEventArgs args = new ActiveTextPositionChangedEventArgs(TextRangeFromTextPointers(rangeStart, rangeEnd));
                    AutomationInteropProvider.RaiseAutomationEvent(
                        AutomationElementIdentifiers.ActiveTextPositionChangedEvent,
                        provider,
                        args);
                }
            }
        }

        /// <summary>
        /// Convert TextPointers to ITextRangeProvider
        /// </summary>
        private ITextRangeProvider TextRangeFromTextPointers(TextPointer rangeStart, TextPointer rangeEnd)
        {
            TextAdaptor textAdaptor = GetPattern(PatternInterface.Text) as TextAdaptor;
            return textAdaptor?.TextRangeFromTextPointers(rangeStart, rangeEnd);
        }

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


