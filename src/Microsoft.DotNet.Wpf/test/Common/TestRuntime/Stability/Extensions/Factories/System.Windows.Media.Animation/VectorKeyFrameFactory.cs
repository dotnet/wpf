// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal abstract class VectorKeyFrameFactory<VectorKeyFrameType> : DiscoverableFactory<VectorKeyFrameType> where VectorKeyFrameType : VectorKeyFrame
    {
        #region Public Members

        public Vector Value { get; set; }

        public KeyTime KeyTime { get; set; }

        #endregion

        protected void ApplyVectorKeyFrameProperties(VectorKeyFrame vectorKeyFrame, DeterministicRandom random)
        {
            vectorKeyFrame.Value = Value;
            vectorKeyFrame.KeyTime = KeyTime;
        }
    }
}
