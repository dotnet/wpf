// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using Microsoft.Test.Stability.Core;
using System.Windows.Media.Media3D;
using Microsoft.Test.Stability.Extensions;
using System.Windows.Media;
using Microsoft.Test.Stability.Extensions.Factories;
using System.Collections.Generic;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class TranslateTransform3DFactory : DiscoverableFactory<TranslateTransform3D>
    {
        public Vector3D Offset { get; set; }

        public override TranslateTransform3D Create(DeterministicRandom random)
        {
            TranslateTransform3D translateTransform3D = new TranslateTransform3D(Offset);
            return translateTransform3D;
        }
    }

    class RotateTransform3DFactory : DiscoverableFactory<RotateTransform3D>
    {
        public Rotation3D Rotation3D { get; set; }
        public Point3D Center3D { get; set; }

        public override RotateTransform3D Create(DeterministicRandom random)
        {
            RotateTransform3D rotateTransform3D = new RotateTransform3D(Rotation3D, Center3D);
            return rotateTransform3D;
        }
    }

    class ScaleTransform3DFactory : DiscoverableFactory<ScaleTransform3D>
    {
        public Point3D Center { get; set; }
        public Vector3D Scale { get; set; }

        public override ScaleTransform3D Create(DeterministicRandom random)
        {
            return new ScaleTransform3D(Scale, Center);
        }
    }

    class MatrixTransform3DFactory : DiscoverableFactory<MatrixTransform3D>
    {
        public Matrix3D Matrix3D { get; set; }

        public override MatrixTransform3D Create(DeterministicRandom random)
        {
            return new MatrixTransform3D(Matrix3D);
        }
    }

    class Transform3DCollectionFactory : DiscoverableCollectionFactory<Transform3DCollection, Transform3D> { };

    class Transform3DGroupFactory : DiscoverableFactory<Transform3DGroup>
    {
        public Transform3DCollection Transform3DCollection { get; set; }

        public override Transform3DGroup Create(DeterministicRandom random)
        {
            Transform3DGroup transform3DGroup = new Transform3DGroup();
            transform3DGroup.Children = Transform3DCollection;
            return transform3DGroup;
        }
    }
}

