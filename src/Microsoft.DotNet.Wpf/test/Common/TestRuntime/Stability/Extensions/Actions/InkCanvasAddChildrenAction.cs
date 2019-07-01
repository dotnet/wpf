// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Add UIElement to InkCanvas
    /// </summary>
    public class InkCanvasAddChildrenAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public InkCanvas Target { get; set; }

        public UIElement UIElement { get; set; }

        public override void Perform()
        {
            Target.Children.Add(UIElement);
        }
    }
}
