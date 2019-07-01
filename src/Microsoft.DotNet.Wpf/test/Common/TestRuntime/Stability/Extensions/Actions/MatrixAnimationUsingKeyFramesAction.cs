// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action performs MatrixAnimationUsingKeyFrames animation to animate MatrixTransform's Matrix Property
    /// </summary>
    public class MatrixAnimationUsingKeyFramesAction : StoryBoardAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public FrameworkElement FrameworkElement { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public MatrixAnimationUsingKeyFrames MatrixAnimationUsingKeyFrames { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        MatrixTransform MatrixTransform { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            FrameworkElement.LayoutTransform = MatrixTransform;
            Storyboard.SetTarget(MatrixAnimationUsingKeyFrames, FrameworkElement.LayoutTransform);
            BeginAnimation(MatrixAnimationUsingKeyFrames, FrameworkElement, MatrixTransform.MatrixProperty);
        }

        #endregion
    }
}
