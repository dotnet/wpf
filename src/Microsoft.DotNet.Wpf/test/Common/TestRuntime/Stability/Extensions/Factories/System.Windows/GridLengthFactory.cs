// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class GridLengthFactory : DiscoverableFactory<GridLength>
    {
        public override GridLength Create(DeterministicRandom random)
        {
            switch (random.Next(3))
            {
                case 0:
                    return new GridLength();
                case 1:
                    return new GridLength(random.NextDouble() * 100);
                case 2:
                    return new GridLength(random.NextDouble() * 100, random.NextEnum<GridUnitType>());
                default:
                    return new GridLength();
            }
        }
    }
}
