// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class LinearRotation3DKeyFrameFactory : Rotation3DKeyFrameFactory<LinearRotation3DKeyFrame>
    {
        public override LinearRotation3DKeyFrame Create(DeterministicRandom random)
        {
            LinearRotation3DKeyFrame linearRotation3DKeyFrame = new LinearRotation3DKeyFrame();
            ApplyRotation3DKeyFrameProperties(linearRotation3DKeyFrame, random);

            return linearRotation3DKeyFrame;
        }
    }
}
