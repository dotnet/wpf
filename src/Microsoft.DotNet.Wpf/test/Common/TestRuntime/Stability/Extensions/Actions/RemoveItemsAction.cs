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
    public class ItemsControlRemoveAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public ItemsControl MyItemsControl { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory)]
        public int ItemIndex { get; set; }

        public override void Perform()
        {
            int count = MyItemsControl.Items.Count;
            if (count > 2)
            {                
                MyItemsControl.Items.RemoveAt((ItemIndex % count));             
            }
        }
    }
#endif
}
