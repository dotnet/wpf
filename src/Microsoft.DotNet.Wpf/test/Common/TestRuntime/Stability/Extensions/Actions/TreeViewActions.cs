// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Input;
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40
        
    public class TreeViewAddItemAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public TreeView TreeViewTest { get; set; }        

        [InputAttribute(ContentInputSource.CreateFromFactory)]
        public TreeViewItem SubTree { get; set; }
        
        public override void Perform()
        {                        
            TreeViewTest.Items.Add(SubTree);            
        }        
    }

    public class TreeViewSelectItemAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public TreeView TreeViewTest { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory)]
        public int ItemIndex { get; set; }

        public override void Perform()
        {
            TreeViewTest.Focus();
            int count = TreeViewTest.Items.Count;
            if (count > 1)
            {
                ((TreeViewItem)TreeViewTest.Items[(ItemIndex % count)]).IsSelected = true;                
            }
        }
    }

    public class TreeViewExpandAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public TreeView TreeViewTest { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory)]
        public int ItemIndex { get; set; }

        public override void Perform()
        {
            int count = 0;            
            TreeViewTest.Focus();
            count = TreeViewTest.Items.Count;
            if (count > 1)
            {
                ((TreeViewItem)TreeViewTest.Items[(ItemIndex % count)]).IsSelected = true;
                ((TreeViewItem)TreeViewTest.Items[(ItemIndex % count)]).ExpandSubtree();
            }
        }
    }

    public class TreeViewExpandAllAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public TreeView TreeViewTest { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory)]
        public int ItemIndex { get; set; }

        public override void Perform()
        {
            TreeViewTest.Focus();                              
            HomelessTestHelpers.KeyPress(System.Windows.Input.Key.Multiply);            
        }
    }
#endif
}
