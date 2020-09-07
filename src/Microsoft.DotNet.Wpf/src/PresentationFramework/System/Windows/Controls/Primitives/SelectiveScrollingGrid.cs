// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// Subclass of Grid that knows how to freeze certain cells in place when scrolled.
    /// Used as the panel for the DataGridRow to hold the header, cells, and details.
    /// </summary>
    public class SelectiveScrollingGrid : Grid
    {
        /// <summary>
        /// Attached property to specify the selective scroll behaviour of cells
        /// </summary>
        public static readonly DependencyProperty SelectiveScrollingOrientationProperty =
            DependencyProperty.RegisterAttached(
                "SelectiveScrollingOrientation",
                typeof(SelectiveScrollingOrientation),
                typeof(SelectiveScrollingGrid),
                new FrameworkPropertyMetadata(SelectiveScrollingOrientation.Both, new PropertyChangedCallback(OnSelectiveScrollingOrientationChanged)));

        /// <summary>
        /// Getter for the SelectiveScrollingOrientation attached property
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static SelectiveScrollingOrientation GetSelectiveScrollingOrientation(DependencyObject obj)
        {
            return (SelectiveScrollingOrientation)obj.GetValue(SelectiveScrollingOrientationProperty);
        }

        /// <summary>
        /// Setter for the SelectiveScrollingOrientation attached property
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public static void SetSelectiveScrollingOrientation(DependencyObject obj, SelectiveScrollingOrientation value)
        {
            obj.SetValue(SelectiveScrollingOrientationProperty, value);
        }

        /// <summary>
        /// Property changed call back for SelectiveScrollingOrientation property
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnSelectiveScrollingOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement element = d as UIElement;
            SelectiveScrollingOrientation orientation = (SelectiveScrollingOrientation)e.NewValue;
            ScrollViewer scrollViewer = DataGridHelper.FindVisualParent<ScrollViewer>(element);
            if (scrollViewer != null && element != null)
            {
                Transform transform = element.RenderTransform;

                if (transform != null)
                {
                    BindingOperations.ClearBinding(transform, TranslateTransform.XProperty);
                    BindingOperations.ClearBinding(transform, TranslateTransform.YProperty);
                }

                if (orientation == SelectiveScrollingOrientation.Both)
                {
                    element.RenderTransform = null;
                }
                else
                {
                    TranslateTransform translateTransform = new TranslateTransform();

                    // Add binding to XProperty of transform if orientation is not horizontal
                    if (orientation != SelectiveScrollingOrientation.Horizontal)
                    {
                        Binding horizontalBinding = new Binding("ContentHorizontalOffset");
                        horizontalBinding.Source = scrollViewer;
                        BindingOperations.SetBinding(translateTransform, TranslateTransform.XProperty, horizontalBinding);
                    }

                    // Add binding to YProperty of transfrom if orientation is not vertical
                    if (orientation != SelectiveScrollingOrientation.Vertical)
                    {
                        Binding verticalBinding = new Binding("ContentVerticalOffset");
                        verticalBinding.Source = scrollViewer;
                        BindingOperations.SetBinding(translateTransform, TranslateTransform.YProperty, verticalBinding);
                    }

                    element.RenderTransform = translateTransform;
                }
            }
        }
    }
}
