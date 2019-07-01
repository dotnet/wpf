// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using Microsoft.Test.Stability.Core;
using System.Collections.Generic;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class Viewport3DFactory : DiscoverableFactory<Viewport3D>
    {
        public Camera Camera { get; set; }
        //HACK:Visual3DCollection has an internal only constructor!!!
        public List<Visual3D> Children { get; set; }

        public override Viewport3D Create(DeterministicRandom random)
        {
            Viewport3D viewport3D = new Viewport3D();
            viewport3D.Camera = Camera;
            HomelessTestHelpers.Merge(viewport3D.Children, Children);
            return viewport3D;
        }
    }
}
