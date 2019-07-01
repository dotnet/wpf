// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal abstract class QuaternionKeyFrameFactory<QuaternionKeyFrameType> : DiscoverableFactory<QuaternionKeyFrameType> where QuaternionKeyFrameType : QuaternionKeyFrame
    {
        #region Public Members

        public Quaternion Value { get; set; }

        public KeyTime KeyTime { get; set; }

        #endregion

        protected void ApplyQuaternionKeyFrameProperties(QuaternionKeyFrame quaternionKeyFrame, DeterministicRandom random)
        {
            quaternionKeyFrame.Value = Value;
            quaternionKeyFrame.KeyTime = KeyTime;
        }
    }
}
