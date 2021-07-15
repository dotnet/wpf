// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    #region Using declarations
    
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows;
    using System.Windows.Media;
#if !RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Automation.Peers;
#endif
    
    #endregion Using declarations

    /// <summary>
    ///  A container for controls placed on the ribbon.
    /// </summary>
    [TemplatePart(Name = PART_ContentPresenter, Type = typeof(RibbonContentPresenter))]
    public class RibbonControl : ContentControl
    {
        #region Fields

        private RibbonContentPresenter _partContentPresenter;

        private const string PART_ContentPresenter = "PART_ContentPresenter";

        #endregion Fields

        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonControl class.  Here we override the
        ///   default style, and add a couple callbacks.
        /// </summary>
        static RibbonControl()
        {
            Type ownerType = typeof(RibbonControl);

            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            FocusableProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));

            IsInQuickAccessToolBarProperty.OverrideMetadata(ownerType,
                new FrameworkPropertyMetadata(OnIsInQuickAccessToolBarChanged),
                RibbonControlService.IsInQuickAccessToolBarPropertyKey);

            IsInControlGroupProperty.OverrideMetadata(ownerType,
                new FrameworkPropertyMetadata(OnIsInControlGroupChanged),
                RibbonControlService.IsInControlGroupPropertyKey);

            ControlSizeDefinitionProperty.OverrideMetadata(ownerType,
                new FrameworkPropertyMetadata(OnControlSizeDefinitionChanged, CoerceControlSizeDefinition));
            
             ItemForItemContainerProperty.OverrideMetadata(ownerType,
                new FrameworkPropertyMetadata(OnItemForItemContainerChanged));

#if RIBBON_IN_FRAMEWORK
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideMetadata(typeof(RibbonControl), new FrameworkPropertyMetadata(IsOffscreenBehavior.FromClip));
#endif
        }

        #endregion

        #region PseudoInheritedProperties

        internal static readonly DependencyProperty ItemForItemContainerProperty =
            ItemContainerGenerator.ItemForItemContainerProperty.AddOwner(typeof(RibbonControl));

        /// <summary>
        ///     DependencyProperty for ControlSizeDefinition property.
        /// </summary>
        public static readonly DependencyProperty ControlSizeDefinitionProperty = 
            RibbonControlService.ControlSizeDefinitionProperty.AddOwner(typeof(RibbonControl));

        /// <summary>
        ///     Size definition for control hosted by this RibbonControl. 
        /// </summary>
        public RibbonControlSizeDefinition ControlSizeDefinition
        {
            get { return RibbonControlService.GetControlSizeDefinition(this); }
            set { RibbonControlService.SetControlSizeDefinition(this, value); }
        }

        /// <summary>
        ///     DependencyProperty for IsInControlGroup property.
        /// </summary>
        public static readonly DependencyProperty IsInControlGroupProperty = 
            RibbonControlService.IsInControlGroupProperty.AddOwner(typeof(RibbonControl));

        /// <summary>
        ///     This property indicates whether the control is part of a RibbonControlGroup.
        /// </summary>
        public bool IsInControlGroup
        {
            get { return RibbonControlService.GetIsInControlGroup(this); }
            internal set { RibbonControlService.SetIsInControlGroup(this, value); }
        }
        
        /// <summary>
        ///     DependencyProperty for IsInQuickAccessToolBar property.
        /// </summary>
        public static readonly DependencyProperty IsInQuickAccessToolBarProperty =
            RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner(typeof(RibbonControl));

        /// <summary>
        ///     This property indicates whether the control and it's child is part of a QuickAccessToolBar.
        /// </summary>
        public bool IsInQuickAccessToolBar
        {
            get { return RibbonControlService.GetIsInQuickAccessToolBar(this); }
            internal set { RibbonControlService.SetIsInQuickAccessToolBar(this, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _partContentPresenter = GetTemplateChild(PART_ContentPresenter) as RibbonContentPresenter;
            TransferPseudoInheritedProperties();
        }

        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            base.OnTemplateChanged(oldTemplate, newTemplate);

            if ((oldTemplate != null) && (_partContentPresenter != null))
            {
                RibbonHelper.ClearPseudoInheritedProperties(_partContentPresenter);
                _partContentPresenter = null;
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonControlAutomationPeer(this);
        }

        /// <summary>
        /// Whenever an item is linked to a RibbonControl we want to set the RibbonControl as a controller for the item's 
        /// position in set and size of set, this enables controls set directly under RibbonGroup and RibbonControlGroup to correctly report these properties.
        /// </summary>
        private static void OnItemForItemContainerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                UIElement element = e.OldValue as UIElement;
                if (element != null)
                {
                    element.PositionAndSizeOfSetController = null;
                }
            }
            if (e.NewValue != null)
            {
                UIElement element = e.NewValue as UIElement;
                if (element != null)
                {
                    RibbonControl rc = (RibbonControl)d;
                    element.PositionAndSizeOfSetController = rc;
                }
            }
        }

        private static void OnControlSizeDefinitionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonControl rc = (RibbonControl)d;
            rc.TransferPseudoInheritedProperties();
            RibbonHelper.FixMeasureInvalidationPaths(rc);
        }

        private static object CoerceControlSizeDefinition(DependencyObject d, object baseValue)
        {
            return baseValue;
        }

        private static void OnIsInQuickAccessToolBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonControl rc = (RibbonControl)d;
            rc.TransferPseudoInheritedProperties();
        }

        private static void OnIsInControlGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonControl rc = (RibbonControl)d;
            rc.TransferPseudoInheritedProperties();
        }

        private void TransferPseudoInheritedProperties()
        {
            if (_partContentPresenter != null)
            {
                RibbonHelper.TransferPseudoInheritedProperties(this, _partContentPresenter);
            }
        }

        #endregion PseudoInheritedProperties

        #region Internal Methods

        internal bool HostsRibbonGroup()
        {
            if (_partContentPresenter != null)
            {
                if (VisualTreeHelper.GetChildrenCount(_partContentPresenter) > 0 &&
                   VisualTreeHelper.GetChild(_partContentPresenter, 0) is RibbonGroup)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Internal Properties

        internal UIElement ContentChild
        {
            get { return _partContentPresenter != null ? _partContentPresenter.ContentChild : null; }
        }

        internal bool ChildHasLargeImage
        {
            get { return _partContentPresenter != null ? _partContentPresenter.ChildHasLargeImage : false; }
        }

        internal bool ChildHasSmallImage
        {
            get { return _partContentPresenter != null ? _partContentPresenter.ChildHasSmallImage : false; }
        }

        internal bool ChildHasLabel
        {
            get { return _partContentPresenter != null ? _partContentPresenter.ChildHasLabel : false; }
        }

        #endregion
    }
}
