// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class LinearByteKeyFrameFactory : ByteKeyFrameFactory<LinearByteKeyFrame>
    {
        #region Public Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new LinearByteKeyFrame</returns>
        public override LinearByteKeyFrame Create(DeterministicRandom random)
        {
            LinearByteKeyFrame linearByteKeyFrame = new LinearByteKeyFrame();
            ApplyByteKeyFrameProperties(linearByteKeyFrame, random);
            return linearByteKeyFrame;
        }

        #endregion
    }
}
