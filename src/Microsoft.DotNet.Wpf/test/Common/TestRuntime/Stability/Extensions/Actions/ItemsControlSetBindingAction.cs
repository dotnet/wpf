// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Set binding to ItemsControl.
    /// </summary>
    [TargetTypeAttribute(typeof(ItemsBindingSourceAddItemAction))]
    public class ItemsControlSetBindingAction : SimpleDiscoverableAction
    {
        public enum DataSource { ADO, Object, XML }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public StressBindingInfo BindingInfo { get; set; }

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public ItemsControl ItemsControl { get; set; }

        public int Index { get; set; }

        public bool IsComposite { get; set; }

        public int Size { get; set; }

        public DataSource SourceType { get; set; }

        public override void Perform()
        {
            CollectionContainer collectionContainer;
            Binding binding;

            Index %= BindingInfo.Libraries.Length;
            if (IsComposite)
            {
                CompositeCollection compositeCollection = new CompositeCollection();

                int size = (Size % BindingInfo.Libraries.Length) + 1;
                for (int i = 0; i < size; i++)
                {
                    collectionContainer = new CollectionContainer();
                    binding = new Binding();

                    switch (SourceType)
                    {
                        case DataSource.Object:
                            binding.Source = BindingInfo.Libraries[Index];
                            break;
                        case DataSource.XML:
                            binding.Source = BindingInfo.XMLLibraries[Index];
                            binding.XPath = "Library/*";
                            break;
                        case DataSource.ADO:
                            binding.Source = BindingInfo.ADOLibraries[Index];
                            break;
                    }

                    BindingOperations.SetBinding(collectionContainer, CollectionContainer.CollectionProperty, binding);
                    compositeCollection.Add(collectionContainer);
                    Index++;
                    if (Index >= BindingInfo.Libraries.Length)
                    {
                        Index = 0;
                    }
                }

                // Items collection must be empty before using ItemsSource.
                if (ItemsControl.ItemsSource == null)
                {
                    ItemsControl.Items.Clear();
                }

                ItemsControl.ItemsSource = compositeCollection;
            }
            else
            {
                binding = new Binding();

                switch (SourceType)
                {
                    case DataSource.Object:
                        binding.Source = BindingInfo.Libraries[Index];
                        break;
                    case DataSource.XML:
                        binding.Source = BindingInfo.XMLLibraries[Index];
                        binding.XPath = "Library/*";
                        break;
                    case DataSource.ADO:
                        binding.Source = BindingInfo.ADOLibraries[Index];
                        break;
                }

                // Items collection must be empty before using ItemsSource.
                if (ItemsControl.ItemsSource == null)
                {
                    ItemsControl.Items.Clear();
                }

                ItemsControl.SetBinding(ItemsControl.ItemsSourceProperty, binding);
            }
        }
    }
}
