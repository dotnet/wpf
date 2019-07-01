// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class LinearQuaternionKeyFrameFactory : QuaternionKeyFrameFactory<LinearQuaternionKeyFrame>
    {
        public bool UseShortestPath { get; set; }

        public override LinearQuaternionKeyFrame Create(DeterministicRandom random)
        {
            LinearQuaternionKeyFrame linearQuaternionKeyFrame = new LinearQuaternionKeyFrame();
            linearQuaternionKeyFrame.UseShortestPath = UseShortestPath;
            ApplyQuaternionKeyFrameProperties(linearQuaternionKeyFrame, random);

            return linearQuaternionKeyFrame;
        }
    }
}
