// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Media.Animation;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action performs BooleanAnimationUsingKeyFrames animation to animate FrameworkElement object's IsEnabled Property
    /// </summary>
    public class BooleanAnimationUsingKeyFramesAction : StoryBoardAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public FrameworkElement FrameworkElement { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public BooleanAnimationUsingKeyFrames BooleanAnimationUsingKeyFrames { get; set; }

        #endregion

        #region Override Member

        public override void Perform()
        {
            BeginAnimation(BooleanAnimationUsingKeyFrames, FrameworkElement, FrameworkElement.IsEnabledProperty);
        }

        #endregion
    }
}
