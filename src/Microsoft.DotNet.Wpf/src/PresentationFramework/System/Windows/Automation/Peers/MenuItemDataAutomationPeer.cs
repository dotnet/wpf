// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Automation.Provider;


namespace System.Windows.Automation.Peers
{
    public class MenuItemDataAutomationPeer : ItemAutomationPeer, IExpandCollapseProvider, IInvokeProvider, IToggleProvider
    {
        #region Constructors

        public MenuItemDataAutomationPeer(object item, ItemsControlAutomationPeer itemsControlPeer)
            : base(item, itemsControlPeer)
        {
        }

        #endregion

        #region AutomationPeer overrides

        protected override string GetClassNameCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetClassName();
            }

            return "MenuItem";
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetAutomationControlType();
            }

            return AutomationControlType.MenuItem;
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            switch (patternInterface)
            {
                case PatternInterface.ExpandCollapse:
                case PatternInterface.Invoke:
                case PatternInterface.Toggle:
                    return this;
            }        

            return base.GetPattern(patternInterface);
        }

        private void EnsureEnabled()
        {

            FrameworkElementAutomationPeer itemsControllerPeer = GetItemsControlAutomationPeer();
            if (!itemsControllerPeer.IsEnabled())
            {
                throw new ElementNotEnabledException();
            }
        }

        #endregion

        #region IExpandCollapseProvider Members

        void IExpandCollapseProvider.Expand()
        {
            MenuItemAutomationPeer wrapperPeer = GetWrapperPeer() as MenuItemAutomationPeer;
            if (wrapperPeer != null)
            {
                IExpandCollapseProvider expandCollapseProvider = wrapperPeer as IExpandCollapseProvider;
                expandCollapseProvider.Expand();
            }
            ThrowElementNotAvailableException();
        }

        void IExpandCollapseProvider.Collapse()
        {
            MenuItemAutomationPeer wrapperPeer = GetWrapperPeer() as MenuItemAutomationPeer;
            if (wrapperPeer != null)
            {
                IExpandCollapseProvider expandCollapseProvider = wrapperPeer as IExpandCollapseProvider;
                expandCollapseProvider.Collapse();
            }
            ThrowElementNotAvailableException();
        }

        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get
            {
                MenuItemAutomationPeer wrapperPeer = GetWrapperPeer() as MenuItemAutomationPeer;
                if (wrapperPeer != null)
                {
                    IExpandCollapseProvider expandCollapseProvider = wrapperPeer as IExpandCollapseProvider;
                    return expandCollapseProvider.ExpandCollapseState;
                }
                ThrowElementNotAvailableException();
                return ExpandCollapseState.LeafNode;
            }
        }

        #endregion

        #region IInvokeProvider Members

        void IInvokeProvider.Invoke()
        {
            // check if enabled
            EnsureEnabled();

            MenuItemAutomationPeer wrapperPeer = GetWrapperPeer() as MenuItemAutomationPeer;
            if (wrapperPeer != null)
            {
                IInvokeProvider invokeProvider = wrapperPeer as IInvokeProvider;
                invokeProvider.Invoke();
            }
            else
            {
                ThrowElementNotAvailableException();
            }
        }

        #endregion

        #region IToggleProvider Members

        void IToggleProvider.Toggle()
        {
            MenuItemAutomationPeer wrapperPeer = GetWrapperPeer() as MenuItemAutomationPeer;
            if (wrapperPeer != null)
            {
                IToggleProvider toggleProvider = wrapperPeer as IToggleProvider;
                toggleProvider.Toggle();
            }
            else
            {
                ThrowElementNotAvailableException();
            }
        }

        ToggleState IToggleProvider.ToggleState
        {
            get
            {
                MenuItemAutomationPeer wrapperPeer = GetWrapperPeer() as MenuItemAutomationPeer;
                if (wrapperPeer != null)
                {
                    IToggleProvider toggleProvider = wrapperPeer as IToggleProvider;
                    return toggleProvider.ToggleState;
                }
                ThrowElementNotAvailableException();
                return ToggleState.Indeterminate;
            }
        }

        #endregion
    }
}
