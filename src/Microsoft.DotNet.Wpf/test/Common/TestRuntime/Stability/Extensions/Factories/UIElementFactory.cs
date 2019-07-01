// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class UIElementFactory : DiscoverableFactory<UIElement>
    {
        #region Public Members

        public Effect Effect { get; set; }
        public Geometry Geometry { get; set; }
        public Vector Vector { get; set; }
        public Brush Brush { get; set; }
        public Size Size { get; set; }
        public Transform Transform { get; set; }
        public Point Point { get; set; }
        public DoubleCollection DoubleCollection { get; set; }

#if TESTBUILD_CLR40
        public BitmapCache BitmapCache { get; set; }
#endif

        #endregion

        #region Override Members

        public override UIElement Create(DeterministicRandom random)
        {
            UIElement uIElement = new UIElement();

            uIElement.AllowDrop = random.NextBool();
            uIElement.ClipToBounds = random.NextBool();
            uIElement.Focusable = random.NextBool();
            uIElement.IsEnabled = random.NextBool();
            uIElement.IsHitTestVisible = random.NextBool();
#if TESTBUILD_CLR40
            uIElement.IsManipulationEnabled = random.NextBool();
#endif
            uIElement.Opacity = random.NextDouble();
            uIElement.Visibility = random.NextEnum<Visibility>();
            uIElement.Effect = Effect;
            uIElement.Clip = Geometry;
            uIElement.Opacity = random.NextDouble();
            uIElement.OpacityMask = Brush;
            uIElement.RenderSize = Size;
            uIElement.RenderTransform = Transform;
            uIElement.RenderTransformOrigin = Point;
            uIElement.SnapsToDevicePixels = random.NextBool();

#if TESTBUILD_CLR40
            uIElement.CacheMode = BitmapCache;
#endif

            return uIElement;
        }

        #endregion
    }
}
