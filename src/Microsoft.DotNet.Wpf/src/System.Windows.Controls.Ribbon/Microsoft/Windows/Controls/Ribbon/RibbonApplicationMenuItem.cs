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
    using System.Windows.Controls;
    using System.Windows.Input;
#if RIBBON_IN_FRAMEWORK
    using Microsoft.Windows.Controls;
#endif

    #endregion

    /// <summary>
    ///   Implements a MenuItem in the Ribbon's ApplicationMenu.
    /// </summary>
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(RibbonApplicationMenuItem))]
    public class RibbonApplicationMenuItem : RibbonMenuItem
    {
        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonApplicationMenuItem class.  This also
        ///   overrides the default style, the Command PropertyChangedCallback, and the default
        ///   RibbonControlSizeDefintion for a MenuItem.
        /// </summary>
        static RibbonApplicationMenuItem()
        {
            Type ownerType = typeof(RibbonApplicationMenuItem);

            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
            IsSubmenuOpenProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(null, new CoerceValueCallback(CoerceIsSubmenuOpen)));
        }

        #endregion
        
        #region ContainerGeneration

        private object _currentItem;

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            bool ret = (item is RibbonApplicationMenuItem) || (item is RibbonApplicationSplitMenuItem) || (item is RibbonSeparator) || (item is RibbonGallery);
            if (!ret)
            {
                _currentItem = item;
            }

            return ret;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            object currentItem = _currentItem;
            _currentItem = null;

            if (UsesItemContainerTemplate)
            {
                DataTemplate itemContainerTemplate = ItemContainerTemplateSelector.SelectTemplate(currentItem, this);
                if (itemContainerTemplate != null)
                {
                    object itemContainer = itemContainerTemplate.LoadContent();
                    if (itemContainer is RibbonApplicationMenuItem || itemContainer is RibbonApplicationSplitMenuItem || itemContainer is RibbonSeparator || itemContainer is RibbonGallery)
                    {
                        return itemContainer as DependencyObject;
                    }
                    else
                    {
                        throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.InvalidApplicationMenuOrItemContainer, this.GetType().Name, itemContainer));
                    }
                }
            }

            return new RibbonApplicationMenuItem();
        }

        protected override bool ShouldApplyItemContainerStyle(DependencyObject container, object item)
        {
            if (container is RibbonApplicationSplitMenuItem ||
                container is RibbonSeparator ||
                container is RibbonGallery)
            {
                return false;
            }
            else
            {
                return base.ShouldApplyItemContainerStyle(container, item);
            }
        }

        /// <summary>
        ///  Called when the container is being attached to the parent ItemsControl
        /// </summary>
        /// <param name="element"></param>
        /// <param name="item"></param>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            RibbonHelper.SetApplicationMenuLevel(this.Level == RibbonApplicationMenuItemLevel.Top, element);
        }

        #endregion ContainerGeneration

        #region Popup Placement

        /// <summary>
        ///   Gets the parent ItemsControl for this MenuItem.
        /// </summary>
        private ItemsControl ParentItemsControl
        {
            get { return ItemsControl.ItemsControlFromItemContainer(this); }
        }

        /// <summary>
        ///   Invoked whenever the control's template is applied.
        /// </summary>
        public override void OnApplyTemplate()
        {
            if (Level == RibbonApplicationMenuItemLevel.Top)
            {
                RibbonHelper.UnhookPopupForTopLevelMenuItem(this);
            }

            base.OnApplyTemplate();

            if (Level == RibbonApplicationMenuItemLevel.Top)
            {
                // Bind properties such as PlacementTarget, Height and 
                // Width for the submenu Popup to the parent 
                // RibbonApplicationMenu's SubmenuPlaceholder element.

                RibbonHelper.HookPopupForTopLevelMenuItem(this, ParentItemsControl);
            }
        }

        // This is a fix for Dev10 bug# 908460. The issue there is that auxiliary pane shows momentarily 
        // when switching between two top level MenuItems within the RibbonApplicationMenu. This gives 
        // the perception of a flicker. The solution to this is to delay the close of the old Popup until 
        // after the new Popup has been shown. This property yeilds the buffer time component for the 
        // CloseSubmenuTimer's interval. This component is a non-zero value only for a top level 
        // RibbonApplicationMenuItem or RibbonApplicationSplitMenuItem.

        internal override int CloseSubmenuTimerDelayBuffer
        {
            get
            {
                if (Level == RibbonApplicationMenuItemLevel.Top && CanOpenSubMenu)
                {
                    return RibbonApplicationMenuItem.CloseSubmenuTimerDelay;
                }

                return base.CloseSubmenuTimerDelayBuffer;
            }
        }

        // The above mentioned solution for Dev10 bug# 908460 needed another supporting change. 
        // The base class for RibbonApplicationMenu viz. MenuBase woudn't wait for the 
        // CloseSubmenuTimer to elapse but instead forcibly closed the first Popup as soon as 
        // the second one was about to show. And this happened when the IsSubmenuOpen property 
        // on the first Popup was being turned off. So in order to counter this behavior, we 
        // now coerce the IsSubenuOpen property for top level RibbonApplicationMenuItems and 
        // RibbonApplicationSplitMenuItems whenever the timer is running and the current 
        // selection has moved to another MenuItem with a submenu.

        private static object CoerceIsSubmenuOpen(DependencyObject d, object baseValue)
        {
            RibbonApplicationMenuItem menuItem = (RibbonApplicationMenuItem)d;
            if (menuItem.Level == RibbonApplicationMenuItemLevel.Top)
            {
                return RibbonHelper.CoerceIsSubmenuOpenForTopLevelItem(menuItem, menuItem.ParentItemsControl, (bool)baseValue);
            }

            return baseValue;
        }

        /// <summary>
        ///   Gets/Sets RibbonApplicationMenuItemLevel property which indicates on which level this item is displayed.
        ///   This property will define visual appearance of the menu item.
        /// </summary>
        public RibbonApplicationMenuItemLevel Level
        {
            get { return (RibbonApplicationMenuItemLevel)GetValue(LevelPropertyKey.DependencyProperty); }
            internal set { SetValue(LevelPropertyKey, value); }
        }

        /// <summary>
        /// DependencyPropertyKey for read only DependencyProperty Level.
        /// </summary>
        private static readonly DependencyPropertyKey LevelPropertyKey =
                DependencyProperty.RegisterReadOnly("Level",
                    typeof(RibbonApplicationMenuItemLevel),
                    typeof(RibbonApplicationMenuItem),
                    new FrameworkPropertyMetadata(RibbonApplicationMenuItemLevel.Top));

        /// <summary>
        ///  Using a DependencyProperty as the backing store for Level to enable binding.
        /// </summary>
        public static readonly DependencyProperty LevelProperty = 
                LevelPropertyKey.DependencyProperty;

        #endregion

        #region KeyTips

        protected override void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipLeftAtTargetLeft;
                e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipTopAtTargetCenter;
                e.KeyTipHorizontalOffset = KeyTipHorizontalOffet;
                e.KeyTipVerticalOffset = 0;
            }
        }

        #endregion

        #region Input

        protected override void OnKeyDown(KeyEventArgs e)
        {
            RibbonHelper.OnApplicationMenuItemUpDownKeyDown(e, this);
            base.OnKeyDown(e);
        }

        #endregion

        #region Private Data
        internal const double KeyTipHorizontalOffet = 20;
        internal const int CloseSubmenuTimerDelay = 100;
        #endregion
    }
}
