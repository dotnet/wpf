// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    #region Public Members

    internal class LinearInt16KeyFrameFactory : Int16KeyFrameFactory<LinearInt16KeyFrame>
    {
        public override LinearInt16KeyFrame Create(DeterministicRandom random)
        {
            LinearInt16KeyFrame linearInt16KeyFrame = new LinearInt16KeyFrame();
            ApplyInt16KeyFrameProperties(linearInt16KeyFrame, random);
            return linearInt16KeyFrame;
        }
    }

    #endregion
}
