// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal abstract class Vector3DKeyFrameFactory<Vector3DKeyFrameType> : DiscoverableFactory<Vector3DKeyFrameType> where Vector3DKeyFrameType : Vector3DKeyFrame
    {
        #region Public Members

        public Vector3D Value { get; set; }

        public KeyTime KeyTime { get; set; }

        #endregion

        protected void ApplyVector3DKeyFrameProperties(Vector3DKeyFrame vector3DKeyFrame, DeterministicRandom random)
        {
            vector3DKeyFrame.Value = Value;
            vector3DKeyFrame.KeyTime = KeyTime;
        }
    }
}
