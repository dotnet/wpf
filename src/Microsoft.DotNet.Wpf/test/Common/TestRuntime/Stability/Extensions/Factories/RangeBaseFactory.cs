// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(RangeBase))]
    abstract class RangeBaseFactory<T> : DiscoverableFactory<T> where T : RangeBase
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double Value { get; set; }

        protected void ApplyRangeBaseProperties(RangeBase rangeBase, DeterministicRandom random)
        {
            rangeBase.Value = Value;
            rangeBase.Minimum = random.NextDouble();
            rangeBase.Maximum = random.NextDouble() * 10;
        }
    }
}
