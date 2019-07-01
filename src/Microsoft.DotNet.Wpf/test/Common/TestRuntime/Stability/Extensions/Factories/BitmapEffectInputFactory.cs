// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using Microsoft.Test.Stability.Core;
using System.Windows.Media.Effects;
using System.Windows.Media;
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class BitmapEffectInputFactory : DiscoverableFactory<BitmapEffectInput>
    {
        public Rect AreaToApplyEffect { get; set; }

        public override BitmapEffectInput Create(DeterministicRandom random)
        {
            BitmapEffectInput effect = new BitmapEffectInput();
            effect.AreaToApplyEffect = AreaToApplyEffect;
            effect.AreaToApplyEffectUnits = random.NextEnum<BrushMappingMode>();
            effect.Input = BitmapEffectInput.ContextInputSource;
            return effect;
        }
    }
}
