// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media.Media3D;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class Matrix3DFactory : DiscoverableFactory<Matrix3D>
    {

        //TODO: this is an ugly matrix, which will produce off-screen results. We can do better.
        public override Matrix3D Create(DeterministicRandom random)
        {
            Matrix3D matrix3D = new Matrix3D(random.NextDouble() * 10,
                random.NextDouble() * 10,
                random.NextDouble() * 10,
                random.NextDouble() * 10,
                random.NextDouble() * 10,
                random.NextDouble() * 10,
                random.NextDouble() * 10,
                random.NextDouble() * 10,
                random.NextDouble() * 10,
                random.NextDouble() * 10,
                random.NextDouble() * 10,
                random.NextDouble() * 10,
                random.NextDouble() * 100,
                random.NextDouble() * 100,
                random.NextDouble() * 100,
                random.NextDouble() * 10);
            return matrix3D;
        }
    }
}
