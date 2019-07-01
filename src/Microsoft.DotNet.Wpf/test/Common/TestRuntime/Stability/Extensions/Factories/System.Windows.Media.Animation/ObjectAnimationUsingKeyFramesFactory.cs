// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class ObjectAnimationUsingKeyFramesFactory : TimelineFactory<ObjectAnimationUsingKeyFrames>
    {
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public ObjectKeyFrameCollection KeyFrames { get; set; }

        public override ObjectAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            ObjectAnimationUsingKeyFrames objectAnimationUsingKeyFrames = new ObjectAnimationUsingKeyFrames();
            objectAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(objectAnimationUsingKeyFrames, random);

            return objectAnimationUsingKeyFrames;
        }
    }
}
