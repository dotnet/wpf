// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create simple DataTemplate.
    /// </summary>
    internal class SimpleDataTemplateFactory : DiscoverableFactory<DataTemplate>
    {
        /// <summary>
        /// Create a simple DataTemplate which only contains a Border and a CheckBox in a StackPanel.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override DataTemplate Create(DeterministicRandom random)
        {
            DataTemplate dataTemplate = new DataTemplate();

            FrameworkElementFactory panel = new FrameworkElementFactory(typeof(StackPanel));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            FrameworkElementFactory checkBox = new FrameworkElementFactory(typeof(CheckBox));
            panel.AppendChild(border);
            panel.AppendChild(checkBox);

            dataTemplate.VisualTree = panel;

            return dataTemplate;
        }
    }
}
