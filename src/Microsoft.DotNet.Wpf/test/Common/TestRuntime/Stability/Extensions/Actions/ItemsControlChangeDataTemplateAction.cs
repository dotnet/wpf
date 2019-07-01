// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Change DataTemplate to ItemsControl.
    /// </summary>
    public class ItemsControlChangeDataTemplateAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public ItemsControl ItemsControl { get; set; }

        public DataTemplate DataTemplate { get; set; }

        public override void Perform()
        {
            ItemsControl.ItemTemplate = DataTemplate;
        }
    }
}
