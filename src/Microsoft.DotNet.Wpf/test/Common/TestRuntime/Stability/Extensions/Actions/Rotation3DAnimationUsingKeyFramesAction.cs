// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action performs Rotation3DAnimationUsingKeyFrames animation to animate RotateTransform3D's Rotation Property
    /// </summary>
    public class Rotation3DAnimationUsingKeyFramesAction : StoryBoardAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public ProjectionCamera ProjectionCamera { get; set; }

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Viewport3D Viewport3D { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public Rotation3DAnimationUsingKeyFrames Rotation3DAnimationUsingKeyFrames { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public RotateTransform3D RotateTransform3D { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            ProjectionCamera.Transform = RotateTransform3D;
            Viewport3D.Camera = ProjectionCamera;
            Storyboard.SetTarget(Rotation3DAnimationUsingKeyFrames, Viewport3D.Camera.Transform);
            BeginAnimation(Rotation3DAnimationUsingKeyFrames, Viewport3D, RotateTransform3D.RotationProperty);
        }

        #endregion
    }
}
