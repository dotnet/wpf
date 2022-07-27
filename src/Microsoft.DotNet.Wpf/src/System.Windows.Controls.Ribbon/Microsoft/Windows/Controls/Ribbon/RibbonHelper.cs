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
    using System.Collections;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Security;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Markup;
    using System.Windows.Markup.Primitives;
    using System.Windows.Media;
    using System.Windows.Threading;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon.Primitives;
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Controls.Ribbon.Primitives;
#endif
    using MS.Internal;


    #endregion

    /// <summary>
    ///     A static class which defines various helper methods.
    /// </summary>
    internal static class RibbonHelper
    {
        #region ToolTip

        /// <summary>
        ///     Helper method which serves as the coercion callback for
        ///     ToolTip property of ribbon controls. It creates and updates a RibbonToolTip
        ///     if needed and if possible and returns that as the coerced value.
        /// </summary>
        public static object CoerceRibbonToolTip(DependencyObject d, object value)
        {
            if (value == null)
            {
                string toolTipTitle = RibbonControlService.GetToolTipTitle(d);
                string toolTipDescription = RibbonControlService.GetToolTipDescription(d);
                ImageSource toolTipImageSource = RibbonControlService.GetToolTipImageSource(d);
                string toolTipFooterTitle = RibbonControlService.GetToolTipFooterTitle(d);
                string toolTipFooterDescription = RibbonControlService.GetToolTipFooterDescription(d);
                ImageSource toolTipFooterImageSource = RibbonControlService.GetToolTipFooterImageSource(d);

                if (!string.IsNullOrEmpty(toolTipTitle) ||
                    !string.IsNullOrEmpty(toolTipDescription) ||
                    toolTipImageSource != null ||
                    !string.IsNullOrEmpty(toolTipFooterTitle) ||
                    !string.IsNullOrEmpty(toolTipFooterDescription) ||
                    toolTipFooterImageSource != null)
                {
                    RibbonToolTip ribbonToolTip = new RibbonToolTip();
                    ribbonToolTip.Title = toolTipTitle;
                    ribbonToolTip.Description = toolTipDescription;
                    ribbonToolTip.ImageSource = toolTipImageSource;
                    ribbonToolTip.FooterTitle = toolTipFooterTitle;
                    ribbonToolTip.FooterDescription = toolTipFooterDescription;
                    ribbonToolTip.FooterImageSource = toolTipFooterImageSource;
                    value = ribbonToolTip;
                }
            }

            return value;
        }

        /// <summary>
        ///     Helper method which serves as the property changed callback for
        ///     properties which impact ToolTip (like ToolTipTitle etc.). It calls
        ///     the coercion on ToolTip property.
        /// </summary>
        public static void OnRibbonToolTipPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(FrameworkElement.ToolTipProperty);
        }

        /// <summary>
        /// Determines whether a ToolTip is available on the first Visual child of a FrameworkElement.
        /// </summary>
        /// <param name="visualChild">First visual child of control</param>
        /// <param name="content">Content to be set as ToolTip</param>
        /// <returns></returns>
        public static bool GetIsContentTooltip(FrameworkElement visualChild, object content)
        {
            if (content == null || visualChild == null)
            {
                return false;
            }

            RibbonToolTip ribbonToolTip = visualChild.ToolTip as RibbonToolTip;
            if (ribbonToolTip == null)
            {
                return false;
            }

            return content.Equals(ribbonToolTip.Title);
        }

        /// <summary>
        /// Sets ToolTip on the first Visual child of a FrameworkElement.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="visualChild">First visual child of control</param>
        /// <param name="content">content to be set as ToolTip</param>
        /// <param name="value">Set or Unset ToolTip</param>
        public static void SetContentAsToolTip(FrameworkElement element, FrameworkElement visualChild, object content, bool value)
        {
            if (visualChild != null)
            {
                // Checks if ToolTip is not already set on the element
                if (value && element.ToolTip == null && content != null)
                {
                    RibbonToolTip ribbonToolTip = visualChild.ToolTip as RibbonToolTip;
                    if (ribbonToolTip == null ||
                        ribbonToolTip.Title != content.ToString())
                    {
                        ribbonToolTip = new RibbonToolTip();
                        ribbonToolTip.Title = content.ToString();
                        visualChild.ToolTip = ribbonToolTip;
                    }
                }
                else
                {
                    visualChild.ToolTip = null;
                }
            }
        }

        internal static void FindAndHookPopup(DependencyObject element, ref Popup popup)
        {
            // Fetch the Popup parent

            if (popup == null)
            {
                DependencyObject rootVisual = TreeHelper.FindVisualRoot(element);
                if (rootVisual != null)
                {
                    popup = LogicalTreeHelper.GetParent(rootVisual) as Popup;

                    if (popup != null)
                    {
                        popup.Opened += OnPopupOpenedOrClosed;
                        popup.Closed += OnPopupOpenedOrClosed;

                        popup.PopupAnimation = PopupAnimation.None;
                    }
                }
            }
        }

        private static void OnPopupOpenedOrClosed(object sender, EventArgs e)
        {
            RibbonHelper.UpdatePopupAnimation((Popup)sender);
        }

        private static void UpdatePopupAnimation(Popup popup)
        {
#if RIBBON_IN_FRAMEWORK
            if (SystemParameters.HighContrast || !popup.IsOpen)
#else
            if (Microsoft.Windows.Shell.SystemParameters2.Current.HighContrast || !popup.IsOpen)
#endif
            {
                popup.PopupAnimation = PopupAnimation.None;
            }
            else
            {
                popup.PopupAnimation = PopupAnimation.Fade;
            }
        }

        #endregion ToolTip

        #region Workaround for hetrogenous triad of ItemsControl

        public class ValueAndValueSource
        {
            public object Value { get; set; }
            public BaseValueSource ValueSource { get; set; }
        }

        public static ValueAndValueSource GetValueAndValueSource(DependencyObject d, DependencyProperty property)
        {
            if (d == null)
            {
                return null;
            }
            Debug.Assert(property != null);
            return new ValueAndValueSource() { Value = d.GetValue(property), ValueSource = DependencyPropertyHelper.GetValueSource(d, property).BaseValueSource };
        }

        private static void RestoreValue(DependencyObject d, DependencyProperty property, ValueAndValueSource v)
        {
            Debug.Assert(d != null);
            Debug.Assert(property != null);
            Debug.Assert(v != null);
            if (v.ValueSource == BaseValueSource.Local)
            {
                d.SetValue(property, v.Value);
            }
            else
            {
                d.ClearValue(property);
                d.CoerceValue(property);
            }
        }

        /// <summary>
        /// This is an helper function to overcome the restriction on WPF framework to have
        /// hetrogenious hierarchy ItemsControl. The problem is an ItemsControl in it's
        /// internal logic fetches Item related properties from it's parent if that is an ItemsControl.
        /// That approach can only be successful only in homogenous hierarchy of controls from first layer
        /// as in case of TreeViewItem.
        ///
        /// In Ribbon control it is not true at various places and most common example could RibbonGalleryCategory.
        /// RibbonGalleryCategory's parent is RibbonGallery which is an ItemsControl and hence the above explained
        /// fetch happens. But the property specified on RibbonGallery are applicable to RibbonGalleryCategory only.
        /// Now, RibbonGalleryCategory's Item are not RibbonGalleryCategory but RibbonGalleryItem and hence a conflict.
        ///
        /// This function gets called from PrepareContainerOverride after the base.PrepareContainerOverride is being called
        /// and if the properties being fetched from the parent (changed) it sets them back to original values passed as arguments.
        /// </summary>
        /// <param name="itemsControl"> current ItemsControl</param>
        /// <param name="itemTemplate"></param>
        /// <param name="itemTemplateSelector"></param>
        /// <param name="itemStringFormat"></param>
        /// <param name="itemContainerStyle"></param>
        /// <param name="itemContainerStyleSelector"></param>
        /// <param name="alternationCount"></param>
        /// <param name="itemBindingGroup"></param>
        public static void IgnoreDPInheritedFromParentItemsControl(
                ItemsControl itemsControl,
                ItemsControl parentItemsControl,
                ValueAndValueSource itemTemplate,
                ValueAndValueSource itemTemplateSelector,
                ValueAndValueSource itemStringFormat,
                ValueAndValueSource itemContainerStyle,
                ValueAndValueSource itemContainerStyleSelector,
                ValueAndValueSource alternationCount,
                ValueAndValueSource itemBindingGroup,
                ValueAndValueSource headerTemplate,
                ValueAndValueSource headerTemplateSelector,
                ValueAndValueSource headerStringFormat)
        {
            // HeaderedItemsControl needs special consideration as some of the properties needs to be kept as is
            // if HierarichalDataTemplate is used. The reason is internal method PrepareHeirarchy() in HeaderedItemsControl
            // fetches appropriate values from the template and updates the one assigned using parentItemsControl and
            // those values should be kept for master-detail scenarios to be working correctly.

            HeaderedItemsControl hic = itemsControl as HeaderedItemsControl;
            HierarchicalDataTemplate hTemplate = hic != null ? hic.HeaderTemplate as HierarchicalDataTemplate : null;

            if (itemsControl.ItemTemplate == parentItemsControl.ItemTemplate)
            {
                if (hTemplate == null || (hTemplate.ItemTemplate == null && string.IsNullOrEmpty(hTemplate.ItemStringFormat) && hTemplate.ItemTemplateSelector == null))
                {
                    RestoreValue(itemsControl, ItemsControl.ItemTemplateProperty, itemTemplate);
                }
            }

            if (itemsControl.ItemTemplateSelector == parentItemsControl.ItemTemplateSelector)
            {
                if (hTemplate == null || (string.IsNullOrEmpty(hTemplate.ItemStringFormat) && hTemplate.ItemTemplateSelector == null))
                {
                    RestoreValue(itemsControl, ItemsControl.ItemTemplateSelectorProperty, itemTemplateSelector);
                }
            }

            if (itemsControl.ItemStringFormat == parentItemsControl.ItemStringFormat)
            {
                if (hTemplate == null || string.IsNullOrEmpty(hTemplate.ItemStringFormat))
                {
                    RestoreValue(itemsControl, ItemsControl.ItemStringFormatProperty, itemStringFormat);
                }
            }

            if (itemsControl.ItemContainerStyle == parentItemsControl.ItemContainerStyle)
            {
                if (hTemplate == null || (hTemplate.ItemContainerStyleSelector == null && hTemplate.ItemContainerStyle == null))
                {
                    RestoreValue(itemsControl, ItemsControl.ItemContainerStyleProperty, itemContainerStyle);
                }
            }

            if (itemsControl.ItemContainerStyleSelector == parentItemsControl.ItemContainerStyleSelector)
            {
                if (hTemplate == null || hTemplate.ItemContainerStyleSelector == null)
                {
                    RestoreValue(itemsControl, ItemsControl.ItemContainerStyleSelectorProperty, itemContainerStyleSelector);
                }
            }

            if (itemsControl.AlternationCount == parentItemsControl.AlternationCount)
            {
                // Potential issue if 0 is set intentionally, but one can argue if it is explicitly defined on
                // ItemsControl itself then use that one.
                if (hTemplate == null || hTemplate.AlternationCount == 0)
                {
                    RestoreValue(itemsControl, ItemsControl.AlternationCountProperty, alternationCount);
                }
            }

            if (itemsControl.ItemBindingGroup == parentItemsControl.ItemBindingGroup)
            {
                if (hTemplate == null || hTemplate.ItemBindingGroup == null)
                {
                    RestoreValue(itemsControl, ItemsControl.ItemBindingGroupProperty, itemBindingGroup);
                }
            }

            if (hic != null)
            {
                if (headerTemplate != null && hic.HeaderTemplate == parentItemsControl.ItemTemplate && hTemplate == null)
                {
                    RestoreValue(hic, HeaderedItemsControl.HeaderTemplateProperty, headerTemplate);
                }

                if (headerTemplateSelector != null && hic.HeaderTemplateSelector == parentItemsControl.ItemTemplateSelector && hTemplate == null)
                {
                    RestoreValue(hic, HeaderedItemsControl.HeaderTemplateSelectorProperty, headerTemplateSelector);
                }

                if (headerStringFormat != null && hic.HeaderStringFormat == parentItemsControl.ItemStringFormat && hTemplate == null)
                {
                    RestoreValue(hic, HeaderedItemsControl.HeaderStringFormatProperty, headerStringFormat);
                }
            }
        }

        #endregion

        #region Workaround of IsOffScreen Issue in UI Automation in 3.5

#if !RIBBON_IN_FRAMEWORK
        /// <summary>
        /// Calculates the real visible rectangle within parent chain, borrowed from 4.0 IsOffScreen fix for UIA.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static Rect CalculateVisibleBoundingRect(UIElement element)
        {

            Rect boundingRect = Rect.Empty;

            boundingRect = new Rect(element.RenderSize);
            // Compute visible portion of the rectangle.

            Visual visual = VisualTreeHelper.GetParent(element) as Visual;
            while (visual != null && boundingRect != Rect.Empty && boundingRect.Height != 0 && boundingRect.Width != 0)
            {
                Geometry clipGeometry = VisualTreeHelper.GetClip(visual);
                if (clipGeometry != null)
                {
                    GeneralTransform transform = element.TransformToAncestor(visual).Inverse;
                    // Safer version of transform to descendent (doing the inverse ourself and saves us changing the co-ordinate space of the owner's bounding rectangle),
                    // we want the rect inside of our space. (Which is always rectangular and much nicer to work with)
                    if (transform != null)
                    {
                        Rect clipBounds = clipGeometry.Bounds;
                        clipBounds = transform.TransformBounds(clipBounds);
                        boundingRect.Intersect(clipBounds);
                    }
                    else
                    {
                        // No visibility if non-invertable transform exists.
                        boundingRect = Rect.Empty;
                    }
                }
                visual = VisualTreeHelper.GetParent(visual) as Visual;
            }

            return boundingRect;
        }
#endif

        #endregion

        #region Mneumonics

        // Returns the index of _ marker.
        // _ can be escaped by double _
        public static int FindAccessKeyMarker(string text)
        {
            int length = text.Length;
            int startIndex = 0;
            while (startIndex < length)
            {
                int index = text.IndexOf('_', startIndex);
                if (index == -1)
                    return -1;
                // If next char exist and different from _
                if (index + 1 < length && text[index + 1] != '_')
                    return index;
                startIndex = index + 2;
            }

            return -1;
        }

        #endregion

        #region PseudoInheritedProperties

        internal static void TransferPseudoInheritedProperties(DependencyObject parent, DependencyObject child)
        {
            if (RibbonControlService.GetIsInQuickAccessToolBar(parent))
            {
                RibbonControlService.SetIsInQuickAccessToolBar(child, true);

                RibbonControlSizeDefinition qatControlSizeDefinition = RibbonControlService.GetQuickAccessToolBarControlSizeDefinition(child);
                RibbonControlService.SetControlSizeDefinition(child, qatControlSizeDefinition);
            }
            else
            {
                if (RibbonControlService.GetIsInControlGroup(parent))
                {
                    RibbonControlService.SetIsInControlGroup(child, true);
                }

                RibbonControlSizeDefinition controlSizeDefinition = RibbonControlService.GetControlSizeDefinition(parent);
                if (controlSizeDefinition != null)
                {
                    RibbonControlService.SetControlSizeDefinition(child, controlSizeDefinition);
                }
            }
        }

        internal static void ClearPseudoInheritedProperties(DependencyObject child)
        {
            if (child != null)
            {
                child.ClearValue(RibbonControlService.IsInQuickAccessToolBarPropertyKey);
                child.ClearValue(RibbonControlService.IsInControlGroupPropertyKey);
                child.ClearValue(RibbonControlService.ControlSizeDefinitionProperty);
            }
        }

        #endregion PseudoInheritedProperties

        #region KeyboardNavigation

        internal static void EnableFocusVisual(DependencyObject d)
        {
            if (IsKeyboardMostRecentInputDevice())
            {
                RibbonControlService.SetShowKeyboardCues(d, true);
            }
        }

        internal static void DisableFocusVisual(DependencyObject d)
        {
            RibbonControlService.SetShowKeyboardCues(d, false);
        }

        internal static bool IsKeyboardMostRecentInputDevice()
        {
            return (InputManager.Current.MostRecentInputDevice is KeyboardDevice);
        }

        public static bool MoveFocus(FocusNavigationDirection direction)
        {
            UIElement uie = Keyboard.FocusedElement as UIElement;
            if (uie != null)
            {
                return uie.MoveFocus(new TraversalRequest(direction));
            }
            ContentElement ce = Keyboard.FocusedElement as ContentElement;
            if (ce != null)
            {
                return ce.MoveFocus(new TraversalRequest(direction));
            }
            return false;
        }

        public static DependencyObject PredictFocus(DependencyObject element, FocusNavigationDirection direction)
        {
            UIElement uie;
            ContentElement ce;
            UIElement3D uie3d;

            if ((uie = element as UIElement) != null)
            {
                return uie.PredictFocus(direction);
            }
            else if ((ce = element as ContentElement) != null)
            {
                return ce.PredictFocus(direction);
            }
            else if ((uie3d = element as UIElement3D) != null)
            {
                return uie3d.PredictFocus(direction);
            }

            return null;
        }

        public static bool Focus(DependencyObject element)
        {
            UIElement uie;
            ContentElement ce;
            UIElement3D uie3d;

            if ((uie = element as UIElement) != null)
            {
                return uie.Focus();
            }
            else if ((ce = element as ContentElement) != null)
            {
                return ce.Focus();
            }
            else if ((uie3d = element as UIElement3D) != null)
            {
                return uie3d.Focus();
            }

            return false;
        }

        #endregion KeyboardNavigation

        #region ItemsControl Navigation

        internal static bool NavigateToFirstItem(ItemsControl itemsControl, Action<int> bringIntoViewCallback, Func<FrameworkElement, bool> additionalCheck)
        {
            FrameworkElement firstItem = FindContainer(itemsControl, 0, 1, bringIntoViewCallback, additionalCheck);
            if (firstItem != null)
            {
                firstItem.Focus();
                return true;
            }
            return false;
        }

        internal static bool NavigateToLastItem(ItemsControl itemsControl, Action<int> bringIntoViewCallback, Func<FrameworkElement, bool> additionalCheck)
        {
            FrameworkElement lastItem = FindContainer(itemsControl, itemsControl.Items.Count - 1, -1, bringIntoViewCallback, additionalCheck);
            if (lastItem != null)
            {
                lastItem.Focus();
                return true;
            }
            return false;
        }

        internal static FrameworkElement FindContainer(ItemsControl itemsControl, int startIndex, int direction,  Action<int> bringIntoViewCallback, Func<FrameworkElement, bool> additionalCheck)
        {
            if (itemsControl.HasItems)
            {
                int count = itemsControl.Items.Count;
                for (; startIndex >= 0 && startIndex < count; startIndex += direction)
                {
                    FrameworkElement container = itemsControl.ItemContainerGenerator.ContainerFromIndex(startIndex) as FrameworkElement;

                    // If container is virtualized, call BringIntoView.
                    if (container == null && bringIntoViewCallback != null)
                    {
                        bringIntoViewCallback(startIndex);
                        container = itemsControl.ItemContainerGenerator.ContainerFromIndex(startIndex) as FrameworkElement;
                    }

                    if (container != null && (additionalCheck == null || additionalCheck(container)))
                    {
                        return container;
                    }
                }
            }

            return null;
        }

        internal static bool NavigateToItem(ItemsControl parent, int itemIndex, Action<int> bringIntoViewCallback)
        {
            if (itemIndex < parent.Items.Count)
            {
                FrameworkElement nextElement = RibbonHelper.FindContainer(parent, itemIndex, 1, bringIntoViewCallback, null);
                if (nextElement != null)
                {
                    nextElement.Focus();
                    return true;
                }
            }
            return false;
        }

        internal static bool NavigateToNextMenuItemOrGallery(ItemsControl parent, int startIndex, Action<int> bringIntoViewCallback)
        {
            if (startIndex == parent.Items.Count - 1)
                startIndex = -1;

            if (startIndex < parent.Items.Count - 1)
            {
                FrameworkElement nextElement = RibbonHelper.FindContainer(parent, startIndex + 1, 1, bringIntoViewCallback, IsMenuItemFocusable);
                if (nextElement != null)
                {
                    RibbonGallery gallery = nextElement as RibbonGallery;
                    if (gallery == null)
                    {
                        nextElement.Focus();
                        return true;
                    }
                    else
                    {
                        // Move focus into the gallery if it does not have already has focus.
                        if (!nextElement.IsKeyboardFocusWithin)
                        {
                            return NavigateDownToGallery(gallery);
                        }
                    }
                }
            }
            return false;
        }

        internal static bool NavigateToPreviousMenuItemOrGallery(ItemsControl parent, int startIndex, Action<int> bringIntoViewCallback)
        {
            if (startIndex <= 0)
                startIndex = parent.Items.Count;

            if (startIndex > 0)
            {
                FrameworkElement previousElement = RibbonHelper.FindContainer(parent, startIndex - 1, -1, bringIntoViewCallback, IsMenuItemFocusable);
                if (previousElement != null)
                {
                    RibbonGallery gallery = previousElement as RibbonGallery;
                    if (gallery == null)
                    {
                        previousElement.Focus();
                        return true;
                    }
                    else
                    {
                        return NavigateUpToGallery(gallery);
                    }
                }
            }
            return false;
        }

        private static bool NavigateUpToGallery(RibbonGallery gallery)
        {
            if (gallery != null)
            {
                RibbonGalleryCategory lastCategory = RibbonHelper.FindContainer(gallery, gallery.Items.Count - 1, -1, null, IsContainerVisible) as RibbonGalleryCategory;
                if (lastCategory != null)
                {
                    return RibbonHelper.NavigateToLastItem(lastCategory, /* BringIntoView callback */ null, IsContainerFocusable);
                }
            }
            return false;
        }

        private static bool NavigateDownToGallery(RibbonGallery gallery)
        {
            if (gallery != null)
            {
                if (gallery.CanUserFilter)
                {
                    // Move focus to FilterContentPane of FilterMenuButton accordingly.
                    FrameworkElement focusObject = null;
                    ContentPresenter filterContentPane = gallery.FilterContentPane;
                    if (filterContentPane != null &&
                        filterContentPane.IsVisible)
                    {
                        focusObject = filterContentPane;
                    }
                    if (focusObject == null)
                    {
                        RibbonFilterMenuButton filterButton = gallery.FilterMenuButton;
                        if (filterButton != null &&
                            filterButton.IsVisible)
                        {
                            focusObject = filterButton;
                        }
                    }
                    if (focusObject != null)
                    {
                        focusObject.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                        return true;
                    }
                }

                RibbonGalleryCategory firstCategory = RibbonHelper.FindContainer(gallery, 0, 1, null, IsContainerVisible) as RibbonGalleryCategory;
                if (firstCategory != null)
                {
                    return RibbonHelper.NavigateToFirstItem(firstCategory, /* BringIntoView callback */ null, IsContainerFocusable);
                }
            }
            return false;
        }

        private static bool IsContainerFocusable(FrameworkElement container)
        {
            return container != null && container.Focusable;
        }

        private static bool IsContainerVisible(FrameworkElement container)
        {
            return container != null && container.Visibility == Visibility.Visible;
        }

        private static bool IsMenuItemFocusable(FrameworkElement container)
        {
            return container != null && (container is RibbonGallery || container.Focusable);
        }

        // This method is called when trying to navigate a single step up, down, left or
        // right within a RibbonGallery.

        internal static bool NavigateAndHighlightGalleryItem(RibbonGalleryItem focusedElement, FocusNavigationDirection direction)
        {
            if (focusedElement != null)
            {
                RibbonGalleryItem predictedFocus = focusedElement.PredictFocus(direction) as RibbonGalleryItem;
                if (predictedFocus != null)
                {
                    predictedFocus.IsHighlighted = true;
                    return true;
                }
            }

            return false;
        }

        internal static bool NavigatePageAndHighlightRibbonGalleryItem(RibbonGallery gallery, RibbonGalleryItem galleryItem, FocusNavigationDirection direction)
        {
            RibbonGalleryItem highlightedGalleryItem;
            return NavigatePageAndHighlightRibbonGalleryItem(gallery, galleryItem, direction, out highlightedGalleryItem);
        }

        // This method is called when trying to navigate pages within a RibbonGallery.
        // We approximate the RibbonGalleryItem that is a page away from the currently
        // focused item based upon the precomputed MaxColumnWidth and MaxRowHeight values.

        internal static bool NavigatePageAndHighlightRibbonGalleryItem(
            RibbonGallery gallery,
            RibbonGalleryItem galleryItem,
            FocusNavigationDirection direction,
            out RibbonGalleryItem highlightedGalleryItem)
        {
            highlightedGalleryItem = null;

            RibbonGalleryCategoriesPanel categoriesPanel = gallery.ItemsHostSite as RibbonGalleryCategoriesPanel;
            if (categoriesPanel != null)
            {
                double viewportWidth = categoriesPanel.ViewportWidth;
                double viewportHeight = categoriesPanel.ViewportHeight;

                RibbonGalleryCategory category, prevCategory = null;
                if (galleryItem != null)
                {
                    category = galleryItem.RibbonGalleryCategory;
                }
                else
                {
                    category = gallery.Items.Count > 0 ? gallery.ItemContainerGenerator.ContainerFromIndex(0) as RibbonGalleryCategory : null;
                    galleryItem = category != null && category.Items.Count > 0 ? category.ItemContainerGenerator.ContainerFromIndex(0) as RibbonGalleryItem : null;
                }

                if (category != null)
                {
                    Debug.Assert(category.RibbonGallery == gallery, "The reference RibbongalleryItem and the RibbonGallery must be related.");

                    int startCatIndex = gallery.ItemContainerGenerator.IndexFromContainer(category);
                    int endCatIndex, incr;

                    if (direction == FocusNavigationDirection.Up)
                    {
                        endCatIndex = -1;
                        incr = -1;
                    }
                    else
                    {
                        endCatIndex = gallery.Items.Count;
                        incr = 1;
                    }

                    for (int catIndex = startCatIndex; catIndex != endCatIndex && highlightedGalleryItem == null; catIndex += incr)
                    {
                        category = gallery.ItemContainerGenerator.ContainerFromIndex(catIndex) as RibbonGalleryCategory;
                        RibbonGalleryItemsPanel galleryItemsPanel = category.ItemsHostSite as RibbonGalleryItemsPanel;

                        // We want to skip over filtered categories

                        if (category.Visibility != Visibility.Visible)
                        {
                            continue;
                        }

                        int startItemIndex, endItemIndex, startColumnIndex, endColumnIndex, columnCount;
                        columnCount = (int)(viewportWidth / galleryItemsPanel.MaxColumnWidth);

                        if (direction == FocusNavigationDirection.Up)
                        {
                            startItemIndex = galleryItem != null ? category.ItemContainerGenerator.IndexFromContainer(galleryItem) : category.Items.Count - 1;
                            endItemIndex = -1;

                            if (prevCategory != null)
                            {
                                viewportHeight -= prevCategory.HeaderPresenter.ActualHeight;

                                if (DoubleUtil.LessThanOrClose(viewportHeight, 0))
                                {
                                    highlightedGalleryItem = category.ItemContainerGenerator.ContainerFromIndex(startItemIndex) as RibbonGalleryItem;
                                    break;
                                }
                            }

                            // startColumnIndex is the last column in the last row or the column of the anchor item

                            if (columnCount == 1)
                            {
                                startColumnIndex = 0;
                                endColumnIndex = 0;
                            }
                            else
                            {
                                startColumnIndex = (galleryItem != null ? startItemIndex : category.Items.Count - 1) % columnCount;
                                endColumnIndex = 0;
                            }
                        }
                        else
                        {
                            startItemIndex = galleryItem != null ? category.ItemContainerGenerator.IndexFromContainer(galleryItem) : 0;
                            endItemIndex = category.Items.Count;

                            if (prevCategory != null)
                            {
                                viewportHeight -= category.HeaderPresenter.ActualHeight;

                                if (DoubleUtil.LessThanOrClose(viewportHeight, 0))
                                {
                                    highlightedGalleryItem = category.ItemContainerGenerator.ContainerFromIndex(startItemIndex) as RibbonGalleryItem;
                                    break;
                                }
                            }

                            // endColumnIndex is the last column in the first row

                            if (columnCount == 1)
                            {
                                startColumnIndex = 0;
                                endColumnIndex = 0;
                            }
                            else
                            {
                                int remainingItems = category.Items.Count;
                                bool isLastRow = remainingItems <= columnCount;

                                startColumnIndex = galleryItem != null ? (startItemIndex % columnCount) : 0;
                                endColumnIndex = isLastRow ? remainingItems - 1 : columnCount - 1;
                            }
                        }

                        galleryItem = null;

                        for (int itemIndex = startItemIndex, columnIndex = startColumnIndex; itemIndex != endItemIndex; itemIndex += incr)
                        {
                            if (columnIndex == endColumnIndex)
                            {
                                // We are at the end of a row

                                viewportHeight -= galleryItemsPanel.MaxRowHeight;

                                if (DoubleUtil.LessThanOrClose(viewportHeight, 0) ||
                                    (itemIndex == endItemIndex - incr && catIndex == endCatIndex - incr))
                                {
                                    // If we have scrolled a page or have reached the boundary
                                    // of the gallery, highlight that item

                                    highlightedGalleryItem = category.ItemContainerGenerator.ContainerFromIndex(itemIndex) as RibbonGalleryItem;
                                    break;
                                }

                                if (direction == FocusNavigationDirection.Up)
                                {
                                    if (columnCount > 1)
                                    {
                                        startColumnIndex = columnCount - 1;
                                        endColumnIndex = 0;
                                    }
                                }
                                else
                                {
                                    if (columnCount > 1)
                                    {
                                        int remainingItems = category.Items.Count - itemIndex;
                                        bool isLastRow = remainingItems <= columnCount;

                                        startColumnIndex = 0;
                                        endColumnIndex = isLastRow ? remainingItems - 1 : columnCount - 1;
                                    }
                                }

                                columnIndex = startColumnIndex;
                            }
                            else
                            {
                                // We are interating through the cells in a row

                                columnIndex += incr;
                            }
                        }

                        prevCategory = category;
                    }

                    if (highlightedGalleryItem != null)
                    {
                        highlightedGalleryItem.IsHighlighted = true;
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region DismissPopup

        internal static void AddHandler(DependencyObject element, RoutedEvent routedEvent, Delegate handler)
        {
            Debug.Assert(element != null, "Element must not be null");
            Debug.Assert(routedEvent != null, "RoutedEvent must not be null");

            UIElement uiElement = element as UIElement;
            if (uiElement != null)
            {
                uiElement.AddHandler(routedEvent, handler);
            }
            else
            {
                ContentElement contentElement = element as ContentElement;
                if (contentElement != null)
                {
                    contentElement.AddHandler(routedEvent, handler);
                }
                else
                {
                    UIElement3D uiElement3D = element as UIElement3D;
                    if (uiElement3D != null)
                    {
                        uiElement3D.AddHandler(routedEvent, handler);
                    }
                }
            }
        }

        internal static void RemoveHandler(DependencyObject element, RoutedEvent routedEvent, Delegate handler)
        {
            Debug.Assert(element != null, "Element must not be null");
            Debug.Assert(routedEvent != null, "RoutedEvent must not be null");

            UIElement uiElement = element as UIElement;
            if (uiElement != null)
            {
                uiElement.RemoveHandler(routedEvent, handler);
            }
            else
            {
                ContentElement contentElement = element as ContentElement;
                if (contentElement != null)
                {
                    contentElement.RemoveHandler(routedEvent, handler);
                }
                else
                {
                    UIElement3D uiElement3D = element as UIElement3D;
                    if (uiElement3D != null)
                    {
                        uiElement3D.RemoveHandler(routedEvent, handler);
                    }
                }
            }
        }

        /// <summary>
        ///     Helper method which determines if given hwnd belongs
        ///     to the same Dispatcher as that of given element.
        /// </summary>
        public static bool IsOurWindow(IntPtr hwnd, DependencyObject element)
        {
            Debug.Assert(element != null);
            if (hwnd != IntPtr.Zero)
            {
                HwndSource hwndSource;
                hwndSource = HwndSource.FromHwnd(hwnd);
                if (hwndSource != null &&
                    hwndSource.Dispatcher == element.Dispatcher)
                {
                    // The window has the same dispatcher, must be ours.
                    return true;
                }
            }
            return false;
        }

        public static void HandleLostMouseCapture(UIElement element,
            MouseEventArgs e,
            Func<bool> getter,
            Action<bool> setter,
            UIElement targetCapture,
            UIElement targetFocus)
        {
            if (getter() && targetCapture != null)
            {
                IntPtr capturedHwnd = IntPtr.Zero;
                bool isOurWindowCaptured = false;
                if (Mouse.Captured == null)
                {
                    // If we are losing capture to some other window
                    // then close all the popups.
                    capturedHwnd = NativeMethods.GetCapture();
                    if (capturedHwnd != IntPtr.Zero &&
                        !(isOurWindowCaptured = IsOurWindow(capturedHwnd, element)))
                    {
                        element.RaiseEvent(new RibbonDismissPopupEventArgs());
                        e.Handled = true;
                        return;
                    }
                }

                if (e.OriginalSource == targetCapture)
                {
                    if (Mouse.Captured == null)
                    {
                        PresentationSource mouseSource = Mouse.PrimaryDevice.ActiveSource;
                        if (mouseSource == null &&
                            (capturedHwnd == IntPtr.Zero ||
                            isOurWindowCaptured))
                        {
                            // If the active source is null and current captured
                            // is null, this capture loss is bacause of Mouse
                            // deactivation (because mouse is not on the window
                            // anymore). Hence reacquire capture and focus.
                            // Note that we do it only if the capture is not lost to
                            // some other window, and genuine closing of
                            // popups when both active source and current captured is
                            // null due to clicking some where else should be handled by
                            // click through event handler.
                            if (!ReacquireCapture(targetCapture, targetFocus))
                            {
                                // call the setter if we couldn't reacquire capture
                                setter(false);
                            }
                            e.Handled = true;
                        }
                        else
                        {
                            setter(false);
                        }
                    }
                    else if (!RibbonHelper.IsAncestorOf(targetCapture, Mouse.Captured as DependencyObject))
                    {
                        setter(false);
                    }
                }
                else if (RibbonHelper.IsAncestorOf(targetCapture, e.OriginalSource as DependencyObject))
                {
                    if (Mouse.Captured == null)
                    {
                        // If a descendant of targetCapture is losing capture
                        // then take capture on targetCapture
                        if (!ReacquireCapture(targetCapture, targetFocus))
                        {
                            // call the setter if we couldn't reacquire capture
                            setter(false);
                        }
                        e.Handled = true;
                    }
                    else if (!IsCaptureInSubtree(targetCapture))
                    {
                        // If a descendant of targetCapture is losing capture
                        // to an element outside targetCapture's subtree
                        // then call setter
                        setter(false);
                    }
                }
            }
        }

        private static bool ReacquireCapture(UIElement targetCapture, UIElement targetFocus)
        {
            bool success = Mouse.Capture(targetCapture, CaptureMode.SubTree);
            if (success && targetFocus != null && !targetFocus.IsKeyboardFocusWithin)
            {
                targetFocus.Focus();
            }
            return success;
        }

        public static bool IsMousePhysicallyOver(UIElement element)
        {
            if (element == null)
            {
                return false;
            }
            Point position = Mouse.GetPosition(element);
            if (DoubleUtil.GreaterThan(position.X, 0) &&
                DoubleUtil.GreaterThan(position.Y, 0) &&
                DoubleUtil.LessThanOrClose(position.X, element.RenderSize.Width) &&
                DoubleUtil.LessThanOrClose(position.Y, element.RenderSize.Height))
            {
                return true;
            }
            return false;
        }

        internal static void HandleClickThrough(
            object sender,
            MouseButtonEventArgs e,
            UIElement alternateCaptureHost)
        {
            if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right)
            {
                // If this is the element with the leaf popup,
                // then raise DismissPopupEvent. If capture belongs
                // to the visual subtree of popup or the element,
                // then we decide that this is the leaf popup.
                UIElement element = sender as UIElement;
                if (IsCaptureInVisualSubtree(element) ||
                    IsCaptureInVisualSubtree(alternateCaptureHost))
                {
                    UIElement source = Mouse.Captured as UIElement;
                    if (source == null)
                    {
                        source = element;
                    }
                    if (source != null)
                    {
                        source.RaiseEvent(new RibbonDismissPopupEventArgs(RibbonDismissPopupMode.MousePhysicallyNotOver));
                    }
                }
            }
        }

        internal static void HandleDismissPopup(
            RibbonDismissPopupEventArgs e,
            Action<bool> setter,
            Predicate<DependencyObject> cancelPredicate,
            UIElement mouseOverTarget,
            UIElement alternateMouseOverTarget)
        {
            if (!cancelPredicate(e.OriginalSource as DependencyObject))
            {
                // Call setter if the dismiss mode is always or
                // if the mouse is not directly over either of
                // the targets.
                if (e.DismissMode == RibbonDismissPopupMode.Always ||
                    (!IsMousePhysicallyOver(mouseOverTarget) &&
                    !IsMousePhysicallyOver(alternateMouseOverTarget)))
                {
                    setter(false);
                }
                else
                {
                    e.Handled = true;
                }
            }
            else
            {
                e.Handled = true;
            }
        }

        public static void AsyncSetFocusAndCapture(UIElement element,
            Func<bool> getter,
            UIElement targetCapture,
            UIElement targetFocus)
        {
            element.Dispatcher.BeginInvoke(
                (Action)delegate()
                {
                    if (getter())
                    {
                        if (targetFocus != null && !targetFocus.IsKeyboardFocusWithin)
                        {
                            targetFocus.Focus();
                        }

                        if (targetCapture != null &&
                            (Mouse.Captured == null ||
                            !IsCaptureInSubtree(targetCapture)))
                        {
                            Mouse.Capture(targetCapture, CaptureMode.SubTree);
                        }
                    }
                },
                DispatcherPriority.Input);
        }

        public static void RestoreFocusAndCapture(UIElement targetCapture,
            UIElement targetFocus)
        {
            if (targetFocus != null &&
                targetFocus.IsKeyboardFocusWithin)
            {
                Keyboard.Focus(null);
            }

            if (targetCapture != null &&
                Mouse.Captured != null &&
                IsCaptureInSubtree(targetCapture))
            {
                Mouse.Capture(null);
            }
        }

        public static void HandleIsDropDownChanged(UIElement element,
            Func<bool> getter,
            UIElement targetCapture,
            UIElement targetFocus)
        {
            if (targetCapture == null &&
                targetFocus == null)
            {
                return;
            }

            if (getter())
            {
                AsyncSetFocusAndCapture(element,
                    getter,
                    targetCapture,
                    targetFocus);
            }
            else
            {
                RestoreFocusAndCapture(targetCapture,
                    targetFocus);
            }
        }

        internal static void HandleDropDownKeyDown(
            object sender, KeyEventArgs e, Func<bool> gettor, Action<bool> settor, UIElement targetFocusOnFalse, UIElement targetFocusContainerOnTrue)
        {
            Key key = e.Key;
            switch (key)
            {
                case Key.Escape:
                    {
                        if (gettor())
                        {
                            settor(false);
                            e.Handled = true;
                            if (targetFocusOnFalse != null)
                            {
                                targetFocusOnFalse.Focus();
                            }
                        }
                    }
                    break;

                case Key.System:
                    if (KeyTipService.Current.State != KeyTipService.KeyTipState.Enabled && ((e.SystemKey == Key.LeftAlt) || (e.SystemKey == Key.RightAlt))
                        || (e.SystemKey == Key.F10))
                    {
                        if (gettor())
                        {
                            // Raise DismissPopup event and hence the key down event.
                            UIElement uie = sender as UIElement;
                            if (uie != null)
                            {
                                RibbonDismissPopupEventArgs dismissArgs = new RibbonDismissPopupEventArgs();
                                uie.RaiseEvent(dismissArgs);
                                e.Handled = true;
                            }
                        }
                    }
                    break;
                case Key.F4:
                    {
                        if (gettor())
                        {
                            settor(false);
                            e.Handled = true;
                            if (targetFocusOnFalse != null)
                            {
                                targetFocusOnFalse.Focus();
                            }
                        }
                        else
                        {
                            settor(true);
                            if (targetFocusContainerOnTrue != null)
                            {
                                targetFocusContainerOnTrue.Dispatcher.BeginInvoke(
                                    (Action)delegate()
                                    {
                                        targetFocusContainerOnTrue.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                                    },
                                    DispatcherPriority.Input,
                                    null);
                            }
                            e.Handled = true;
                        }

                        // Technically one needs to change active key tip scope,
                        // but since we do not have public api to do that yet,
                        // we dismiss keytips
                        KeyTipService.DismissKeyTips();
                    }
                    break;
            }
        }

        public static UIElement TryGetChild(this Popup popup)
        {
            return (popup == null ? null : popup.Child);
        }

        public static bool IsCaptureInSubtree(UIElement element)
        {
            return (Mouse.Captured == element ||
                RibbonHelper.IsAncestorOf(element, Mouse.Captured as DependencyObject));
        }

        public static bool IsCaptureInVisualSubtree(UIElement element)
        {
            return (element != null && Mouse.Captured != null && (Mouse.Captured == element ||
                TreeHelper.IsVisualAncestorOf(element, Mouse.Captured as DependencyObject)));
        }

        internal static bool IsAncestorOf(DependencyObject ancestor, DependencyObject element)
        {
            if (ancestor == null || element == null)
            {
                return false;
            }
            return TreeHelper.FindAncestor(element, delegate(DependencyObject d) { return d == ancestor; }) != null;
        }

        /// <summary>
        ///     Helper method to coerce the given property
        ///     of the element at input priority.
        /// </summary>
        public static void DelayCoerceProperty(DependencyObject element, DependencyProperty property)
        {
            element.Dispatcher.BeginInvoke(
                (Action)delegate()
                {
                    element.CoerceValue(property);
                },
                DispatcherPriority.Input,
                null);
        }

        #endregion DismissPopup

        #region StarLayoutHelper

        public static bool IsISupportStarLayout(DependencyObject d)
        {
            return (d is ISupportStarLayout);
        }

        // Registers itself to StarLayout manager in the parent chain.
        public static void InitializeStarLayoutManager(DependencyObject starLayoutProvider)
        {
            Debug.Assert(starLayoutProvider != null);

            ISupportStarLayout starLayoutManager = TreeHelper.FindVisualAncestor(starLayoutProvider,
                RibbonHelper.IsISupportStarLayout) as ISupportStarLayout;
            IContainsStarLayoutManager iContainsStarLayoutManager = starLayoutProvider as IContainsStarLayoutManager;

            if (iContainsStarLayoutManager != null)
            {
                IProvideStarLayoutInfoBase iProvideStarLayoutInfoBase = (IProvideStarLayoutInfoBase)starLayoutProvider;
                if (starLayoutManager == null && iContainsStarLayoutManager.StarLayoutManager != null)
                {
                    iContainsStarLayoutManager.StarLayoutManager.UnregisterStarLayoutProvider(iProvideStarLayoutInfoBase);
                    iContainsStarLayoutManager.StarLayoutManager = null;
                }
                else if (starLayoutManager != null)
                {
                    if (starLayoutManager != iContainsStarLayoutManager.StarLayoutManager)
                    {
                        if (iContainsStarLayoutManager.StarLayoutManager != null)
                        {
                            iContainsStarLayoutManager.StarLayoutManager.UnregisterStarLayoutProvider(iProvideStarLayoutInfoBase);
                        }
                        starLayoutManager.RegisterStarLayoutProvider(iProvideStarLayoutInfoBase);
                        iContainsStarLayoutManager.StarLayoutManager = starLayoutManager;
                    }

                    // It isn't appropriate for the star layout element to be
                    // measure outside the context of the parent star layout
                    // manager. Layout for suh an element will only be correct
                    // if we do the two pass star layout drill. So just in case
                    // we got here out of turn, we should invalidate measure
                    // on the manager so that we can settle things. In normal
                    // course of action if we arrived here from the manager,
                    // the InvalidateMeasure would no-o because the manager
                    // will already be dirty for measure.

                    UIElement managerElement = starLayoutManager as UIElement;
                    if (managerElement != null)
                    {
                        managerElement.InvalidateMeasure();
                    }
                }
            }
        }

        #endregion StarLayoutHelper

        #region RibbonContextMenu

        internal static object OnCoerceContextMenu(DependencyObject d, object baseValue)
        {
            DependencyProperty dp = ContextMenuService.ContextMenuProperty;

            if (PropertyHelper.IsPropertyTransferEnabled(d, dp))
            {
                var propertySource = DependencyPropertyHelper.GetValueSource(d, dp);
                var baseValueSource = propertySource.BaseValueSource;

                if (baseValueSource == BaseValueSource.Default)
                {
                    RibbonContextMenu cm = RibbonContextMenu.ChooseContextMenu(d);
                    if (cm != null)
                    {
                        return cm;
                    }
                }
            }

            return baseValue;
        }

        internal static void OnContextMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PropertyHelper.TransferProperty(d, ContextMenuService.ContextMenuProperty);
        }

        #endregion

        #region OnCommandChanged

        // Performs proper hooking/unhooking and coerces QAT ID.
        //
        // All Ribbon controls should hook this delegate to coerce the QAT ID whenever the Command changes.
        internal static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(RibbonControlService.QuickAccessToolBarIdProperty);
        }

        #endregion OnCommandChanged

        #region QAT

        // For Ribbon controls that implement ICommandSource, coerce the QAT ID to be the Command property if QAT ID is unspecified.
        internal static object OnCoerceQuickAccessToolBarId(DependencyObject d, object baseValue)
        {
            ICommandSource commandSource = d as ICommandSource;
            if (baseValue == null &&
                commandSource != null)
            {
                return commandSource.Command;
            }

            return baseValue;
        }

        internal static void OnIsInQATChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PropertyHelper.TransferProperty(d, ContextMenuService.ContextMenuProperty);
        }

        internal static object OnCoerceCanAddToQuickAccessToolBarDirectly(DependencyObject d, object baseValue)
        {
            DependencyProperty dp = RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty;

            if (PropertyHelper.IsPropertyTransferEnabled(d, dp))
            {
                var propertySource = DependencyPropertyHelper.GetValueSource(d, dp);
                var baseValueSource = propertySource.BaseValueSource;

                if (baseValueSource == BaseValueSource.Default)
                {
                    FrameworkElement fe =  d as FrameworkElement;
                    if (fe != null && fe.TemplatedParent != null && !(fe.TemplatedParent is ContentPresenter))
                    {
                        return false;
                    }
                }
            }

            return baseValue;
        }

        internal static void OnCanAddToQuickAccessToolBarDirectlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PropertyHelper.TransferProperty(d, RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty);
            PropertyHelper.TransferProperty(d, ContextMenuService.ContextMenuProperty);
        }

        /// <summary>
        ///   Determines whether or not an item exists in the QAT.
        /// </summary>
        internal static bool ExistsInQAT(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            Ribbon ribbon = (Ribbon)element.GetValue(RibbonControlService.RibbonProperty);
            object qatID = RibbonControlService.GetQuickAccessToolBarId(element);

            if (ribbon != null &&
                ribbon.QuickAccessToolBar != null &&
                qatID != null)
            {
                return ribbon.QuickAccessToolBar.ContainsId(qatID);
            }

            return false;
        }

        [Flags]
        private enum TransferMode
        {
            AlwaysTransfer = 0x01,                     // Always performs the transfer except if it is the default value.
            OnlyTransferIfTemplateBound = 0x02,        // Requires BaseValueSource.ParentTemplate or higher to perform the transfer.
        }

        private static void TransferProperty(UIElement original, UIElement clone, DependencyProperty dp, TransferMode mode)
        {
            TransferProperty(original, clone, dp, dp, mode);
        }

        private static void TransferProperty(UIElement original, UIElement clone, DependencyProperty originalProperty, DependencyProperty cloneProperty, TransferMode mode)
        {
            bool performTransfer = false;

            if ((mode & TransferMode.AlwaysTransfer) == TransferMode.AlwaysTransfer)
            {
                performTransfer |= DependencyPropertyHelper.GetValueSource(original, originalProperty).BaseValueSource > BaseValueSource.Default;
            }
            else if ((mode & TransferMode.OnlyTransferIfTemplateBound) == TransferMode.OnlyTransferIfTemplateBound)
            {
                performTransfer |= DependencyPropertyHelper.GetValueSource(original, originalProperty).BaseValueSource >= BaseValueSource.ParentTemplate;
            }

            if (performTransfer)
            {
                // Actually perform the transfer.

                BindingBase binding = BindingOperations.GetBindingBase(original, originalProperty);
                if (binding != null)
                {
                    // Transfer Bindings

                    BindingOperations.SetBinding(clone, cloneProperty, binding);
                }
                else
                {
                    Expression expr = original.ReadLocalValue(originalProperty) as Expression;
                    if (expr != null)
                    {
                        // Transfer DynamicResource

                        DynamicResourceExtension dynamicResource = _rreConverter.ConvertTo(expr, typeof(MarkupExtension)) as DynamicResourceExtension;
                        if (dynamicResource != null)
                        {
                            clone.SetValue(cloneProperty, dynamicResource.ProvideValue(null));
                        }
                    }
                    else
                    {
                        // Transfer other DPs

                        object originalValue = original.GetValue(originalProperty);
                        clone.SetValue(cloneProperty, CreateClone(originalValue));
                    }
                }
            }
        }

        private static ResourceReferenceExpressionConverter _rreConverter = new ResourceReferenceExpressionConverter();

        private struct PropertyAndTransferMode
        {
            internal DependencyProperty Property;
            internal TransferMode Mode;
        }

        private static PropertyAndTransferMode[] _automationProperties;
        private static PropertyAndTransferMode[] _feProperties;
        private static PropertyAndTransferMode[] _controlProperties;
        private static PropertyAndTransferMode[] _contentControlProperties;
        private static PropertyAndTransferMode[] _buttonProperties;
        private static PropertyAndTransferMode[] _toggleButtonProperties;
        private static PropertyAndTransferMode[] _itemsControlProperties;
        private static PropertyAndTransferMode[] _headeredItemsControlProperties;
        private static PropertyAndTransferMode[] _ribbonProperties;
        private static PropertyAndTransferMode[] _ribbonBrushProperties;
        private static PropertyAndTransferMode[] _ribbonMenuButtonProperties;
        private static PropertyAndTransferMode[] _ribbonSplitButtonProperties;
        private static PropertyAndTransferMode[] _ribbonMenuItemProperties;
        private static PropertyAndTransferMode[] _ribbonSplitMenuItemProperties;
        private static PropertyAndTransferMode[] _ribbonGalleryProperties;
        private static PropertyAndTransferMode[] _ribbonGalleryCategoryProperties;
        private static PropertyAndTransferMode[] _ribbonGalleryItemProperties;
        private static PropertyAndTransferMode[] _ribbonGroupProperties;
        private static PropertyAndTransferMode[] _textBoxProperties;
        private static PropertyAndTransferMode[] _ribbonComboBoxProperties;
        private static PropertyAndTransferMode[] _scrollProperties;

        private static object _syncRoot = new object();

        internal static void PopulatePropertyLists()
        {
            lock (_syncRoot)
            {
                if (_feProperties == null)
                {
                    _automationProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode() { Property = AutomationProperties.AcceleratorKeyProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode() { Property = AutomationProperties.AccessKeyProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode() { Property = AutomationProperties.AutomationIdProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode() { Property = AutomationProperties.HelpTextProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode() { Property = AutomationProperties.IsColumnHeaderProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode() { Property = AutomationProperties.IsRequiredForFormProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode() { Property = AutomationProperties.IsRowHeaderProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode() { Property = AutomationProperties.ItemStatusProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode() { Property = AutomationProperties.ItemTypeProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode() { Property = AutomationProperties.LabeledByProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode() { Property = AutomationProperties.NameProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _feProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = FrameworkElement.DataContextProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = FrameworkElement.BindingGroupProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _controlProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = Control.BackgroundProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = Control.BorderBrushProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = Control.BorderThicknessProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = Control.ForegroundProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = Control.FontFamilyProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = Control.FontSizeProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = Control.FontStretchProperty, Mode = TransferMode.AlwaysTransfer},
                        new PropertyAndTransferMode () { Property = Control.FontStyleProperty, Mode = TransferMode.AlwaysTransfer},
                        new PropertyAndTransferMode () { Property = Control.FontWeightProperty, Mode = TransferMode.AlwaysTransfer},
                    };

                    _contentControlProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = ContentControl.ContentProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = ContentControl.ContentStringFormatProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = ContentControl.ContentTemplateProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = ContentControl.ContentTemplateSelectorProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _buttonProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = ButtonBase.CommandProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = ButtonBase.CommandParameterProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = ButtonBase.CommandTargetProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _toggleButtonProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = ToggleButton.IsCheckedProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _itemsControlProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = ItemsControl.ItemBindingGroupProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = ItemsControl.ItemContainerStyleProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = ItemsControl.ItemContainerStyleSelectorProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = ItemsControl.ItemsPanelProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = ItemsControl.ItemsSourceProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = ItemsControl.ItemStringFormatProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = ItemsControl.ItemTemplateProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = ItemsControl.ItemTemplateSelectorProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = ItemsControl.DisplayMemberPathProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = ItemsControl.AlternationCountProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _headeredItemsControlProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = HeaderedItemsControl.HeaderProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = HeaderedItemsControl.HeaderStringFormatProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = HeaderedItemsControl.HeaderTemplateProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = HeaderedItemsControl.HeaderTemplateSelectorProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _ribbonProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = RibbonControlService.LabelProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.SmallImageSourceProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.LargeImageSourceProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.ToolTipTitleProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.ToolTipDescriptionProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.ToolTipImageSourceProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.ToolTipFooterTitleProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.ToolTipFooterDescriptionProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.ToolTipFooterImageSourceProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.QuickAccessToolBarIdProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.QuickAccessToolBarControlSizeDefinitionProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _ribbonBrushProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = RibbonControlService.MouseOverBackgroundProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.MouseOverBorderBrushProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.PressedBackgroundProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.PressedBorderBrushProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.FocusedBackgroundProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.FocusedBorderBrushProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.CheckedBackgroundProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.CheckedBorderBrushProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonControlService.CornerRadiusProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _ribbonMenuButtonProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = RibbonMenuButton.DropDownHeightProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonMenuButton.CanUserResizeHorizontallyProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonMenuButton.CanUserResizeVerticallyProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonMenuButton.ItemContainerTemplateSelectorProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonMenuButton.UsesItemContainerTemplateProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _ribbonSplitButtonProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = RibbonSplitButton.IsCheckableProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitButton.IsCheckedProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitButton.DropDownToolTipTitleProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitButton.DropDownToolTipDescriptionProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitButton.DropDownToolTipImageSourceProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitButton.DropDownToolTipFooterTitleProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitButton.DropDownToolTipFooterDescriptionProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitButton.DropDownToolTipFooterImageSourceProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitButton.HeaderQuickAccessToolBarIdProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitButton.CommandProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitButton.CommandParameterProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitButton.CommandTargetProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitButton.LabelPositionProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _ribbonMenuItemProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = RibbonMenuItem.CommandProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonMenuItem.CommandParameterProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonMenuItem.CommandTargetProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonMenuItem.ImageSourceProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonMenuItem.QuickAccessToolBarImageSourceProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonMenuItem.IsCheckableProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonMenuItem.IsCheckedProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonMenuItem.ItemContainerTemplateSelectorProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonMenuItem.UsesItemContainerTemplateProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonMenuItem.CanUserResizeHorizontallyProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonMenuItem.CanUserResizeVerticallyProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonMenuItem.DropDownHeightProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonMenuItem.StaysOpenOnClickProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _ribbonSplitMenuItemProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = RibbonSplitMenuItem.DropDownToolTipTitleProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitMenuItem.DropDownToolTipDescriptionProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitMenuItem.DropDownToolTipImageSourceProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitMenuItem.DropDownToolTipFooterTitleProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitMenuItem.DropDownToolTipFooterDescriptionProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitMenuItem.DropDownToolTipFooterImageSourceProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonSplitMenuItem.HeaderQuickAccessToolBarIdProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _ribbonGalleryProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = RibbonGallery.CanUserFilterProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.CategoryStyleProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.CategoryTemplateProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.CommandParameterProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.CommandProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.CommandTargetProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.FilterItemContainerStyleProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.FilterItemContainerStyleSelectorProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.FilterItemTemplateProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.FilterItemTemplateSelectorProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.FilterMenuButtonStyleProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.FilterPaneContentProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.FilterPaneContentTemplateProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.GalleryItemStyleProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.GalleryItemTemplateProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.IsSharedColumnSizeScopeProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.IsSynchronizedWithCurrentItemProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.MaxColumnCountProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.MinColumnCountProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.PreviewCommandParameterProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.SelectedItemProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.SelectedValuePathProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGallery.SelectedValueProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _ribbonGalleryCategoryProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = RibbonGalleryCategory.HeaderVisibilityProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGalleryCategory.IsSharedColumnSizeScopeProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGalleryCategory.MaxColumnCountProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonGalleryCategory.MinColumnCountProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _ribbonGalleryItemProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = RibbonGalleryItem.IsSelectedProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _ribbonGroupProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = RibbonGroup.GroupSizeDefinitionsProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _textBoxProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = TextBox.TextProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _ribbonComboBoxProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = RibbonComboBox.IsEditableProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonComboBox.IsReadOnlyProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonComboBox.StaysOpenOnEditProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = RibbonComboBox.TextProperty, Mode = TransferMode.AlwaysTransfer },
                    };

                    _scrollProperties = new PropertyAndTransferMode[]
                    {
                        new PropertyAndTransferMode () { Property = ScrollViewer.HorizontalScrollBarVisibilityProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = ScrollViewer.VerticalScrollBarVisibilityProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = ScrollViewer.CanContentScrollProperty, Mode = TransferMode.AlwaysTransfer },
                        new PropertyAndTransferMode () { Property = ScrollViewer.IsDeferredScrollingEnabledProperty, Mode = TransferMode.AlwaysTransfer },
                    };
                }
            }
        }

        private static object CreateClone(object original)
        {
            bool allowTransformations = false;
            return CreateClone(original, allowTransformations);
        }

        internal static object CreateClone(object original, bool allowTransformations)
        {
            if (original is UIElement ||
                original is ContentElement)
            {
                FrameworkElement feOriginal = original as FrameworkElement;
                if (feOriginal != null)
                {
                    FrameworkElement feClone = CreateInstance(feOriginal, allowTransformations);

                    if (feOriginal.TemplatedParent == null ||
                        feOriginal.TemplatedParent is ContentPresenter)
                    {
                        TransferProperties(feOriginal, feClone, /*cloningForTemplatePart*/ false);
                    }
                    else
                    {
                        TransferProperties(feOriginal.TemplatedParent as FrameworkElement, feClone, /*cloningForTemplatePart*/ true);
                    }

                    FrameworkElement feWrapper = WrapClone(feClone, allowTransformations);
                    if (feWrapper != feClone)
                    {
                        TransferProperties(feClone, feWrapper, /*cloningForTemplatePart*/ false);
                    }

                    return feWrapper;
                }
                else
                {
                    object clone = Activator.CreateInstance(original.GetType());
                    TransferMarkupProperties(original, clone);
                    return clone;
                }
            }
            else
            {
                Freezable freezable = original as Freezable;
                if (freezable != null && !freezable.CanFreeze)
                {
                    return freezable.Clone();
                }
            }

            return original;
        }

        private static FrameworkElement CreateInstance(FrameworkElement original, bool allowTransformations)
        {
            if (allowTransformations)
            {
                RibbonMenuItem menuItem = original as RibbonMenuItem;

                if (menuItem != null)
                {
                    // Determine which control type the wrapper should be
                    // based on the Items and IsCheckable values.
                    if (!(original is RibbonSplitMenuItem))
                    {
                        if (menuItem.IsCheckable)
                        {
                            return new RibbonCheckBox();
                        }
                        else if (menuItem.Items.Count == 0)
                        {
                            return new RibbonButton();
                        }
                        else
                        {
                            return new RibbonMenuButton();
                        }
                    }
                    else
                    {
                        if (menuItem.Items.Count == 0)
                        {
                            if (menuItem.IsCheckable)
                            {
                                return new RibbonToggleButton();
                            }
                            else
                            {
                                return new RibbonButton();
                            }
                        }
                        else
                        {
                            return new RibbonSplitButton();
                        }
                    }
                }

#if IN_RIBBON_GALLERY
                InRibbonGallery inRibbonGallery = original as InRibbonGallery;
                if (inRibbonGallery != null)
                {
                    return new RibbonMenuButton();
                }
#endif
            }

            return (FrameworkElement)Activator.CreateInstance(original.GetType());
        }

        // RibbonGallery cannot be added to the QAT directly.  Instead, the
        // RibbonGallery is wrapped inside a RibbonMenuButton when adding
        // it to the QAT. Here we create that RibbonMenuButton host and
        // transfer over interesting properties from the RibbonGallery to
        // the RibbonMenuButton host
        private static FrameworkElement WrapClone(FrameworkElement clone, bool allowTransformations)
        {
            if (allowTransformations && clone is RibbonGallery)
            {
                RibbonMenuButton wrapperButton = new RibbonMenuButton();
                wrapperButton.Items.Add(clone);
                return wrapperButton;
            }

            return clone;
        }

        // The following is a list of usual suspects that are template bound, data bound or locally set.
        //
        // These are the possible combinations
        // - Original               - Clone
        //   --------                 -----
#if IN_RIBBON_GALLERY
        // - InRibbonGallery        - RibbonMenuButton
        // - InRibbonGallery        - InRibbonGallery
#endif
        // - RibbonButton           - RibbonButton
        // - RibbonToggleButton     - RibbonToggleButton
        // - RibbonRadioButton      - RibbonRadioButton
        // - RibbonCheckBox         - RibbonCheckBox
        // - RibbonTextBox          - RibbonTextBox
        // - RibbonMenuButton       - RibbonMenuButton
        // - RibbonSplitButton      - RibbonButton
        // - RibbonSplitButton      - RibbonToggleButton
        // - RibbonSplitButton      - RibbonSplitButton
        // - RibbonMenuItem         - RibbonButton
        // - RibbonMenuItem         - RibbonToggleButton
        // - RibbonMenuItem         - RibbonMenuButton
        // - RibbonMenuItem         - RibbonMenuItem
        // - RibbonSplitMenuItem    - RibbonButton
        // - RibbonSplitMenuItem    - RibbonToggleButton
        // - RibbonSplitMenuItem    - RibbonSplitButton
        // - RibbonSplitMenuItem    - RibbonSplitMenuItem
        // - RibbonGallery          - RibbonMenuButton
        // - RibbonGallery          - RibbonGallery
        // - RibbonSeparator        - RibbonSeparator
        // - RibbonGroup            - RibbonGroup
        // - RibbonComboBox         - RibbonComboBox
        // - RibbonGalleryCategory  - RibbonGalleryCategory
        // - RibbonGalleryItem      - RibbonGalleryItem

        private static void TransferProperties(FrameworkElement original, FrameworkElement clone, bool cloningForTemplatePart)
        {
#if IN_RIBBON_GALLERY
            if (original is InRibbonGallery)
            {
                Debug.Assert(clone is RibbonMenuButton);

                TransferProperties(original, clone, _automationProperties);
                TransferProperties(original, clone, _feProperties);
                TransferProperties(original, clone, _controlProperties);
                TransferProperties(original, clone, _ribbonProperties);
                TransferProperties(original, clone, _ribbonBrushProperties);
                TransferProperties(original, clone, _itemsControlProperties);
                TransferItems((ItemsControl)original, (ItemsControl)clone);
                TransferProperties(original, clone, _ribbonMenuButtonProperties);
                TransferProperties(original, clone, _scrollProperties);
            }
            else if (clone.GetType().IsInstanceOfType(original))
#else
            if (clone.GetType().IsInstanceOfType(original))
#endif
            {
                TransferProperties(original, clone, _automationProperties);

                TransferProperties(original, clone, _feProperties);
                TransferProperty(original, clone, FrameworkElement.StyleProperty, TransferMode.AlwaysTransfer);

                if (original is Control)
                {
                    TransferProperties(original, clone, _controlProperties);
                    TransferProperties(original, clone, _ribbonProperties);
                    TransferProperties(original, clone, _ribbonBrushProperties);

                    if (original is ContentControl)
                    {
                        TransferProperties(original, clone, _contentControlProperties);

                        if (original is ButtonBase)
                        {
                            TransferProperties(original, clone, _buttonProperties);

                            if (original is ToggleButton)
                            {
                                TransferProperties(original, clone, _toggleButtonProperties);
                            }
                        }
                        else if (original is RibbonGalleryItem)
                        {
                            TransferProperties(original, clone, _ribbonGalleryItemProperties);
                        }
                    }
                    else if (original is ItemsControl)
                    {
                        TransferProperties(original, clone, _itemsControlProperties);
                        TransferProperties(original, clone, _scrollProperties);
                        TransferItems((ItemsControl)original, (ItemsControl)clone);

                        if (original is HeaderedItemsControl)
                        {
                            TransferProperties(original, clone, _headeredItemsControlProperties);

                            if (original is RibbonMenuItem)
                            {
                                TransferProperties(original, clone, _ribbonMenuItemProperties);

                                if (original is RibbonSplitMenuItem)
                                {
                                    TransferProperties(original, clone, _ribbonSplitMenuItemProperties);
                                }
                            }
                            else if (original is RibbonGalleryCategory)
                            {
                                TransferProperties(original, clone, _ribbonGalleryCategoryProperties);
                            }
                            else if (original is RibbonGroup)
                            {
                                TransferProperties(original, clone, _ribbonGroupProperties);
                            }
                        }
                        else if (original is RibbonMenuButton)
                        {
                            TransferProperties(original, clone, _ribbonMenuButtonProperties);

                            if (original is RibbonSplitButton)
                            {
                                TransferProperties(original, clone, _ribbonSplitButtonProperties);
                            }
                            else if (original is RibbonComboBox)
                            {
                                TransferProperties(original, clone, _ribbonComboBoxProperties);
                            }
                        }
                        else if (original is RibbonGallery)
                        {
                            TransferProperties(original, clone, _ribbonGalleryProperties);
                        }
                    }
                    else if (original is TextBox)
                    {
                        TransferProperties(original, clone, _textBoxProperties);
                    }
                }

                TransferMarkupProperties(original, clone);
            }
            else
            {
                TransferProperties(original, clone, _automationProperties);
                TransferProperties(original, clone, _feProperties);
                TransferProperties(original, clone, _controlProperties);

                if (original is RibbonSplitButton)
                {
                    Debug.Assert(clone is ButtonBase,
                        "We should only be here if a SplitButton's header is being added to the QAT");

                    TransferProperties(original, clone, _ribbonProperties);
                    TransferProperties(original, clone, _ribbonBrushProperties);

                    TransferProperty(original, clone, RibbonSplitButton.IsCheckedProperty, ToggleButton.IsCheckedProperty, TransferMode.AlwaysTransfer);
                    TransferProperty(original, clone, RibbonSplitButton.HeaderQuickAccessToolBarIdProperty, RibbonControlService.QuickAccessToolBarIdProperty, TransferMode.AlwaysTransfer);
                    TransferProperty(original, clone, RibbonSplitButton.CommandProperty, ButtonBase.CommandProperty, TransferMode.AlwaysTransfer);
                    TransferProperty(original, clone, RibbonSplitButton.CommandParameterProperty, ButtonBase.CommandParameterProperty, TransferMode.AlwaysTransfer);
                    TransferProperty(original, clone, RibbonSplitButton.CommandTargetProperty, ButtonBase.CommandTargetProperty, TransferMode.AlwaysTransfer);
                }
                else if (original is RibbonMenuItem)
                {
                    TransferProperties(original, clone, _ribbonBrushProperties);

                    if (original.GetValue(RibbonMenuItem.HeaderProperty) is String)
                    {
                        TransferProperty(original, clone, RibbonMenuItem.HeaderProperty, RibbonControlService.LabelProperty, TransferMode.AlwaysTransfer);
                    }

                    TransferProperty(original, clone, RibbonMenuItem.QuickAccessToolBarImageSourceProperty, RibbonControlService.SmallImageSourceProperty, TransferMode.AlwaysTransfer);

                    TransferProperty(original, clone, RibbonControlService.ToolTipTitleProperty, TransferMode.AlwaysTransfer);
                    TransferProperty(original, clone, RibbonControlService.ToolTipDescriptionProperty, TransferMode.AlwaysTransfer);
                    TransferProperty(original, clone, RibbonControlService.ToolTipImageSourceProperty, TransferMode.AlwaysTransfer);
                    TransferProperty(original, clone, RibbonControlService.ToolTipFooterTitleProperty, TransferMode.AlwaysTransfer);
                    TransferProperty(original, clone, RibbonControlService.ToolTipFooterDescriptionProperty, TransferMode.AlwaysTransfer);
                    TransferProperty(original, clone, RibbonControlService.ToolTipFooterImageSourceProperty, TransferMode.AlwaysTransfer);
                    TransferProperty(original, clone, RibbonControlService.QuickAccessToolBarIdProperty, TransferMode.AlwaysTransfer);

                    if (original is RibbonSplitMenuItem)
                    {
                        Debug.Assert(clone is ButtonBase || clone is RibbonMenuButton,
                            "We could be here if either the header of a SplitMenuItem is being added to the QAT as a Button or a Toggle Button or the entire SplitMenuItem is being added to the QAT as MenuButton or a SplitButton");

                        if (clone is ButtonBase)
                        {
                            TransferProperty(original, clone, RibbonMenuItem.IsCheckedProperty, ToggleButton.IsCheckedProperty, TransferMode.AlwaysTransfer);
                            TransferProperty(original, clone, RibbonMenuItem.CommandProperty, ButtonBase.CommandProperty, TransferMode.AlwaysTransfer);
                            TransferProperty(original, clone, RibbonMenuItem.CommandParameterProperty, ButtonBase.CommandParameterProperty, TransferMode.AlwaysTransfer);
                            TransferProperty(original, clone, RibbonMenuItem.CommandTargetProperty, ButtonBase.CommandTargetProperty, TransferMode.AlwaysTransfer);
                            if (cloningForTemplatePart)
                            {
                                TransferProperty(original, clone, RibbonSplitMenuItem.HeaderQuickAccessToolBarIdProperty, RibbonControlService.QuickAccessToolBarIdProperty, TransferMode.AlwaysTransfer);
                            }
                        }
                        else if (clone is RibbonMenuButton)
                        {
                            TransferProperties(original, clone, _itemsControlProperties);
                            TransferItems((ItemsControl)original, (ItemsControl)clone);
                            TransferProperties(original, clone, _ribbonMenuButtonProperties);

                            if (clone is RibbonSplitButton)
                            {
                                TransferProperty(original, clone, RibbonMenuItem.IsCheckableProperty, RibbonSplitButton.IsCheckableProperty, TransferMode.AlwaysTransfer);
                                TransferProperty(original, clone, RibbonMenuItem.IsCheckedProperty, RibbonSplitButton.IsCheckedProperty, TransferMode.AlwaysTransfer);
                                TransferProperty(original, clone, RibbonMenuItem.CommandProperty, RibbonSplitButton.CommandProperty, TransferMode.AlwaysTransfer);
                                TransferProperty(original, clone, RibbonMenuItem.CommandParameterProperty, RibbonSplitButton.CommandParameterProperty, TransferMode.AlwaysTransfer);
                                TransferProperty(original, clone, RibbonMenuItem.CommandTargetProperty, RibbonSplitButton.CommandTargetProperty, TransferMode.AlwaysTransfer);
                                TransferProperty(original, clone, RibbonSplitMenuItem.DropDownToolTipTitleProperty, RibbonSplitButton.DropDownToolTipTitleProperty, TransferMode.AlwaysTransfer);
                                TransferProperty(original, clone, RibbonSplitMenuItem.DropDownToolTipDescriptionProperty, RibbonSplitButton.DropDownToolTipDescriptionProperty, TransferMode.AlwaysTransfer);
                                TransferProperty(original, clone, RibbonSplitMenuItem.DropDownToolTipImageSourceProperty, RibbonSplitButton.DropDownToolTipImageSourceProperty, TransferMode.AlwaysTransfer);
                                TransferProperty(original, clone, RibbonSplitMenuItem.DropDownToolTipFooterTitleProperty, RibbonSplitButton.DropDownToolTipFooterTitleProperty, TransferMode.AlwaysTransfer);
                                TransferProperty(original, clone, RibbonSplitMenuItem.DropDownToolTipFooterDescriptionProperty, RibbonSplitButton.DropDownToolTipFooterDescriptionProperty, TransferMode.AlwaysTransfer);
                                TransferProperty(original, clone, RibbonSplitMenuItem.DropDownToolTipFooterImageSourceProperty, RibbonSplitButton.DropDownToolTipFooterImageSourceProperty, TransferMode.AlwaysTransfer);
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert(clone is ButtonBase || clone is RibbonMenuButton,
                            "We could be here if either a leaf level MenuItem is being added to the QAT as a Button or a Toggle Button or the entire MenuItem is being added to the QAT as MenuButton");

                        if (clone is ButtonBase)
                        {
                            TransferProperty(original, clone, RibbonMenuItem.IsCheckedProperty, ToggleButton.IsCheckedProperty, TransferMode.AlwaysTransfer);
                            TransferProperty(original, clone, RibbonMenuItem.CommandProperty, ButtonBase.CommandProperty, TransferMode.AlwaysTransfer);
                            TransferProperty(original, clone, RibbonMenuItem.CommandParameterProperty, ButtonBase.CommandParameterProperty, TransferMode.AlwaysTransfer);
                            TransferProperty(original, clone, RibbonMenuItem.CommandTargetProperty, ButtonBase.CommandTargetProperty, TransferMode.AlwaysTransfer);
                        }
                        else
                        {
                            TransferProperties(original, clone, _itemsControlProperties);
                            TransferItems((ItemsControl)original, (ItemsControl)clone);
                            TransferProperties(original, clone, _ribbonMenuButtonProperties);
                        }
                    }
                }
                else if (original is RibbonGallery)
                {
                    Debug.Assert(clone is RibbonMenuButton,
                        "We should only be here if a RibbonGallery is being wrapped in a RibbonMenuButton");

                    TransferProperties(original, clone, _ribbonProperties);
                    TransferProperties(original, clone, _ribbonBrushProperties);
                }
            }
        }

        private static void TransferProperties(FrameworkElement original, FrameworkElement clone, PropertyAndTransferMode[] properties)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                TransferProperty(original, clone, properties[i].Property, properties[i].Mode);
            }
        }

        private static void TransferItems(ItemsControl originalItemsControl, ItemsControl cloneItemsControl)
        {
            if (originalItemsControl.ItemsSource == null)
            {
                for (int i = 0; i < originalItemsControl.Items.Count; i++)
                {
                    object clonedItem = CreateClone(originalItemsControl.Items[i]);
                    cloneItemsControl.Items.Add(clonedItem);
                }
            }
        }

        private static void TransferMarkupProperties(object original, object clone)
        {
            MarkupObject markupObjOriginal = MarkupWriter.GetMarkupObjectFor(original);
            foreach (MarkupProperty markupProp in markupObjOriginal.Properties)
            {
                if (markupProp.DependencyProperty != null)
                {
                    DependencyObject cloneElement = (DependencyObject)clone;

                    BaseValueSource baseValueSource = DependencyPropertyHelper.GetValueSource(
                        cloneElement, markupProp.DependencyProperty).BaseValueSource;
                    if (baseValueSource >= BaseValueSource.ParentTemplate)
                    {
                        // If we have already transferred this property
                        // previously omit it now.

                        continue;
                    }

                    if (markupProp.DependencyProperty == KeyTipService.KeyTipProperty ||
                        markupProp.DependencyProperty == RibbonSplitButton.HeaderKeyTipProperty ||
                        markupProp.DependencyProperty == RibbonMenuButton.IsDropDownOpenProperty ||
                        markupProp.DependencyProperty == RibbonGroup.IsDropDownOpenProperty ||
                        markupProp.DependencyProperty == MenuItem.IsSubmenuOpenProperty)
                    {
                        // Do not copy the KeyTip properties. KeyTips for elements
                        // in the QAT will be generated separately (except HeaderKeyTip).

                        continue;
                    }


                    BindingBase binding = markupProp.Value as BindingBase;
                    if (binding != null)
                    {
                        // Transfer bindings.

                        BindingOperations.SetBinding(cloneElement, markupProp.DependencyProperty, binding);
                    }
                    else
                    {
                        MarkupExtension markupExtension = markupProp.Value as MarkupExtension;
                        if (markupExtension != null)
                        {
                            // Transfer dynamic resources and other markup extensions.

                            cloneElement.SetValue(markupProp.DependencyProperty, markupExtension.ProvideValue(null));
                        }
                        else
                        {
                            // Transfer other DependencyProperties.

                            object clonedPropertyValue = CreateClone(markupProp.Value);
                            cloneElement.SetValue(markupProp.DependencyProperty, clonedPropertyValue);
                        }
                    }
                }
                else if (markupProp.PropertyDescriptor != null)
                {
                    // Transfer CLR properties.

                    if (markupProp.PropertyDescriptor.SerializationVisibility == DesignerSerializationVisibility.Content)
                    {
                        if (markupProp.Name == "Items" &&
                            markupProp.PropertyDescriptor.ComponentType == typeof(ItemsControl))
                        {
                            // Skip the ItemsControl.Items property
                            // since this will be copied automatically.

                            continue;
                        }

                        IList items = markupProp.PropertyDescriptor.GetValue(clone) as IList;
                        if (items != null)
                        {
                            foreach (MarkupObject subObj in markupProp.Items)
                            {
                                items.Add(CreateClone(subObj));
                            }
                        }
                        else
                        {
                            object clonedPropertyValue = CreateClone(markupProp.Value);
                            markupProp.PropertyDescriptor.SetValue(clone, clonedPropertyValue);
                        }
                    }
                    else
                    {
                        object clonedPropertyValue = CreateClone(markupProp.Value);
                        markupProp.PropertyDescriptor.SetValue(clone, clonedPropertyValue);
                    }
                }
            }
        }

        #endregion QAT

        #region ApplicationMenu

        internal static void SetApplicationMenuLevel(bool parentIsTopLevel, DependencyObject element)
        {
            RibbonApplicationMenuItem rami = element as RibbonApplicationMenuItem;
            if (rami != null)
            {
                if (parentIsTopLevel)
                {
                    rami.Level = RibbonApplicationMenuItemLevel.Middle;
                }
                else
                {
                    rami.Level = RibbonApplicationMenuItemLevel.Sub;
                }
            }
            else
            {
                RibbonApplicationSplitMenuItem rasmi = element as RibbonApplicationSplitMenuItem;
                if (rasmi != null)
                {
                    if (parentIsTopLevel)
                    {
                        rasmi.Level = RibbonApplicationMenuItemLevel.Middle;
                    }
                    else
                    {
                        rasmi.Level = RibbonApplicationMenuItemLevel.Sub;
                    }
                }
            }
        }

        internal static bool CoerceIsSubmenuOpenForTopLevelItem(RibbonMenuItem menuItem, ItemsControl parentItemsControl, bool baseValue)
        {
            bool isSubMenuOpen = (bool)baseValue;
            if (!isSubMenuOpen && menuItem.CloseSubmenuTimer != null && menuItem.CloseSubmenuTimer.IsEnabled)
            {
                RibbonApplicationMenu ram = parentItemsControl as RibbonApplicationMenu;
                if (ram != null)
                {
                    RibbonMenuItem currentMenuItem = ram.RibbonCurrentSelection as RibbonMenuItem;
                    if (currentMenuItem != null && currentMenuItem.CanOpenSubMenu && currentMenuItem != menuItem)
                    {
                        return true;
                    }
                }
            }

            return baseValue;
        }

        internal static void HookPopupForTopLevelMenuItem(RibbonMenuItem menuItem, ItemsControl parentItemsControl)
        {
            Popup popup = menuItem.Popup;
            if (popup != null)
            {
                Binding binding = new Binding("SubmenuPlaceholder");
                binding.Source = parentItemsControl;
                BindingOperations.SetBinding(popup, Popup.PlacementTargetProperty, binding);

                binding = new Binding("SubmenuPlaceholder.ActualWidth");
                binding.Source = parentItemsControl;
                BindingOperations.SetBinding(popup, Popup.WidthProperty, binding);

                binding = new Binding("SubmenuPlaceholder.ActualHeight");
                binding.Source = parentItemsControl;
                BindingOperations.SetBinding(popup, Popup.HeightProperty, binding);
                BindingOperations.SetBinding(menuItem, RibbonMenuItem.DropDownHeightProperty, binding);
            }
        }

        internal static void UnhookPopupForTopLevelMenuItem(RibbonMenuItem menuItem)
        {
            Popup popup = menuItem.Popup;
            if (popup != null)
            {
                popup.ClearValue(Popup.PlacementTargetProperty);
                popup.ClearValue(Popup.WidthProperty);
                popup.ClearValue(Popup.HeightProperty);
                menuItem.CoerceValue(RibbonMenuItem.DropDownHeightProperty);
            }
        }

        public static void OnApplicationMenuItemUpDownKeyDown(KeyEventArgs e, RibbonMenuItem menuItem)
        {
            if (e.Handled || menuItem.IsSubmenuOpen)
            {
                return;
            }

            if (e.Key == Key.Up ||
                e.Key == Key.Down)
            {
                RibbonApplicationMenu applicationMenu = ItemsControl.ItemsControlFromItemContainer(menuItem) as RibbonApplicationMenu;
                if (applicationMenu != null)
                {
                    if (RibbonHelper.IsEndFocusableMenuItem(menuItem, e.Key == Key.Up /* isFirst */))
                    {
                        if (e.Key == Key.Down)
                        {
                            // If the focus is at the last focusable item,
                            // then try moving the focus to first element of
                            // auxiliary pane and then to first element of footer
                            // pane if needed.
                            if (applicationMenu.AuxiliaryPaneMoveFocus(FocusNavigationDirection.First) ||
                                applicationMenu.FooterPaneMoveFocus(FocusNavigationDirection.First))
                            {
                                e.Handled = true;
                            }
                        }
                        else
                        {
                            // If the focus is at the first focusable item,
                            // then try moving the focus to last element of
                            // footer pane and then to last element of auxiliary
                            // pane if needed.
                            if (applicationMenu.FooterPaneMoveFocus(FocusNavigationDirection.Last) ||
                                applicationMenu.AuxiliaryPaneMoveFocus(FocusNavigationDirection.Last))
                            {
                                e.Handled = true;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Helper method to determine if the given menu item
        ///     is the first / last focusable item of its parent.
        /// </summary>
        private static bool IsEndFocusableMenuItem(RibbonMenuItem menuItem, bool isFirst)
        {
            ItemsControl parentItemsControl = ItemsControl.ItemsControlFromItemContainer(menuItem);
            Debug.Assert(parentItemsControl != null);

            int parentItemCount = parentItemsControl.Items.Count;
            int itemIndex = parentItemsControl.ItemContainerGenerator.IndexFromContainer(menuItem);

            int incr = 1;
            if (isFirst)
            {
                incr = -1;
            }

            for (int i = itemIndex + incr; i < parentItemCount && i >= 0; i += incr)
            {
                UIElement container = parentItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as UIElement;
                if (container != null &&
                    container.IsVisible &&
                    container.IsEnabled &&
                    container.Focusable)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region KeyTips

#if RIBBON_IN_FRAMEWORK
#endif
        public static PresentationSource GetPresentationSourceFromVisual(Visual visual)
        {
            if (visual == null)
            {
                return null;
            }

#if RIBBON_IN_FRAMEWORK
            return PresentationSource.CriticalFromVisual(visual);
#else
            return PresentationSource.FromVisual(visual);
#endif
        }

        /// <summary>
        ///  Produces default system beep.
        /// </summary>
        public static void Beep()
        {
            // Ignore the results of Beep because it is of very low importance.
            NativeMethods.MessageBeep(0);
        }

        // ------------------------------------------------------------------
        // Retrieve CultureInfo property from specified element.
        // ------------------------------------------------------------------
        public static CultureInfo GetCultureInfo(DependencyObject element)
        {
            XmlLanguage language = (XmlLanguage)element.GetValue(FrameworkElement.LanguageProperty);
            try
            {
                return language.GetSpecificCulture();
            }
            catch (InvalidOperationException)
            {
                // We default to en-US if no part of the language tag is recognized.
                return InvariantEnglishUS;
            }
        }

        private static CultureInfo invariantEnglishUS;
        public static CultureInfo InvariantEnglishUS
        {
            get
            {
                if (invariantEnglishUS == null)
                {
                    invariantEnglishUS = CultureInfo.ReadOnly(new CultureInfo("en-us", false));
                }
                return invariantEnglishUS;
            }
        }

        /// <summary>
        ///     Returns the first UIElement in the ancestral chain
        ///     including the element itself.
        /// </summary>
        public static UIElement GetContainingUIElement(DependencyObject element)
        {
            UIElement uie = element as UIElement;
            if (uie != null)
            {
                return uie;
            }
            else
            {
                ContentElement ce = element as ContentElement;
                if (ce != null)
                {
                    DependencyObject parent = ContentOperations.GetParent(ce);
                    if (parent == null)
                    {
                        parent = LogicalTreeHelper.GetParent(ce);
                    }

                    if (parent != null)
                    {
                        return GetContainingUIElement(parent);
                    }
                }
            }
            return null;
        }

        public static void SetDefaultQatKeyTipPlacement(ActivatingKeyTipEventArgs e)
        {
            e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter;
            e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipTopAtTargetCenter;
            e.KeyTipHorizontalOffset = e.KeyTipVerticalOffset = 0;
        }

        /// <summary>
        ///     Helper method to determine the keytip position for
        ///     RibbonButton, RibbonToggleButton, RibbonMenuButton,
        ///     and RibbonRadioButton.
        /// </summary>
        public static void SetKeyTipPlacementForButton(DependencyObject element,
            ActivatingKeyTipEventArgs e,
            UIElement mediumPlacementTarget)
        {
            if (RibbonControlService.GetIsInQuickAccessToolBar(element))
            {
                SetDefaultQatKeyTipPlacement(e);
            }
            else
            {
                e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter;
                e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetCenter;
                e.KeyTipHorizontalOffset = e.KeyTipVerticalOffset = 0;
                RibbonControlSizeDefinition controlSizeDefinition = RibbonControlService.GetControlSizeDefinition(element);
                if (controlSizeDefinition != null)
                {
                    if (controlSizeDefinition.IsLabelVisible)
                    {
                        if (controlSizeDefinition.ImageSize == RibbonImageSize.Large)
                        {
                            e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetBottom;
                        }
                        else if (controlSizeDefinition.ImageSize == RibbonImageSize.Small)
                        {
                            e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipLeftAtTargetCenter;
                            e.PlacementTarget = mediumPlacementTarget;
                        }
                    }
                    else
                    {
                        if (controlSizeDefinition.ImageSize == RibbonImageSize.Small)
                        {
                            e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipLeftAtTargetCenter;
                        }
                    }
                }
                else
                {
                    e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetBottom;
                }
            }
        }

        /// <summary>
        ///     Helper method which determines the keytip position for
        ///     RibbonTextBox, RibbonCheckBox and RibbonComboBox.
        /// </summary>
        public static void SetKeyTipPlacementForTextBox(DependencyObject element,
            ActivatingKeyTipEventArgs e,
            UIElement nonLargePlacementTarget)
        {
            if (RibbonControlService.GetIsInQuickAccessToolBar(element))
            {
                SetDefaultQatKeyTipPlacement(e);
            }
            else
            {
                e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter;
                e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetCenter;
                e.KeyTipHorizontalOffset = e.KeyTipVerticalOffset = 0;
                RibbonControlSizeDefinition controlSizeDefinition = RibbonControlService.GetControlSizeDefinition(element);
                if (controlSizeDefinition != null)
                {
                    if (controlSizeDefinition.ImageSize == RibbonImageSize.Large)
                    {
                        e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetBottom;
                    }
                    else
                    {
                        e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipLeftAtTargetCenter;
                        e.PlacementTarget = nonLargePlacementTarget;
                    }
                }
                else
                {
                    e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetBottom;
                }
            }
        }

        public static void SetKeyTipPlacementForSplitButtonHeader(RibbonSplitButton splitButton,
            ActivatingKeyTipEventArgs e,
            UIElement mediumPlacementTarget)
        {
            bool dropDownKeyTipSet = !string.IsNullOrEmpty(splitButton.KeyTip);
            if (splitButton.IsInQuickAccessToolBar)
            {
                SetDefaultQatKeyTipPlacement(e);
            }
            else
            {
                e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter;
                e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetCenter;
                e.KeyTipHorizontalOffset = e.KeyTipVerticalOffset = 0;
                RibbonControlSizeDefinition controlSizeDefinition = splitButton.ControlSizeDefinition;
                if (controlSizeDefinition != null)
                {
                    if (controlSizeDefinition.IsLabelVisible)
                    {
                        if (controlSizeDefinition.ImageSize == RibbonImageSize.Large)
                        {
                            e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetTop;
                        }
                        else if (controlSizeDefinition.ImageSize == RibbonImageSize.Small)
                        {
                            e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipLeftAtTargetCenter;
                            e.PlacementTarget = mediumPlacementTarget;
                        }
                    }
                    else
                    {
                        if (controlSizeDefinition.ImageSize == RibbonImageSize.Small)
                        {
                            if (dropDownKeyTipSet)
                            {
                                e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter;
                            }
                            else
                            {
                                e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipLeftAtTargetCenter;
                            }
                        }
                    }
                }
                else
                {
                    e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetTop;
                }
            }
        }

        public static void SetKeyTipPlacementForSplitButtonDropDown(RibbonSplitButton splitButton,
            ActivatingKeyTipEventArgs e,
            UIElement mediumPlacementTarget)
        {
            bool headerKeyTipSet = !(string.IsNullOrEmpty(splitButton.HeaderKeyTip));
            if (splitButton.IsInQuickAccessToolBar)
            {
                SetDefaultQatKeyTipPlacement(e);
                if (headerKeyTipSet)
                {
                    e.PlacementTarget = mediumPlacementTarget;
                }
            }
            else
            {
                e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter;
                e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetCenter;
                e.KeyTipHorizontalOffset = e.KeyTipVerticalOffset = 0;
                RibbonControlSizeDefinition controlSizeDefinition = splitButton.ControlSizeDefinition;
                if (controlSizeDefinition != null)
                {
                    if (controlSizeDefinition.IsLabelVisible)
                    {
                        if (controlSizeDefinition.ImageSize == RibbonImageSize.Large)
                        {
                            e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetBottom;
                        }
                        else
                        {
                            e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter;
                            if (headerKeyTipSet)
                            {
                                e.PlacementTarget = mediumPlacementTarget;
                            }
                        }
                    }
                    else
                    {
                        if (controlSizeDefinition.ImageSize == RibbonImageSize.Small)
                        {
                            e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter;
                            if (headerKeyTipSet)
                            {
                                e.PlacementTarget = mediumPlacementTarget;
                            }
                        }
                    }
                }
                else
                {
                    e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetBottom;
                }
            }
        }

#if IN_RIBBON_GALLERY
        public static void SetKeyTipPlacementForInRibbonGallery(InRibbonGallery irg,
            ActivatingKeyTipEventArgs e)
        {
            if (irg.IsInQuickAccessToolBar)
            {
                SetDefaultQatKeyTipPlacement(e);
            }
            else
            {
                e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipLeftAtTargetCenter;
                e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipTopAtTargetBottom;
                e.KeyTipHorizontalOffset = e.KeyTipVerticalOffset = 0;

                if (irg.PartToggleButton != null)
                {
                    e.PlacementTarget = irg.PartToggleButton;
                }
            }
        }
#endif

        public static void OpenParentRibbonGroupDropDownSync(FrameworkElement fe, bool templateApplied)
        {
            if (!templateApplied)
            {
                // Apply template if not yet applied.
                fe.ApplyTemplate();
            }

            // Get the Parent RibbonGroup and open its dropdown if needed.
            RibbonGroup ribbonGroup = TreeHelper.FindAncestor(fe, delegate(DependencyObject element) { return (element is RibbonGroup); }) as RibbonGroup;
            if (ribbonGroup == null)
            {
                ribbonGroup = TreeHelper.FindLogicalAncestor<RibbonGroup>(fe);
            }
            if (ribbonGroup != null &&
                ribbonGroup.IsCollapsed &&
                !ribbonGroup.IsDropDownOpen)
            {
                ribbonGroup.IsDropDownOpen = true;
                fe.UpdateLayout();
            }
        }

        #endregion

        #region UIA

        internal static AutomationPeer CreatePeer(UIElement element)
        {
            AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(element);
            if (peer == null)
            {
                FrameworkElement elementFE = element as FrameworkElement;
                if (elementFE != null)
                    peer = new FrameworkElementAutomationPeer(elementFE);
                else
                    peer = new UIElementAutomationPeer(element);
            }

            return peer;
        }

        #endregion UIA

        #region DropDown ItemsControls

        public static void SetDropDownHeight(FrameworkElement itemsPresenter, bool hasGallery, double dropDownHeight)
        {
            if (itemsPresenter != null)
            {
                // First time the dropdown opens, HasGallery is always false
                // because item container generation never happened yet and hence
                // it ignores DropDownHeight. Hence reuse DropDownHeight when HasGallery
                // value changes.
                double oldHeight = itemsPresenter.Height;
                double newHeight = double.NaN;
                if (hasGallery)
                {
                    newHeight = dropDownHeight;
                }

                if (!DoubleUtil.AreClose(oldHeight, newHeight) &&
                    !(double.IsNaN(oldHeight) && double.IsNaN(newHeight)))
                {
                    itemsPresenter.Height = newHeight;
                    itemsPresenter.Dispatcher.BeginInvoke(
                        (Action)delegate()
                        {
                            TreeHelper.InvalidateMeasureForVisualAncestorPath<Popup>(itemsPresenter);
                        },
                        DispatcherPriority.Normal,
                        null);
                }
            }
        }

        internal static void InvalidateScrollBarVisibility(ScrollViewer submenuScrollViewer)
        {
            if (submenuScrollViewer != null)
            {
                // The scroll viewer needs to re-evaluate the visibility of the scrollbars
                // and that happens in its MeasureOverride call. Also note that we need to
                // make this invalidate call async because we may already be within a
                // ScrollViewer measure pass, by which we would miss the boat.

                submenuScrollViewer.Dispatcher.BeginInvoke((Action)delegate()
                {
                    submenuScrollViewer.InvalidateMeasure();
                },
                DispatcherPriority.Render);
            }
        }

        #endregion

        #region Transforms

        internal static Matrix GetTransformToDevice(Visual targetVisual)
        {
            HwndSource hwndSource = null;
            if (targetVisual != null)
            {
                hwndSource = PresentationSource.FromVisual(targetVisual) as HwndSource;
            }

            if (hwndSource != null)
            {
                CompositionTarget ct = hwndSource.CompositionTarget;
                if (ct != null)
                {
                    return ct.TransformToDevice;
                }
            }

            return Matrix.Identity;
        }

        #endregion Transforms

        #region Resizing

        /// <summary>
        ///     Recursively patches the measure invalidation across the
        ///     descendant paths
        /// </summary>
        public static bool FixMeasureInvalidationPaths(DependencyObject element)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(element);
            bool measureInvalid = false;
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(element, i);
                if (child != null)
                {
                    // Determine if any of the child's subtree is
                    // dirty for measure
                    measureInvalid |= FixMeasureInvalidationPaths(child);
                }
            }
            UIElement uie = element as UIElement;
            if (uie != null)
            {
                if (!uie.IsMeasureValid)
                {
                    measureInvalid = true;
                }
                else if (measureInvalid)
                {
                    // Invalidate self for measure if any of the
                    // descendants in the subtree is invalid
                    uie.InvalidateMeasure();
                }
            }
            return measureInvalid;
        }

        #endregion
    }
}
