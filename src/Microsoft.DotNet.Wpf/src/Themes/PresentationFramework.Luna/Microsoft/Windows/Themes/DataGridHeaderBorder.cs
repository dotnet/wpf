// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        /// <summary>
        ///    DependencyProperty to assign the ThemeColor to use
        /// </summary>
        public static readonly DependencyProperty ThemeColorProperty =
                DependencyProperty.Register(
                        "ThemeColor",
                        typeof(ThemeColor),
                        typeof(DataGridHeaderBorder),
                        new FrameworkPropertyMetadata(
                                ThemeColor.NormalColor,
                                OnThemeColorChanged),
                        new ValidateValueCallback(IsValidThemeColor));

        /// <summary>
        /// The color variation of the button.
        /// </summary>
        public ThemeColor ThemeColor
        {
            get { return (ThemeColor)GetValue(ThemeColorProperty); }
            set { SetValue(ThemeColorProperty, value); }
        }

        private static bool IsValidThemeColor(object o)
        {
            ThemeColor color = (ThemeColor)o;
            return color == ThemeColor.NormalColor ||
                color == ThemeColor.Homestead ||
                color == ThemeColor.Metallic;
        }

        private static void OnThemeColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Release all the old resources.
            ReleaseCache();

            DataGridHeaderBorder border = (DataGridHeaderBorder)d;

            // Everything needs to be re-done.
            border.InvalidateMeasure();
            border.InvalidateArrange();
            border.InvalidateVisual();
        }

        private void RenderTheme(DrawingContext dc)
        {
            ThemeColor themeColor = ThemeColor;
            Size size = RenderSize;
            bool horizontal = Orientation == Orientation.Horizontal;
            bool isClickable = IsClickable && IsEnabled;
            bool isHovered = isClickable && IsHovered;
            bool isPressed = isClickable && IsPressed;
            ListSortDirection? sortDirection = SortDirection;
            bool isSorted = sortDirection != null;
            bool isSelected = IsSelected;

            EnsureCache((int)LunaFreezables.NumFreezables);

            if (horizontal)
            {
                // When horizontal, rotate the rendering by -90 degrees
                Matrix m1 = new Matrix();
                m1.RotateAt(-90.0, 0.0, 0.0);
                Matrix m2 = new Matrix();
                m2.Translate(0.0, size.Height);

                MatrixTransform horizontalRotate = new MatrixTransform(m1 * m2);
                horizontalRotate.Freeze();
                dc.PushTransform(horizontalRotate);

                double temp = size.Width;
                size.Width = size.Height;
                size.Height = temp;
            }

            // Draw the background
            LunaFreezables backgroundType = isPressed ? LunaFreezables.PressedBackground : isHovered ? LunaFreezables.HoveredBackground : LunaFreezables.NormalBackground;
            LinearGradientBrush background = (LinearGradientBrush)GetCachedFreezable((int)backgroundType);
            if (background == null)
            {
                background = new LinearGradientBrush();
                background.StartPoint = new Point();
                background.EndPoint = new Point(0.0, 1.0);

                if (isPressed)
                {
                    if (themeColor == ThemeColor.Metallic)
                    {
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xB9, 0xB9, 0xC8), 0.0));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEC, 0xEC, 0xF3), 0.1));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEC, 0xEC, 0xF3), 1.0));
                    }
                    else
                    {
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC1, 0xC2, 0xB8), 0.0));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xDE, 0xDF, 0xD8), 0.1));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xDE, 0xDF, 0xD8), 1.0));
                    }
                }
                else if (isHovered || isSelected)
                {
                    if (themeColor == ThemeColor.Metallic)
                    {
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFE, 0xFE, 0xFE), 0.0));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFE, 0xFE, 0xFE), 0.85));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xBD, 0xBE, 0xCE), 1.0));
                    }
                    else
                    {
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFA, 0xF9, 0xF4), 0.0));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFA, 0xF9, 0xF4), 0.85));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEC, 0xE9, 0xD8), 1.0));
                    }
                }
                else
                {
                    if (themeColor == ThemeColor.Metallic)
                    {
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF9, 0xFA, 0xFD), 0.0));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF9, 0xFA, 0xFD), 0.85));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xBD, 0xBE, 0xCE), 1.0));
                    }
                    else
                    {
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEB, 0xEA, 0xDB), 0.0));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEB, 0xEA, 0xDB), 0.85));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xCB, 0xC7, 0xB8), 1.0));
                    }
                }

                background.Freeze();
                CacheFreezable(background, (int)backgroundType);
            }

            dc.DrawRectangle(background, null, new Rect(0.0, 0.0, size.Width, size.Height));

            if (isHovered && !isPressed && (size.Width >= 6.0) && (size.Height >= 4.0))
            {
                // When hovered, there is a colored tab at the bottom
                TranslateTransform positionTransform = new TranslateTransform(0.0, size.Height - 3.0);
                positionTransform.Freeze();
                dc.PushTransform(positionTransform);

                PathGeometry tabGeometry = new PathGeometry();
                PathFigure tabFigure = new PathFigure();

                tabFigure.StartPoint = new Point(0.5, 0.5);

                LineSegment line = new LineSegment(new Point(size.Width - 0.5, 0.5), true);
                line.Freeze();
                tabFigure.Segments.Add(line);

                ArcSegment arc = new ArcSegment(new Point(size.Width - 2.5, 2.5), new Size(2.0, 2.0), 90.0, false, SweepDirection.Clockwise, true);
                arc.Freeze();
                tabFigure.Segments.Add(arc);

                line = new LineSegment(new Point(2.5, 2.5), true);
                line.Freeze();
                tabFigure.Segments.Add(line);

                arc = new ArcSegment(new Point(0.5, 0.5), new Size(2.0, 2.0), 90.0, false, SweepDirection.Clockwise, true);
                arc.Freeze();
                tabFigure.Segments.Add(arc);

                tabFigure.IsClosed = true;
                tabFigure.Freeze();

                tabGeometry.Figures.Add(tabFigure);
                tabGeometry.Freeze();

                Pen tabStroke = (Pen)GetCachedFreezable((int)LunaFreezables.TabStroke);
                if (tabStroke == null)
                {
                    SolidColorBrush tabStrokeBrush = new SolidColorBrush((themeColor == ThemeColor.Homestead) ? Color.FromArgb(0xFF, 0xCF, 0x72, 0x25) : Color.FromArgb(0xFF, 0xF8, 0xA9, 0x00));
                    tabStrokeBrush.Freeze();

                    tabStroke = new Pen(tabStrokeBrush, 1.0);
                    tabStroke.Freeze();

                    CacheFreezable(tabStroke, (int)LunaFreezables.TabStroke);
                }

                LinearGradientBrush tabFill = (LinearGradientBrush)GetCachedFreezable((int)LunaFreezables.TabFill);
                if (tabFill == null)
                {
                    tabFill = new LinearGradientBrush();
                    tabFill.StartPoint = new Point();
                    tabFill.EndPoint = new Point(1.0, 0.0);
                    if (themeColor == ThemeColor.Homestead)
                    {
                        tabFill.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xE3, 0x91, 0x4F), 0.0));
                        tabFill.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xE3, 0x91, 0x4F), 1.0));
                    }
                    else
                    {
                        tabFill.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFC, 0xE0, 0xA6), 0.0));
                        tabFill.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF6, 0xC4, 0x56), 0.1));
                        tabFill.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF6, 0xC4, 0x56), 0.9));
                        tabFill.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xDF, 0x97, 0x00), 1.0));
                    }

                    tabFill.Freeze();
                    CacheFreezable(tabFill, (int)LunaFreezables.TabFill);
                }

                dc.DrawGeometry(tabFill, tabStroke, tabGeometry);

                dc.Pop(); // Translate Transform
            }

            if (isPressed && (size.Width >= 2.0) && (size.Height >= 2.0))
            {
                // When pressed, there is a border on the left and bottom
                SolidColorBrush border = (SolidColorBrush)GetCachedFreezable((int)LunaFreezables.PressedBorder);
                if (border == null)
                {
                    border = new SolidColorBrush((themeColor == ThemeColor.Metallic) ? Color.FromArgb(0xFF, 0x80, 0x80, 0x99) : Color.FromArgb(0xFF, 0xA5, 0xA5, 0x97));
                    border.Freeze();
                    CacheFreezable(border, (int)LunaFreezables.PressedBorder);
                }

                dc.DrawRectangle(border, null, new Rect(0.0, 0.0, 1.0, size.Height));
                dc.DrawRectangle(border, null, new Rect(0.0, Max0(size.Height - 1.0), size.Width, 1.0));
            }

            if (!isPressed && !isHovered && (size.Width >= 4.0))
            {
                if (SeparatorVisibility == Visibility.Visible)
                {
                    Brush sideBrush;
                    if (SeparatorBrush != null)
                    {
                        sideBrush = SeparatorBrush;
                    }
                    else
                    {
                        // When not pressed or hovered, draw the resize gripper
                        LinearGradientBrush gripper = (LinearGradientBrush)GetCachedFreezable((int)(horizontal ? LunaFreezables.HorizontalGripper : LunaFreezables.VerticalGripper));
                        if (gripper == null)
                        {
                            gripper = new LinearGradientBrush();
                            gripper.StartPoint = new Point();
                            gripper.EndPoint = new Point(1.0, 0.0);

                            Color highlight = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
                            Color shadow = Color.FromArgb(0xFF, 0xC7, 0xC5, 0xB2);

                            if (horizontal)
                            {
                                gripper.GradientStops.Add(new GradientStop(highlight, 0.0));
                                gripper.GradientStops.Add(new GradientStop(highlight, 0.25));
                                gripper.GradientStops.Add(new GradientStop(shadow, 0.75));
                                gripper.GradientStops.Add(new GradientStop(shadow, 1.0));
                            }
                            else
                            {
                                gripper.GradientStops.Add(new GradientStop(shadow, 0.0));
                                gripper.GradientStops.Add(new GradientStop(shadow, 0.25));
                                gripper.GradientStops.Add(new GradientStop(highlight, 0.75));
                                gripper.GradientStops.Add(new GradientStop(highlight, 1.0));
                            }

                            gripper.Freeze();
                            CacheFreezable(gripper, (int)(horizontal ? LunaFreezables.HorizontalGripper : LunaFreezables.VerticalGripper));
                        }

                        sideBrush = gripper;
                    }

                    dc.DrawRectangle(sideBrush, null, new Rect(horizontal ? 0.0 : Max0(size.Width - 2.0), 4.0, 2.0, Max0(size.Height - 8.0)));
                }
            }

            if (isSorted && (size.Width > 14.0) && (size.Height > 10.0))
            {
                // When sorted, draw an arrow on the right
                TranslateTransform positionTransform = new TranslateTransform(size.Width - 15.0, (size.Height - 5.0) * 0.5);
                positionTransform.Freeze();
                dc.PushTransform(positionTransform);

                bool ascending = (sortDirection == ListSortDirection.Ascending);
                PathGeometry arrowGeometry = (PathGeometry)GetCachedFreezable(ascending ? (int)LunaFreezables.ArrowUpGeometry : (int)LunaFreezables.ArrowDownGeometry);
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

                    CacheFreezable(arrowGeometry, ascending ? (int)LunaFreezables.ArrowUpGeometry : (int)LunaFreezables.ArrowDownGeometry);
                }

                SolidColorBrush arrowFill = (SolidColorBrush)GetCachedFreezable((int)LunaFreezables.ArrowFill);
                if (arrowFill == null)
                {
                    arrowFill = new SolidColorBrush(Color.FromArgb(0xFF, 0xAC, 0xA8, 0x99));
                    arrowFill.Freeze();
                    CacheFreezable(arrowFill, (int)LunaFreezables.ArrowFill);
                }

                dc.DrawGeometry(arrowFill, null, arrowGeometry);

                dc.Pop(); // Position Transform
            }

            if (horizontal)
            {
                dc.Pop(); // Horizontal Rotate
            }
        }

        private enum LunaFreezables
        {
            NormalBackground,
            HoveredBackground,
            PressedBackground,
            HorizontalGripper,
            VerticalGripper,
            PressedBorder,
            TabGeometry,
            TabStroke,
            TabFill,
            ArrowFill,
            ArrowUpGeometry,
            ArrowDownGeometry,
            NumFreezables
        }

        #endregion
    }
}
