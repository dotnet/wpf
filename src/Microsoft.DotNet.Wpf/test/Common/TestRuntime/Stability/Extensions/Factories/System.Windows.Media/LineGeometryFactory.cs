// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary/>
    internal class LineGeometryFactory : GeometryFactory<LineGeometry>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets LineGeometry StartPoint
        /// </summary>
        public Point StartPoint { get; set; }

        /// <summary>
        /// Gets or sets LineGeometry EndPoint
        /// </summary>
        public Point EndPoint { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new LineGeometry.</returns>
        public override LineGeometry Create(DeterministicRandom random)
        {
            LineGeometry geometry;

            if (UseDefaultConstructor)
            {
                geometry = new LineGeometry();
                geometry.StartPoint = StartPoint;
                geometry.EndPoint = EndPoint;
            }
            else
            {
                geometry = new LineGeometry(StartPoint, EndPoint);
            }

            ApplyTransform(geometry);

            return geometry;
        }

        #endregion
    }
}
