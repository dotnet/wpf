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
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
#if !RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Automation.Peers;
#endif
    
    #endregion

    /// <summary>
    ///   Groups a set of Ribbon controls into a visual and conceptual unit.
    /// </summary>
    public class RibbonControlGroup : ItemsControl
    {
        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonControlGroup class.
        /// </summary>
        static RibbonControlGroup()
        {
            Type ownerType = typeof(RibbonControlGroup);

            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            FocusableProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));

            FrameworkElementFactory fef = new FrameworkElementFactory(typeof(StackPanel));
            fef.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            ItemsPanelTemplate template = new ItemsPanelTemplate(fef);
            template.Seal();
            ItemsPanelProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(template));

            RibbonControlService.DefaultControlSizeDefinitionProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(CoerceDefaultControlSizeDefinition)));
        }

        #endregion Constructors

        #region Protected Methods

        public override void OnApplyTemplate()
        {
            CoerceValue(ControlSizeDefinitionProperty);
            base.OnApplyTemplate();
        }

        #endregion

        #region Resizing

        /// <summary>
        ///     DependencyProperty for ControlSizeDefinition property.
        /// </summary>
        public static readonly DependencyProperty ControlSizeDefinitionProperty =
            RibbonControlService.ControlSizeDefinitionProperty.AddOwner(typeof(RibbonControlGroup), new FrameworkPropertyMetadata(OnControlSizeDefinitionChanged));

        /// <summary>
        ///     Size definition for controls contained within this RibbonControlGroup. 
        /// </summary>
        public RibbonControlSizeDefinition ControlSizeDefinition
        {
            get { return RibbonControlService.GetControlSizeDefinition(this); }
            set { RibbonControlService.SetControlSizeDefinition(this, value); }
        }

        private static void OnControlSizeDefinitionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonControlGroup rcg = (RibbonControlGroup)d;
            rcg.TransferPseudoInheritedProperties();
        }

        private void TransferPseudoInheritedProperties()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                DependencyObject child = ItemContainerGenerator.ContainerFromIndex(i);
                if (child != null)
                {
                    RibbonHelper.TransferPseudoInheritedProperties(this, child);
                }
            }
        }

        private static object CoerceDefaultControlSizeDefinition(DependencyObject d, object baseValue)
        {
            if (baseValue == null)
            {
                RibbonControlGroup controlGroup = (RibbonControlGroup)d;
                RibbonImageSize imageSize = RibbonImageSize.Collapsed;
                bool isLabelVisible = false;
                int itemCount = controlGroup.Items.Count;
                bool childFound = false;

                // Get the largest ControlSizeDefinition variant for all
                // the child controls and construct a union ControlSizeDefinition.
                for (int i = 0; i < itemCount; i++)
                {
                    RibbonControl ribbonControl = controlGroup.ItemContainerGenerator.ContainerFromIndex(i) as RibbonControl;
                    if (ribbonControl != null && ribbonControl.Visibility != Visibility.Collapsed)
                    {
                        UIElement contentChild = ribbonControl.ContentChild;
                        if (contentChild != null && contentChild.Visibility != Visibility.Collapsed)
                        {
                            RibbonControlSizeDefinition currentLargeCsd = RibbonControlService.GetDefaultControlSizeDefinition(contentChild);
                            if (currentLargeCsd == null)
                            {
                                contentChild.CoerceValue(RibbonControlService.DefaultControlSizeDefinitionProperty);
                                currentLargeCsd = RibbonControlService.GetDefaultControlSizeDefinition(contentChild);
                            }

                            if (currentLargeCsd != null)
                            {
                                childFound = true;
                                if (imageSize == RibbonImageSize.Collapsed)
                                {
                                    imageSize = currentLargeCsd.ImageSize;
                                }
                                else if (currentLargeCsd.ImageSize == RibbonImageSize.Large)
                                {
                                    imageSize = RibbonImageSize.Large;
                                }

                                isLabelVisible |= currentLargeCsd.IsLabelVisible;

                                if (isLabelVisible && imageSize == RibbonImageSize.Large)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                if (childFound)
                {
                    return RibbonControlSizeDefinition.GetFrozenControlSizeDefinition(imageSize, isLabelVisible);
                }
            }
            return baseValue;
        }

        #endregion Resizing

        #region Container Generation

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new RibbonControl();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is RibbonControl;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            RibbonControl ribbonControl = (RibbonControl)element;
            ribbonControl.IsInControlGroup = true;
            ribbonControl.ControlSizeDefinition = this.ControlSizeDefinition;
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);

            RibbonHelper.ClearPseudoInheritedProperties(element);
        }

        #endregion Container Generation

        #region VisualStates

        /// <summary>
        ///     DependencyProperty for Ribbon property.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty =
            RibbonControlService.RibbonProperty.AddOwner(typeof(RibbonControlGroup));

        /// <summary>
        ///     This property is used to access visual style brushes defined on the Ribbon class.
        /// </summary>
        public Ribbon Ribbon
        {
            get { return RibbonControlService.GetRibbon(this); }
        }

        #endregion VisualStates

        #region UI Automation

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RibbonControlGroupAutomationPeer(this);
        }

        #endregion
    }
}
