// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Constraints
{
    public class ConstrainedPageSize : ConstrainedDataSource
    {
        public ConstrainedPageSize() { }

        public override object GetData(DeterministicRandom r)
        {
            return new Size(r.NextDouble() * 1000 + 200, r.NextDouble() * 1000 + 300);
        }
    }
}
