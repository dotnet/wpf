// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal abstract class Int64KeyFrameFactory<Int64KeyFrameType> : DiscoverableFactory<Int64KeyFrameType> where Int64KeyFrameType : Int64KeyFrame
    {
        #region Public Members

        public long Value { get; set; }

        public KeyTime KeyTime { get; set; }

        #endregion

        protected void ApplyInt64KeyFrameProperties(Int64KeyFrame int64KeyFrame, DeterministicRandom random)
        {
            int64KeyFrame.Value = Value;
            int64KeyFrame.KeyTime = KeyTime;
        }
    }
}
