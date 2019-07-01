// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// ItemsControl add Items.
    /// </summary>
    public class ItemsControlAddItemAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public ItemsControl ItemsControl { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public ContentControl Item { get; set; }

        public override void Perform()
        {
            // Operation is not valid while ItemsSource is in use. Clear ItemsSource.
            ItemsControl.ItemsSource = null;

            // Add items to the ItemsControl.
            ItemsControl.Items.Add(Item);
        }
    }
}
