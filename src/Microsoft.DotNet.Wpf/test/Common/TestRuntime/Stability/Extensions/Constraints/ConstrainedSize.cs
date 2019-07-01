// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using Microsoft.Test.Stability.Core;
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    public class ConstrainedSize : ConstrainedDataSource
    {
        public ConstrainedSize() { }

        public double MinWidth { get; set; }
        public double MaxWidth { get; set; }
        public double MinHeight { get; set; }
        public double MaxHeight { get; set; }

        public override object GetData(DeterministicRandom r)
        {
            double rangeWidth = MaxWidth - MinWidth;
            double rangeHeight = MaxHeight - MinHeight;
            return new Size(MinWidth + rangeWidth * r.NextDouble(), MinHeight + rangeHeight * r.NextDouble());
        }
    }
}
