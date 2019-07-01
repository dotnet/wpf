// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class KeyTimeFactory : DiscoverableFactory<KeyTime>
    {
        public override KeyTime Create(DeterministicRandom random)
        {
            KeyTime keyTime;
            if (random.NextBool())
            {
                keyTime = KeyTime.FromPercent(random.NextDouble());
            }
            else
            {
                keyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(random.Next(60)));
            }
            return keyTime;
        }
    }
}
