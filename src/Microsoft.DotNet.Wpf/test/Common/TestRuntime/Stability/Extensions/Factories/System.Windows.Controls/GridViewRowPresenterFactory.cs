// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create GridViewRowPresenter.
    /// </summary>
    internal class GridViewRowPresenterFactory : GridViewRowPresenterBaseFactory<GridViewRowPresenter>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets an object to set GridViewRowPresenter Content property.
        /// </summary>
        public object Content { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a GridViewRowPresenter.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override GridViewRowPresenter Create(DeterministicRandom random)
        {
            GridViewRowPresenter gridViewRowPresenter = new GridViewRowPresenter();

            ApplyGridViewRowPresenterBaseProperties(gridViewRowPresenter);
            gridViewRowPresenter.Content = Content;

            return gridViewRowPresenter;
        }

        #endregion
    }
}
