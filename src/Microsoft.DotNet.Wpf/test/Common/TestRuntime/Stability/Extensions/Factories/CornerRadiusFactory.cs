// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class CornerRadiusFactory : DiscoverableFactory<CornerRadius>
    {
        public override CornerRadius Create(DeterministicRandom random)
        {
            return new CornerRadius(random.NextDouble() * 100, random.NextDouble() * 100, random.NextDouble() * 100, random.NextDouble() * 100);
        }
    }
}
