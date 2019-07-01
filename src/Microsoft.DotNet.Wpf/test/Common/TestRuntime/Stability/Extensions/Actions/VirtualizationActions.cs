// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40            
    [TargetTypeAttribute(typeof(ScrollItemsControlAction))]
    public class ScrollItemsControlAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public ItemsControl MyItemsControl { get; set; }
        
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public int NumberOfItems { get; set; }

        public override void Perform()
        {        
            //Idea here is to add a lot of items to the ItemsControl and then scroll. ToDo: when factory is able to provide this big list, change code to directly ask factory
            for (int i = 0; i < NumberOfItems; i++)
            {
                ContentControl itemControl = new ContentControl();
                MyItemsControl.Items.Add(itemControl);
            }

            List<DependencyObject> listOfObjects = HomelessTestHelpers.VisualTreeWalk(MyItemsControl);
            ScrollViewer scrollViewer;

            foreach (DependencyObject eachObject in listOfObjects)
            {
                if (eachObject.GetType() == typeof(ScrollViewer))
                {
                    scrollViewer = (ScrollViewer)eachObject;
                    scrollViewer.ScrollToBottom();
                    return;
                }
            }          
        }
    }
#endif
}
