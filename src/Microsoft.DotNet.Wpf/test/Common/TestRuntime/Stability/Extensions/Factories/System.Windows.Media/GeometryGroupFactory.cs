// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary/>
    internal class GeometryGroupFactory : GeometryFactory<GeometryGroup>
    {
        #region Public Members

        public GeometryCollection GeometryCollection { get; set; }

        public FillRule FillRule { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new GeometryGroup</returns>
        public override GeometryGroup Create(DeterministicRandom random)
        {
            GeometryGroup group = new GeometryGroup();
            group.FillRule = FillRule;
            group.Children = GeometryCollection;

            ApplyTransform(group);

            return group;
        }

        #endregion
    }
}
