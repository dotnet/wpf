// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal abstract class ThicknessKeyFrameFactory<ThicknessKeyFrameType> : DiscoverableFactory<ThicknessKeyFrameType> where ThicknessKeyFrameType : ThicknessKeyFrame
    {
        #region Public Members

        public Thickness Value { get; set; }

        public KeyTime KeyTime { get; set; }


        #endregion

        protected void ApplyThicknessKeyFrameProperties(ThicknessKeyFrame thicknessKeyFrame, DeterministicRandom random)
        {
            thicknessKeyFrame.Value = Value;
            thicknessKeyFrame.KeyTime = KeyTime;
        }
    }
}
