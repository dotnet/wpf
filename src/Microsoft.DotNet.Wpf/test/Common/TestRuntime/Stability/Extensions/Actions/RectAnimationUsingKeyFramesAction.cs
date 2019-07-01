// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action performs RectAnimationUsingKeyFrames animation to animate RectangleGeometry's Rect Property
    /// </summary>
    public class RectAnimationUsingKeyFramesAction : StoryBoardAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Path Path { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public RectangleGeometry RectangleGeometry { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public RectAnimationUsingKeyFrames RectAnimationUsingKeyFrames { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Path.Data = RectangleGeometry;
            Storyboard.SetTarget(RectAnimationUsingKeyFrames, Path.Data);
            BeginAnimation(RectAnimationUsingKeyFrames, Path, RectangleGeometry.RectProperty);
        }

        #endregion
    }
}
