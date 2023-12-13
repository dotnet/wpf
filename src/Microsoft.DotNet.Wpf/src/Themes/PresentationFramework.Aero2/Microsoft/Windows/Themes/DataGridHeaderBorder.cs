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
                if (Orientation == Orientation.Vertical)
                {
                    return new Thickness(5.0, 4.0, 5.0, 4.0);
                }
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
            bool hasBevel = (!isHovered && !isPressed && !isSorted && !isSelected);

            EnsureCache((int)AeroFreezables.NumFreezables);

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

            if (hasBevel)
            {
                // This is a highlight that can be drawn by just filling the background with the color.
                // It will be seen through the gab between the border and the background.
                LinearGradientBrush bevel = (LinearGradientBrush)GetCachedFreezable((int)AeroFreezables.NormalBevel);
                if (bevel == null)
                {
                    bevel = new LinearGradientBrush();
                    bevel.StartPoint = new Point();
                    bevel.EndPoint = new Point(0.0, 1.0);
                    bevel.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0.0));
                    bevel.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0.4));
                    bevel.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFC, 0xFC, 0xFD), 0.4));
                    bevel.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFB, 0xFC, 0xFC), 1.0));
                    bevel.Freeze();

                    CacheFreezable(bevel, (int)AeroFreezables.NormalBevel);
                }

                dc.DrawRectangle(bevel, null, new Rect(0.0, 0.0, size.Width, size.Height));
            }

            // Fill the background
            AeroFreezables backgroundType = AeroFreezables.NormalBackground;
            if (isPressed)
            {
                backgroundType = AeroFreezables.PressedBackground;
            }
            else if (isHovered)
            {
                backgroundType = AeroFreezables.HoveredBackground;
            }
            else if (isSorted || isSelected)
            {
                backgroundType = AeroFreezables.SortedBackground;
            }

            LinearGradientBrush background = (LinearGradientBrush)GetCachedFreezable((int)backgroundType);
            if (background == null)
            {
                background = new LinearGradientBrush();
                background.StartPoint = new Point();
                background.EndPoint = new Point(0.0, 1.0);

                switch (backgroundType)
                {
                    case AeroFreezables.NormalBackground:
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0.0));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0.4));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF7, 0xF8, 0xFA), 0.4));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF1, 0xF2, 0xF4), 1.0));
                        break;

                    case AeroFreezables.PressedBackground:
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xBC, 0xE4, 0xF9), 0.0));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xBC, 0xE4, 0xF9), 0.4));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x8D, 0xD6, 0xF7), 0.4));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x8A, 0xD1, 0xF5), 1.0));
                        break;

                    case AeroFreezables.HoveredBackground:
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xE3, 0xF7, 0xFF), 0.0));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xE3, 0xF7, 0xFF), 0.4));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xBD, 0xED, 0xFF), 0.4));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xB7, 0xE7, 0xFB), 1.0));
                        break;

                    case AeroFreezables.SortedBackground:
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF2, 0xF9, 0xFC), 0.0));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF2, 0xF9, 0xFC), 0.4));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xE1, 0xF1, 0xF9), 0.4));
                        background.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xD8, 0xEC, 0xF6), 1.0));
                        break;
                }

                background.Freeze();

                CacheFreezable(background, (int)backgroundType);
            }

            dc.DrawRectangle(background, null, new Rect(0.0, 0.0, size.Width, size.Height));

            if (size.Width >= 2.0)
            {
                // Draw the borders on the sides
                AeroFreezables sideType = AeroFreezables.NormalSides;
                if (isPressed)
                {
                    sideType = AeroFreezables.PressedSides;
                }
                else if (isHovered)
                {
                    sideType = AeroFreezables.HoveredSides;
                }
                else if (isSorted || isSelected)
                {
                    sideType = AeroFreezables.SortedSides;
                }

                if (SeparatorVisibility == Visibility.Visible)
                {
                    Brush sideBrush;
                    if (SeparatorBrush != null)
                    {
                        sideBrush = SeparatorBrush;
                    }
                    else
                    {
                        sideBrush = (Brush)GetCachedFreezable((int)sideType);
                        if (sideBrush == null)
                        {
                            LinearGradientBrush lgBrush = null;
                            if (sideType != AeroFreezables.SortedSides)
                            {
                                lgBrush = new LinearGradientBrush();
                                lgBrush.StartPoint = new Point();
                                lgBrush.EndPoint = new Point(0.0, 1.0);
                                sideBrush = lgBrush;
                            }

                            switch (sideType)
                            {
                                case AeroFreezables.NormalSides:
                                    lgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xF2, 0xF2, 0xF2), 0.0));
                                    lgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xEF, 0xEF, 0xEF), 0.4));
                                    lgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xE7, 0xE8, 0xEA), 0.4));
                                    lgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xDE, 0xDF, 0xE1), 1.0));
                                    break;

                                case AeroFreezables.PressedSides:
                                    lgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x7A, 0x9E, 0xB1), 0.0));
                                    lgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x7A, 0x9E, 0xB1), 0.4));
                                    lgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x50, 0x91, 0xAF), 0.4));
                                    lgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x4D, 0x8D, 0xAD), 1.0));
                                    break;

                                case AeroFreezables.HoveredSides:
                                    lgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x88, 0xCB, 0xEB), 0.0));
                                    lgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x88, 0xCB, 0xEB), 0.4));
                                    lgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x69, 0xBB, 0xE3), 0.4));
                                    lgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x69, 0xBB, 0xE3), 1.0));
                                    break;

                                case AeroFreezables.SortedSides:
                                    sideBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x96, 0xD9, 0xF9));
                                    break;
                            }

                            sideBrush.Freeze();

                            CacheFreezable(sideBrush, (int)sideType);
                        }
                    }

                    dc.DrawRectangle(sideBrush, null, new Rect(0.0, 0.0, 1.0, Max0(size.Height - 0.95)));
                    dc.DrawRectangle(sideBrush, null, new Rect(size.Width - 1.0, 0.0, 1.0, Max0(size.Height - 0.95)));
                }
            }

            if (isPressed && (size.Width >= 4.0) && (size.Height >= 4.0))
            {
                // When pressed, there are added borders on the left and top
                LinearGradientBrush topBrush = (LinearGradientBrush)GetCachedFreezable((int)AeroFreezables.PressedTop);
                if (topBrush == null)
                {
                    topBrush = new LinearGradientBrush();
                    topBrush.StartPoint = new Point();
                    topBrush.EndPoint = new Point(0.0, 1.0);
                    topBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x86, 0xA3, 0xB2), 0.0));
                    topBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x86, 0xA3, 0xB2), 0.1));
                    topBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xAA, 0xCE, 0xE1), 0.9));
                    topBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xAA, 0xCE, 0xE1), 1.0));
                    topBrush.Freeze();

                    CacheFreezable(topBrush, (int)AeroFreezables.PressedTop);
                }

                dc.DrawRectangle(topBrush, null, new Rect(0.0, 0.0, size.Width, 2.0));

                LinearGradientBrush pressedBevel = (LinearGradientBrush)GetCachedFreezable((int)AeroFreezables.PressedBevel);
                if (pressedBevel == null)
                {
                    pressedBevel = new LinearGradientBrush();
                    pressedBevel.StartPoint = new Point();
                    pressedBevel.EndPoint = new Point(0.0, 1.0);
                    pressedBevel.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xA2, 0xCB, 0xE0), 0.0));
                    pressedBevel.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xA2, 0xCB, 0xE0), 0.4));
                    pressedBevel.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x72, 0xBC, 0xDF), 0.4));
                    pressedBevel.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x6E, 0xB8, 0xDC), 1.0));
                    pressedBevel.Freeze();

                    CacheFreezable(pressedBevel, (int)AeroFreezables.PressedBevel);
                }

                dc.DrawRectangle(pressedBevel, null, new Rect(1.0, 0.0, 1.0, size.Height - 0.95));
                dc.DrawRectangle(pressedBevel, null, new Rect(size.Width - 2.0, 0.0, 1.0, size.Height - 0.95));
            }

            if (size.Height >= 2.0)
            {
                // Draw the bottom border
                AeroFreezables bottomType = AeroFreezables.NormalBottom;
                if (isPressed)
                {
                    bottomType = AeroFreezables.PressedOrHoveredBottom;
                }
                else if (isHovered)
                {
                    bottomType = AeroFreezables.PressedOrHoveredBottom;
                }
                else if (isSorted || isSelected)
                {
                    bottomType = AeroFreezables.SortedBottom;
                }

                SolidColorBrush bottomBrush = (SolidColorBrush)GetCachedFreezable((int)bottomType);
                if (bottomBrush == null)
                {
                    switch (bottomType)
                    {
                        case AeroFreezables.NormalBottom:
                            bottomBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xD5, 0xD5, 0xD5));
                            break;

                        case AeroFreezables.PressedOrHoveredBottom:
                            bottomBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x93, 0xC9, 0xE3));
                            break;

                        case AeroFreezables.SortedBottom:
                            bottomBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x96, 0xD9, 0xF9));
                            break;
                    }

                    bottomBrush.Freeze();

                    CacheFreezable(bottomBrush, (int)bottomType);
                }

                dc.DrawRectangle(bottomBrush, null, new Rect(0.0, size.Height - 1.0, size.Width, 1.0));
            }

            if (isSorted && (size.Width > 14.0) && (size.Height > 10.0))
            {
                // Draw the sort arrow
                TranslateTransform positionTransform = new TranslateTransform((size.Width - 8.0) * 0.5, 1.0);
                positionTransform.Freeze();
                dc.PushTransform(positionTransform);

                bool ascending = (sortDirection == ListSortDirection.Ascending);
                PathGeometry arrowGeometry = (PathGeometry)GetCachedFreezable(ascending ? (int)AeroFreezables.ArrowUpGeometry : (int)AeroFreezables.ArrowDownGeometry);
                if (arrowGeometry == null)
                {
                    arrowGeometry = new PathGeometry();
                    PathFigure arrowFigure = new PathFigure();

                    if (ascending)
                    {
                        arrowFigure.StartPoint = new Point(0.0, 4.0);

                        LineSegment line = new LineSegment(new Point(4.0, 0.0), false);
                        line.Freeze();
                        arrowFigure.Segments.Add(line);

                        line = new LineSegment(new Point(8.0, 4.0), false);
                        line.Freeze();
                        arrowFigure.Segments.Add(line);
                    }
                    else
                    {
                        arrowFigure.StartPoint = new Point(0.0, 0.0);

                        LineSegment line = new LineSegment(new Point(8.0, 0.0), false);
                        line.Freeze();
                        arrowFigure.Segments.Add(line);

                        line = new LineSegment(new Point(4.0, 4.0), false);
                        line.Freeze();
                        arrowFigure.Segments.Add(line);
                    }

                    arrowFigure.IsClosed = true;
                    arrowFigure.Freeze();

                    arrowGeometry.Figures.Add(arrowFigure);
                    arrowGeometry.Freeze();

                    CacheFreezable(arrowGeometry, ascending ? (int)AeroFreezables.ArrowUpGeometry : (int)AeroFreezables.ArrowDownGeometry);
                }

                // Draw two arrows, one inset in the other. This is to achieve a double gradient over both the border and the fill.
                LinearGradientBrush arrowBorder = (LinearGradientBrush)GetCachedFreezable((int)AeroFreezables.ArrowBorder);
                if (arrowBorder == null)
                {
                    arrowBorder = new LinearGradientBrush();
                    arrowBorder.StartPoint = new Point();
                    arrowBorder.EndPoint = new Point(1.0, 1.0);
                    arrowBorder.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x3C, 0x5E, 0x72), 0.0));
                    arrowBorder.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x3C, 0x5E, 0x72), 0.1));
                    arrowBorder.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xC3, 0xE4, 0xF5), 1.0));
                    arrowBorder.Freeze();
                    CacheFreezable(arrowBorder, (int)AeroFreezables.ArrowBorder);
                }

                dc.DrawGeometry(arrowBorder, null, arrowGeometry);

                LinearGradientBrush arrowFill = (LinearGradientBrush)GetCachedFreezable((int)AeroFreezables.ArrowFill);
                if (arrowFill == null)
                {
                    arrowFill = new LinearGradientBrush();
                    arrowFill.StartPoint = new Point();
                    arrowFill.EndPoint = new Point(1.0, 1.0);
                    arrowFill.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x61, 0x96, 0xB6), 0.0));
                    arrowFill.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x61, 0x96, 0xB6), 0.1));
                    arrowFill.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xCA, 0xE6, 0xF5), 1.0));
                    arrowFill.Freeze();
                    CacheFreezable(arrowFill, (int)AeroFreezables.ArrowFill);
                }

                // Inset the fill arrow inside the border arrow
                ScaleTransform arrowScale = (ScaleTransform)GetCachedFreezable((int)AeroFreezables.ArrowFillScale);
                if (arrowScale == null)
                {
                    arrowScale = new ScaleTransform(0.75, 0.75, 3.5, 4.0);
                    arrowScale.Freeze();
                    CacheFreezable(arrowScale, (int)AeroFreezables.ArrowFillScale);
                }

                dc.PushTransform(arrowScale);

                dc.DrawGeometry(arrowFill, null, arrowGeometry);

                dc.Pop(); // Scale Transform
                dc.Pop(); // Position Transform
            }

            if (horizontal)
            {
                dc.Pop(); // Horizontal Rotate
            }
        }

        private enum AeroFreezables : int
        {
            NormalBevel,
            NormalBackground,
            PressedBackground,
            HoveredBackground,
            SortedBackground,
            PressedTop,
            NormalSides,
            PressedSides,
            HoveredSides,
            SortedSides,
            PressedBevel,
            NormalBottom,
            PressedOrHoveredBottom,
            SortedBottom,
            ArrowBorder,
            ArrowFill,
            ArrowFillScale,
            ArrowUpGeometry,
            ArrowDownGeometry,
            NumFreezables
        }

        #endregion
    }
}
