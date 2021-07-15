// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

using MS.Internal;
using MS.Win32;

namespace System.Windows.Automation.Peers
{
    /// 
    public class ExpanderAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
    {
        ///
        public ExpanderAutomationPeer(Expander owner): base(owner)
        {}

        ///
        override protected string GetClassNameCore()
        {
            return "Expander";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }
        
        /// Go through the children and if we find the templated toggle button set its event source to this AutomationPeer
        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> children = base.GetChildrenCore();
            ToggleButton expanderToggleButton = ((Expander)Owner).ExpanderToggleButton;

            if (!AccessibilitySwitches.UseNetFx47CompatibleAccessibilityFeatures && children != null)
            {
                foreach (UIElementAutomationPeer peer in children)
                {
                    if (peer.Owner == expanderToggleButton)
                    {
                        peer.EventsSource = (!AccessibilitySwitches.UseNetFx472CompatibleAccessibilityFeatures && this.EventsSource != null) ? this.EventsSource : this;
                        break;
                    }
                }
            }

            return children;
        }

        /// The expander should have Automation Keyboard focus whenever the actual focus is set in the toggle button
        override protected bool HasKeyboardFocusCore()
        {
            return ((!AccessibilitySwitches.UseNetFx47CompatibleAccessibilityFeatures && ((Expander)Owner).IsExpanderToggleButtonFocused) 
                    || base.HasKeyboardFocusCore());
        }
        
        ///
        override public object GetPattern(PatternInterface pattern)
        {
            object iface = null;
            
            if(pattern == PatternInterface.ExpandCollapse)
            {
                iface = this;
            }
            else
            {
                iface = base.GetPattern(pattern);
            }

            return iface;
        }

        #region ExpandCollapse
        
        /// <summary>
        /// Blocking method that returns after the element has been expanded.
        /// </summary>
        /// <returns>true if the node was successfully expanded</returns>
        void IExpandCollapseProvider.Expand()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            Expander owner = (Expander)((ExpanderAutomationPeer)this).Owner;
            owner.IsExpanded = true;
        }

        /// <summary>
        /// Blocking method that returns after the element has been collapsed.
        /// </summary>
        /// <returns>true if the node was successfully collapsed</returns>
        void IExpandCollapseProvider.Collapse()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            Expander owner = (Expander)((ExpanderAutomationPeer)this).Owner;
            owner.IsExpanded = false;
        }

        ///<summary>indicates an element's current Collapsed or Expanded state</summary>
        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get
            {
                Expander owner = (Expander)((ExpanderAutomationPeer)this).Owner;
                return owner.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
            }
        }

        // BUG 1555137: Never inline, as we don't want to unnecessarily link the automation DLL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseExpandCollapseAutomationEvent(bool oldValue, bool newValue)
        {
            RaisePropertyChangedEvent(
                ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
        }

        #endregion ExpandCollapse
    }
}


