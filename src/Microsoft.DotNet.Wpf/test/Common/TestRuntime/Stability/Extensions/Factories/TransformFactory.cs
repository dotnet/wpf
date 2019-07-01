// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class MatrixTransformFactory : DiscoverableFactory<MatrixTransform>
    {
        public Matrix Matrix { get; set; }
        public override MatrixTransform Create(DeterministicRandom random)
        {
            return new MatrixTransform(Matrix);
        }
    }

    class RotateTransformFactory : DiscoverableFactory<RotateTransform>
    {
        public override RotateTransform Create(DeterministicRandom random)
        {
            RotateTransform rotateTransform = new RotateTransform();
            rotateTransform.Angle = random.NextDouble() * 360;
            rotateTransform.CenterX = random.NextDouble() * 10;
            rotateTransform.CenterY = random.NextDouble() * 10;
            return rotateTransform;
        }
    }

    class ScaleTransformFactory : DiscoverableFactory<ScaleTransform>
    {
        public override ScaleTransform Create(DeterministicRandom random)
        {
            ScaleTransform scaleTransform = new ScaleTransform();
            scaleTransform.ScaleX = random.NextDouble();
            scaleTransform.ScaleY = random.NextDouble();
            scaleTransform.CenterX = random.NextDouble() * 10;
            scaleTransform.CenterY = random.NextDouble() * 10;
            return scaleTransform;
        }
    }

    class SkewTransformFactory : DiscoverableFactory<SkewTransform>
    {
        public override SkewTransform Create(DeterministicRandom random)
        {
            SkewTransform skewTransform = new SkewTransform();
            skewTransform.AngleX = random.NextDouble() * 30;
            skewTransform.AngleY = random.NextDouble() * 30;
            skewTransform.CenterX = random.NextDouble() * 50;
            skewTransform.CenterY = random.NextDouble() * 50;
            return skewTransform;
        }
    }

    class TranslateTransformFactory : DiscoverableFactory<TranslateTransform>
    {
        public override TranslateTransform Create(DeterministicRandom random)
        {
            TranslateTransform translateTransform = new TranslateTransform(random.NextDouble() * 10, random.NextDouble() * 10);
            return translateTransform;
        }
    }

    class TransformGroupFactory : DiscoverableFactory<TransformGroup>
    {
        public TransformCollection TransformCollection { get; set; }

        public override TransformGroup Create(DeterministicRandom random)
        {
            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children = TransformCollection;
            return transformGroup;
        }
    }

    class TransformCollectionFactory : DiscoverableCollectionFactory<TransformCollection, Transform> { }
}
