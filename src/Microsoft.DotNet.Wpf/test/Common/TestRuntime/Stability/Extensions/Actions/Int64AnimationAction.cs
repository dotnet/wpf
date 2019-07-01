// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media.Animation;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// This action performs Int64Animation to animate CustomControl's Int64Value Property
    /// </summary>
    public class Int64AnimationAction : StoryBoardAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree, IsEssentialContent = true)]
        public CustomControlForAnimaion CustomControl { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public Int64Animation Int64Animation { get; set; }

        #endregion

        #region Public Members

        public override void Perform()
        {
            BeginAnimation(Int64Animation, CustomControl, CustomControlForAnimaion.Int64ValueProperty);
        }

        #endregion
    }
}
