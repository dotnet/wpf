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

        private void RenderTheme(DrawingContext dc)
        {
            Size size = RenderSize;
            bool horizontal = Orientation == Orientation.Horizontal;
            bool isClickable = IsClickable && IsEnabled;
            bool isHovered = isClickable && IsHovered;
            bool isPressed = isClickable && IsPressed;
            ListSortDirection? sortDirection = SortDirection;
            bool isSorted = sortDirection != null;
            bool isSelected = IsSelected;

            EnsureCache((int)RoyaleFreezables.NumFreezables);

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
            RoyaleFreezables backgroundType = isPressed ? RoyaleFreezables.PressedBackground : isHovered ? RoyaleFreezables.HoveredBackground : RoyaleFreezables.NormalBackground;
            LinearGradientBrush background = (LinearGradientBrush)GetCachedFreezable((int)backgroundType);
            if (background == null)
            {
                background = new LinearGradientBrush();
                background.StartPoint = new Point();
                background.EndPoint = new Point(0.0, 1.0);

                if (isPressed)
                {
                    background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xB9, 0xB9, 0xC8), 0.0));
                    background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEC, 0xEC, 0xF3), 0.1));
                    background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEC, 0xEC, 0xF3), 1.0));
                }
                else if (isHovered || isSelected)
                {
                    background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFE, 0xFE, 0xFE), 0.0));
                    background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFE, 0xFE, 0xFE), 0.85));
                    background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xBD, 0xBE, 0xCE), 1.0));
                }
                else
                {
                    background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF9, 0xFA, 0xFD), 0.0));
                    background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF9, 0xFA, 0xFD), 0.85));
                    background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xBD, 0xBE, 0xCE), 1.0));
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

                Pen tabStroke = (Pen)GetCachedFreezable((int)RoyaleFreezables.TabStroke);
                if (tabStroke == null)
                {
                    SolidColorBrush tabStrokeBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xF8, 0xA9, 0x00));
                    tabStrokeBrush.Freeze();

                    tabStroke = new Pen(tabStrokeBrush, 1.0);
                    tabStroke.Freeze();

                    CacheFreezable(tabStroke, (int)RoyaleFreezables.TabStroke);
                }

                LinearGradientBrush tabFill = (LinearGradientBrush)GetCachedFreezable((int)RoyaleFreezables.TabFill);
                if (tabFill == null)
                {
                    tabFill = new LinearGradientBrush();
                    tabFill.StartPoint = new Point();
                    tabFill.EndPoint = new Point(1.0, 0.0);

                    tabFill.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFC, 0xE0, 0xA6), 0.0));
                    tabFill.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF6, 0xC4, 0x56), 0.1));
                    tabFill.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF6, 0xC4, 0x56), 0.9));
                    tabFill.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xDF, 0x97, 0x00), 1.0));

                    tabFill.Freeze();
                    CacheFreezable(tabFill, (int)RoyaleFreezables.TabFill);
                }

                dc.DrawGeometry(tabFill, tabStroke, tabGeometry);

                dc.Pop(); // Translate Transform
            }

            if (isPressed && (size.Width >= 2.0) && (size.Height >= 2.0))
            {
                // When pressed, there is a border on the left and bottom
                SolidColorBrush border = (SolidColorBrush)GetCachedFreezable((int)RoyaleFreezables.PressedBorder);
                if (border == null)
                {
                    border = new SolidColorBrush(Color.FromArgb(0xFF, 0x80, 0x80, 0x99));
                    border.Freeze();
                    CacheFreezable(border, (int)RoyaleFreezables.PressedBorder);
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
                        LinearGradientBrush gripper = (LinearGradientBrush)GetCachedFreezable((int)(horizontal ? RoyaleFreezables.HorizontalGripper : RoyaleFreezables.VerticalGripper));
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
                            CacheFreezable(gripper, (int)(horizontal ? RoyaleFreezables.HorizontalGripper : RoyaleFreezables.VerticalGripper));
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
                PathGeometry arrowGeometry = (PathGeometry)GetCachedFreezable(ascending ? (int)RoyaleFreezables.ArrowUpGeometry : (int)RoyaleFreezables.ArrowDownGeometry);
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

                    CacheFreezable(arrowGeometry, ascending ? (int)RoyaleFreezables.ArrowUpGeometry : (int)RoyaleFreezables.ArrowDownGeometry);
                }

                SolidColorBrush arrowFill = (SolidColorBrush)GetCachedFreezable((int)RoyaleFreezables.ArrowFill);
                if (arrowFill == null)
                {
                    arrowFill = new SolidColorBrush(Color.FromArgb(0xFF, 0xAC, 0xA8, 0x99));
                    arrowFill.Freeze();
                    CacheFreezable(arrowFill, (int)RoyaleFreezables.ArrowFill);
                }

                dc.DrawGeometry(arrowFill, null, arrowGeometry);

                dc.Pop(); // Position Transform
            }

            if (horizontal)
            {
                dc.Pop(); // Horizontal Rotate
            }
        }

        private enum RoyaleFreezables
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
