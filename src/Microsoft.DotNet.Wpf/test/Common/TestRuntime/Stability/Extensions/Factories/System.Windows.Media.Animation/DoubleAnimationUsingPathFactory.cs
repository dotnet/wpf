// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary/>
    internal class DoubleAnimationUsingPathFactory : TimelineFactory<DoubleAnimationUsingPath>
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public PathGeometry AnimationPath { get; set; }

        public PathAnimationSource PathAnimationSource { get; set; }

        #endregion

        #region Override Members

        public override DoubleAnimationUsingPath Create(DeterministicRandom random)
        {
            DoubleAnimationUsingPath doubleAnimationUsingPath = new DoubleAnimationUsingPath();
            doubleAnimationUsingPath.PathGeometry = AnimationPath;
            doubleAnimationUsingPath.Source = PathAnimationSource;
            ApplyTimelineProperties(doubleAnimationUsingPath, random);
            return doubleAnimationUsingPath;
        }

        #endregion
    }
}
