// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Layout an Element in Grid.
    /// </summary>
    public class LayoutElementInGridAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Grid Grid { get; set; }

        public int ChildIndex { get; set; }

        public int NewColumn { get; set; }

        public int NewRow { get; set; }

        public int NewColumnSpan { get; set; }

        public int NewRowSpan { get; set; }

        #endregion

        #region Override Members

        public override bool CanPerform()
        {
            return Grid.Children.Count > 0;
        }

        public override void Perform()
        {
            ChildIndex %= Grid.Children.Count;
            UIElement child = Grid.Children[ChildIndex];

            int columnCount = Grid.ColumnDefinitions.Count;
            if (columnCount > 0)
            {
                NewColumn %= columnCount;
                Grid.SetColumn(child, NewColumn);
            }

            int rowCount = Grid.RowDefinitions.Count;
            if (rowCount > 0)
            {
                NewRow %= rowCount;
                Grid.SetRow(child, NewRow);
            }

            if (columnCount - NewColumn > 0)
            {
                NewColumnSpan = NewColumnSpan % (columnCount - NewColumn) + 1;
                Grid.SetColumnSpan(child, NewColumnSpan);
            }

            if (rowCount - NewRow > 0)
            {
                NewRowSpan = NewRowSpan % (rowCount - NewRow) + 1;
                Grid.SetRowSpan(child, NewRowSpan);
            }
        }

        #endregion
    }
}
