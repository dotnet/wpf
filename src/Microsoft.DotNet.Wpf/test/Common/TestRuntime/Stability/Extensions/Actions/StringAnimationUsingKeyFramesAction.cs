// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action performs StringAnimationUsingKeyFrames animation to animate Page's Title Property
    /// </summary>
    public class StringAnimationUsingKeyFramesAction : StoryBoardAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Window Window { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public StringAnimationUsingKeyFrames StringAnimationUsingKeyFrames { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            BeginAnimation(StringAnimationUsingKeyFrames, Window, Window.TitleProperty);
        }

        #endregion
    }
}
