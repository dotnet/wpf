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
    using System.Collections.Generic;
    using System.Windows.Controls;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif
    
    #endregion

    /// <summary>
    ///   An automation peer class which automates RibbonComboBox control.
    /// </summary>
    public class RibbonComboBoxAutomationPeer : RibbonMenuButtonAutomationPeer, IValueProvider
    {
        #region Constructors

        /// <summary>
        ///   Initialize Automation Peer for RibbonComboBox
        /// </summary>
        public RibbonComboBoxAutomationPeer(RibbonComboBox owner)
            : base(owner)
        {
        }

        #endregion

        #region AutomationPeer overrides

        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Value)
            {
                return this;
            }
            return base.GetPattern(patternInterface);
        }

        protected override System.Collections.Generic.List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> children = base.GetChildrenCore();

            // Add TextBox to the children collection
            RibbonComboBox owner = OwningComboBox;
            if (owner != null && owner.IsEditable && owner.EditableTextBoxSite != null)
            {
                AutomationPeer peer = CreatePeerForElement(owner.EditableTextBoxSite);
                if (peer != null)
                {
                    if (children == null)
                    {
                        children = new List<AutomationPeer>(1);
                    }
                    children.Insert(0, peer);
                }
            }

            return children;
        }

        protected override void SetFocusCore()
        {
            RibbonComboBox owner = (RibbonComboBox)Owner;
            if (owner.Focusable)
            {
                if (!owner.Focus())
                {
                    //The focus might have gone to the TextBox inside Combobox if it is editable.
                    if (owner.IsEditable)
                    {
                        TextBox tb = owner.EditableTextBoxSite;
                        if (tb == null || !tb.IsKeyboardFocused)
                            throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.SetFocusFailed));
                    }
                    else
                        throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.SetFocusFailed));
                }
            }
            else
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.SetFocusFailed));
            }
        }

        #endregion
        
        #region IValueProvider Members

        public bool IsReadOnly
        {
            get
            {
                return !OwningComboBox.IsEnabled;
            }
        }

        public void SetValue(string value)
        {
            RibbonComboBox owner = OwningComboBox;
            if (!owner.IsEnabled)
                throw new ElementNotEnabledException();

            if (value == null)
                throw new ArgumentNullException("value");

            owner.Text = value;
        }

        public string Value
        {
            get
            {
                return OwningComboBox.Text;
            }
        }

        #endregion

        #region Internal Methods

        // BUG 1555137: Never inline, as we don't want to unnecessarily link the automation DLL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseValuePropertyChangedEvent(string oldValue, string newValue)
        {
            if (oldValue != newValue)
            {
                RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValue, newValue);
            }
        } 

        #endregion

        #region Private members

        private RibbonComboBox OwningComboBox
        {
            get
            {
                return (RibbonComboBox)Owner;
            }
        }

        #endregion
    }
}
