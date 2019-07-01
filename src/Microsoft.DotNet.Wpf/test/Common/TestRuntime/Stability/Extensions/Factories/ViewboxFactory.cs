// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(Viewbox))]
    class ViewboxFactory : DecoratorFactory<Viewbox>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double Width { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double Height { get; set; }

        public override Viewbox Create(DeterministicRandom random)
        {
            Viewbox viewBox = new Viewbox();
            viewBox.Width = Width;
            viewBox.Height = Height;
            viewBox.Stretch = random.NextEnum<Stretch>();
            viewBox.StretchDirection = random.NextEnum<StretchDirection>();
            ApplyDecoratorProperties(viewBox, random);
            return viewBox;
        }
    }
}
