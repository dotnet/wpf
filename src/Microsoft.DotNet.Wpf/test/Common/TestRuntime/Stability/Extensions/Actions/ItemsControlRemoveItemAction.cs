// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Remove an item from ItemsControl.
    /// </summary>
    public class ItemsControlRemoveItemAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public ItemsControl ItemsControl { get; set; }

        public int Index { get; set; }

        public bool IsRemove { get; set; }

        public override void Perform()
        {
            int removalTypeIndex = Index % ItemsControl.Items.Count;
            if (IsRemove)
            {
                ItemsControl.Items.Remove(ItemsControl.Items[removalTypeIndex]);
            }
            else
            {
                ItemsControl.Items.RemoveAt(removalTypeIndex);
            }
        }

        /// <summary>
        /// Operation is not valid while ItemsSource is in use.
        /// </summary>
        /// <returns></returns>
        public override bool CanPerform()
        {
            return ItemsControl.HasItems && ItemsControl.ItemsSource == null;
        }
    }
}
