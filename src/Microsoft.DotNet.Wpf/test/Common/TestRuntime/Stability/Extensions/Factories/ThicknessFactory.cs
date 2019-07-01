// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class ThicknessFactory : DiscoverableFactory<Thickness>
    {
        public override Thickness Create(DeterministicRandom random)
        {
            return new Thickness(random.NextDouble() * 5, random.NextDouble() * 5,
                random.NextDouble() * 5, random.NextDouble() * 5);
        }
    }
}
