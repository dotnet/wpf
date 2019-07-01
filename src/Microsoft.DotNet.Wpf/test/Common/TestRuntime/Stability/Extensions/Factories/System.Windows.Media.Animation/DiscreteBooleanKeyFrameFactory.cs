// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class DiscreteBooleanKeyFrameFactory : DiscoverableFactory<DiscreteBooleanKeyFrame>
    {
        #region Public Members

        public bool Value { get; set; }
        public KeyTime KeyTime { get; set; }

        #endregion

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new DiscreteBooleanKeyFrame</returns>
        public override DiscreteBooleanKeyFrame Create(DeterministicRandom random)
        {
            DiscreteBooleanKeyFrame discreteBooleanKeyFrame = new DiscreteBooleanKeyFrame();
            discreteBooleanKeyFrame.Value = Value;
            discreteBooleanKeyFrame.KeyTime = KeyTime;

            return discreteBooleanKeyFrame;
        }
    }
}
