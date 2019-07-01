// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Inherited this abstract class to implement a concrete GridViewRowPresenter factory.
    /// </summary>
    /// <typeparam name="GridViewRowPresenterType"></typeparam>
    internal abstract class GridViewRowPresenterBaseFactory<GridViewRowPresenterType> : DiscoverableFactory<GridViewRowPresenterType> where GridViewRowPresenterType : GridViewRowPresenterBase
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a list of GridViewColumn to set GridViewRowPresenter Columns property.
        /// </summary>
        public List<GridViewColumn> Children { get; set; }

        #endregion

        #region Protected Members

        /// <summary>
        /// Apply common GridViewRowPresenter properties.
        /// </summary>
        /// <param name="gridViewRowPresenter"></param>
        protected void ApplyGridViewRowPresenterBaseProperties(GridViewRowPresenterBase gridViewRowPresenter)
        {
            HomelessTestHelpers.Merge(gridViewRowPresenter.Columns, Children);
        }

        #endregion
    }
}
