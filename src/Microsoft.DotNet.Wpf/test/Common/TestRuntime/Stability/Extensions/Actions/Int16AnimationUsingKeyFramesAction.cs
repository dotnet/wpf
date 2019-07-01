// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action performs Int16AnimationUsingKeyFrames animation to animate CustomControl's Int16Value Property
    /// </summary>
    public class Int16AnimationUsingKeyFramesAction : StoryBoardAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree, IsEssentialContent = true)]
        public CustomControlForAnimaion CustomControl { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public Int16AnimationUsingKeyFrames Int16AnimationUsingKeyFrames { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            BeginAnimation(Int16AnimationUsingKeyFrames, CustomControl, CustomControlForAnimaion.Int16ValueProperty);
        }

        #endregion
    }
}
