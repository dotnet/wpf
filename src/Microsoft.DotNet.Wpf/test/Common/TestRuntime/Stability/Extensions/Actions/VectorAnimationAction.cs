// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action performs VectorAnimation to animate CustomControl's VectorValue Property
    /// </summary>
    public class VectorAnimationAction : StoryBoardAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree, IsEssentialContent = true)]
        public CustomControlForAnimaion CustomControl { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public VectorAnimation VectorAnimation { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            BeginAnimation(VectorAnimation, CustomControl, CustomControlForAnimaion.VectorValueProperty);
        }

        #endregion
    }
}
