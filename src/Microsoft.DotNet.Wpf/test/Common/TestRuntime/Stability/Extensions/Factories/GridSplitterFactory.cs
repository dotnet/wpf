// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(GridSplitter))]
    class GridSplitterFactory : DiscoverableFactory<GridSplitter>
    {
        public Brush Background { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double Width { get; set; }

        public override GridSplitter Create(DeterministicRandom random)
        {
            GridSplitter gridSplitter = new GridSplitter();
            gridSplitter.Background = Background;
            gridSplitter.Width = Width;

            gridSplitter.VerticalAlignment = random.NextEnum<VerticalAlignment>();
            gridSplitter.HorizontalAlignment = random.NextEnum<HorizontalAlignment>();

            return gridSplitter;
        }
    }
}
