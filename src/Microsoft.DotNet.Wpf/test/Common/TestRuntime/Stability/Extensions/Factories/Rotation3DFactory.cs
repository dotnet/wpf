// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Stability.Extensions.Factories
{

    class AxisAngleRotation3DFactory : DiscoverableFactory<AxisAngleRotation3D>
    {
        public Vector3D Axis { get; set; }

        public override AxisAngleRotation3D Create(DeterministicRandom random)
        {
            return new AxisAngleRotation3D(Axis, random.NextDouble());
        }
    }

    class QuaternionRotation3DFactory : DiscoverableFactory<QuaternionRotation3D>
    {
        public Quaternion Quaternion { get; set; }

        public override QuaternionRotation3D Create(DeterministicRandom random)
        {
            return new QuaternionRotation3D(Quaternion);
        }
    }

    class QuaternionFactory : DiscoverableFactory<Quaternion>
    {
        public Vector3D Axis { get; set; }

        public override Quaternion Create(DeterministicRandom random)
        {
            // Quaternion..ctor throws exception if Axis.Length == 0. Trying to avoid that.
            if (Axis.Length == 0)
            {
                Vector3D temp = Axis;
                temp.X += 1;
                Axis = temp;
                Axis.Normalize();
            }
            return new Quaternion(Axis,random.NextDouble() * 180);
        }
    }
}
