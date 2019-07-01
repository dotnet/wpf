// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;


namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(ColorContext))]
    class ColorContextFactory : DiscoverableFactory<ColorContext>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public PixelFormat PixelFormat { get; set; }

        public override ColorContext Create(DeterministicRandom random)
        {
            ColorContext colorContext = new ColorContext(PixelFormat);
            return colorContext;
        }
    }
}
