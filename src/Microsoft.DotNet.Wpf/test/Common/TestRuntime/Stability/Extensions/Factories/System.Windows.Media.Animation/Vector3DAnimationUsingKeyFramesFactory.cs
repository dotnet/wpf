// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class Vector3DAnimationUsingKeyFramesFactory : TimelineFactory<Vector3DAnimationUsingKeyFrames>
    {
        #region Public Members

        public bool IsAdditive { get; set; }

        public bool IsCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public Vector3DKeyFrameCollection KeyFrames { get; set; }

        #endregion

        #region Override Members

        public override Vector3DAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            Vector3DAnimationUsingKeyFrames vector3DAnimationUsingKeyFrames = new Vector3DAnimationUsingKeyFrames();
            vector3DAnimationUsingKeyFrames.IsAdditive = IsAdditive;
            vector3DAnimationUsingKeyFrames.IsCumulative = IsCumulative;
            vector3DAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(vector3DAnimationUsingKeyFrames, random);

            return vector3DAnimationUsingKeyFrames;
        }

        #endregion
    }
}
