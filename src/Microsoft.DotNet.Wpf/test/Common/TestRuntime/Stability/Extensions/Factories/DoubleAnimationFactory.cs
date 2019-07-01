// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// DoubleAnimation factory. Creates a new DoubleAnimation timeline with random duration. 
    /// </summary>
    [TargetTypeAttribute(typeof(DoubleAnimation))]
    class DoubleAnimationFactory : DiscoverableFactory<DoubleAnimation>
    {
        public override DoubleAnimation Create(DeterministicRandom random)
        {
            DoubleAnimation doubleAnimation = new DoubleAnimation();            
            double r = (double)random.NextDouble();
            doubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(r * 10));            
            return doubleAnimation;
        }
    }
}
