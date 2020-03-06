// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
#if RIBBON_IN_FRAMEWORK
using System.Windows.Controls.Ribbon;
#else
using Microsoft.Windows.Controls.Ribbon;
#endif
using MS.Internal;
#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls
#else
namespace Microsoft.Windows.Controls
#endif
{
    /// <summary>
    ///     The adorner control which is used to render KeyTip
    /// </summary>
    internal class KeyTipAdorner : Adorner
    {
        #region Constructor

        public KeyTipAdorner(UIElement adornedElement,
            UIElement placementTarget,
            KeyTipHorizontalPlacement horizontalPlacement,
            KeyTipVerticalPlacement verticalPlacement,
            double horizontalOffset,
            double verticalOffset,
            RibbonGroup ownerRibbonGroup)
            : base(adornedElement)
        {
            PlacementTarget = (placementTarget == null ? adornedElement : placementTarget);
            HorizontalPlacement = horizontalPlacement;
            VerticalPlacement = verticalPlacement;
            HorizontalOffset = horizontalOffset;
            VerticalOffset = verticalOffset;
            OwnerRibbonGroup = ownerRibbonGroup;
        }

        #endregion

        #region Basic Adorner

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0 ||
                _keyTipControl == null)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return _keyTipControl;
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return (_keyTipControl == null ? 0 : 1);
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (_keyTipControl != null)
            {
                Size childConstraint = new Size(Double.PositiveInfinity, Double.PositiveInfinity);
                _keyTipControl.Measure(childConstraint);
            }
            return new Size(0, 0);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_keyTipControl != null)
            {
                _keyTipControl.Arrange(new Rect(_keyTipControl.DesiredSize));
            }
            return finalSize;
        }

        #endregion

        #region KeyTipControl Management

        /// <summary>
        ///     Links the given KeyTipControl as the visual child of self.
        ///     In the process sets various properties of the control.
        /// </summary>
        public void LinkKeyTipControl(DependencyObject keyTipElement, KeyTipControl keyTipControl)
        {
            Debug.Assert(_keyTipControl == null && keyTipControl.KeyTipAdorner == null);
            _keyTipControl = keyTipControl;
            _keyTipControl.KeyTipAdorner = this;
            _keyTipControl.Text = KeyTipService.GetKeyTip(keyTipElement).ToUpper(KeyTipService.GetCultureForElement(keyTipElement));
            _keyTipControl.IsEnabled = (bool)keyTipElement.GetValue(UIElement.IsEnabledProperty);
            Style keyTipStyle = KeyTipService.GetKeyTipStyle(keyTipElement);
            _keyTipControl.Style = keyTipStyle;
            _keyTipControl.RenderTransform = _keyTipTransform;
            bool clearCustomProperties = true;
            if (keyTipStyle == null)
            {
                Ribbon.Ribbon ribbon = RibbonControlService.GetRibbon(PlacementTarget);
                if (ribbon != null)
                {
                    // Use Ribbon properties if the owner element belongs to a Ribbon.
                    keyTipStyle = KeyTipService.GetKeyTipStyle(ribbon);
                    if (keyTipStyle != null)
                    {
                        _keyTipControl.Style = keyTipStyle;
                    }
                    else
                    {
                        clearCustomProperties = false;
                        _keyTipControl.Background = ribbon.Background;
                        _keyTipControl.BorderBrush = ribbon.BorderBrush;
                        _keyTipControl.Foreground = ribbon.Foreground;
                    }
                }
            }
            if (clearCustomProperties)
            {
                _keyTipControl.ClearValue(Control.BackgroundProperty);
                _keyTipControl.ClearValue(Control.BorderBrushProperty);
                _keyTipControl.ClearValue(Control.ForegroundProperty);
            }
            AddVisualChild(_keyTipControl);
            EnsureTransform();
        }

        /// <summary>
        ///     Unlinks the earlier linked KeyTipControl from visual tree.
        /// </summary>
        public void UnlinkKeyTipControl()
        {
            if (_keyTipControl != null)
            {
                _keyTipControl.KeyTipAdorner = null;
                RemoveVisualChild(_keyTipControl);
                _keyTipControl = null;
            }
        }

        public KeyTipControl KeyTipControl
        {
            get
            {
                return _keyTipControl;
            }
        }

        #endregion

        #region KeyTip Placement

        private KeyTipHorizontalPlacement HorizontalPlacement { get; set; }
        private KeyTipVerticalPlacement VerticalPlacement { get; set; }
        private double HorizontalOffset { get; set; }
        private double VerticalOffset { get; set; }
        private UIElement PlacementTarget { get; set; }
        private RibbonGroup OwnerRibbonGroup { get; set; }

        /// <summary>
        ///     Invalidate X/Y properties of keytip transform
        ///     when size of KeyTipControl changes accordingly.
        /// </summary>
        /// <param name="e"></param>
        internal void OnKeyTipControlSizeChanged(SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                EnsureTransformX();
            }
            if (e.HeightChanged)
            {
                EnsureTransformY();
            }
        }

        private void EnsureTransform()
        {
            EnsureTransformX();
            EnsureTransformY();
        }

        /// <summary>
        ///     Updates X of keytip transform.
        /// </summary>
        private void EnsureTransformX()
        {
            UIElement placementTarget = PlacementTarget;
            if (placementTarget != null)
            {
                int horizontalPlacementValue = (int)HorizontalPlacement;
                double horizontalPosition = 0;
                if (horizontalPlacementValue >= 0 && horizontalPlacementValue < 9)
                {
                    switch (horizontalPlacementValue % 3)
                    {
                        case 1:
                            // compensate horizontal position for center of target
                            horizontalPosition += (placementTarget.RenderSize.Width / 2);
                            break;
                        case 2:
                            // compensate horizontal position for right of target
                            horizontalPosition += placementTarget.RenderSize.Width;
                            break;
                    }

                    if (_keyTipControl != null)
                    {
                        if (horizontalPlacementValue >= 6)
                        {
                            // compensate horizontal position for right of keytip
                            horizontalPosition -= _keyTipControl.ActualWidth;
                        }
                        else if (horizontalPlacementValue >= 3)
                        {
                            // compensate horizontal position for center of keytip
                            horizontalPosition -= (_keyTipControl.ActualWidth / 2);
                        }
                    }
                }

                horizontalPosition += HorizontalOffset;
                _keyTipTransform.X = horizontalPosition;
            }
            else
            {
                _keyTipTransform.X = 0;
            }
        }

        /// <summary>
        ///     Updates Y of keytip transform.
        /// </summary>
        private void EnsureTransformY()
        {
            UIElement placementTarget = PlacementTarget;
            if (placementTarget == null)
            {
                placementTarget = AdornedElement;
            }

            if (placementTarget != null)
            {
                int verticalPlacementValue = (int)VerticalPlacement;
                double verticalPosition = 0;
                if (verticalPlacementValue >= 0 && verticalPlacementValue < 9)
                {
                    switch (verticalPlacementValue % 3)
                    {
                        case 1:
                            // compensate vertical position for center of target
                            verticalPosition += (placementTarget.RenderSize.Height / 2);
                            break;
                        case 2:
                            // compensate vertical position for bottom of target
                            verticalPosition += placementTarget.RenderSize.Height;
                            break;
                    }

                    if (_keyTipControl != null)
                    {
                        if (verticalPlacementValue >= 6)
                        {
                            // compensate vertical position for bottom of keytip
                            verticalPosition -= _keyTipControl.ActualHeight;
                        }
                        else if (verticalPlacementValue >= 3)
                        {
                            // compensate vertical position for center of keytip
                            verticalPosition -= (_keyTipControl.ActualHeight / 2);
                        }
                    }
                }
                verticalPosition += VerticalOffset;
                verticalPosition = NudgeToRibbonGroupAxis(placementTarget, verticalPosition);
                _keyTipTransform.Y = verticalPosition;
            }
            else
            {
                _keyTipTransform.Y = 0;
            }
        }

        /// <summary>
        ///     Helper method to nudge the vertical postion of keytip,
        ///     to RibbonGroup's top/bottom axis if applicable.
        /// </summary>
        private double NudgeToRibbonGroupAxis(UIElement placementTarget, double verticalPosition)
        {
            if (OwnerRibbonGroup != null)
            {
                ItemsPresenter itemsPresenter = OwnerRibbonGroup.ItemsPresenter;
                if (itemsPresenter != null)
                {
                    GeneralTransform transform = placementTarget.TransformToAncestor(itemsPresenter);
                    Point targetOrigin = transform.Transform(new Point());
                    double keyTipTopY = verticalPosition + targetOrigin.Y;
                    double keyTipCenterY = keyTipTopY;
                    double keyTipBottomY = keyTipTopY;
                    if (_keyTipControl != null)
                    {
                        keyTipBottomY += _keyTipControl.ActualHeight;
                        keyTipCenterY += _keyTipControl.ActualHeight / 2;
                    }

                    if (DoubleUtil.LessThan(Math.Abs(keyTipTopY), RibbonGroupKeyTipAxisNudgeSpace))
                    {
                        // Nudge to top axis
                        verticalPosition -= (keyTipCenterY - RibbonGroupKeyTipAxisOffset);
                    }
                    else if (DoubleUtil.LessThan(Math.Abs(itemsPresenter.ActualHeight - keyTipBottomY), RibbonGroupKeyTipAxisNudgeSpace))
                    {
                        // Nudge to bottom axis
                        double centerOffsetFromGroupBottom = keyTipCenterY - itemsPresenter.ActualHeight;
                        verticalPosition -= (centerOffsetFromGroupBottom + RibbonGroupKeyTipAxisOffset);
                    }
                }
            }
            return verticalPosition;
        }

        /// <summary>
        ///     Helper method to nudge the keytip into the
        ///     boundary of the adorner layer.
        /// </summary>
        internal void NudgeIntoAdornerLayerBoundary(AdornerLayer adornerLayer)
        {
            if (_keyTipControl != null && _keyTipControl.IsLoaded)
            {
                Point adornerOrigin = this.TranslatePoint(new Point(), adornerLayer);
                Rect adornerLayerRect = new Rect(0, 0, adornerLayer.ActualWidth, adornerLayer.ActualHeight);
                Rect keyTipControlRect = new Rect(adornerOrigin.X + _keyTipTransform.X,
                    adornerOrigin.Y + _keyTipTransform.Y,
                    _keyTipControl.ActualWidth,
                    _keyTipControl.ActualHeight);
                if (adornerLayerRect.IntersectsWith(keyTipControlRect) &&
                    !adornerLayerRect.Contains(keyTipControlRect))
                {
                    double deltaX = 0;
                    double deltaY = 0;

                    // Nudge the keytip control horizontally if its left or right
                    // edge falls outside the adornerlayer.
                    if (DoubleUtil.LessThan(keyTipControlRect.Left, adornerLayerRect.Left))
                    {
                        deltaX = adornerLayerRect.Left - keyTipControlRect.Left;
                    }
                    else if (DoubleUtil.GreaterThan(keyTipControlRect.Right, adornerLayerRect.Right))
                    {
                        deltaX = adornerLayerRect.Right - keyTipControlRect.Right;
                    }

                    // Nudge the keytip control vertically if its top or bottom
                    // edge falls outside the adornerlayer.
                    if (DoubleUtil.LessThan(keyTipControlRect.Top, adornerLayerRect.Top))
                    {
                        deltaY = adornerLayerRect.Top - keyTipControlRect.Top;
                    }
                    else if (DoubleUtil.GreaterThan(keyTipControlRect.Bottom, adornerLayerRect.Bottom))
                    {
                        deltaY = adornerLayerRect.Bottom - keyTipControlRect.Bottom;
                    }

                    _keyTipTransform.X += deltaX;
                    _keyTipTransform.Y += deltaY;
                }
            }
        }

        #endregion

        #region Private Data

        KeyTipControl _keyTipControl;
        private TranslateTransform _keyTipTransform = new TranslateTransform(0, 0);

        private const double RibbonGroupKeyTipAxisNudgeSpace = 15;
        private const double RibbonGroupKeyTipAxisOffset = 5;

        #endregion
    }
}