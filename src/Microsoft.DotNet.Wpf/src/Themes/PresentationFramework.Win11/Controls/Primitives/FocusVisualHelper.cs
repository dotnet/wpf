// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PresentationFramework.Win11.Controls.Primitives
{
    public static class FocusVisualHelper
    {
        #region FocusVisualPrimaryBrush

        /// <summary>
        /// Gets the brush used to draw the outer border of a HighVisibility focus
        /// visual for a FrameworkElement.
        /// </summary>
        /// <param name="element">The element from which to read the property value.</param>
        /// <returns>The brush used to draw the outer border of a HighVisibility focus visual.</returns>
        public static Brush GetFocusVisualPrimaryBrush(FrameworkElement element)
        {
            return (Brush)element.GetValue(FocusVisualPrimaryBrushProperty);
        }

        /// <summary>
        /// Sets the brush used to draw the outer border of a HighVisibility focus
        /// visual for a FrameworkElement.
        /// </summary>
        /// <param name="element">The element on which to set the attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFocusVisualPrimaryBrush(FrameworkElement element, Brush value)
        {
            element.SetValue(FocusVisualPrimaryBrushProperty, value);
        }

        /// <summary>
        /// Identifies the FocusVisualPrimaryBrush dependency property.
        /// </summary>
        public static readonly DependencyProperty FocusVisualPrimaryBrushProperty =
            DependencyProperty.RegisterAttached(
                "FocusVisualPrimaryBrush",
                typeof(Brush),
                typeof(FocusVisualHelper));

        #endregion

        #region FocusVisualSecondaryBrush

        /// <summary>
        /// Gets the brush used to draw the inner border of a HighVisibility focus
        /// visual for a FrameworkElement.
        /// </summary>
        /// <param name="element">The element from which to read the property value.</param>
        /// <returns>The brush used to draw the inner border of a HighVisibility focus visual.</returns>
        public static Brush GetFocusVisualSecondaryBrush(FrameworkElement element)
        {
            return (Brush)element.GetValue(FocusVisualSecondaryBrushProperty);
        }

        /// <summary>
        /// Sets the brush used to draw the inner border of a HighVisibility focus
        /// visual for a FrameworkElement.
        /// </summary>
        /// <param name="element">The element on which to set the attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFocusVisualSecondaryBrush(FrameworkElement element, Brush value)
        {
            element.SetValue(FocusVisualSecondaryBrushProperty, value);
        }

        /// <summary>
        /// Identifies the FocusVisualSecondaryBrush dependency property.
        /// </summary>
        public static readonly DependencyProperty FocusVisualSecondaryBrushProperty =
            DependencyProperty.RegisterAttached(
                "FocusVisualSecondaryBrush",
                typeof(Brush),
                typeof(FocusVisualHelper));

        #endregion

        #region FocusVisualPrimaryThickness

        /// <summary>
        /// Gets the thickness of the outer border of a HighVisibility focus visual
        /// for a FrameworkElement.
        /// </summary>
        /// <param name="element">The element from which to read the property value.</param>
        /// <returns>
        /// The thickness of the outer border of a HighVisibility focus visual. The default
        /// value is 2.
        /// </returns>
        public static Thickness GetFocusVisualPrimaryThickness(FrameworkElement element)
        {
            return (Thickness)element.GetValue(FocusVisualPrimaryThicknessProperty);
        }

        /// <summary>
        /// Sets the thickness of the outer border of a HighVisibility focus visual
        /// for a FrameworkElement.
        /// </summary>
        /// <param name="element">The element on which to set the attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFocusVisualPrimaryThickness(FrameworkElement element, Thickness value)
        {
            element.SetValue(FocusVisualPrimaryThicknessProperty, value);
        }

        /// <summary>
        /// Identifies the FocusVisualPrimaryThickness dependency property.
        /// </summary>
        public static readonly DependencyProperty FocusVisualPrimaryThicknessProperty =
            DependencyProperty.RegisterAttached(
                "FocusVisualPrimaryThickness",
                typeof(Thickness),
                typeof(FocusVisualHelper),
                new FrameworkPropertyMetadata(new Thickness(2)));

        #endregion

        #region FocusVisualSecondaryThickness

        /// <summary>
        /// Gets the thickness of the inner border of a HighVisibility focus visual
        /// for a FrameworkElement.
        /// </summary>
        /// <param name="element">The element from which to read the property value.</param>
        /// <returns>
        /// The thickness of the inner border of a HighVisibility focus visual. The default
        /// value is 1.
        /// </returns>
        public static Thickness GetFocusVisualSecondaryThickness(FrameworkElement element)
        {
            return (Thickness)element.GetValue(FocusVisualSecondaryThicknessProperty);
        }

        /// <summary>
        /// Sets the thickness of the inner border of a HighVisibility focus visual
        /// for a FrameworkElement.
        /// </summary>
        /// <param name="element">The element on which to set the attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFocusVisualSecondaryThickness(FrameworkElement element, Thickness value)
        {
            element.SetValue(FocusVisualSecondaryThicknessProperty, value);
        }

        /// <summary>
        /// Identifies the FocusVisualSecondaryThickness dependency property.
        /// </summary>
        public static readonly DependencyProperty FocusVisualSecondaryThicknessProperty =
            DependencyProperty.RegisterAttached(
                "FocusVisualSecondaryThickness",
                typeof(Thickness),
                typeof(FocusVisualHelper),
                new FrameworkPropertyMetadata(new Thickness(1)));

        #endregion

        #region FocusVisualMargin

        /// <summary>
        /// Gets the outer margin of the focus visual for a FrameworkElement.
        /// </summary>
        /// <param name="element">The element from which to read the property value.</param>
        /// <returns>
        /// Provides margin values for the focus visual. The default value is a default Thickness
        /// with all properties (dimensions) equal to 0.
        /// </returns>
        public static Thickness GetFocusVisualMargin(FrameworkElement element)
        {
            return (Thickness)element.GetValue(FocusVisualMarginProperty);
        }

        /// <summary>
        /// Sets the outer margin of the focus visual for a FrameworkElement.
        /// </summary>
        /// <param name="element">The element on which to set the attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetFocusVisualMargin(FrameworkElement element, Thickness value)
        {
            element.SetValue(FocusVisualMarginProperty, value);
        }

        /// <summary>
        /// Identifies the FocusVisualMargin dependency property.
        /// </summary>
        public static readonly DependencyProperty FocusVisualMarginProperty =
            DependencyProperty.RegisterAttached(
                "FocusVisualMargin",
                typeof(Thickness),
                typeof(FocusVisualHelper),
                new FrameworkPropertyMetadata(new Thickness()));

        #endregion

        #region UseSystemFocusVisuals

        /// <summary>
        /// Identifies the UseSystemFocusVisuals dependency property.
        /// </summary>
        public static readonly DependencyProperty UseSystemFocusVisualsProperty =
            DependencyProperty.RegisterAttached(
                "UseSystemFocusVisuals",
                typeof(bool),
                typeof(FocusVisualHelper),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets a value that indicates whether the control uses focus visuals that
        /// are drawn by the system or those defined in the control template.
        /// </summary>
        /// <param name="control">The object from which the property value is read.</param>
        /// <returns>
        /// **true** if the control uses focus visuals drawn by the system; **false** if
        /// the control uses focus visuals defined in the ControlTemplate. The default is
        /// **false**; see Remarks.
        /// </returns>
        public static bool GetUseSystemFocusVisuals(Control control)
        {
            return (bool)control.GetValue(UseSystemFocusVisualsProperty);
        }

        /// <summary>
        /// Sets a value that indicates whether the control uses focus visuals that
        /// are drawn by the system or those defined in the control template.
        /// </summary>
        /// <param name="control">The object to which the property value is written.</param>
        /// <param name="value">The value to set.</param>
        public static void SetUseSystemFocusVisuals(Control control, bool value)
        {
            control.SetValue(UseSystemFocusVisualsProperty, value);
        }

        #endregion

        #region IsTemplateFocusTarget

        /// <summary>
        /// Identifies the Control.IsTemplateFocusTarget XAML attached property.
        /// </summary>
        public static readonly DependencyProperty IsTemplateFocusTargetProperty =
            DependencyProperty.RegisterAttached(
                "IsTemplateFocusTarget",
                typeof(bool),
                typeof(FocusVisualHelper),
                new PropertyMetadata(OnIsTemplateFocusTargetChanged));

        /// <summary>
        /// Gets the value of the Control.IsTemplateFocusTarget XAML attached property for
        /// the target element.
        /// </summary>
        /// <param name="element">The object from which the property value is read.</param>
        /// <returns>
        /// The Control.IsTemplateFocusTarget XAML attached property value of the specified
        /// object.
        /// </returns>
        public static bool GetIsTemplateFocusTarget(FrameworkElement element)
        {
            return (bool)element.GetValue(IsTemplateFocusTargetProperty);
        }

        /// <summary>
        /// Sets the value of the Control.IsTemplateFocusTarget XAML attached property for
        /// a target element.
        /// </summary>
        /// <param name="element">The object to which the property value is written.</param>
        /// <param name="value">The value to set.</param>
        public static void SetIsTemplateFocusTarget(FrameworkElement element, bool value)
        {
            element.SetValue(IsTemplateFocusTargetProperty, value);
        }

        private static void OnIsTemplateFocusTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = (FrameworkElement)d;
            if (element.TemplatedParent is Control control)
            {
                if ((bool)e.NewValue)
                {
                    SetTemplateFocusTarget(control, element);
                }
                else
                {
                    control.ClearValue(TemplateFocusTargetProperty);
                }
            }
        }

        #endregion

        #region IsSystemFocusVisual

        public static bool GetIsSystemFocusVisual(Control control)
        {
            return (bool)control.GetValue(IsSystemFocusVisualProperty);
        }

        public static void SetIsSystemFocusVisual(Control control, bool value)
        {
            control.SetValue(IsSystemFocusVisualProperty, value);
        }

        public static readonly DependencyProperty IsSystemFocusVisualProperty =
            DependencyProperty.RegisterAttached(
                "IsSystemFocusVisual",
                typeof(bool),
                typeof(FocusVisualHelper),
                new PropertyMetadata(OnIsSystemFocusVisualChanged));

        private static void OnIsSystemFocusVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (Control)d;
            if ((bool)e.NewValue)
            {
                control.IsVisibleChanged += OnFocusVisualIsVisibleChanged;
            }
            else
            {
                control.IsVisibleChanged -= OnFocusVisualIsVisibleChanged;
            }
        }

        #endregion

        #region ShowFocusVisual

        public static bool GetShowFocusVisual(FrameworkElement element)
        {
            return (bool)element.GetValue(ShowFocusVisualProperty);
        }

        private static void SetShowFocusVisual(FrameworkElement element, bool value)
        {
            element.SetValue(ShowFocusVisualPropertyKey, value);
        }

        private static readonly DependencyPropertyKey ShowFocusVisualPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "ShowFocusVisual",
                typeof(bool),
                typeof(FocusVisualHelper),
                new PropertyMetadata(OnShowFocusVisualChanged));

        public static readonly DependencyProperty ShowFocusVisualProperty =
            ShowFocusVisualPropertyKey.DependencyProperty;

        private static void OnShowFocusVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Control control && GetTemplateFocusTarget(control) is { } target)
            {
                if ((bool)e.NewValue)
                {
                    bool shouldShowFocusVisual = true;
                    if (target is Control targetAsControl)
                    {
                        shouldShowFocusVisual = GetUseSystemFocusVisuals(targetAsControl);
                    }

                    if (shouldShowFocusVisual)
                    {
                        ShowFocusVisual(control, target);
                    }
                }
                else
                {
                    HideFocusVisual();
                }
            }

            static void HideFocusVisual()
            {
                // Remove the existing focus visual
                if (_focusVisualAdornerCache != null)
                {
                    AdornerLayer adornerlayer = VisualTreeHelper.GetParent(_focusVisualAdornerCache) as AdornerLayer;
                    Debug.Assert(adornerlayer != null);
                    if (adornerlayer != null)
                    {
                        adornerlayer.Remove(_focusVisualAdornerCache);
                    }
                    _focusVisualAdornerCache = null;
                }
            }

            static void ShowFocusVisual(Control control, FrameworkElement target)
            {
                HideFocusVisual();

                AdornerLayer adornerlayer = AdornerLayer.GetAdornerLayer(target);
                if (adornerlayer == null)
                    return;

                Style fvs = target.FocusVisualStyle;

                if (fvs != null && fvs.BasedOn == null && fvs.Setters.Count == 0)
                {
                    fvs = target.TryFindResource(SystemParameters.FocusVisualStyleKey) as Style;
                }

                if (fvs != null)
                {
                    _focusVisualAdornerCache = new FocusVisualAdorner(control, target, fvs);
                    adornerlayer.Add(_focusVisualAdornerCache);

                    // Hide the focus visual when IsVisible changes to avoid an internal WPF exception
                    control.IsVisibleChanged += OnControlIsVisibleChanged;
                }
            }

            static void OnControlIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
            {
                ((Control)sender).IsVisibleChanged -= OnControlIsVisibleChanged;
                Debug.Assert((bool)e.NewValue == false);
                if (_focusVisualAdornerCache != null && _focusVisualAdornerCache.FocusedElement == sender)
                {
                    HideFocusVisual();
                }
            }
        }

        #endregion

        #region FocusedElement

        private static FrameworkElement GetFocusedElement(Control focusVisual)
        {
            return (FrameworkElement)focusVisual.GetValue(FocusedElementProperty);
        }

        private static void SetFocusedElement(Control focusVisual, FrameworkElement value)
        {
            focusVisual.SetValue(FocusedElementProperty, value);
        }

        private static readonly DependencyProperty FocusedElementProperty =
            DependencyProperty.RegisterAttached(
                "FocusedElement",
                typeof(FrameworkElement),
                typeof(FocusVisualHelper));

        #endregion

        #region TemplateFocusTarget

        private static readonly DependencyProperty TemplateFocusTargetProperty =
            DependencyProperty.RegisterAttached(
                "TemplateFocusTarget",
                typeof(FrameworkElement),
                typeof(FocusVisualHelper));

        private static FrameworkElement GetTemplateFocusTarget(Control control)
        {
            return (FrameworkElement)control.GetValue(TemplateFocusTargetProperty);
        }

        private static void SetTemplateFocusTarget(Control control, FrameworkElement value)
        {
            control.SetValue(TemplateFocusTargetProperty, value);
        }

        #endregion

        private static void OnFocusVisualIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var focusVisual = (Control)sender;
            if ((bool)e.NewValue)
            {
                if ((VisualTreeHelper.GetParent(focusVisual) as Adorner)?.AdornedElement is FrameworkElement focusedElement)
                {
                    SetShowFocusVisual(focusedElement, true);

                    if (focusedElement is Control focusedControl &&
                        (!GetUseSystemFocusVisuals(focusedControl) || GetTemplateFocusTarget(focusedControl) != null))
                    {
                        focusVisual.Template = null;
                    }
                    else
                    {
                        TransferValue(focusedElement, focusVisual, FocusVisualPrimaryBrushProperty);
                        TransferValue(focusedElement, focusVisual, FocusVisualPrimaryThicknessProperty);
                        TransferValue(focusedElement, focusVisual, FocusVisualSecondaryBrushProperty);
                        TransferValue(focusedElement, focusVisual, FocusVisualSecondaryThicknessProperty);
                        focusVisual.Margin = GetFocusVisualMargin(focusedElement);
                    }

                    SetFocusedElement(focusVisual, focusedElement);
                }
            }
            else
            {
                FrameworkElement focusedElement = GetFocusedElement(focusVisual);
                if (focusedElement != null)
                {
                    focusedElement.ClearValue(ShowFocusVisualPropertyKey);
                    focusVisual.ClearValue(FocusVisualPrimaryBrushProperty);
                    focusVisual.ClearValue(FocusVisualPrimaryThicknessProperty);
                    focusVisual.ClearValue(FocusVisualSecondaryBrushProperty);
                    focusVisual.ClearValue(FocusVisualSecondaryThicknessProperty);
                    focusVisual.ClearValue(FrameworkElement.MarginProperty);
                    focusVisual.ClearValue(Control.TemplateProperty);
                    focusVisual.ClearValue(FocusedElementProperty);
                }
            }
        }

        private static void TransferValue(DependencyObject source, DependencyObject target, DependencyProperty dp)
        {
            // if (!Helper.HasDefaultValue(source, dp))
            // {
            //     target.SetValue(dp, source.GetValue(dp));
            // }
        }

        private sealed class FocusVisualAdorner : Adorner
        {
            public FocusVisualAdorner(Control focusedElement, UIElement adornedElement, Style focusVisualStyle) : base(adornedElement)
            {
                Debug.Assert(focusedElement != null, "focusedElement should not be null");
                Debug.Assert(adornedElement != null, "adornedElement should not be null");
                Debug.Assert(focusVisualStyle != null, "focusVisual should not be null");

                FocusedElement = focusedElement;

                Control control = new Control();
                SetIsSystemFocusVisual(control, false);
                control.Style = focusVisualStyle;
                control.Margin = GetFocusVisualMargin(focusedElement);
                TransferValue(focusedElement, control, FocusVisualPrimaryBrushProperty);
                TransferValue(focusedElement, control, FocusVisualPrimaryThicknessProperty);
                TransferValue(focusedElement, control, FocusVisualSecondaryBrushProperty);
                TransferValue(focusedElement, control, FocusVisualSecondaryThicknessProperty);
                _adorderChild = control;
                IsClipEnabled = true;
                IsHitTestVisible = false;
                IsEnabled = false;
                AddVisualChild(_adorderChild);
            }

            public Control FocusedElement { get; }

            /// <summary>
            /// Measure adorner. Default behavior is to size to match the adorned element.
            /// </summary>
            protected override Size MeasureOverride(Size constraint)
            {
                Size desiredSize = AdornedElement.RenderSize;

                // Measure the child
                ((UIElement)GetVisualChild(0)).Measure(desiredSize);

                return desiredSize;
            }

            /// <summary>
            ///     Default control arrangement is to only arrange
            ///     the first visual child. No transforms will be applied.
            /// </summary>
            protected override Size ArrangeOverride(Size size)
            {
                Size finalSize = base.ArrangeOverride(size);

                ((UIElement)GetVisualChild(0)).Arrange(new Rect(new Point(), finalSize));

                return finalSize;
            }

            /// <summary>
            ///  Derived classes override this property to enable the Visual code to enumerate
            ///  the Visual children. Derived classes need to return the number of children
            ///  from this method.
            ///
            ///    By default a Visual does not have any children.
            ///
            ///  Remark:
            ///      During this virtual method the Visual tree must not be modified.
            /// </summary>
            protected override int VisualChildrenCount
            {
                get
                {
                    return 1; // _adorderChild created in ctor.
                }
            }

            /// <summary>
            ///   Derived class must implement to support Visual children. The method must return
            ///    the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
            ///
            ///    By default a Visual does not have any children.
            ///
            ///  Remark:
            ///       During this virtual call it is not valid to modify the Visual tree.
            /// </summary>
            protected override Visual GetVisualChild(int index)
            {
                if (index == 0)
                {
                    return _adorderChild;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("index");
                }
            }

            private UIElement _adorderChild;
        }

        private static FocusVisualAdorner _focusVisualAdornerCache = null;
    }
}
