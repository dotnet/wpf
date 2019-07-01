// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class GradientStopCollectionFactory : DiscoverableCollectionFactory<GradientStopCollection, GradientStop> { }

    class GradientStopFactory : DiscoverableFactory<GradientStop>
    {
        public Color Color { get; set; }

        public override GradientStop Create(DeterministicRandom random)
        {
            return new GradientStop(Color, random.NextDouble());
        }
    }
}
