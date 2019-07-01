// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class BlurBitmapEffectFactory : DiscoverableFactory<BlurBitmapEffect>
    {
        public override BlurBitmapEffect Create(DeterministicRandom random)
        {
            BlurBitmapEffect blurBitmapEffect = new BlurBitmapEffect();
            blurBitmapEffect.Radius = random.NextDouble() * 40;
            blurBitmapEffect.KernelType = random.NextEnum<KernelType>();
            return blurBitmapEffect;
        }
    }

    class BevelBitmapEffectFactory : DiscoverableFactory<BevelBitmapEffect>
    {
        public override BevelBitmapEffect Create(DeterministicRandom random)
        {
            BevelBitmapEffect bevelBitmapEffect = new BevelBitmapEffect();
            bevelBitmapEffect.BevelWidth = random.NextDouble() * 40;
            bevelBitmapEffect.EdgeProfile = random.NextEnum<EdgeProfile>();
            bevelBitmapEffect.LightAngle = random.NextDouble() * 360;
            bevelBitmapEffect.Relief = random.NextDouble() * 40;
            bevelBitmapEffect.Smoothness = random.NextDouble() * 100;
            return bevelBitmapEffect;
        }
    }

    class DropShadowBitmapEffectFactory : DiscoverableFactory<DropShadowBitmapEffect>
    {
        public Color Color { get; set; }

        public override DropShadowBitmapEffect Create(DeterministicRandom random)
        {
            DropShadowBitmapEffect dropShadowBitmapEffect = new DropShadowBitmapEffect();
            dropShadowBitmapEffect.Color = Color;
            dropShadowBitmapEffect.Direction = random.NextDouble() * 50;
            dropShadowBitmapEffect.Noise = random.NextDouble() * 40;
            dropShadowBitmapEffect.Opacity = random.NextDouble();
            dropShadowBitmapEffect.ShadowDepth = random.NextDouble();
            dropShadowBitmapEffect.Softness = random.NextDouble();
            return dropShadowBitmapEffect;
        }
    }

    class EmbossBitmapEffectFactory : DiscoverableFactory<EmbossBitmapEffect>
    {
        public override EmbossBitmapEffect Create(DeterministicRandom random)
        {
            EmbossBitmapEffect embossBitmapEffect = new EmbossBitmapEffect();
            embossBitmapEffect.LightAngle = random.NextDouble() * 360;
            embossBitmapEffect.Relief = random.NextDouble() * 50;
            return embossBitmapEffect;
        }
    }

    class OuterGlowBitmapEffectFactory : DiscoverableFactory<OuterGlowBitmapEffect>
    {
        public Color Color { get; set; }

        public override OuterGlowBitmapEffect Create(DeterministicRandom random)
        {
            OuterGlowBitmapEffect outerGlowBitmapEffect = new OuterGlowBitmapEffect();
            outerGlowBitmapEffect.GlowColor = Color;
            outerGlowBitmapEffect.GlowSize = random.NextDouble() * 40;
            outerGlowBitmapEffect.Noise = random.NextDouble() * 40;
            outerGlowBitmapEffect.Opacity = random.NextDouble();
            return outerGlowBitmapEffect;
        }
    }

    class BitmapEffectGroupFactory : DiscoverableFactory<BitmapEffectGroup>
    {
        public BitmapEffectCollection BitmapEffectCollection { get; set; }

        public override BitmapEffectGroup Create(DeterministicRandom random)
        {
            BitmapEffectGroup bitmapEffectGroup = new BitmapEffectGroup();
            bitmapEffectGroup.Children = this.BitmapEffectCollection;
            return bitmapEffectGroup;
        }
    }

    class BitmapEffectCollectionFactory : DiscoverableCollectionFactory<BitmapEffectCollection, BitmapEffect> { }
}
