// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action performs SizeAnimation to animate CustomControl's SizeValue Property
    /// </summary>
    public class SizeAnimationAction : StoryBoardAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree, IsEssentialContent = true)]
        public CustomControlForAnimaion CustomControl { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public SizeAnimation SizeAnimation { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            if (SizeAnimation.To != null && (CustomControl.SizeValue.Height > SizeAnimation.To.Value.Height || CustomControl.SizeValue.Width > SizeAnimation.To.Value.Width))
                SizeAnimation.IsCumulative = false;
            BeginAnimation(SizeAnimation, CustomControl, CustomControlForAnimaion.SizeValueProperty);
        }

        #endregion
    }
}
