// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action performs ColorAnimationUsingKeyFrames animation to animate SolidColorBrush's Color Property
    /// </summary>
    public class ColorAnimationUsingKeyFramesAction : StoryBoardAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public SolidColorBrush SolidColorBrush { get; set; }

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Panel Panel { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public ColorAnimationUsingKeyFrames ColorAnimationUsingKeyFrames { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Panel.Background = SolidColorBrush;
            Storyboard.SetTarget(ColorAnimationUsingKeyFrames, Panel.Background);
            BeginAnimation(ColorAnimationUsingKeyFrames, Panel, SolidColorBrush.ColorProperty);
        }

        #endregion
    }
}
