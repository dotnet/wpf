// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using Microsoft.Test.Stability.Core;
using System.Windows.Media.Effects;
using System.Windows.Media;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(BlurEffect))]
    class BlurEffectFactory : DiscoverableFactory<BlurEffect>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double Radius { get; set; }

        public override BlurEffect Create(DeterministicRandom random)
        {
            BlurEffect blur = new BlurEffect();
            blur.Radius = Radius;
            blur.RenderingBias = random.NextEnum<RenderingBias>();
            blur.KernelType = random.NextEnum<KernelType>();
            return blur;
        }
    }

    [TargetTypeAttribute(typeof(DropShadowEffect))]
    class DropShadowEffectFactory : DiscoverableFactory<DropShadowEffect>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double BlurRadius { get; set; }
        public Color Color { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double Direction { get; set; }

        public override DropShadowEffect Create(DeterministicRandom random)
        {
            DropShadowEffect dropShadow = new DropShadowEffect();
            dropShadow.BlurRadius = BlurRadius;
            dropShadow.Color = Color;
            dropShadow.Direction = Direction;
            return dropShadow;
        }
    }
}
