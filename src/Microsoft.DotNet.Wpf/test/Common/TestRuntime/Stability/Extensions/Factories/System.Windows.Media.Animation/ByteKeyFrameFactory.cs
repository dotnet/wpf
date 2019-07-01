// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Abstract class for ByteKeyFrame,covered Value and KeyTime Properties
    /// </summary>
    /// <typeparam name="ByteKeyFrameType"></typeparam>
    internal abstract class ByteKeyFrameFactory<ByteKeyFrameType> : DiscoverableFactory<ByteKeyFrameType> where ByteKeyFrameType : ByteKeyFrame
    {
        #region Public Members

        public byte Value { get; set; }

        public KeyTime KeyTime { get; set; }

        #endregion

        /// <summary/>
        /// <param name="byteKeyFrame/">
        /// <param name="random">Apply ByteKeyFrame Properties,include Value and KeyTime property </param>
        protected void ApplyByteKeyFrameProperties(ByteKeyFrame byteKeyFrame, DeterministicRandom random)
        {
            byteKeyFrame.Value = Value;
            byteKeyFrame.KeyTime = KeyTime;
        }
    }
}
