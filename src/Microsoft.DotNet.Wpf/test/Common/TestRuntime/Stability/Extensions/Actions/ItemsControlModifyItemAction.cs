// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Modify an item of ItemsControl. 
    /// </summary>
    public class ItemsControlModifyItemAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public ItemsControl ItemsControl { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public ContentControl Item { get; set; }

        public int Index { get; set; }

        public override void Perform()
        {
            Index %= ItemsControl.Items.Count;
            ItemsControl.Items[Index] = Item;
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
