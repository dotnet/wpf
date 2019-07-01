// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action performs PointAnimationUsingKeyFrames animation to animate EllipseGeometry's Center Property
    /// </summary>
    public class PointAnimationUsingKeyFramesAction : StoryBoardAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Path Path { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public EllipseGeometry EllipseGeometry { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public PointAnimationUsingKeyFrames PointAnimationUsingKeyFrames { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Path.Data = EllipseGeometry;
            Storyboard.SetTarget(PointAnimationUsingKeyFrames, Path.Data);
            BeginAnimation(PointAnimationUsingKeyFrames, Path, EllipseGeometry.CenterProperty);
        }

        #endregion
    }
}
