// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary/>
    internal class PathGeometryFactory : GeometryFactory<PathGeometry>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets PathFigure Figures property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public PathFigureCollection PathFigureCollection { get; set; }

        public FillRule FillRule { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new PathGeometry.</returns>
        public override PathGeometry Create(DeterministicRandom random)
        {
            PathGeometry geometry;

            if (UseDefaultConstructor)
            {
                geometry = new PathGeometry();
                geometry.Figures = PathFigureCollection;
            }
            else
            {
                geometry = new PathGeometry(PathFigureCollection);
            }

            geometry.FillRule = FillRule;
            ApplyTransform(geometry);

            return geometry;
        }

        #endregion
    }
}
