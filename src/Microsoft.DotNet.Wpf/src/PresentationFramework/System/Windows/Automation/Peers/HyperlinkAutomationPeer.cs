// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Automation peer for hyperlink
//

using System.Windows.Automation.Provider;   // IRawElementProviderSimple
using System.Windows.Documents;

namespace System.Windows.Automation.Peers
{
    ///
    public class HyperlinkAutomationPeer : TextElementAutomationPeer, IInvokeProvider
    {
        ///
        public HyperlinkAutomationPeer(Hyperlink owner)
            : base(owner)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="patternInterface"></param>
        /// <returns></returns>
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Invoke)
            {
                return this;
            }
            else
            {
                return base.GetPattern(patternInterface);
            }
        }

        //Default Automation properties
        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Hyperlink;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override string GetNameCore()
        {
            string name = base.GetNameCore();

            if (name.Length == 0)
            {
                Hyperlink owner = (Hyperlink)Owner;

                if (owner.Text != null)
                    name = owner.Text;
            }

            return name;
        }

        ///
        override protected string GetClassNameCore()
        {
            return "Hyperlink";
        }

        /// <summary>
        /// <see cref="AutomationPeer.IsControlElementCore"/>
        /// </summary>
        override protected bool IsControlElementCore()
        {
            // We only want this peer to show up in the Control view if it is visible
            // For compat we allow falling back to legacy behavior (returning true always)
            // based on AppContext flags, IncludeInvisibleElementsInControlView evaluates them.
            return IncludeInvisibleElementsInControlView || IsTextViewVisible == true;
        }

        //Invoke Pattern implementation
        void IInvokeProvider.Invoke()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            Hyperlink owner = (Hyperlink)Owner;
            owner.DoClick();
        }
    }
}
