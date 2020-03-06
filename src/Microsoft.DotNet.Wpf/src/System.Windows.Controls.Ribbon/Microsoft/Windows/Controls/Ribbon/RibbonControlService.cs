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
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon.Primitives;
    using Microsoft.Windows.Controls;
#else
    using Microsoft.Windows.Controls.Ribbon.Primitives;
#endif

    #endregion

    /// <summary>
    ///     A Static class declaring attached dependency properties
    ///     needed for ribbon controls.
    /// </summary>
    public static class RibbonControlService
    {
        #region ImageSource Properties

        /// <summary>
        ///     DependencyProperty for LargeImageSource. At 96dpi this
        ///     is normally a 32x32 icon.
        /// </summary>
        public static readonly DependencyProperty LargeImageSourceProperty =
            DependencyProperty.RegisterAttached("LargeImageSource", typeof(ImageSource), typeof(RibbonControlService), 
                new FrameworkPropertyMetadata(null, OnLargeImageSourceChanged));

        /// <summary>
        ///     Gets the value of LargeImageSource on the specified object
        /// </summary>
        public static ImageSource GetLargeImageSource(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (ImageSource)element.GetValue(LargeImageSourceProperty);
        }

        /// <summary>
        ///     Sets the value of LargeImageSource on the specified object
        /// </summary>
        public static void SetLargeImageSource(DependencyObject element, ImageSource value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(LargeImageSourceProperty, value);
        }

        private static void OnLargeImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateDefaultControlSizeDefinition(d);
        }

        /// <summary>
        ///     DependencyProperty for SmallImageSource. At 96dpi this
        ///     is normally a 16x16 icon.
        /// </summary>
        public static readonly DependencyProperty SmallImageSourceProperty =
            DependencyProperty.RegisterAttached("SmallImageSource", typeof(ImageSource), typeof(RibbonControlService), 
                new FrameworkPropertyMetadata(null, OnSmallImageSourceChanged));

        /// <summary>
        ///     Gets the value of SmallImageSource on the specified object
        /// </summary>
        public static ImageSource GetSmallImageSource(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (ImageSource)element.GetValue(SmallImageSourceProperty);
        }

        /// <summary>
        ///     Sets the value of SmallImageSource on the specified object
        /// </summary>
        public static void SetSmallImageSource(DependencyObject element, ImageSource value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(SmallImageSourceProperty, value);
        }

        private static void OnSmallImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateDefaultControlSizeDefinition(d);
        }

        #endregion

        #region Label Properties

        /// <summary>
        ///     DependencyProperty for Label.  This is the primary
        ///     label that will be used on a bound Ribbon control.
        /// </summary>
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.RegisterAttached("Label", typeof(string), typeof(RibbonControlService), 
                new FrameworkPropertyMetadata(null, OnLabelChanged));

        /// <summary>
        ///     Gets the value of Label on the specified object
        /// </summary>
        public static string GetLabel(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (string)element.GetValue(LabelProperty);
        }

        /// <summary>
        ///     Sets the value of Label on the specified object
        /// </summary>
        public static void SetLabel(DependencyObject element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(LabelProperty, value);
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateDefaultControlSizeDefinition(d);
        }

        #endregion

        #region ToolTip Properties

        /// <summary>
        ///     DependencyProperty for ToolTipTitle. This is
        ///     used as the main header of the ToolTip for a Ribbon control.
        /// </summary>
        public static readonly DependencyProperty ToolTipTitleProperty =
            DependencyProperty.RegisterAttached("ToolTipTitle", typeof(string), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Gets the value of ToolTipTitle on the specified object
        /// </summary>
        public static string GetToolTipTitle(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (string)element.GetValue(ToolTipTitleProperty);
        }

        /// <summary>
        ///     Sets the value of ToolTipTitle on the specified object
        /// </summary>
        public static void SetToolTipTitle(DependencyObject element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(ToolTipTitleProperty, value);
        }

        /// <summary>
        ///     DependencyProperty for ToolTipDescription. This is
        ///     used as the main body description of the ToolTip for a Ribbon control.
        /// </summary>
        public static readonly DependencyProperty ToolTipDescriptionProperty =
            DependencyProperty.RegisterAttached("ToolTipDescription", typeof(string), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Gets the value of ToolTipDescription on the specified object
        /// </summary>
        public static string GetToolTipDescription(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (string)element.GetValue(ToolTipDescriptionProperty);
        }

        /// <summary>
        ///     Sets the value of ToolTipDescription on the specified object
        /// </summary>
        public static void SetToolTipDescription(DependencyObject element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(ToolTipDescriptionProperty, value);
        }

        /// <summary>
        ///     DependencyProperty for ToolTipImageSource. This is
        ///     the main image in the body of the ToolTip for a Ribbon control.
        /// </summary>
        public static readonly DependencyProperty ToolTipImageSourceProperty =
            DependencyProperty.RegisterAttached("ToolTipImageSource", typeof(ImageSource), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Gets the value of ToolTipImageSource on the specified object
        /// </summary>
        public static ImageSource GetToolTipImageSource(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (ImageSource)element.GetValue(ToolTipImageSourceProperty);
        }

        /// <summary>
        ///     Sets the value of ToolTipImageSource on the specified object
        /// </summary>
        public static void SetToolTipImageSource(DependencyObject element, ImageSource value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(ToolTipImageSourceProperty, value);
        }

        /// <summary>
        ///     DependencyProperty for ToolTipFooterTitle. This is
        ///     the title of the footer of the ToolTip for a Ribbon control.
        /// </summary>
        public static readonly DependencyProperty ToolTipFooterTitleProperty =
            DependencyProperty.RegisterAttached("ToolTipFooterTitle", typeof(string), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Gets the value of ToolTipFooterTitle on the specified object
        /// </summary>
        public static string GetToolTipFooterTitle(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (string)element.GetValue(ToolTipFooterTitleProperty);
        }

        /// <summary>
        ///     Sets the value of ToolTipFooterTitle on the specified object
        /// </summary>
        public static void SetToolTipFooterTitle(DependencyObject element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(ToolTipFooterTitleProperty, value);
        }

        /// <summary>
        ///     DependencyProperty for ToolTipFooterDescription. This is
        ///     the main description in the footer of the ToolTip for a Ribbon control.
        /// </summary>
        public static readonly DependencyProperty ToolTipFooterDescriptionProperty =
            DependencyProperty.RegisterAttached("ToolTipFooterDescription", typeof(string), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Gets the value of ToolTipFooterDescription on the specified object
        /// </summary>
        public static string GetToolTipFooterDescription(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (string)element.GetValue(ToolTipFooterDescriptionProperty);
        }

        /// <summary>
        ///     Sets the value of ToolTipFooterDescription on the specified object
        /// </summary>
        public static void SetToolTipFooterDescription(DependencyObject element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(ToolTipFooterDescriptionProperty, value);
        }

        /// <summary>
        ///     DependencyProperty for ToolTipFooterImageSource. This is
        ///     the image used in the footer of the ToolTip for a Ribbon control.
        /// </summary>
        public static readonly DependencyProperty ToolTipFooterImageSourceProperty =
            DependencyProperty.RegisterAttached("ToolTipFooterImageSource", typeof(ImageSource), typeof(RibbonControlService), new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Gets the value of ToolTipFooterImageSource on the specified object
        /// </summary>
        public static ImageSource GetToolTipFooterImageSource(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (ImageSource)element.GetValue(ToolTipFooterImageSourceProperty);
        }

        /// <summary>
        ///     Sets the value of ToolTipFooterImageSource on the specified object
        /// </summary>
        public static void SetToolTipFooterImageSource(DependencyObject element, ImageSource value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(ToolTipFooterImageSourceProperty, value);
        }

        #endregion

        #region Visual States Properties
 
        /// <summary>
        ///     DependencyProperty for Ribbon property. This property is set on the parent ribbon control and
        ///     is inherited by the child controls. This property allows to access common visual states brush 
        ///     properties which are set on the parent Ribbon.
        /// </summary>
        internal static readonly DependencyPropertyKey RibbonPropertyKey =
                DependencyProperty.RegisterAttachedReadOnly(
                            "Ribbon",
                            typeof(Ribbon), 
                            typeof(RibbonControlService),
                            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        ///     Expose Ribbon DependencyProperty for controls to read State brushes off the Ribbon parent.
        /// </summary>
        public static readonly DependencyProperty RibbonProperty = RibbonPropertyKey.DependencyProperty;

        /// <summary>
        ///     Gets the value of the Ribbon property.
        /// </summary>
        public static Ribbon GetRibbon(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (Ribbon)element.GetValue(RibbonProperty);
        }

        /// <summary>
        ///     Sets the value of the Ribbon property.
        /// </summary>
        internal static void SetRibbon(DependencyObject element, Ribbon value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(RibbonPropertyKey, value);
        }

        /// <summary>
        ///     DependencyProperty for MouseOverBorderBrush property.
        /// </summary>
        public static readonly DependencyProperty MouseOverBorderBrushProperty =
                DependencyProperty.RegisterAttached(
                            "MouseOverBorderBrush",
                            typeof(Brush),
                            typeof(RibbonControlService),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Gets the value of the outer border brush used in a "hover" state of the ribbon controls.
        /// </summary>
        public static Brush GetMouseOverBorderBrush(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (Brush)element.GetValue(MouseOverBorderBrushProperty); 
        }

        /// <summary>
        ///     Sets the value of the outer border brush used in a "hover" state of the ribbon controls.
        /// </summary>
        public static void SetMouseOverBorderBrush(DependencyObject element, Brush value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(MouseOverBorderBrushProperty, value); 
        }
            
        /// <summary>
        ///     DependencyProperty for MouseOverBackground property.
        /// </summary>
        public static readonly DependencyProperty MouseOverBackgroundProperty =
                DependencyProperty.RegisterAttached(
                            "MouseOverBackground",
                            typeof(Brush),
                            typeof(RibbonControlService),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Gets the value of the background brush used in a "hover" state of the ribbon controls.
        /// </summary>
        public static Brush GetMouseOverBackground(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (Brush)element.GetValue(MouseOverBackgroundProperty); 
        }

        /// <summary>
        ///     Sets the value of the background brush used in a "hover" state of the ribbon controls.
        /// </summary>
        public static void SetMouseOverBackground(DependencyObject element, Brush value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(MouseOverBackgroundProperty, value);
        }

        /// <summary>
        ///     DependencyProperty for PressedBorderBrush property.
        /// </summary>
        public static readonly DependencyProperty PressedBorderBrushProperty =
                DependencyProperty.RegisterAttached(
                            "PressedBorderBrush",
                            typeof(Brush),
                            typeof(RibbonControlService),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Gets the value of the outer border brush used in a "Pressed" state of the ribbon controls.
        /// </summary>
        public static Brush GetPressedBorderBrush(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (Brush)element.GetValue(PressedBorderBrushProperty); 
        }

        /// <summary>
        ///     Sets the value of the outer border brush used in a "Pressed" state of the ribbon controls.
        /// </summary>
        public static void SetPressedBorderBrush(DependencyObject element, Brush value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(PressedBorderBrushProperty, value);
        }

        /// <summary>
        ///     DependencyProperty for PressedBackground property.
        /// </summary>
        public static readonly DependencyProperty PressedBackgroundProperty =
                DependencyProperty.RegisterAttached(
                            "PressedBackground",
                            typeof(Brush),
                            typeof(RibbonControlService),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Gets the value of the background brush used in a "Pressed" state of the ribbon controls.
        /// </summary>
        public static Brush GetPressedBackground(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (Brush)element.GetValue(PressedBackgroundProperty);
        }

        /// <summary>
        ///     Sets the value of the background used in a "Pressed" state of the ribbon controls.
        /// </summary>
        public static void SetPressedBackground(DependencyObject element, Brush value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(PressedBackgroundProperty, value);
        }

        /// <summary>
        ///     DependencyProperty for CheckedBackground property.
        /// </summary>
        public static readonly DependencyProperty CheckedBackgroundProperty =
                DependencyProperty.RegisterAttached(
                            "CheckedBackground",
                            typeof(Brush),
                            typeof(RibbonControlService),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Gets the value of the background brush used in a "Checked" state of the ribbon controls.
        /// </summary>
        public static Brush GetCheckedBackground(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (Brush)element.GetValue(CheckedBackgroundProperty);
        }

        /// <summary>
        ///     Sets the value of the background used in a "Checked" state of the ribbon controls.
        /// </summary>
        public static void SetCheckedBackground(DependencyObject element, Brush value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(CheckedBackgroundProperty, value);
        }

        /// <summary>
        ///     DependencyProperty for CheckedBorderBrush property.
        /// </summary>
        public static readonly DependencyProperty CheckedBorderBrushProperty =
                DependencyProperty.RegisterAttached(
                            "CheckedBorderBrush",
                            typeof(Brush),
                            typeof(RibbonControlService),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Gets the value of the border brush used in a "Checked" state of the ribbon controls.
        /// </summary>
        public static Brush GetCheckedBorderBrush(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (Brush)element.GetValue(CheckedBorderBrushProperty);
        }

        /// <summary>
        ///     Sets the value of the border brush used in "Checked" state of the ribbon controls.
        /// </summary>
        public static void SetCheckedBorderBrush(DependencyObject element, Brush value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(CheckedBorderBrushProperty, value);
        }

        /// <summary>
        ///     DependencyProperty for FocusedBackground property.
        /// </summary>
        public static readonly DependencyProperty FocusedBackgroundProperty =
                DependencyProperty.RegisterAttached(
                            "FocusedBackground",
                            typeof(Brush),
                            typeof(RibbonControlService),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Gets the value of the background brush used in a "Focused" state of the ribbon controls.
        /// </summary>
        public static Brush GetFocusedBackground(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (Brush)element.GetValue(FocusedBackgroundProperty);
        }

        /// <summary>
        ///     Sets the value of the background used in a "Focused" state of the ribbon controls.
        /// </summary>
        public static void SetFocusedBackground(DependencyObject element, Brush value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(FocusedBackgroundProperty, value);
        }

        /// <summary>
        ///     DependencyProperty for FocusedBorderBrush property.
        /// </summary>
        public static readonly DependencyProperty FocusedBorderBrushProperty =
                DependencyProperty.RegisterAttached(
                            "FocusedBorderBrush",
                            typeof(Brush),
                            typeof(RibbonControlService),
                            new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Gets the value of the border brush used in a "Focused" state of the ribbon controls.
        /// </summary>
        public static Brush GetFocusedBorderBrush(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (Brush)element.GetValue(FocusedBorderBrushProperty);
        }

        /// <summary>
        ///     Sets the value of the border brush used in "Focused" state of the ribbon controls.
        /// </summary>
        public static void SetFocusedBorderBrush(DependencyObject element, Brush value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(FocusedBorderBrushProperty, value);
        }

        /// <summary>
        /// Dependency Property for the CornerRadius of a button based Control.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.RegisterAttached(
                    "CornerRadius",
                    typeof(CornerRadius),
                    typeof(RibbonControlService),
                    new FrameworkPropertyMetadata(new CornerRadius()));

        /// <summary>
        /// Gets the value of CornerRadius for button based Controls
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static CornerRadius GetCornerRadius(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (CornerRadius)element.GetValue(CornerRadiusProperty);
        }

        /// <summary>
        /// Sets the value of CornerRadius for button based Control.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetCornerRadius(DependencyObject element, CornerRadius value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(CornerRadiusProperty, value);
        }

        #endregion Visual States Properties

        #region Resizing

        /// <summary>
        ///     DependencyProperty for ControlSizeDefinition property.
        /// </summary>
        public static readonly DependencyProperty ControlSizeDefinitionProperty =
            DependencyProperty.RegisterAttached(
                    "ControlSizeDefinition", 
                    typeof(RibbonControlSizeDefinition), 
                    typeof(RibbonControlService),
                    new FrameworkPropertyMetadata(null, 
                        new PropertyChangedCallback(OnControlSizeDefinitionChanged),
                        new CoerceValueCallback(CoerceControlSizeDefinition)));

        /// <summary>
        ///     Gets the value of the ControlSizeDefinition (including image size, and whether or not label is displayed) 
        ///     applied to this element.
        /// </summary>
        public static RibbonControlSizeDefinition GetControlSizeDefinition(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (RibbonControlSizeDefinition)element.GetValue(ControlSizeDefinitionProperty);
        }

        /// <summary>
        ///     Sets the value of the ControlSizeDefinition on this element.
        /// </summary>
        public static void SetControlSizeDefinition(DependencyObject element, RibbonControlSizeDefinition value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(ControlSizeDefinitionProperty, value);
        }

        private static void OnControlSizeDefinitionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DependencyObject ancestor = TreeHelper.FindVisualAncestor(d,
                delegate(DependencyObject element)
                {
                    return (element is RibbonGroupsPanel);
                });
            if (ancestor != null)
            {
                TreeHelper.InvalidateMeasureForVisualAncestorPath(d, delegate(DependencyObject element)
                {
                    return (element == ancestor);
                });
            }
        }

        private static object CoerceControlSizeDefinition(DependencyObject d, object baseValue)
        {
            if (baseValue == null)
            {
                RibbonControlSizeDefinition defaultControlSizeDefinition = RibbonControlService.GetDefaultControlSizeDefinition(d);
                if (defaultControlSizeDefinition == null)
                {
                    d.CoerceValue(DefaultControlSizeDefinitionProperty);
                    defaultControlSizeDefinition = RibbonControlService.GetDefaultControlSizeDefinition(d);
                }
                return defaultControlSizeDefinition;
            }
            return baseValue;
        }

        public static RibbonControlSizeDefinition GetDefaultControlSizeDefinition(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (RibbonControlSizeDefinition)element.GetValue(DefaultControlSizeDefinitionProperty);
        }

        public static void SetDefaultControlSizeDefinition(DependencyObject element, RibbonControlSizeDefinition value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(DefaultControlSizeDefinitionProperty, value);
        }

        // Using a DependencyProperty as the backing store for DefaultControlSizeDefinition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultControlSizeDefinitionProperty =
            DependencyProperty.RegisterAttached("DefaultControlSizeDefinition", typeof(RibbonControlSizeDefinition), typeof(RibbonControlService), 
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnDefaultControlSizeDefinitionChanged), new CoerceValueCallback(CoerceDefaultControlSizeDefinition)));

        private static void OnDefaultControlSizeDefinitionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (GetControlSizeDefinition(d) == e.OldValue)
            {
                RibbonGroupItemsPanel ribbonGroupItemsPanel = TreeHelper.FindVisualAncestor<RibbonGroupItemsPanel>(d);
                if (ribbonGroupItemsPanel != null)
                {
                    RibbonGroup ribbonGroup = TreeHelper.FindVisualAncestor<RibbonGroup>(ribbonGroupItemsPanel);
                    if (ribbonGroup != null)
                    {
                        ribbonGroup.UpdateGroupSizeDefinitionsAsync();
                    }
                }
            }

            d.CoerceValue(ControlSizeDefinitionProperty);
        }

        private static object CoerceDefaultControlSizeDefinition(DependencyObject d, object baseValue)
        {
            if (baseValue == null)
            {
                RibbonImageSize imageSize = RibbonImageSize.Collapsed;
                if (RibbonControlService.GetLargeImageSource(d) != null)
                {
                    imageSize = RibbonImageSize.Large;
                }
                else if (RibbonControlService.GetSmallImageSource(d) != null)
                {
                    imageSize = RibbonImageSize.Small;
                }
                bool isLabelVisible = !string.IsNullOrEmpty(RibbonControlService.GetLabel(d));

                return RibbonControlSizeDefinition.GetFrozenControlSizeDefinition(imageSize, isLabelVisible);
            }
            return baseValue;
        }

        private static void UpdateDefaultControlSizeDefinition(DependencyObject d)
        {
            d.CoerceValue(DefaultControlSizeDefinitionProperty);

            // If the element belongs to a ControlGroup, then
            // coerce DefaultControlSizeDefinition for the ControlGroup too.
            if (RibbonControlService.GetIsInControlGroup(d))
            {
                RibbonControlGroup controlGroup = TreeHelper.FindVisualAncestor<RibbonControlGroup>(d);
                if (controlGroup != null)
                {
                    controlGroup.CoerceValue(DefaultControlSizeDefinitionProperty);
                }
            }
        }

        #endregion Resizing

        #region ControlGroup

        /// <summary>
        ///     DependencyProperty for IsInControlGroup property.
        ///     This is a readonly pseudo inherited property. When set on a RibbonControl,
        ///     this property is transfered to its content presenter and 
        ///     then to the element hosted within the presenter. This property 
        ///     is set by the parent RibbonControlGroup.
        /// </summary>
        internal static readonly DependencyPropertyKey IsInControlGroupPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                    "IsInControlGroup", 
                    typeof(bool), 
                    typeof(RibbonControlService),
                    new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsInControlGroupProperty = IsInControlGroupPropertyKey.DependencyProperty;

        /// <summary>
        ///     Gets the value of the IsInControlGroup on this element, 
        ///     which indicates whether this element belongs to a RibbonControlGroup or not.
        /// </summary>
        public static bool GetIsInControlGroup(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(IsInControlGroupProperty);
        }

        /// <summary>
        ///     Sets the value of the IsInControlGroup on this element.
        ///     Should be set by the RibbonControlGroup parent control.
        /// </summary>
        internal static void SetIsInControlGroup(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(IsInControlGroupPropertyKey, value);
        }

        #endregion ControlGroup

        #region QAT

        /// <summary>
        ///   Property determining whether a control can be added to the RibbonQuickAccessToolBar directly.
        ///   
        ///   Setting this API to false means there is no default ContextMenu.
        /// </summary>
        public static readonly DependencyProperty CanAddToQuickAccessToolBarDirectlyProperty =
            DependencyProperty.RegisterAttached(
                    "CanAddToQuickAccessToolBarDirectly",
                    typeof(bool),
                    typeof(RibbonControlService),
                    new FrameworkPropertyMetadata(false, RibbonHelper.OnCanAddToQuickAccessToolBarDirectlyChanged, RibbonHelper.OnCoerceCanAddToQuickAccessToolBarDirectly));

        public static bool GetCanAddToQuickAccessToolBarDirectly(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(CanAddToQuickAccessToolBarDirectlyProperty);
        }

        public static void SetCanAddToQuickAccessToolBarDirectly(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(CanAddToQuickAccessToolBarDirectlyProperty, value);
        }

        /// <summary>
        ///     DependencyProperty for IsInQuickAccessToolBar property.
        ///     This is a readonly pseudo inherited property. 
        /// </summary>
        internal static readonly DependencyPropertyKey IsInQuickAccessToolBarPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                    "IsInQuickAccessToolBar", 
                    typeof(bool), 
                    typeof(RibbonControlService),
                    new FrameworkPropertyMetadata(false, new PropertyChangedCallback(RibbonHelper.OnIsInQATChanged)));

        public static readonly DependencyProperty IsInQuickAccessToolBarProperty = 
            IsInQuickAccessToolBarPropertyKey.DependencyProperty;

        /// <summary>
        ///     Get property which indicates whether the control is part of a QuickAccessToolBar.
        /// </summary>
        public static bool GetIsInQuickAccessToolBar(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(IsInQuickAccessToolBarProperty);
        }

        /// <summary>
        ///     Set property which indicates whether the control is part of a QuickAccessToolBar.
        /// </summary>
        internal static void SetIsInQuickAccessToolBar(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(IsInQuickAccessToolBarPropertyKey, value);
        }

        /// <summary>
        ///     DependencyProperty for QuickAccessToolBarControlSizeDefinition property.
        /// </summary>
        public static readonly DependencyProperty QuickAccessToolBarControlSizeDefinitionProperty =
            DependencyProperty.RegisterAttached(
                    "QuickAccessToolBarControlSizeDefinition", 
                    typeof(RibbonControlSizeDefinition), 
                    typeof(RibbonControlService),
                    new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Get size definition which is applied to the control when it is displayed as a part of a QuickAccessToolBar.
        /// </summary>
        public static RibbonControlSizeDefinition GetQuickAccessToolBarControlSizeDefinition(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (RibbonControlSizeDefinition)element.GetValue(QuickAccessToolBarControlSizeDefinitionProperty);
        }

        /// <summary>
        ///     Set size definition which is applied to the control when it is displayed as a part of a QuickAccessToolBar.
        /// </summary>
        public static void SetQuickAccessToolBarControlSizeDefinition(DependencyObject element, RibbonControlSizeDefinition value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(QuickAccessToolBarControlSizeDefinitionProperty, value);
        }

        /// <summary>
        ///   DependencyProperty for QuickAccessToolBarId property.  This property allows to us to establish a relationship between
        ///   a control in the Ribbon and its counterpart in the QAT.
        /// </summary>
        public static readonly DependencyProperty QuickAccessToolBarIdProperty =
                DependencyProperty.RegisterAttached(
                            "QuickAccessToolBarId",
                            typeof(object),
                            typeof(RibbonControlService),
                            new FrameworkPropertyMetadata(null, RibbonHelper.OnCoerceQuickAccessToolBarId));

        /// <summary>
        ///   Gets the value of the QuickAccessToolBarId property.
        /// </summary>
        public static object GetQuickAccessToolBarId(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return element.GetValue(QuickAccessToolBarIdProperty);
        }

        /// <summary>
        ///   Sets the value of the QuickAccessToolBarId property.
        /// </summary>
        public static void SetQuickAccessToolBarId(DependencyObject element, object value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(QuickAccessToolBarIdProperty, value);
        }

        #endregion QAT

        #region KeyboardNavigation

        /// <summary>
        ///     Gets the value of the ShowKeyboardCues property.
        /// </summary>
        public static bool GetShowKeyboardCues(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(ShowKeyboardCuesProperty);
        }

        /// <summary>
        ///     Sets the value of the ShowKeyboardCues property.
        /// </summary>
        internal static void SetShowKeyboardCues(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(ShowKeyboardCuesPropertyKey, value);
        }

        private static readonly DependencyPropertyKey ShowKeyboardCuesPropertyKey = 
            DependencyProperty.RegisterAttachedReadOnly("ShowKeyboardCues", typeof(bool), typeof(RibbonControlService), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty ShowKeyboardCuesProperty = ShowKeyboardCuesPropertyKey.DependencyProperty;

        #endregion KeyboardNavigation

        #region DismissPopup

        public static readonly RoutedEvent DismissPopupEvent = EventManager.RegisterRoutedEvent("DismissPopup", RoutingStrategy.Bubble, typeof(RibbonDismissPopupEventHandler), typeof(RibbonControlService));

        public static void AddDismissPopupHandler(DependencyObject element, RibbonDismissPopupEventHandler handler)
        {
            RibbonHelper.AddHandler(element, DismissPopupEvent, handler);
        }

        public static void RemoveDismissPopupHandler(DependencyObject element, RibbonDismissPopupEventHandler handler)
        {
            RibbonHelper.RemoveHandler(element, DismissPopupEvent, handler);
        }

        #endregion DismissPopup

    }
}
