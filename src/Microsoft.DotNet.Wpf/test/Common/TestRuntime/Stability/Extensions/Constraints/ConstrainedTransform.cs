// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    public class ConstrainedTransform : ConstrainedDataSource
    {
        public Transform Transform { set; get; }

        public ConstrainedTransform() { }

        public override object GetData(DeterministicRandom r)
        {
            if (r.NextBool())
            {
                Transform = new RotateTransform(r.Next(4) * 90, r.NextDouble() * 10, r.NextDouble() * 10);
            }
            else
            {
                Transform = new ScaleTransform(r.NextDouble(), r.NextDouble(), r.NextDouble() * 10, r.NextDouble() * 10);
            }

            return Transform;
        }
    }
}
