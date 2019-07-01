// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Stability.Extensions.CustomTypes;
using Microsoft.Test.Stability.Extensions.Factories;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    [TargetTypeAttribute(typeof(CustomHwndHostControlFactory))]
    public class DisposeHwndHostAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public CustomHwndHostControl.HwndHostControlRecord HwndHostRecord { get; set; }

        public int DisposeIndex { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            HwndHostRecord.DisposeHwndHostControl(DisposeIndex);
        }

        #endregion
    }
}
