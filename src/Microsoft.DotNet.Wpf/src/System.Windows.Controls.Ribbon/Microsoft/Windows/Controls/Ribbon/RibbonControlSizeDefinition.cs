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

    using System.Diagnostics;
    using System.Windows;
    using MS.Internal;

    #endregion

    /// <summary>
    /// This class contains information about image size, visibility, and label
    /// visibility for a particular size configuration of a Ribbon control.
    /// </summary>
    public class RibbonControlSizeDefinition : Freezable
    {
        #region Public Properties

        public RibbonImageSize ImageSize
        {
            get { return (RibbonImageSize)GetValue(ImageSizeProperty); }
            set { SetValue(ImageSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImageSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageSizeProperty =
            DependencyProperty.Register("ImageSize", typeof(RibbonImageSize), typeof(RibbonControlSizeDefinition), new FrameworkPropertyMetadata(RibbonImageSize.Large));

        public bool IsLabelVisible
        {
            get { return (bool)GetValue(IsLabelVisibleProperty); }
            set { SetValue(IsLabelVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsLabelVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsLabelVisibleProperty =
            DependencyProperty.Register("IsLabelVisible", typeof(bool), typeof(RibbonControlSizeDefinition), new FrameworkPropertyMetadata(true));

        public bool IsCollapsed
        {
            get { return (bool)GetValue(IsCollapsedProperty); }
            set { SetValue(IsCollapsedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsCollapsed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCollapsedProperty =
            DependencyProperty.Register("IsCollapsed",
                typeof(bool),
                typeof(RibbonControlSizeDefinition),
                new FrameworkPropertyMetadata(false));

        public RibbonControlLength Width
        {
            get { return (RibbonControlLength)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Width.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register("Width",
                typeof(RibbonControlLength), 
                typeof(RibbonControlSizeDefinition),
                new FrameworkPropertyMetadata(RibbonControlLength.Auto),
                new ValidateValueCallback(ValidateWidth));

        public RibbonControlLength MinWidth
        {
            get { return (RibbonControlLength)GetValue(MinWidthProperty); }
            set { SetValue(MinWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MinWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinWidthProperty =
            DependencyProperty.Register("MinWidth",
            typeof(RibbonControlLength), 
            typeof(RibbonControlSizeDefinition),
            new FrameworkPropertyMetadata(new RibbonControlLength(0)),
            new ValidateValueCallback(ValidateMinWidth));

        public RibbonControlLength MaxWidth
        {
            get { return (RibbonControlLength)GetValue(MaxWidthProperty); }
            set { SetValue(MaxWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxWidthProperty =
            DependencyProperty.Register("MaxWidth",
            typeof(RibbonControlLength), 
            typeof(RibbonControlSizeDefinition),
            new FrameworkPropertyMetadata(new RibbonControlLength(double.PositiveInfinity)),
            new ValidateValueCallback(ValidateMaxWidth));

        #endregion

        #region Private Methods

        private static bool ValidateWidth(object width)
        {
            RibbonControlLength length = (RibbonControlLength)width;
            if (double.IsInfinity(length.Value) || DoubleUtil.LessThanOrClose(length.Value, 0))
            {
                return false;
            }
            return true;
        }

        private static bool ValidateMinWidth(object minWidth)
        {
            RibbonControlLength length = (RibbonControlLength)minWidth;
            if (length.IsAuto || length.IsStar || double.IsInfinity(length.Value) || DoubleUtil.LessThan(length.Value, 0))
            {
                return false;
            }
            return true;
        }

        private static bool ValidateMaxWidth(object maxWidth)
        {
            RibbonControlLength length = (RibbonControlLength)maxWidth;
            if (length.IsAuto || length.IsStar || DoubleUtil.LessThan(length.Value, 0))
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Freezable

        protected override Freezable CreateInstanceCore()
        {
            return new RibbonControlSizeDefinition();
        }

        #endregion

        #region Frozen Instances

        internal static RibbonControlSizeDefinition GetFrozenControlSizeDefinition(RibbonImageSize imageSize,
            bool isLabelVisible)
        {
            if (isLabelVisible)
            {
                switch (imageSize)
                {
                    case RibbonImageSize.Large:
                        return LargeImageWithLabel;
                    case RibbonImageSize.Small:
                        return SmallImageWithLabel;
                    default:
                        return NoImageWithLabel;
                }
            }
            else
            {
                switch (imageSize)
                {
                    case RibbonImageSize.Large:
                        return LargeImageWithoutLabel;
                    case RibbonImageSize.Small:
                        return SmallImageWithoutLabel;
                    default:
                        return NoImageWithoutLabel;
                }
            }
        }

        internal static RibbonControlSizeDefinition LargeImageWithLabel
        {
            get
            {
                if (_largeImageWithLabel == null)
                {
                    _largeImageWithLabel = new RibbonControlSizeDefinition();
                    _largeImageWithLabel.ImageSize = RibbonImageSize.Large;
                    _largeImageWithLabel.IsLabelVisible = true;
                    _largeImageWithLabel.Freeze();
                }
                return _largeImageWithLabel;
            }
        }

        internal static RibbonControlSizeDefinition SmallImageWithLabel
        {
            get
            {
                if (_smallImageWithLabel == null)
                {
                    _smallImageWithLabel = new RibbonControlSizeDefinition();
                    _smallImageWithLabel.ImageSize = RibbonImageSize.Small;
                    _smallImageWithLabel.IsLabelVisible = true;
                    _smallImageWithLabel.Freeze();
                }
                return _smallImageWithLabel;
            }
        }

        internal static RibbonControlSizeDefinition NoImageWithLabel
        {
            get
            {
                if (_noImageWithLabel == null)
                {
                    _noImageWithLabel = new RibbonControlSizeDefinition();
                    _noImageWithLabel.ImageSize = RibbonImageSize.Collapsed;
                    _noImageWithLabel.IsLabelVisible = true;
                    _noImageWithLabel.Freeze();
                }
                return _noImageWithLabel;
            }
        }

        internal static RibbonControlSizeDefinition LargeImageWithoutLabel
        {
            get
            {
                if (_largeImageWithoutLabel == null)
                {
                    _largeImageWithoutLabel = new RibbonControlSizeDefinition();
                    _largeImageWithoutLabel.ImageSize = RibbonImageSize.Large;
                    _largeImageWithoutLabel.IsLabelVisible = false;
                    _largeImageWithoutLabel.Freeze();
                }
                return _largeImageWithoutLabel;
            }
        }

        internal static RibbonControlSizeDefinition SmallImageWithoutLabel
        {
            get
            {
                if (_smallImageWithoutLabel == null)
                {
                    _smallImageWithoutLabel = new RibbonControlSizeDefinition();
                    _smallImageWithoutLabel.ImageSize = RibbonImageSize.Small;
                    _smallImageWithoutLabel.IsLabelVisible = false;
                    _smallImageWithoutLabel.Freeze();
                }
                return _smallImageWithoutLabel;
            }
        }

        internal static RibbonControlSizeDefinition NoImageWithoutLabel
        {
            get
            {
                if (_noImageWithoutLabel == null)
                {
                    _noImageWithoutLabel = new RibbonControlSizeDefinition();
                    _noImageWithoutLabel.ImageSize = RibbonImageSize.Collapsed;
                    _noImageWithoutLabel.IsLabelVisible = false;
                    _noImageWithoutLabel.Freeze();
                }
                return _noImageWithoutLabel;
            }
        }

        private static RibbonControlSizeDefinition _largeImageWithLabel;
        private static RibbonControlSizeDefinition _smallImageWithLabel;
        private static RibbonControlSizeDefinition _noImageWithLabel;
        private static RibbonControlSizeDefinition _largeImageWithoutLabel;
        private static RibbonControlSizeDefinition _smallImageWithoutLabel;
        private static RibbonControlSizeDefinition _noImageWithoutLabel;

        #endregion
    }
}
