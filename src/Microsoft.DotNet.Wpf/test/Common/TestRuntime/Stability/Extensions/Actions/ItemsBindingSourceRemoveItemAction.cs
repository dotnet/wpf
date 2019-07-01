// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Remove binding source
    /// </summary>
    [TargetTypeAttribute(typeof(ItemsBindingSourceAddItemAction))]
    public class ItemsBindingSourceRemoveItemAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public StressBindingInfo BindingInfo { get; set; }

        public override void Perform()
        {
            BindingInfo.RemoveBook();
        }
    }
}
