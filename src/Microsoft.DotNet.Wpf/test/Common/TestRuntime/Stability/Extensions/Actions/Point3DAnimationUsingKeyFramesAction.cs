// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action performs Point3DAnimationUsingKeyFrames animation to animate ProjectionCamera's Position Property
    /// </summary>
    public class Point3DAnimationUsingKeyFramesAction : StoryBoardAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public ProjectionCamera ProjectionCamera { get; set; }

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Viewport3D Viewport3D { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public Point3DAnimationUsingKeyFrames Point3DAnimationUsingKeyFrames { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Viewport3D.Camera = ProjectionCamera;
            Storyboard.SetTarget(Point3DAnimationUsingKeyFrames, Viewport3D.Camera);
            BeginAnimation(Point3DAnimationUsingKeyFrames, Viewport3D, ProjectionCamera.PositionProperty);
        }

        #endregion
    }
}
