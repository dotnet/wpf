// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Data;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Set a filter to the view.
    /// </summary>
    [TargetTypeAttribute(typeof(ItemsBindingSourceAddItemAction))]
    public class ItemsControlFilterAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public StressBindingInfo BindingInfo { get; set; }

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public ItemsControl ItemsControl { get; set; }

        public int Index { get; set; }

        public int OptionIndex { get; set; }

        public override void Perform()
        {
            if (ItemsControl.Items.CanFilter)
            {
                Index = Index % BindingInfo.Filters.Length;
                Predicate<object> filter = BindingInfo.Filters[Index];
                ItemsControl.Items.Filter = filter;
            }
            else if (ItemsControl.ItemsSource.GetType() == typeof(DataView))
            {
                BindingListCollectionView view = (BindingListCollectionView)CollectionViewSource.GetDefaultView(((DataView)ItemsControl.ItemsSource).Table);

                if (OptionIndex % 2 == 0)
                {
                    view.CustomFilter = "Price > 50";
                }
                else
                {
                    view.CustomFilter = "Price <= 50";
                }
            }
        }

        public override bool CanPerform()
        {
            return ItemsControl.HasItems;
        }
    }
}
