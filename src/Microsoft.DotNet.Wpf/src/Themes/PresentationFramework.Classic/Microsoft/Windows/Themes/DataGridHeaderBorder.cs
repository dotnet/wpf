// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using MS.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    ///     A Border used to provide the default look of headers.
    ///     When Background or BorderBrush are set, the rendering will
    ///     revert back to the default Border implementation.
    /// </summary>
    public partial class DataGridHeaderBorder
    {
        #region Theme Rendering

        private Thickness? ThemeDefaultPadding
        {
            get
            {
                return null;
            }
        }

        private static readonly DependencyProperty ControlBrushProperty =
            DependencyProperty.Register("ControlBrush", typeof(Brush), typeof(DataGridHeaderBorder), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        private Brush EnsureControlBrush()
        {
            if (ReadLocalValue(ControlBrushProperty) == DependencyProperty.UnsetValue)
            {
                SetResourceReference(ControlBrushProperty, SystemColors.ControlBrushKey);
            }

            return (Brush)GetValue(ControlBrushProperty);
        }

        private void RenderTheme(DrawingContext dc)
        {
            Size size = RenderSize;
            bool isClickable = IsClickable && IsEnabled;
            bool isPressed = isClickable && IsPressed;
            ListSortDirection? sortDirection = SortDirection;
            bool isSorted = sortDirection != null;
            bool horizontal = Orientation == Orientation.Horizontal;
            Brush background = EnsureControlBrush();
            Brush light = SystemColors.ControlLightBrush;
            Brush dark = SystemColors.ControlDarkBrush;
            bool shouldDrawRight = true;
            bool shouldDrawBottom = true;
            bool usingSeparatorBrush = false;

            Brush darkDarkRight = null;
            if (!horizontal)
            {
                if (SeparatorVisibility == Visibility.Visible && SeparatorBrush != null)
                {
                    darkDarkRight = SeparatorBrush;
                    usingSeparatorBrush = true;
                }
                else
                {
                    shouldDrawRight = false;
                }
            }
            else
            {
                darkDarkRight = SystemColors.ControlDarkDarkBrush;
            }

            Brush darkDarkBottom = null;
            if (horizontal)
            {
                if (SeparatorVisibility == Visibility.Visible && SeparatorBrush != null)
                {
                    darkDarkBottom = SeparatorBrush;
                    usingSeparatorBrush = true;
                }
                else
                {
                    shouldDrawBottom = false;
                }
            }
            else
            {
                darkDarkBottom = SystemColors.ControlDarkDarkBrush;
            }

            EnsureCache((int)ClassicFreezables.NumFreezables);

            // Draw the background
            dc.DrawRectangle(background, null, new Rect(0.0, 0.0, size.Width, size.Height));

            if ((size.Width > 3.0) && (size.Height > 3.0))
            {
                // Draw the border
                if (isPressed)
                {
                    dc.DrawRectangle(dark, null, new Rect(0.0, 0.0, size.Width, 1.0));
                    dc.DrawRectangle(dark, null, new Rect(0.0, 0.0, 1.0, size.Height));
                    dc.DrawRectangle(dark, null, new Rect(0.0, Max0(size.Height - 1.0), size.Width, 1.0));
                    dc.DrawRectangle(dark, null, new Rect(Max0(size.Width - 1.0), 0.0, 1.0, size.Height));
                }
                else
                {
                    dc.DrawRectangle(light, null, new Rect(0.0, 0.0, 1.0, Max0(size.Height - 1.0)));
                    dc.DrawRectangle(light, null, new Rect(0.0, 0.0, Max0(size.Width - 1.0), 1.0));

                    if (shouldDrawRight)
                    {
                        if (!usingSeparatorBrush)
                        {
                            dc.DrawRectangle(dark, null, new Rect(Max0(size.Width - 2.0), 1.0, 1.0, Max0(size.Height - 2.0)));
                        }

                        dc.DrawRectangle(darkDarkRight, null, new Rect(Max0(size.Width - 1.0), 0.0, 1.0, size.Height));
                    }

                    if (shouldDrawBottom)
                    {
                        if (!usingSeparatorBrush)
                        {
                            dc.DrawRectangle(dark, null, new Rect(1.0, Max0(size.Height - 2.0), Max0(size.Width - 2.0), 1.0));
                        }

                        dc.DrawRectangle(darkDarkBottom, null, new Rect(0.0, Max0(size.Height - 1.0), size.Width, 1.0));
                    }
                }
            }

            if (isSorted && (size.Width > 14.0) && (size.Height > 10.0))
            {
                // If sorted, draw an arrow on the right
                TranslateTransform positionTransform = new TranslateTransform(size.Width - 15.0, (size.Height - 5.0) * 0.5);
                positionTransform.Freeze();
                dc.PushTransform(positionTransform);

                bool ascending = (sortDirection == ListSortDirection.Ascending);
                PathGeometry arrowGeometry = (PathGeometry)GetCachedFreezable(ascending ? (int)ClassicFreezables.ArrowUpGeometry : (int)ClassicFreezables.ArrowDownGeometry);
                if (arrowGeometry == null)
                {
                    arrowGeometry = new PathGeometry();
                    PathFigure arrowFigure = new PathFigure();

                    if (ascending)
                    {
                        arrowFigure.StartPoint = new Point(0.0, 5.0);

                        LineSegment line = new LineSegment(new Point(5.0, 0.0), false);
                        line.Freeze();
                        arrowFigure.Segments.Add(line);

                        line = new LineSegment(new Point(10.0, 5.0), false);
                        line.Freeze();
                        arrowFigure.Segments.Add(line);
                    }
                    else
                    {
                        arrowFigure.StartPoint = new Point(0.0, 0.0);

                        LineSegment line = new LineSegment(new Point(10.0, 0.0), false);
                        line.Freeze();
                        arrowFigure.Segments.Add(line);

                        line = new LineSegment(new Point(5.0, 5.0), false);
                        line.Freeze();
                        arrowFigure.Segments.Add(line);
                    }

                    arrowFigure.IsClosed = true;
                    arrowFigure.Freeze();

                    arrowGeometry.Figures.Add(arrowFigure);
                    arrowGeometry.Freeze();

                    CacheFreezable(arrowGeometry, ascending ? (int)ClassicFreezables.ArrowUpGeometry : (int)ClassicFreezables.ArrowDownGeometry);
                }

                // DDVSO:447486
                // In high contrast scenarios, don't draw as disabled text color as this is confusing.  Instead, draw the same color as the control text.
                Brush sortArrowColor = 
                    (!AccessibilitySwitches.UseNetFx47CompatibleAccessibilityFeatures && SystemParameters.HighContrast) 
                    ? SystemColors.ControlTextBrush : SystemColors.GrayTextBrush;

                dc.DrawGeometry(sortArrowColor, null, arrowGeometry);

                dc.Pop(); // Position Transform
            }
        }

        private enum ClassicFreezables
        {
            ArrowUpGeometry,
            ArrowDownGeometry,
            NumFreezables
        }

        #endregion
    }
}
