// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action performs ThicknessAnimation to animate Control object's Padding Property
    /// </summary>
    public class ThicknessAnimationAction : StoryBoardAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Control Control { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public ThicknessAnimation ThicknessAnimation { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            BeginAnimation(ThicknessAnimation, Control, Control.PaddingProperty);
        }

        #endregion
    }
}
