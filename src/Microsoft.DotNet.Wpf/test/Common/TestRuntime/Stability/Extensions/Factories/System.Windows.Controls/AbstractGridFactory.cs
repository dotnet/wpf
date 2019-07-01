// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Inherited this abstract class to implement a concrete Grid factory.
    /// </summary>
    /// <typeparam name="GridType"></typeparam>
    internal abstract class AbstractGridFactory<GridType> : PanelFactory<GridType> where GridType : Grid
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a list of ColumnDefinition to set Grid ColumnDefinitions property.
        /// </summary>
        public List<ColumnDefinition> ColumnDefinitions { get; set; }

        /// <summary>
        /// Gets or sets a list of RowDefinition to set Grid RowDefinitions property.
        /// </summary>
        public List<RowDefinition> RowDefinitions { get; set; }

        #endregion

        #region Protected Members

        /// <summary>
        /// Apply common Grid properties.
        /// </summary>
        /// <param name="grid"/>
        /// <param name="random"/>
        protected void ApplyGridProperties(GridType grid, DeterministicRandom random)
        {
            ApplyCommonProperties(grid, random);
            HomelessTestHelpers.Merge(grid.ColumnDefinitions, ColumnDefinitions);
            HomelessTestHelpers.Merge(grid.RowDefinitions, RowDefinitions);
            SetChildrenLayout(grid, random);
            grid.ShowGridLines = random.NextBool();
        }

        #endregion

        #region Private Members

        private void SetChildrenLayout(GridType grid, DeterministicRandom random)
        {
            int childrenCount = grid.Children.Count;
            //Add more rows and columns to make sure they are enough to set children layout.
            for(int i=0;i<childrenCount;i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
            }
            //Set children layout
            foreach (UIElement item in grid.Children)
            {
                int startRow = random.Next() % grid.RowDefinitions.Count;
                int startColumn = random.Next() % grid.ColumnDefinitions.Count;

                Grid.SetColumn(item, startColumn);
                Grid.SetRow(item, startRow);

                if (random.NextBool())
                {
                    int spanColumn = random.Next() % (grid.ColumnDefinitions.Count - startColumn) + 1;
                    Grid.SetColumnSpan(item, spanColumn);
                }

                if (random.NextBool())
                {
                    int spanRow = random.Next() % (grid.RowDefinitions.Count - startRow) + 1;
                    Grid.SetRowSpan(item, spanRow);
                }
            }
        }

        #endregion
    }
}
