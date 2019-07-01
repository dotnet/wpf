// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class PointAnimationUsingPathFactory : TimelineFactory<PointAnimationUsingPath>
    {
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public PathGeometry AnimationPath { get; set; }

        public override PointAnimationUsingPath Create(DeterministicRandom random)
        {
            PointAnimationUsingPath pointAnimationUsingPath = new PointAnimationUsingPath();

            pointAnimationUsingPath.PathGeometry = AnimationPath;

            ApplyTimelineProperties(pointAnimationUsingPath, random);

            return pointAnimationUsingPath;
        }

    }
}
