// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Set GroupDescriptions to the Collection View.
    /// </summary>
    [TargetTypeAttribute(typeof(ItemsBindingSourceAddItemAction))]
    public class ItemsControlGroupAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public StressBindingInfo BindingInfo { get; set; }

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public ItemsControl ItemsControl { get; set; }

        public int Index { get; set; }

        public override void Perform()
        {
            ICollectionView collectionView = null;

            if (ItemsControl.ItemsSource is CompositeCollection)
            {
                CompositeCollection compositeCollection = ItemsControl.ItemsSource as CompositeCollection;
                if (compositeCollection != null && compositeCollection.Count > 0)
                {
                    Index %= compositeCollection.Count;
                    CollectionContainer container = (CollectionContainer)compositeCollection[Index];
                    if (container != null && container.Collection != null)
                    {
                        collectionView = CollectionViewSource.GetDefaultView(container.Collection);
                    }
                }
            }
            else
            {
                collectionView = ItemsControl.Items;
            }

            if (collectionView != null)
            {
                Index %= BindingInfo.GroupDescriptionArray.Length;
                PropertyGroupDescription propertyGroupDescription = BindingInfo.GroupDescriptionArray[Index];
                using (collectionView.DeferRefresh())
                {
                    collectionView.GroupDescriptions.Clear();
                    collectionView.GroupDescriptions.Add(propertyGroupDescription);
                }
            }
        }
    }
}
