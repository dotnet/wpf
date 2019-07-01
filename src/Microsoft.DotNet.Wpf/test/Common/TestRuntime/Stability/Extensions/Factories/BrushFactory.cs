// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(Brush))]
    internal abstract class BrushFactory<T> : DiscoverableFactory<T> where T : Brush
    {
        public Transform RelativeTransform { get; set; }
        public Transform AbsoluteTransform { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double Opacity { get; set; }

        protected void ApplyBrushProperties(Brush brush, DeterministicRandom random)
        {
            brush.Opacity = Opacity;
            //Work around bug 891739.
            if (AbsoluteTransform != null)
            {
                brush.Transform = AbsoluteTransform;
            }
            brush.RelativeTransform = RelativeTransform;
        }
    }
}
