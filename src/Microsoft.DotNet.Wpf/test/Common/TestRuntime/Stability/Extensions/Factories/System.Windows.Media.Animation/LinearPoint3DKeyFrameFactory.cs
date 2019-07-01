// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class LinearPoint3DKeyFrameFactory : Point3DKeyFrameFactory<LinearPoint3DKeyFrame>
    {
        public override LinearPoint3DKeyFrame Create(DeterministicRandom random)
        {
            LinearPoint3DKeyFrame linearPoint3DKeyFrame = new LinearPoint3DKeyFrame();
            ApplyPoint3DKeyFrameProperties(linearPoint3DKeyFrame, random);

            return linearPoint3DKeyFrame;
        }
    }
}
