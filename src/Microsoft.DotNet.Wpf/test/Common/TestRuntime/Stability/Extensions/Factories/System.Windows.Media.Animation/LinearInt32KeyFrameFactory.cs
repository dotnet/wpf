// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class LinearInt32KeyFrameFactory : Int32KeyFrameFactory<LinearInt32KeyFrame>
    {
        #region Override Members

        public override LinearInt32KeyFrame Create(DeterministicRandom random)
        {
            LinearInt32KeyFrame linearInt32KeyFrame = new LinearInt32KeyFrame();
            ApplyInt32KeyFrameProperties(linearInt32KeyFrame, random);
            return linearInt32KeyFrame;
        }

        #endregion
    }
}
