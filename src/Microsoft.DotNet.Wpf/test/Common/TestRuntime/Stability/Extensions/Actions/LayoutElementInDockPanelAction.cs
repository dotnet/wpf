// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Layout an Element in DockPanel.
    /// </summary>
    public class LayoutElementInDockPanelAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public DockPanel DockPanel { get; set; }

        public int ChildIndex { get; set; }

        public Dock Dock { get; set; }

        #endregion

        #region Override Members

        public override bool CanPerform()
        {
            return DockPanel.Children.Count > 0;
        }

        public override void Perform()
        {
            ChildIndex %= DockPanel.Children.Count;
            UIElement child = DockPanel.Children[ChildIndex];
            DockPanel.SetDock(child, Dock);
        }

        #endregion
    }
}
