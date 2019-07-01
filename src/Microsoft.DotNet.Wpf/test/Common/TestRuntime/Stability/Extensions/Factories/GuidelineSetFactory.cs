// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using Microsoft.Test.Stability.Core;
using System.Windows.Media;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class GuidelineSetFactory : DiscoverableFactory<GuidelineSet>
    {
        #region Methods
        public override bool CanCreate(Type desiredType)
        {
            return desiredType == typeof(GuidelineSet);
        }

        public override GuidelineSet Create(DeterministicRandom random)
        {
            return new GuidelineSet(CreateAxisLines(random), CreateAxisLines(random));
        }

        Double[] CreateAxisLines(DeterministicRandom random)
        {
            int size = random.Next(10);
            Double[] axisline = new Double[size];
            for (int i = 0; i < size; i++)
            {
                axisline[i] = random.NextDouble() * 10;
            }
            return axisline;
        }
        #endregion
    }
}
