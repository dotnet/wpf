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

namespace Microsoft.Test.Stability.Extensions.Actions
{
#if TESTBUILD_CLR40
    [TargetTypeAttribute(typeof(ResourceDictionary))]
    public class ResourceDictionaryCodeAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public StackPanel stackPanel { get; set; }
       
        public ResourceDictionary rd1 { get; set; }
        public ResourceDictionary rd2 { get; set; }

        public override void Perform()
        {
            rd1.BeginInit();
            rd1.Add("1", System.Windows.Media.Brushes.RosyBrown);
            rd1.Add("2", System.Windows.Media.Brushes.Snow);     
            rd1.EndInit();

            DictionaryEntry[] array1 = new DictionaryEntry[rd1.Count + 2];
            //Copy Dictionary to an array
            rd1.CopyTo(array1, 2);

            //Merge dictionaries
            rd2 = stackPanel.Resources;
            rd2.MergedDictionaries.Add(rd1);

            //remove one entry and clear dictionary
            rd1.Remove("1");
            rd1.Clear();
        }
    }

    [TargetTypeAttribute(typeof(ResourceDictionary))]
    public class ResourceDictionaryXamlAction : SimpleDiscoverableAction
    {        
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public StackPanel StackPanel1 { get; set; }                
                
        public SolidColorBrush Brush { get; set; }
        public TextBox TextBox1 { get; set; }
        
        public override void Perform()
        {            
            Button button1 = (Button)StackPanel1.FindName("button1");
            TextBox1.Background = Brush;
            if (button1 != null)
            {
                button1.Background = TextBox1.Background;             
            }
        }
    }


#endif
}
