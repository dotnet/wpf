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
using System.Windows.Media;

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40    

    [TargetTypeAttribute(typeof(AttachStyleAction))]
    public class AttachStyleAction : SimpleDiscoverableAction
    {        
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public FrameworkElement MyElement { get; set; }                       
        
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double MyNewOpacity { get; set; }

        public bool OverrideStyle { get; set; }
        public Style MyStyle { get; set; }        

        public override void Perform()
        {
            if (MyStyle != null)
            {
                MyStyle.TargetType = typeof(FrameworkElement);                
            }            
            MyElement.Style = MyStyle;            

            if (OverrideStyle)
            {
                //Override Width property which is set by Style in xaml
                MyElement.Width = 100;

                //Attach another Style to button, overriding the one set in xaml                
                MyElement.Opacity = MyNewOpacity;
            }
        }
    }    
#endif
}
