// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class LinearVector3DKeyFrameFactory : Vector3DKeyFrameFactory<LinearVector3DKeyFrame>
    {
        public override LinearVector3DKeyFrame Create(DeterministicRandom random)
        {
            LinearVector3DKeyFrame linearVector3DKeyFrame = new LinearVector3DKeyFrame();
            ApplyVector3DKeyFrameProperties(linearVector3DKeyFrame, random);

            return linearVector3DKeyFrame;
        }
    }
}
