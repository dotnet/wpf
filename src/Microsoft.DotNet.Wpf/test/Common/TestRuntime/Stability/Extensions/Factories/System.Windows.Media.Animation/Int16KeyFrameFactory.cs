// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal abstract class Int16KeyFrameFactory<Int16KeyFrameType> : DiscoverableFactory<Int16KeyFrameType> where Int16KeyFrameType : Int16KeyFrame
    {
        #region Public Members

        public short Value { get; set; }

        public KeyTime KeyTime { get; set; }

        #endregion

        protected void ApplyInt16KeyFrameProperties(Int16KeyFrame int16KeyFrame, DeterministicRandom random)
        {
            int16KeyFrame.Value = Value;
            int16KeyFrame.KeyTime = KeyTime;
        }
    }
}
