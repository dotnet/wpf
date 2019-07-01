// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary/>
    internal class StreamGeometryFactory : GeometryFactory<StreamGeometry>
    {
        #region Public Members

        public FillRule FillRule { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new StreamGeometry.</returns>
        public override StreamGeometry Create(DeterministicRandom random)
        {
            StreamGeometry geometry = new StreamGeometry();
            geometry.FillRule = FillRule;

            ApplyTransform(geometry);
            
            return geometry;
        }

        #endregion
    }
}
