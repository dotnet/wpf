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

    using Microsoft.Windows.Controls;
    using System;
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif
    
    #endregion

    /// <summary>
    ///   An automation peer class which automates RibbonSplitButton control.
    /// </summary>
    public class RibbonSplitButtonAutomationPeer : RibbonMenuButtonAutomationPeer, IToggleProvider, IInvokeProvider
    {
        #region Constructors

        /// <summary>
        ///   Initialize Automation Peer for RibbonSplitButton
        /// </summary>
        public RibbonSplitButtonAutomationPeer(RibbonSplitButton owner)
            : base(owner)
        {
        }

        #endregion

        #region AutomationPeer overrides

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.SplitButton;
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Toggle && OwningSplitButton.IsCheckable)
            {
                // When IsCheckable is true, TogglePattern should be used
                return this;
            }
            else if (patternInterface == PatternInterface.Invoke && !OwningSplitButton.IsCheckable)
            {
                // When IsCheckable is false, InvokePattern should be used
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        #endregion

        #region IToggleProvider Members

        void IToggleProvider.Toggle()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            if (!OwningSplitButton.IsCheckable)
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
            }

            if (OwningSplitButton.HeaderButton != null)
            {
                ToggleButtonAutomationPeer headerButtonAutomationPeer = CreatePeerForElement(OwningSplitButton.HeaderButton) as ToggleButtonAutomationPeer;
                if (headerButtonAutomationPeer != null)
                {
                    ((IToggleProvider)headerButtonAutomationPeer).Toggle();
                }
            }
        }

        ToggleState IToggleProvider.ToggleState
        {
            get
            {
                return OwningSplitButton.IsChecked ? ToggleState.On : ToggleState.Off;
            }
        }

        #endregion

        #region IInvokeProvider Members

        void IInvokeProvider.Invoke()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            // When IsCheckable is true, TogglePattern should be used
            if (OwningSplitButton.IsCheckable)
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
            }

            if (OwningSplitButton.HeaderButton != null)
            {
                ButtonAutomationPeer headerButtonAutomationPeer = CreatePeerForElement(OwningSplitButton.HeaderButton) as ButtonAutomationPeer;
                if (headerButtonAutomationPeer != null)
                {
                    ((IInvokeProvider)headerButtonAutomationPeer).Invoke();
                }
            }
        }

        #endregion

        #region Internal Methods

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseToggleStatePropertyChangedEvent(bool oldValue, bool newValue)
        {
            RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, ConvertToToggleState(oldValue), ConvertToToggleState(newValue));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseInvokeAutomationEvent()
        {
            RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
        }

        private static ToggleState ConvertToToggleState(bool value)
        {
            switch (value)
            {
                case (true): return ToggleState.On;
                case (false): return ToggleState.Off;
            }
        }

        #endregion

        #region Private members

        private RibbonSplitButton OwningSplitButton
        {
            get
            {
                return (RibbonSplitButton)Owner;
            }
        }

        #endregion
    }
}
