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

    using System;
    using System.Collections.Generic;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;
    using System.Windows.Automation;
    using Microsoft.Windows.Controls;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    /// <summary>
    ///   An automation peer class which automates RibbonMenuButton control.
    /// </summary>
    public class RibbonMenuButtonAutomationPeer : ItemsControlAutomationPeer, IExpandCollapseProvider, ITransformProvider
    {
        #region Constructors

        /// <summary>
        ///   Initialize Automation Peer for RibbonButton
        /// </summary>
        public RibbonMenuButtonAutomationPeer(RibbonMenuButton owner)
            : base(owner)
        {
        }

        #endregion

        #region AutomationPeer overrides

        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> children = base.GetChildrenCore();

            // Add PartToggleButton to the children collection.  Partially fixes Dev11 42908.
            RibbonMenuButton owner = OwningMenuButton;
            if (owner != null && owner.PartToggleButton != null )
            {
                AutomationPeer peer = CreatePeerForElement(owner.PartToggleButton);
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

        /// <summary>
        ///   Get KeyTip of the owner control.
        /// </summary>
        protected override string GetAccessKeyCore()
        {
            string accessKey = ((RibbonMenuButton)Owner).KeyTip;
            if (string.IsNullOrEmpty(accessKey))
            {
                accessKey = base.GetAccessKeyCore();
            }
            return accessKey;
        }

        /// <summary>
        ///   Return class name for automation clients to display
        /// </summary> 
        protected override string GetClassNameCore()
        {
            return Owner.GetType().Name;
        }

        /// <summary>
        ///   Returns name for automation clients to display
        /// </summary>
        protected override string GetNameCore()
        {
            string name = base.GetNameCore();
            RibbonMenuButton owner = OwningMenuButton;

            if (String.IsNullOrEmpty(name))
            {
                name = owner.Label;
            }

            // Get ToggleButton.Content
            if (String.IsNullOrEmpty(name) && owner.PartToggleButton != null)
            {
                AutomationPeer buttonPeer = UIElementAutomationPeer.CreatePeerForElement(owner.PartToggleButton);
                if (buttonPeer != null)
                {
                    name = buttonPeer.GetName();
                }
            }

            return name;
        }

        /// <summary>
        ///   Returns help text 
        /// </summary>
        protected override string GetHelpTextCore()
        {
            string helpText = base.GetHelpTextCore();
            if (String.IsNullOrEmpty(helpText))
            {
                RibbonToolTip toolTip = ((RibbonMenuButton)Owner).ToolTip as RibbonToolTip;
                if (toolTip != null)
                {
                    helpText = toolTip.Description;
                }
            }

            return helpText;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            // AutomationControlType.MenuBar contains both MenuItems and other controls (RibbonGallery in this case).
            // See http://msdn.microsoft.com/en-us/library/ms752322.aspx
            return AutomationControlType.MenuBar;
        }

        #endregion

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new RibbonMenuItemDataAutomationPeer(item, this);
        }

        public override object GetPattern(PatternInterface patternInterface)
        {

            if (patternInterface == PatternInterface.ExpandCollapse && OwningMenuButton.HasItems)
            {
                return this;
            }
            else if (patternInterface == PatternInterface.Transform)
            {
                if ((OwningMenuButton.CanUserResizeHorizontally || OwningMenuButton.CanUserResizeVertically) 
                    && OwningMenuButton.IsDropDownOpen)
                    return this;
            }

            return base.GetPattern(patternInterface);
        }

        #region IExpandCollapseProvider Members

        void IExpandCollapseProvider.Collapse()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            RibbonMenuButton owner = OwningMenuButton;
            if (!owner.HasItems)
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
            }

            owner.IsDropDownOpen = false;
        }

        void IExpandCollapseProvider.Expand()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            RibbonMenuButton owner = OwningMenuButton;
            if (!owner.HasItems)
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
            }

            owner.IsDropDownOpen = true;
        }

        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get 
            {
                ExpandCollapseState result = ExpandCollapseState.Collapsed;
                RibbonMenuButton owner = OwningMenuButton;
            
                if (!owner.HasItems)
                {
                    result = ExpandCollapseState.LeafNode;
                }
                else if (owner.IsDropDownOpen)
                {
                    result = ExpandCollapseState.Expanded;
                }

                return result;
            }
        }

        #endregion

        #region ITransformProvider Members

        bool ITransformProvider.CanMove
        {
            get { return false; }
        }

        bool ITransformProvider.CanResize
        {
            get 
            {
                return IsEnabled() && (OwningMenuButton.CanUserResizeVertically || OwningMenuButton.CanUserResizeHorizontally);
            }
        }

        bool ITransformProvider.CanRotate
        {
            get { return false; }
        }

        void ITransformProvider.Move(double x, double y)
        {
            throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
        }

        void ITransformProvider.Resize(double width, double height)
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            if (!((ITransformProvider)this).CanResize || width <= 0 || height <= 0)
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));

            if (!OwningMenuButton.ResizePopupInternal(width, height))
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.ResizeParametersNotValid));
            }
        }

        void ITransformProvider.Rotate(double degrees)
        {
            throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
        }

        #endregion

        #region Internal methods

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseExpandCollapseAutomationEvent(bool oldValue, bool newValue)
        {
            RaisePropertyChangedEvent(
                ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
        }

        #endregion

        #region Private members

        private RibbonMenuButton OwningMenuButton
        {
            get
            {
                return (RibbonMenuButton)Owner;
            }
        }

        #endregion
    }
}
