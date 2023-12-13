// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System;
using System.Windows;
using System.Windows.Data;
#if !RIBBON_IN_FRAMEWORK
using System.Windows.Controls;
#endif

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    /// <summary>
    ///   The filter menu button in a RibbonGallery has a different default Style (with a much different Template) than
    ///   a normal RibbonMenuButton.  Therefore, we've decided to create a new class for it so that we can specify our
    ///   Style as the default.
    ///
    ///   This way, if the user specifies RibbonGallery.FilterMenuButtonStyle, the supplied Style is merged with our
    ///   default Style instead of throwing it away.  Thus, app authors don't have to retemplate the filter menu button
    ///   if they just want to restyle a few properties.
    /// </summary>
    [TemplatePart(Name = CurrentFilterItemTemplatePartName, Type = typeof(ContentPresenter))]
    public class RibbonFilterMenuButton : RibbonMenuButton
    {
        #region Constructor
  
        /// <summary>
        ///   Initializes static members of the RibbonFilterMenuButton class.  Here we override the default Style.
        /// </summary>
        static RibbonFilterMenuButton()
        {
            Type ownerType = typeof(RibbonFilterMenuButton);
            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));
        }
        
        #endregion

        #region Template
        
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            RibbonToggleButton filterToggleButton = this.Template.FindName(RibbonMenuButton.ToggleButtonTemplatePartName, this) as RibbonToggleButton;

            if (filterToggleButton != null)
            {
                filterToggleButton.Loaded += new RoutedEventHandler(OnFilterToggleButtonLoaded);
            }
        }
        
        // We must set up bindings so that the current filter, a separate RibbonMenuItem hosted in
        // a RibbonToggleButton, displays the same way as the filters themselves, which are RibbonMenuItems.
        private void OnFilterToggleButtonLoaded(object sender, RoutedEventArgs e)
        {
            RibbonToggleButton filterToggleButton = (RibbonToggleButton)sender;
#if RIBBON_IN_FRAMEWORK
            _currentFilterItem = filterToggleButton.GetTemplateChild(CurrentFilterItemTemplatePartName) as RibbonMenuItem;
#else
            _currentFilterItem = filterToggleButton.FindName(CurrentFilterItemTemplatePartName) as RibbonMenuItem;
#endif

            // It's possible, e.g. in the InRibbonGallery case, for filterToggleButton.Loaded to
            // fire before its template is applied.  filterToggleButton.Loaded will fire again
            // after its template is applied.  Therefore, only treat this as handled and remove
            // our handler if filterToggleButton's template is available.
            if (_currentFilterItem != null)
            {
                filterToggleButton.Loaded -= new RoutedEventHandler(OnFilterToggleButtonLoaded);

                RibbonGallery parentGallery = this.TemplatedParent as RibbonGallery;
                if (parentGallery != null)
                {
                    Binding currentFilterBinding = new Binding("CurrentFilter") { Source = parentGallery };
                    filterToggleButton.SetBinding(RibbonToggleButton.ContentProperty, currentFilterBinding);
                    
                    _currentFilterItem.SetBinding(RibbonMenuItem.DataContextProperty, currentFilterBinding);
                    Binding currentFilterStyleBinding = new Binding("CurrentFilterStyle") { Source = parentGallery };
                    _currentFilterItem.SetBinding(RibbonMenuItem.StyleProperty, currentFilterStyleBinding);
                    parentGallery.SetHeaderBindingForCurrentFilterItem();
                    parentGallery.SetTemplateBindingForCurrentFilterItem();
                }
            }
        }

        #endregion

        #region DismissPopup

        bool _retainFocusOnDismiss = false;
        internal override void OnIsDropDownOpenChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsDropDownOpenChanged(e);
            if (IsDropDownOpen)
            {
                _retainFocusOnDismiss = RibbonHelper.IsKeyboardMostRecentInputDevice();
            }
        }

        internal override void OnAnyMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnAnyMouseDown(e);
            _retainFocusOnDismiss = false;
        }

        protected override void OnDismissPopup(RibbonDismissPopupEventArgs e)
        {
            base.OnDismissPopup(e);

            if (e.DismissMode == RibbonDismissPopupMode.Always)
            {
                // DismissPopup in RibbonFilterMenuButton shouldn't dismiss the parent Popup.
                // Retain focus on self if needed.
                if (_retainFocusOnDismiss)
                {
                    Focus();
                    _retainFocusOnDismiss = false;
                }
                e.Handled = true;
            }
        }

        #endregion

        #region KeyTips

        protected override void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter;
                e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetCenter;
                e.KeyTipHorizontalOffset = e.KeyTipVerticalOffset = 0;
            }
        }

        #endregion

        #region Data

        internal RibbonMenuItem CurrentFilterItem
        {
            get
            {
                return _currentFilterItem;
            }
        }

        private const string CurrentFilterItemTemplatePartName = "PART_CurrentFilterItem";
        private RibbonMenuItem _currentFilterItem;

        #endregion Data

    }
}
