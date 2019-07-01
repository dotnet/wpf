// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class BorderFactory : DiscoverableFactory<Border>
    {
        public Brush Background { get; set; }
        public Brush BorderBrush { get; set; }
        public Thickness BorderThickness { get; set; }
        public Thickness Padding { get; set; }
        public CornerRadius CornerRadius { get; set; }

        public override Border Create(DeterministicRandom random)
        {
            Border border = new Border();
            border.Background = Background;
            border.BorderBrush = BorderBrush;
            border.BorderThickness = BorderThickness;
            border.CornerRadius = CornerRadius;
            border.Padding = Padding;
            return border;
        }
    }
}
