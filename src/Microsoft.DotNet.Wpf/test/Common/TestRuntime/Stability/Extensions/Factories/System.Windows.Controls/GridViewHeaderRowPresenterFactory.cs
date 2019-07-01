// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create GridViewHeaderRowPresenter.
    /// </summary>
    internal class GridViewHeaderRowPresenterFactory : GridViewRowPresenterBaseFactory<GridViewHeaderRowPresenter>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a Style to set GridViewHeaderRowPresenter ColumnHeaderContainerStyle property.
        /// </summary>
        public Style ColumnHeaderContainerStyle { get; set; }

        /// <summary>
        /// Gets or sets an object to set GridViewHeaderRowPresenter ColumnHeaderToolTip property.
        /// </summary>
        public object ColumnHeaderToolTip { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a GridViewHeaderRowPresenter.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override GridViewHeaderRowPresenter Create(DeterministicRandom random)
        {
            GridViewHeaderRowPresenter gridViewHeaderRowPresenter = new GridViewHeaderRowPresenter();

            ApplyGridViewRowPresenterBaseProperties(gridViewHeaderRowPresenter);
            gridViewHeaderRowPresenter.AllowsColumnReorder = random.NextBool();
            gridViewHeaderRowPresenter.ColumnHeaderContainerStyle = ColumnHeaderContainerStyle;
            gridViewHeaderRowPresenter.ColumnHeaderStringFormat = "Title:{0}";
            gridViewHeaderRowPresenter.ColumnHeaderToolTip = ColumnHeaderToolTip;
            // HACK: Can't covered ColumnHeaderContextMenu property unless fixed the issues with ContextMenuFactory

            return gridViewHeaderRowPresenter;
        }

        #endregion
    }
}
