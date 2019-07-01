// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary/>
    internal class CombinedGeometryFactory : GeometryFactory<CombinedGeometry>
    {
        #region Public Members

        public Geometry Geometry1 { get; set; }

        public Geometry Geometry2 { get; set; }

        public GeometryCombineMode GeometryCombineMode { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new CombinedGeometry.</returns>
        public override CombinedGeometry Create(DeterministicRandom random)
        {
            CombinedGeometry geometry;

            if (UseDefaultConstructor)
            {
                geometry = new CombinedGeometry();
                geometry.GeometryCombineMode = GeometryCombineMode;
                geometry.Geometry1 = Geometry1;
                geometry.Geometry2 = Geometry2;
            }
            else
            {
                geometry = new CombinedGeometry(GeometryCombineMode, Geometry1, Geometry2);
            }

            ApplyTransform(geometry);
            return geometry;
        }

        #endregion
    }
}
