// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class PointFactory : DiscoverableFactory<Point>
    {
        public override Point Create(DeterministicRandom random)
        {
            return new Point(random.NextDouble() * 100, random.NextDouble() * 100);
        }
    }

    class PointCollectionFactory : DiscoverableCollectionFactory<PointCollection, Point> 
    {
        [InputAttribute(ContentInputSource.CreateFromFactory, MinListSize=1)]
        public override List<Point> ContentList { get; set; }
    }

    class RectFactory : DiscoverableFactory<Rect>
    {
        public override Rect Create(DeterministicRandom random)
        {
            return new Rect(random.NextDouble() * 100, random.NextDouble() * 100, random.NextDouble() * 100, random.NextDouble() * 100);
        }
    }

    class SizeFactory : DiscoverableFactory<Size>
    {
        public override Size Create(DeterministicRandom random)
        {
            return new Size(random.NextDouble() * 100, random.NextDouble() * 100);
        }
    }

    class MatrixFactory : DiscoverableFactory<Matrix>
    {
        public override Matrix Create(DeterministicRandom random)
        {
            return new Matrix(random.NextDouble() * 100, random.NextDouble() * 100, random.NextDouble() * 100, random.NextDouble() * 100, random.NextDouble() * 100, random.NextDouble() * 100);
        }
    }

    //HACK: We cannot control collection size in a consistent way yet. This is an issue for 3D content...
    class Int32CollectionFactory : DiscoverableFactory<Int32Collection>
    {
        public override Int32Collection Create(DeterministicRandom random)
        {
            Int32Collection col = new Int32Collection();
            for (int i = 0; i < random.Next(20); i++)
            {
                col.Add(random.Next(100));
            }
            return col;
        }
    }

}
