// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using Microsoft.Test.Stability.Core;
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    public class ConstrainedPoint : ConstrainedDataSource
    {
        public ConstrainedPoint() { }

        public double MinX { get; set; }
        public double MaxX { get; set; }
        public double MinY { get; set; }
        public double MaxY { get; set; }

        public override object GetData(DeterministicRandom r)
        {
            double rangeX = MaxX - MinX;
            double rangeY = MaxY - MinY;
            return new Point(MinX + rangeX * r.NextDouble(), MinY + rangeY * r.NextDouble());
        }
    }
}
