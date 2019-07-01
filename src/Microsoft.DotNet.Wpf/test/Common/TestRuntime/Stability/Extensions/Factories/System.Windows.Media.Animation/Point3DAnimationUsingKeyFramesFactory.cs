// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class Point3DAnimationUsingKeyFramesFactory : TimelineFactory<Point3DAnimationUsingKeyFrames>
    {
        #region

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public Point3DKeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        public override Point3DAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            Point3DAnimationUsingKeyFrames point3DAnimationUsingKeyFrames = new Point3DAnimationUsingKeyFrames();
            point3DAnimationUsingKeyFrames.IsAdditive = IsAdditive;
            point3DAnimationUsingKeyFrames.IsCumulative = IsCumulative;
            point3DAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(point3DAnimationUsingKeyFrames, random);

            return point3DAnimationUsingKeyFrames;
        }

        #endregion
    }
}
