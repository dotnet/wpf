// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Media.Animation;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action performs DoubleAnimationUsingKeyFrames animation to animate FrameworkElement object's Opacity Property
    /// </summary>
    public class DoubleAnimationUsingKeyFramesAction : StoryBoardAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public FrameworkElement FrameworkElement { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public DoubleAnimationUsingKeyFrames DoubleAnimationUsingKeyFrames { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            BeginAnimation(DoubleAnimationUsingKeyFrames, FrameworkElement, FrameworkElement.OpacityProperty);
        }

        #endregion
    }
}
