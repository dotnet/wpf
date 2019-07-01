// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action performs VectorAnimationUsingKeyFrames animation to animate CustomControl's VectorValue Property
    /// </summary>
    public class VectorAnimationUsingKeyFramesAction : StoryBoardAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree, IsEssentialContent = true)]
        public CustomControlForAnimaion CustomControl { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public VectorAnimationUsingKeyFrames VectorAnimationUsingKeyFrames { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            BeginAnimation(VectorAnimationUsingKeyFrames, CustomControl, CustomControlForAnimaion.VectorValueProperty);
        }

        #endregion
    }
}
