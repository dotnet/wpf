// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(CustomHwndHostControlFactory))]
    internal class CustomHwndHostControlFactory : DiscoverableFactory<CustomHwndHostControl>
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public CustomHwndHostControl.HwndHostControlRecord HwndHostRecord { get; set; }

        #endregion

        #region Override Members

        public override CustomHwndHostControl Create(DeterministicRandom random)
        {
            return HwndHostRecord.CreateCustomHwndHostControl();
        }

        #endregion
    }
}
