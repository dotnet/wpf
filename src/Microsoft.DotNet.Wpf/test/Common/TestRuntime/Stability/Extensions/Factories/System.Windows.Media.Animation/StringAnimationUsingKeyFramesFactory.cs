// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class StringAnimationUsingKeyFramesFactory : TimelineFactory<StringAnimationUsingKeyFrames>
    {
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public StringKeyFrameCollection KeyFrames { get; set; }

        public override StringAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            StringAnimationUsingKeyFrames stringAnimationUsingKeyFrames = new StringAnimationUsingKeyFrames();
            stringAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(stringAnimationUsingKeyFrames, random);

            return stringAnimationUsingKeyFrames;
        }
    }
}
