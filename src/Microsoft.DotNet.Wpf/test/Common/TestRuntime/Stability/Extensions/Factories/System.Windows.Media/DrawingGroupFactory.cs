// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary/>
    internal class DrawingGroupFactory : DiscoverableFactory<DrawingGroup>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets DrawingGroup Clip property.
        /// </summary>
        public Geometry ClipGeometry { get; set; }

        public GuidelineSet GuidelineSet { get; set; }

        public Double Opacity { get; set; }

        public Brush OpacityMask { get; set; }

        public Transform Transform { get; set; }

        public DrawingCollection Children { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new DrawingGroup.</returns>
        public override DrawingGroup Create(DeterministicRandom random)
        {
            DrawingGroup group = new DrawingGroup();
            group.ClipGeometry = ClipGeometry;
            group.GuidelineSet = GuidelineSet;
            group.Opacity = Opacity;
            group.OpacityMask = OpacityMask;
            group.Transform = Transform;
            group.Children = Children;
            return group;
        }

        #endregion
    }
}
