// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Inherited this abstract class to implement a concrete HeaderedItemsControl factory.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class HeaderedItemsControlFactory<T> : ItemsControlFactory<T> where T : HeaderedItemsControl
    {
        #region Public Members

        /// <summary>
        /// Gets or sets an UIElement to set HeaderedItemsControl Header property.
        /// </summary>
        public UIElement Header { get; set; }

        #endregion

        #region Protected Members

        /// <summary>
        /// Apply common HeaderedItemsControl properties.
        /// </summary>
        /// <param name="headeredItemsControl"></param>
        /// <param name="random"></param>
        protected void ApplyHeaderedItemsControlProperties(T headeredItemsControl, DeterministicRandom random)
        {
            ApplyItemsControlProperties(headeredItemsControl, random);
            headeredItemsControl.Header = Header;
        }

        #endregion
    }
}
