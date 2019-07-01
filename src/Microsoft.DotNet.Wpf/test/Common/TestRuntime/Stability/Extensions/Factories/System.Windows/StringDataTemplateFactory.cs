// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Test.Stability.Core;
using System;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create string DataTemplate.
    /// </summary>
    internal class StringDataTemplateFactory : DiscoverableFactory<DataTemplate>
    {
        /// <summary>
        /// Create a simple DataTemplate which contains a Button and a Label in a StackPanel.
        /// Label content is the string Length.
        /// Button content is the string.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override DataTemplate Create(DeterministicRandom random)
        {
            DataTemplate dataTemplate = new DataTemplate(typeof(string));

            FrameworkElementFactory panel = new FrameworkElementFactory(typeof(StackPanel));

            FrameworkElementFactory label = new FrameworkElementFactory(typeof(Label));
            //Bind string Length to Label content.
            Binding lengthBinding = new Binding("Length");
            lengthBinding.Mode = BindingMode.OneWay;
            label.SetBinding(Label.ContentProperty, lengthBinding);

            FrameworkElementFactory button = new FrameworkElementFactory(typeof(Button));
            //Bind string to Button content.
            Binding contentBinding = new Binding();
            contentBinding.Mode = BindingMode.OneWay;
            button.SetBinding(Button.ContentProperty, contentBinding);

            panel.AppendChild(label);
            panel.AppendChild(button);

            dataTemplate.VisualTree = panel;

            return dataTemplate;
        }

        
    }

    
}
