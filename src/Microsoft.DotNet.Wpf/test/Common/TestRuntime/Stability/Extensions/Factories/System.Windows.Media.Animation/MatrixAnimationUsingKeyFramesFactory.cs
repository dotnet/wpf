// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class MatrixAnimationUsingKeyFramesFactory : TimelineFactory<MatrixAnimationUsingKeyFrames>
    {
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public MatrixKeyFrameCollection KeyFrames { get; set; }

        public override MatrixAnimationUsingKeyFrames Create(DeterministicRandom random)
        {
            MatrixAnimationUsingKeyFrames matrixAnimationUsingKeyFrames = new MatrixAnimationUsingKeyFrames();
            matrixAnimationUsingKeyFrames.KeyFrames = KeyFrames;
            ApplyTimelineProperties(matrixAnimationUsingKeyFrames, random);

            return matrixAnimationUsingKeyFrames;
        }
    }
}
