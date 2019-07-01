// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;
using System;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(Slider))]
    class SliderFactory : RangeBaseFactory<Slider>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 AutoToolTipPrecision { get; set; }

        public override Slider Create(DeterministicRandom random)
        {
            Slider slider = new Slider();
            ApplyRangeBaseProperties(slider, random);
            slider.Orientation = random.NextEnum<Orientation>();
            slider.AutoToolTipPlacement = random.NextEnum<AutoToolTipPlacement>();
            slider.AutoToolTipPrecision = AutoToolTipPrecision;
            slider.IsDirectionReversed = random.NextBool();
            slider.IsSelectionRangeEnabled = random.NextBool();
            slider.IsSnapToTickEnabled = random.NextBool();
            slider.TickPlacement = random.NextEnum<TickPlacement>();
            return slider;
        }
    }
}
