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
    ///     A Border used to provide the default look of DataGrid headers.
    ///     When Background or BorderBrush are set, the rendering will
    ///     revert back to the default Border implementation.
    /// </summary>
    public sealed partial class DataGridHeaderBorder : Border
    {
        static DataGridHeaderBorder()
        {
            // We always set this to true on these borders, so just default it to true here.
            SnapsToDevicePixelsProperty.OverrideMetadata(typeof(DataGridHeaderBorder), new FrameworkPropertyMetadata(true));
        }

        #region Header Appearance Properties

        /// <summary>
        ///     Whether the hover look should be applied.
        /// </summary>
        public bool IsHovered
        {
            get { return (bool)GetValue(IsHoveredProperty); }
            set { SetValue(IsHoveredProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for IsHovered.
        /// </summary>
        public static readonly DependencyProperty IsHoveredProperty =
            DependencyProperty.Register("IsHovered", typeof(bool), typeof(DataGridHeaderBorder), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///     Whether the pressed look should be applied.
        /// </summary>
        public bool IsPressed
        {
            get { return (bool)GetValue(IsPressedProperty); }
            set { SetValue(IsPressedProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for IsPressed.
        /// </summary>
        public static readonly DependencyProperty IsPressedProperty =
            DependencyProperty.Register("IsPressed", typeof(bool), typeof(DataGridHeaderBorder), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        ///     When false, will not apply the hover look even when IsHovered is true.
        /// </summary>
        public bool IsClickable
        {
            get { return (bool)GetValue(IsClickableProperty); }
            set { SetValue(IsClickableProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for IsClickable.
        /// </summary>
        public static readonly DependencyProperty IsClickableProperty =
            DependencyProperty.Register("IsClickable", typeof(bool), typeof(DataGridHeaderBorder), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        ///     Whether to appear sorted.
        /// </summary>
        public ListSortDirection? SortDirection
        {
            get { return (ListSortDirection?)GetValue(SortDirectionProperty); }
            set { SetValue(SortDirectionProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for SortDirection.
        /// </summary>
        public static readonly DependencyProperty SortDirectionProperty =
            DependencyProperty.Register("SortDirection", typeof(ListSortDirection?), typeof(DataGridHeaderBorder), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///     Whether to appear selected.
        /// </summary>
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for IsSelected.
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(DataGridHeaderBorder), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///     Vertical = column header
        ///     Horizontal = row header
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for Orientation.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(DataGridHeaderBorder), new FrameworkPropertyMetadata(Orientation.Vertical, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///     When there is a Background or BorderBrush, revert to the Border implementation.
        /// </summary>
        private bool UsingBorderImplementation
        {
            get
            {
                return (Background != null) || (BorderBrush != null);
            }
        }

        /// <summary>
        ///     Property that indicates the brush to use when drawing seperators between headers.
        /// </summary>
        public Brush SeparatorBrush
        {
            get { return (Brush)GetValue(SeparatorBrushProperty); }
            set { SetValue(SeparatorBrushProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for SeparatorBrush.
        /// </summary>
        public static readonly DependencyProperty SeparatorBrushProperty =
            DependencyProperty.Register("SeparatorBrush", typeof(Brush), typeof(DataGridHeaderBorder), new FrameworkPropertyMetadata(null));

        /// <summary>
        ///     Property that indicates the Visibility for the header seperators.
        /// </summary>
        public Visibility SeparatorVisibility
        {
            get { return (Visibility)GetValue(SeparatorVisibilityProperty); }
            set { SetValue(SeparatorVisibilityProperty, value); }
        }

        /// <summary>
        ///     DependencyProperty for SeperatorBrush.
        /// </summary>
        public static readonly DependencyProperty SeparatorVisibilityProperty =
            DependencyProperty.Register("SeparatorVisibility", typeof(Visibility), typeof(DataGridHeaderBorder), new FrameworkPropertyMetadata(Visibility.Visible));
        
        #endregion

        #region Layout

        /// <summary>
        ///     Calculates the desired size of the element given the constraint.
        /// </summary>
        protected override Size MeasureOverride(Size constraint)
        {
            if (UsingBorderImplementation)
            {
                // Revert to the Border implementation
                return base.MeasureOverride(constraint);
            }

            UIElement child = Child;
            if (child != null)
            {
                // Use the public Padding property if it's set
                Thickness padding = Padding;
                if (padding.Equals(new Thickness()))
                {
                    padding = DefaultPadding;
                }

                double childWidth = constraint.Width;
                double childHeight = constraint.Height;

                // If there is an actual constraint, then reserve space for the chrome
                if (!Double.IsInfinity(childWidth))
                {
                    childWidth = Math.Max(0.0, childWidth - padding.Left - padding.Right);
                }

                if (!Double.IsInfinity(childHeight))
                {
                    childHeight = Math.Max(0.0, childHeight - padding.Top - padding.Bottom);
                }

                child.Measure(new Size(childWidth, childHeight));
                Size desiredSize = child.DesiredSize;

                // Add on the reserved space for the chrome
                return new Size(desiredSize.Width + padding.Left + padding.Right, desiredSize.Height + padding.Top + padding.Bottom);
            }

            return new Size();
        }

        /// <summary>
        ///     Positions children and returns the final size of the element.
        /// </summary>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if (UsingBorderImplementation)
            {
                // Revert to the Border implementation
                return base.ArrangeOverride(arrangeSize);
            }

            UIElement child = Child;
            if (child != null)
            {
                // Use the public Padding property if it's set
                Thickness padding = Padding;
                if (padding.Equals(new Thickness()))
                {
                    padding = DefaultPadding;
                }

                // Reserve space for the chrome
                double childWidth = Math.Max(0.0, arrangeSize.Width - padding.Left - padding.Right);
                double childHeight = Math.Max(0.0, arrangeSize.Height - padding.Top - padding.Bottom);

                child.Arrange(new Rect(padding.Left, padding.Top, childWidth, childHeight));
            }

            return arrangeSize;
        }

        #endregion

        #region Rendering

        /// <summary>
        ///     Returns a default padding for the various themes for use
        ///     by measure and arrange.
        /// </summary>
        private Thickness DefaultPadding
        {
            get
            {
                Thickness padding = new Thickness(3.0); // The default padding
                Thickness? themePadding = ThemeDefaultPadding;
                if (themePadding == null)
                {
                    if (Orientation == Orientation.Vertical)
                    {
                        // Reserve space to the right for the arrow
                        padding.Right = 15.0;
                    }
                }
                else
                {
                    padding = (Thickness)themePadding;
                }

                // When pressed, offset the child
                if (IsPressed && IsClickable)
                {
                    padding.Left += 1.0;
                    padding.Top += 1.0;
                    padding.Right -= 1.0;
                    padding.Bottom -= 1.0;
                }

                return padding;
            }
        }

        /// <summary>
        ///     Called when this element should re-render.
        /// </summary>
        protected override void OnRender(DrawingContext dc)
        {
            if (UsingBorderImplementation)
            {
                // Revert to the Border implementation
                base.OnRender(dc);
            }
            else
            {
                RenderTheme(dc);
            }
        }

        private static double Max0(double d)
        {
            return Math.Max(0.0, d);
        }

        #endregion

        #region Freezable Cache

        /// <summary>
        ///     Creates a cache of frozen Freezable resources for use 
        ///     across all instances of the border.
        /// </summary>
        private static void EnsureCache(int size)
        {
            // Quick check to avoid locking
            if (_freezableCache == null) 
            {
                lock (_cacheAccess)
                {
                    // Re-check in case another thread created the cache
                    if (_freezableCache == null) 
                    {
                        _freezableCache = new List<Freezable>(size);
                        for (int i = 0; i < size; i++)
                        {
                            _freezableCache.Add(null);
                        }
                    }
                }
            }

            Debug.Assert(_freezableCache.Count == size, "The cache size does not match the requested amount.");
        }

        /// <summary>
        ///     Releases all resources in the cache.
        /// </summary>
        private static void ReleaseCache()
        {
            // Avoid locking if necessary
            if (_freezableCache != null) 
            {
                lock (_cacheAccess)
                {
                    // No need to re-check if non-null since it's OK to set it to null multiple times
                    _freezableCache = null;
                }
            }
        }

        /// <summary>
        ///     Retrieves a cached resource.
        /// </summary>
        private static Freezable GetCachedFreezable(int index)
        {
            lock (_cacheAccess)
            {
                Freezable freezable = _freezableCache[index];
                Debug.Assert((freezable == null) || freezable.IsFrozen, "Cached Freezables should have been frozen.");
                return freezable;
            }
        }

        /// <summary>
        ///     Caches a resources.
        /// </summary>
        private static void CacheFreezable(Freezable freezable, int index)
        {
            Debug.Assert(freezable.IsFrozen, "Cached Freezables should be frozen.");

            lock (_cacheAccess)
            {
                if (_freezableCache[index] != null)
                {
                    _freezableCache[index] = freezable;
                }
            }
        }

        private static List<Freezable> _freezableCache;
        private static object _cacheAccess = new object();

        #endregion
    }
}
