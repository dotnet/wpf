// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class LinearInt64KeyFrameFactory : Int64KeyFrameFactory<LinearInt64KeyFrame>
    {
        public override LinearInt64KeyFrame Create(DeterministicRandom random)
        {
            LinearInt64KeyFrame linearInt64KeyFrame = new LinearInt64KeyFrame();
            ApplyInt64KeyFrameProperties(linearInt64KeyFrame, random);
            return linearInt64KeyFrame;
        }
    }
}
