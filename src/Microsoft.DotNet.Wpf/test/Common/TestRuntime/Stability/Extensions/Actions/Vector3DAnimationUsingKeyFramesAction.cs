// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action performs Vector3DAnimationUsingKeyFrames animaiton to animate ProjectionCamera's LookDirection Property
    /// </summary>
    public class Vector3DAnimationUsingKeyFramesAction : StoryBoardAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public ProjectionCamera ProjectionCamera { get; set; }

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Viewport3D Viewport3D { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public Vector3DAnimationUsingKeyFrames Vector3DAnimationUsingKeyFrames { get; set; }

        #endregion

        #region Public Members

        public override void Perform()
        {
            Viewport3D.Camera = ProjectionCamera;
            Storyboard.SetTarget(Vector3DAnimationUsingKeyFrames, Viewport3D.Camera);
            BeginAnimation(Vector3DAnimationUsingKeyFrames, Viewport3D, ProjectionCamera.LookDirectionProperty);
        }

        #endregion
    }
}
