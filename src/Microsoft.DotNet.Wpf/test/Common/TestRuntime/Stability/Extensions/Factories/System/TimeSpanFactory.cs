// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    public class TimeSpanFactory : DiscoverableFactory<TimeSpan>
    {
        public override TimeSpan Create(DeterministicRandom random)
        {
            int ConstructorType = random.Next(10);
            //It will return a TimeSpan less than 2 minutes.
            switch (ConstructorType % 10)
            {
                case 0:
                    return TimeSpan.FromDays(random.NextDouble() * 0.001);
                case 1:
                    return TimeSpan.FromHours(random.NextDouble() * 0.02);
                case 2:
                    return TimeSpan.FromMilliseconds(random.NextDouble() * 100000);
                case 3:
                    return TimeSpan.FromMinutes(random.NextDouble());
                case 4:
                    return TimeSpan.FromSeconds(random.NextDouble() * 100);
                case 5:
                    return TimeSpan.FromTicks(random.Next(100000) + 1);
                case 6:
                    return new TimeSpan(random.Next(100000) + 1);
                case 7:
                    return new TimeSpan(0, random.Next(1), random.Next(60) + 1);
                case 8:
                    return new TimeSpan(0, 0, random.Next(1), random.Next(60) + 1);
                case 9:
                    return new TimeSpan(0, 0, random.Next(1), random.Next(30), random.Next(30000) + 1);
                default:
                    return new TimeSpan();
            }
        }
    }
}
