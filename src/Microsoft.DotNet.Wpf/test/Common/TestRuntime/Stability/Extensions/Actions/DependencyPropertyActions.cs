// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Collections;
using Microsoft.Test.Stability.Extensions.Factories;
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Threading;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40
    [TargetTypeAttribute(typeof(DependencyProperty))]
    public class AttachedPropertyAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public StackPanel StackPanel1 { get; set; }

        public Button Button1 { get; set; }       
        public bool BoolValue { get; set; }
        static DependencyProperty ButtonDProperty = DependencyProperty.RegisterAttached("ButtonD", typeof(bool), typeof(AttachedPropertyAction));

        public override void Perform()
        {                                                
            StackPanel1.Children.Add(Button1);
            Button1.SetValue(ButtonDProperty,BoolValue);
            bool test = (bool)Button1.GetValue(ButtonDProperty);
        }
    }

    [TargetTypeAttribute(typeof(DependencyProperty))]
    public class PropertyTriggerAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public StackPanel StackPanel1 { get; set; }               

        public override void Perform()
        {
            Button button1 = (Button)StackPanel1.FindName("button1");            

            //Changing Opacity to 0 should trigger change in Content
            Double opacity = button1.Opacity;
            if (opacity != 0)
            {
                button1.Opacity = 0;
                DispatcherHelper.DoEvents(1000);
                button1.Opacity = 1;
            }
        }
    }
#endif
}
