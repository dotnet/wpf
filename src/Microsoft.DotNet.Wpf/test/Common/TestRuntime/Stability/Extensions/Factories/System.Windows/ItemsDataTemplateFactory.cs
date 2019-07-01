// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Create a dataTemplate for ItemsControl binding.
    /// </summary>
    public class ItemsDataTemplateFactory : DiscoverableFactory<DataTemplate>
    {
        public override DataTemplate Create(DeterministicRandom random)
        {
            FrameworkElementFactory priceBindingText, genreBindingText, authorBindingText;
            DataTemplate dataTemplate = new DataTemplate();
            FrameworkElementFactory stackPanel = new FrameworkElementFactory(typeof(StackPanel));

            stackPanel.SetValue(FrameworkElement.HeightProperty, 30.0);
            // data template with price, genre and Author
            dataTemplate.VisualTree = stackPanel;

            FrameworkElementFactory dockPanel = new FrameworkElementFactory(typeof(DockPanel), "DockPanel");
            priceBindingText = new FrameworkElementFactory(typeof(TextBlock));
            genreBindingText = new FrameworkElementFactory(typeof(TextBlock));
            authorBindingText = new FrameworkElementFactory(typeof(TextBlock));

            priceBindingText.SetBinding(TextBlock.TextProperty, new Binding("Price"));
            priceBindingText.SetValue(FrameworkElement.WidthProperty, 150.0);
            priceBindingText.SetValue(DockPanel.DockProperty, Dock.Right);

            genreBindingText.SetBinding(TextBlock.TextProperty, new Binding("Genre"));
            genreBindingText.SetValue(FrameworkElement.WidthProperty, 150.0);
            genreBindingText.SetValue(DockPanel.DockProperty, Dock.Left);

            authorBindingText.SetBinding(TextBlock.TextProperty, new Binding("Author"));
            authorBindingText.SetValue(FrameworkElement.WidthProperty, 150.0);

            dockPanel.AppendChild(priceBindingText);
            dockPanel.AppendChild(genreBindingText);
            dockPanel.AppendChild(authorBindingText);
            stackPanel.AppendChild(dockPanel);

            return dataTemplate;
        }
    }
}
