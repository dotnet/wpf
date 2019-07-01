// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    public class ConstrainedDouble : ConstrainedDataSource
    {
        public ConstrainedDouble() { }

        public double Min { get; set; }
        public double Max { get; set; }

        public override object GetData(DeterministicRandom r)
        {
            double range = Max - Min;
            return Min + range * r.NextDouble();
        }
    }
}
