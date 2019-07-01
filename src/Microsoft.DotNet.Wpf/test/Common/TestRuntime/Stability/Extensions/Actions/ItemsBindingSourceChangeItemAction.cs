// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Change binding source
    /// </summary>
    [TargetTypeAttribute(typeof(ItemsBindingSourceAddItemAction))]
    public class ItemsBindingSourceChangeItemAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public StressBindingInfo BindingInfo { get; set; }

        public int PublicsherNumber { get; set; }

        public override void Perform()
        {
            BindingInfo.ChangeBookData(PublicsherNumber);
        }
    }
}
