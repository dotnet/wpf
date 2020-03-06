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

            EnsureCache((int)AeroLiteFreezables.NumFreezables);

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

            // Fill the background
            AeroLiteFreezables backgroundType = AeroLiteFreezables.NormalBackground;
            if (isPressed)
            {
                backgroundType = AeroLiteFreezables.PressedBackground;
            }
            else if (isHovered)
            {
                backgroundType = AeroLiteFreezables.HoveredBackground;
            }

            SolidColorBrush background = (SolidColorBrush)GetCachedFreezable((int)backgroundType);
            if (background == null)
            {
                background = new SolidColorBrush();

                switch (backgroundType)
                {
                    case AeroLiteFreezables.NormalBackground:
                        background.Color = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
                        break;

                    case AeroLiteFreezables.PressedBackground:
                        background.Color = Color.FromArgb(0xFF, 0x85, 0xD2, 0xF5);
                        break;

                    case AeroLiteFreezables.HoveredBackground:
                        background.Color = Color.FromArgb(0xFF, 0xBC, 0xEC, 0xFC);
                        break;
                }

                background.Freeze();

                CacheFreezable(background, (int)backgroundType);
            }

            dc.DrawRectangle(background, null, new Rect(0.0, 0.0, size.Width, size.Height));

            if (size.Width >= 2.0 || size.Height >= 2.0)
            {
                // Draw the borders on the sides
                AeroLiteFreezables sideType = AeroLiteFreezables.NormalSides;
                if (isPressed)
                {
                    sideType = AeroLiteFreezables.PressedSides;
                }
                else if (isHovered)
                {
                    sideType = AeroLiteFreezables.HoveredSides;
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
                            switch (sideType)
                            {
                                case AeroLiteFreezables.NormalSides:
                                    sideBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xDE, 0xDF, 0xE1));
                                    break;

                                case AeroLiteFreezables.PressedSides:
                                    sideBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x4F, 0x90, 0xAE));
                                    break;

                                case AeroLiteFreezables.HoveredSides:
                                    sideBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x69, 0xBB, 0xE3));
                                    break;
                            }

                            sideBrush.Freeze();

                            CacheFreezable(sideBrush, (int)sideType);
                        }
                    }

                    if (size.Width >= 2.0)
                    {
                        if (horizontal)
                        {
                            dc.DrawRectangle(sideBrush, null, new Rect(0.0, 0.0, 1.0, size.Height)); // left

                            if (sideType != AeroLiteFreezables.NormalSides)
                            {
                                dc.DrawRectangle(sideBrush, null, new Rect(size.Width - 0.0, 0.0, 1.0, size.Height)); // right
                            }
                        }
                        else
                        {
                            if (sideType != AeroLiteFreezables.NormalSides)
                            {
                                dc.DrawRectangle(sideBrush, null, new Rect(-1.0, 0.0, 1.0, size.Height)); // left
                            }
                            
                            dc.DrawRectangle(sideBrush, null, new Rect(size.Width - 1.0, 0.0, 1.0, size.Height)); // right
                        }
                    }

                    if (size.Height >= 2.0)
                    {
                        dc.DrawRectangle(sideBrush, null, new Rect(0.0, 0.0, size.Width, 1.0)); // top
                        dc.DrawRectangle(sideBrush, null, new Rect(0.0, size.Height - 1.0, size.Width, 1.0)); // bottom
                    }
                }
            }

            if (isSorted && (size.Width > 14.0) && (size.Height > 10.0))
            {
                // Draw the sort arrow
                TranslateTransform positionTransform = new TranslateTransform((size.Width - 8.0) * 0.5, 1.0);
                positionTransform.Freeze();
                dc.PushTransform(positionTransform);

                bool ascending = (sortDirection == ListSortDirection.Ascending);
                PathGeometry arrowGeometry = (PathGeometry)GetCachedFreezable(ascending ? (int)AeroLiteFreezables.ArrowUpGeometry : (int)AeroLiteFreezables.ArrowDownGeometry);
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

                    CacheFreezable(arrowGeometry, ascending ? (int)AeroLiteFreezables.ArrowUpGeometry : (int)AeroLiteFreezables.ArrowDownGeometry);
                }

                // Draw two arrows, one inset in the other. This is to achieve a double gradient over both the border and the fill.
                SolidColorBrush arrowFill = (SolidColorBrush)GetCachedFreezable((int)AeroLiteFreezables.ArrowFill);
                if (arrowFill == null)
                {
                    arrowFill = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0x00));
                    arrowFill.Freeze();
                    CacheFreezable(arrowFill, (int)AeroLiteFreezables.ArrowFill);
                }

                dc.DrawGeometry(arrowFill, null, arrowGeometry);

                dc.Pop(); // Position Transform
            }

            if (horizontal)
            {
                dc.Pop(); // Horizontal Rotate
            }
        }

        private enum AeroLiteFreezables : int
        {
            NormalBackground,
            PressedBackground,
            HoveredBackground,
            NormalSides,
            PressedSides,
            HoveredSides,
            ArrowFill,
            ArrowUpGeometry,
            ArrowDownGeometry,
            NumFreezables
        }

        #endregion
    }
}
