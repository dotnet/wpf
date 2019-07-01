// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class DoubleFactory : DiscoverableFactory<Double>
    {
        public override Double Create(DeterministicRandom random)
        {
            return random.NextDouble();
        }
    }

    class DoubleCollectionFactory : DiscoverableCollectionFactory<DoubleCollection, Double> { }
}
