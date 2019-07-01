// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal abstract class Int32KeyFrameFactory<Int32KeyFrameType> : DiscoverableFactory<Int32KeyFrameType> where Int32KeyFrameType : Int32KeyFrame
    {
        #region Public Members

        public int Value { get; set; }

        public KeyTime KeyTime { get; set; }

        #endregion

        protected void ApplyInt32KeyFrameProperties(Int32KeyFrame int32KeyFrame, DeterministicRandom random)
        {
            int32KeyFrame.Value = Value;
            int32KeyFrame.KeyTime = KeyTime;
        }
    }
}
