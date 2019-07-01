// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal abstract class RectKeyFrameFactory<RectKeyFrameType> : DiscoverableFactory<RectKeyFrameType> where RectKeyFrameType : RectKeyFrame
    {
        #region Public Members

        public Rect Value { get; set; }

        public KeyTime KeyTime { get; set; }

        #endregion

        protected void ApplyRectKeyFrameProperties(RectKeyFrame rectKeyFrame, DeterministicRandom random)
        {
            rectKeyFrame.Value = Value;
            rectKeyFrame.KeyTime = KeyTime;
        }
    }
}
