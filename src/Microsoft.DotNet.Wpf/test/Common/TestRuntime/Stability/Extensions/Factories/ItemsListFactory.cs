// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    
    [TargetTypeAttribute(typeof(TreeViewItem))]
    class TreeViewItemFactory : DiscoverableFactory<TreeViewItem>
    {        
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int NumberOfItems { get; set; }

        public bool Nested { get; set; }

        public override TreeViewItem Create(DeterministicRandom random)
        {
            TreeViewItem item = new TreeViewItem();
            if (Nested)
            {
                item = AddChild(item, NumberOfItems);
            }
            else
            {
                for (int i = 0; i < NumberOfItems; i++)
                {
                    TreeViewItem child = new TreeViewItem();
                    child.Header = (100 + i).ToString();
                    item.Items.Add(child);
                }
            }
            item.Header = (random.Next()).ToString();            
            return item;
        }

        public TreeViewItem AddChild(TreeViewItem parent, int level)
        {
            if (level == 0)
            {
                return parent;
            }
            --level;
            TreeViewItem child = new TreeViewItem();            
            child = AddChild(child, level);
            child.Header = level.ToString();
            parent.Items.Add(child);
            return parent;
        }
    }
}
