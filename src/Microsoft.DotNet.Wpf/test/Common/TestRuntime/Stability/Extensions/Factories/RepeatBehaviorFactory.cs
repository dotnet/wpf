// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class RepeatBehaviorFactory : DiscoverableFactory<RepeatBehavior>
    {
        public override RepeatBehavior Create(DeterministicRandom random)
        {
            RepeatBehavior repeatBehavior;
            int rdm = random.Next(3);
            if (rdm == 0)
            {
                repeatBehavior = RepeatBehavior.Forever;

            }
            else if (rdm == 1)
            {
                repeatBehavior = new RepeatBehavior(random.NextDouble() * 10);

            }
            else
            {
                repeatBehavior = new RepeatBehavior(TimeSpan.FromSeconds(random.NextDouble() * 10));
            }
            return repeatBehavior;
        }
    }
}
