// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Adding an UIElement to Panel Action.
    /// </summary>
    public class AddPanelChildAction : SimpleDiscoverableAction
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the Panel which UIElement is added to.
        /// </summary>
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Panel Target { get; set; }

        /// <summary>
        /// Gets or sets the UIElement to add. 
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public UIElement Element { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Adds an UIElement to Panel.
        /// </summary>
        public override void Perform()
        {
            Target.Children.Add(Element);
            SetChildrenLayout();
        }

        #endregion

        #region Private Members

        private void SetChildrenLayout()
        {
            if (Target is DockPanel)
            {
                DockPanel.SetDock(Element, (Dock)(Target.Children.Count % 4));
            }

            if (Target is Canvas)
            {
                double rate = 1.0 / (double)Target.Children.Count;
                Canvas.SetBottom(Element, rate * Target.ActualHeight);
                Canvas.SetLeft(Element, rate * Target.ActualWidth);
            }

            if (Target is Grid)
            {
                Grid panel = Target as Grid;
                panel.ColumnDefinitions.Add(new ColumnDefinition());
                panel.RowDefinitions.Add(new RowDefinition());
                Grid.SetColumn(Element, panel.ColumnDefinitions.Count - 1);
                Grid.SetRow(Element, panel.RowDefinitions.Count - 1);
            }
        }

        #endregion
    }
}
