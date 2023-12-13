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
    using System.Windows.Controls;
    using System.Windows;
    using System.Windows.Media;

    #endregion Using declarations

    /// <summary>
    ///  A container for controls placed on the ribbon.
    /// </summary>
    public class RibbonContentPresenter : ContentPresenter
    {
        #region Fields

        /// <summary>
        /// Control which is hosted by this ContentPresenter
        /// </summary>
        private FrameworkElement _templateRoot;

        #endregion Fields

        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonContentPresenter class.  
        ///   Here we add a couple callbacks which propagate pseudeinherited proeprties to the child control.
        /// </summary>
        static RibbonContentPresenter()
        {
            Type ownerType = typeof(RibbonContentPresenter);

            IsInQuickAccessToolBarProperty.OverrideMetadata(ownerType,
                new FrameworkPropertyMetadata(OnIsInQuickAccessToolBarChanged),
                RibbonControlService.IsInQuickAccessToolBarPropertyKey);

            IsInControlGroupProperty.OverrideMetadata(ownerType,
                new FrameworkPropertyMetadata(OnIsInControlGroupChanged),
                RibbonControlService.IsInControlGroupPropertyKey);

            ControlSizeDefinitionProperty.OverrideMetadata(ownerType,
                new FrameworkPropertyMetadata(OnControlSizeDefinitionChanged, CoerceControlSizeDefinition));

            RibbonControlService.RibbonPropertyKey.OverrideMetadata(ownerType,
                new FrameworkPropertyMetadata(OnRibbonChanged));
        }

        #endregion

        #region PseudoInheritedProperties

        /// <summary>
        ///     DependencyProperty for ControlSizeDefinition property.
        /// </summary>
        public static readonly DependencyProperty ControlSizeDefinitionProperty = 
            RibbonControlService.ControlSizeDefinitionProperty.AddOwner(typeof(RibbonContentPresenter));

        /// <summary>
        ///     Size definition for control hosted by this RibbonContentPresenter. 
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
            RibbonControlService.IsInControlGroupProperty.AddOwner(typeof(RibbonContentPresenter));

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
            RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner(typeof(RibbonContentPresenter));

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

            if (VisualTreeHelper.GetChildrenCount(this) > 0)
            {
                _templateRoot = VisualTreeHelper.GetChild(this, 0) as FrameworkElement;
            }

            TransferPseudoInheritedProperties();
        }

        protected override void OnTemplateChanged(DataTemplate oldTemplate, DataTemplate newTemplate)
        {
            base.OnTemplateChanged(oldTemplate, newTemplate);

            if (oldTemplate != null)
            {
                RibbonHelper.ClearPseudoInheritedProperties(_templateRoot);
                if (_templateRoot != null)
                {
                    // Clearing the Ribbon property value which was set earlier.
                    _templateRoot.ClearValue(RibbonControlService.RibbonPropertyKey);
                }
                _templateRoot = null;
            }
        }

        private static void OnControlSizeDefinitionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonContentPresenter rcp = (RibbonContentPresenter)d;
            rcp.TransferPseudoInheritedProperties();
        }

        private static object CoerceControlSizeDefinition(DependencyObject d, object baseValue)
        {
            return baseValue;
        }

        private static void OnIsInQuickAccessToolBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonContentPresenter rcp = (RibbonContentPresenter)d;
            rcp.TransferPseudoInheritedProperties();
        }

        private static void OnIsInControlGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonContentPresenter rcp = (RibbonContentPresenter)d;
            rcp.TransferPseudoInheritedProperties();
        }

        private static void OnRibbonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RibbonContentPresenter)d).TransferPseudoInheritedProperties();
        }

        private void TransferPseudoInheritedProperties()
        {
            if (_templateRoot != null)
            {
                // Ribbon is an inherited property. In non-MVVM scenarios where
                // controls are directly added under RibbonGroup in XAML, RibbonGroup
                // is the logical parent of those controls and they get the value
                // of RibbonParent set from their logical parent. When a RibbonGroup
                // get collapsed and its template changes, due to a bug in framework
                // the inheritance value of those controls is lost during visual tree
                // change and never again gets updated. The workaround is to set the
                // local value on those controls from RibbonContentPresenter which
                // would be their visual parent. This works because Ribbon property is
                // readonly.
                RibbonControlService.SetRibbon(_templateRoot, RibbonControlService.GetRibbon(this));
                RibbonHelper.TransferPseudoInheritedProperties(this, _templateRoot);
            }
        }

        #endregion PseudoInheritedProperties

        #region Internal Properties

        internal UIElement ContentChild
        {
            get { return _templateRoot; }
        }

        internal bool ChildHasLargeImage
        {
            get { return (_templateRoot != null) ? (RibbonControlService.GetLargeImageSource(_templateRoot) != null) : false; }
        }

        internal bool ChildHasSmallImage
        {
            get { return (_templateRoot != null) ? (RibbonControlService.GetSmallImageSource(_templateRoot) != null) : false; }
        }

        internal bool ChildHasLabel
        {
            get { return (_templateRoot != null) ? !string.IsNullOrEmpty(RibbonControlService.GetLabel(_templateRoot)) : false; }
        }

        #endregion
    }
}
