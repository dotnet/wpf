// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal abstract class SizeKeyFrameFactory<SizeKeyFrameType> : DiscoverableFactory<SizeKeyFrameType> where SizeKeyFrameType : SizeKeyFrame
    {
        #region Public Members

        public Size Value { get; set; }
        public KeyTime KeyTime { get; set; }

        #endregion

        protected void ApplySizeKeyFrameProperties(SizeKeyFrame sizeKeyFrame, DeterministicRandom random)
        {
            sizeKeyFrame.Value = Value;
            sizeKeyFrame.KeyTime = KeyTime;
        }
    }
}
