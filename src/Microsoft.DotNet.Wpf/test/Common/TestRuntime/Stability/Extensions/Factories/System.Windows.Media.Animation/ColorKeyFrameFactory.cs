// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal abstract class ColorKeyFrameFactory<ColorKeyFrameType> : DiscoverableFactory<ColorKeyFrameType> where ColorKeyFrameType : ColorKeyFrame
    {
        #region Public Members

        public Color Color { get; set; }

        public KeyTime KeyTime { get; set; }

        #endregion

        protected void ApplyColorKeyFrameProperties(ColorKeyFrame colorKeyFrame, DeterministicRandom random)
        {
            colorKeyFrame.Value = Color;
            colorKeyFrame.KeyTime = KeyTime;
        }
    }
}
