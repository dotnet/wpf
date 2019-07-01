// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media.Media3D;
using Microsoft.Test.Stability.Core;
using System.Collections.Generic;
using System.Windows.Media;

namespace Microsoft.Test.Stability.Extensions.Factories
{

    class Point3DFactory : DiscoverableFactory<Point3D>
    {
        public override Point3D Create(DeterministicRandom random)
        {
            return new Point3D(random.NextDouble() * 10,
                random.NextDouble() * 10,
                random.NextDouble() * 10);
        }
    }

    class Vector3DFactory : DiscoverableFactory<Vector3D>
    {
        public override Vector3D Create(DeterministicRandom random)
        {
            return new Vector3D(random.NextDouble() * 10,
                random.NextDouble() * 10,
                random.NextDouble() * 10);
        }
    }

    //HACK: We cannot control collection size in a consistent way yet. This is an issue for 3D content...
    class Vector3DCollectionFactory : DiscoverableCollectionFactory<Vector3DCollection, Vector3D> { };

    //HACK: We cannot control collection size in a consistent way yet. This is an issue for 3D content
    class Point3DCollectionFactory : DiscoverableCollectionFactory<Point3DCollection, Point3D> { }
}
