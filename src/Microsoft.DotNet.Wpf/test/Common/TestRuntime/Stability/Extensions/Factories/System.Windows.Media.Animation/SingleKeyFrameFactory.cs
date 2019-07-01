// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal abstract class SingleKeyFrameFactory<SingleKeyFrameType> : DiscoverableFactory<SingleKeyFrameType> where SingleKeyFrameType : SingleKeyFrame
    {
        #region Public Members

        public float Value { get; set; }

        public KeyTime KeyTime { get; set; }

        #endregion

        protected void ApplySingleKeyFrameProperties(SingleKeyFrame singleKeyFrame, DeterministicRandom random)
        {
            singleKeyFrame.Value = Value;
            singleKeyFrame.KeyTime = KeyTime;
        }
    }
}
