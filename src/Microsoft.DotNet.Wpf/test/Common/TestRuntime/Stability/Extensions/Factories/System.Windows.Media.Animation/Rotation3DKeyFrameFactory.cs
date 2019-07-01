// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using Microsoft.Test.Stability.Core; 

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal abstract class Rotation3DKeyFrameFactory<Rotation3DKeyFrameType> : DiscoverableFactory<Rotation3DKeyFrameType> where Rotation3DKeyFrameType : Rotation3DKeyFrame
    {
        #region Public Members

        public Rotation3D Value { get; set; }

        public KeyTime KeyTime { get; set; }

        #endregion

        protected void ApplyRotation3DKeyFrameProperties(Rotation3DKeyFrame rotation3DKeyFrame, DeterministicRandom random)
        {
            rotation3DKeyFrame.Value = Value;
            rotation3DKeyFrame.KeyTime = KeyTime;
        }
    }
}
