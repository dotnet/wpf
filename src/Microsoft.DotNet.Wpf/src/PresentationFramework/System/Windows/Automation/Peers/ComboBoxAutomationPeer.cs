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
using System.Windows.Threading;

using MS.Internal;
using MS.Win32;

namespace System.Windows.Automation.Peers
{
    ///
    public class ComboBoxAutomationPeer: SelectorAutomationPeer, IValueProvider, IExpandCollapseProvider
    {
        ///
        public ComboBoxAutomationPeer(ComboBox owner): base(owner)
        {}

        ///
        override protected ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            // Use the same peer as ListBox
            return new ListBoxItemAutomationPeer(item, this);
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ComboBox;
        }

        ///
        override protected string GetClassNameCore()
        {
            return "ComboBox";
        }

        ///
        override public object GetPattern(PatternInterface pattern)
        {
            object iface = null;
            ComboBox owner = (ComboBox)Owner;

            if (pattern == PatternInterface.Value)
            {
                if (owner.IsEditable) iface = this;
            }
            else if(pattern == PatternInterface.ExpandCollapse)
            {
                iface = this;
            }
            else if (pattern == PatternInterface.Scroll && !owner.IsDropDownOpen)
            {
                iface = this;
            }
            else
            {
                iface = base.GetPattern(pattern);
            }

            return iface;
        }

        ///
        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> children = base.GetChildrenCore();

            //  include text box part into children collection
            ComboBox owner = (ComboBox)Owner;
            TextBox textBox = owner.EditableTextBoxSite;
            if (textBox != null)
            {
                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(textBox);
                if (peer != null)
                {
                    if (children == null)
                        children = new List<AutomationPeer>();

                    children.Insert(0, peer);
                }
            }

            return children;
        }

        ///
        protected override void SetFocusCore()
        {
            ComboBox owner = (ComboBox)Owner;
            if (owner.Focusable)
            {
                if (!owner.Focus())
                {
                    //The fox might have went to the TextBox inside Combobox if it is editable.
                    if (owner.IsEditable)
                    {
                        TextBox tb = owner.EditableTextBoxSite;
                        if (tb == null || !tb.IsKeyboardFocused)
                            throw new InvalidOperationException(SR.Get(SRID.SetFocusFailed));
                    }
                    else
                        throw new InvalidOperationException(SR.Get(SRID.SetFocusFailed));
                }
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.SetFocusFailed));
            }
        }

        ///
        internal void ScrollItemIntoView(object item)
        {
            // Item can be scrolled into view only when the Combo is expanded.
            // If combo is not expanded the possible solution is expanding scrolling and 
            // bringing back to original state but that is a breaking change and if there is a strong case can be supported later. 
            if(((IExpandCollapseProvider)this).ExpandCollapseState == ExpandCollapseState.Expanded)
            {
                ComboBox owner = (ComboBox)Owner;
                if (owner.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                {
                    owner.OnBringItemIntoView(item);
                }
                else
                {
                    // The items aren't generated, try at a later time
                    Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new DispatcherOperationCallback(owner.OnBringItemIntoView), item);
                }
            }
        }

        #region Value

        /// <summary>
        /// Request to set the value that this UI element is representing
        /// </summary>
        /// <param name="val">Value to set the UI to, as an object</param>
        /// <returns>true if the UI element was successfully set to the specified value</returns>
        //[CodeAnalysis("AptcaMethodsShouldOnlyCallAptcaMethods")] //Tracking Bug: 29647
        void IValueProvider.SetValue(string val)
        {
            if (val == null)
                throw new ArgumentNullException("val");

            ComboBox owner = (ComboBox)Owner;

            if (!owner.IsEnabled)
                throw new ElementNotEnabledException();

            owner.SetCurrentValueInternal(ComboBox.TextProperty, val);
        }

        ///<summary>Value of a value control, as a a string.</summary>
        string IValueProvider.Value
        {
            get
            {
                return ((ComboBox)(((ComboBoxAutomationPeer)this).Owner)).Text;
            }
        }


        ///<summary>Indicates that the value can only be read, not modified.
        ///returns True if the control is read-only</summary>
        bool IValueProvider.IsReadOnly
        {
            get
            {
                ComboBox owner = (ComboBox)Owner;
                return !owner.IsEnabled || owner.IsReadOnly;
            }
        }

        // BUG 1555137: Never inline, as we don't want to unnecessarily link the automation DLL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseValuePropertyChangedEvent(string oldValue, string newValue)
        {
            if (oldValue != newValue)
            {
                RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValue, newValue);
            }
        } 

        #endregion Value

        #region ExpandCollapse
        /// <summary>
        /// Blocking method that returns after the element has been expanded.
        /// </summary>
        /// <returns>true if the node was successfully expanded</returns>
        void IExpandCollapseProvider.Expand()
        {
            if(!IsEnabled())
                throw new ElementNotEnabledException();

            ComboBox owner = (ComboBox)((ComboBoxAutomationPeer)this).Owner;
            owner.SetCurrentValueInternal(ComboBox.IsDropDownOpenProperty, MS.Internal.KnownBoxes.BooleanBoxes.TrueBox);
        }


        /// <summary>
        /// Blocking method that returns after the element has been collapsed.
        /// </summary>
        /// <returns>true if the node was successfully collapsed</returns>
        void IExpandCollapseProvider.Collapse()
        {
            if(!IsEnabled())
                throw new ElementNotEnabledException();

            ComboBox owner = (ComboBox)((ComboBoxAutomationPeer)this).Owner;
            owner.SetCurrentValueInternal(ComboBox.IsDropDownOpenProperty, MS.Internal.KnownBoxes.BooleanBoxes.FalseBox);
        }


        ///<summary>indicates an element's current Collapsed or Expanded state</summary>
        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get
            {
                ComboBox owner = (ComboBox)((ComboBoxAutomationPeer)this).Owner;
                return owner.IsDropDownOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
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



