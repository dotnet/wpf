// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    public class ConstrainedInt32 : ConstrainedDataSource
    {
        public ConstrainedInt32() { }

        public Int32 Min { get; set; }
        public Int32 Max { get; set; }

        public override object GetData(DeterministicRandom r)
        {
            Int32 range = Max - Min;
            return Min + (Int32)(range * r.NextDouble());
        }
    }
}
