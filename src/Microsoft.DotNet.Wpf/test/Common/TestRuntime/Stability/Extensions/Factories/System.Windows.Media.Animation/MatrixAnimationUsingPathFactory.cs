// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class MatrixAnimationUsingPathFactory : TimelineFactory<MatrixAnimationUsingPath>
    {
        #region Public Members

        public bool DoesRotateWithTangent { get; set; }

        public bool IsAdditive { get; set; }

        public bool IsAngleCumulative { get; set; }

        public bool IsOffsetCumulative { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public PathGeometry AnimationPath { get; set; }

        #endregion

        #region Override Members

        public override MatrixAnimationUsingPath Create(DeterministicRandom random)
        {
            MatrixAnimationUsingPath matrixAnimationUsingPath = new MatrixAnimationUsingPath();
            matrixAnimationUsingPath.DoesRotateWithTangent = DoesRotateWithTangent;
            matrixAnimationUsingPath.IsAdditive = IsAdditive;
            matrixAnimationUsingPath.IsAngleCumulative = IsAngleCumulative;
            matrixAnimationUsingPath.IsOffsetCumulative = IsOffsetCumulative;
            matrixAnimationUsingPath.PathGeometry = AnimationPath;
            ApplyTimelineProperties(matrixAnimationUsingPath, random);

            return matrixAnimationUsingPath;
        }

        #endregion
    }
}
