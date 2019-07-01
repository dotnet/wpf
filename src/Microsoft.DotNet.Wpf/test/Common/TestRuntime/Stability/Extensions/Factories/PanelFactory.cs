// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Inherited this abstract class to implement a concrete Panel factory.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class PanelFactory<T> : DiscoverableFactory<T> where T : Panel
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a list of UIElement to set Panel Children property.
        /// </summary>
        public List<UIElement> Children { get; set; }

        /// <summary>
        /// Gets or sets a Brush to set Panel Background property.
        /// </summary>
        public Brush Background { get; set; }

        #endregion

        #region Protected Members

        /// <summary>
        /// Apply common Panel properties.
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="random"></param>
        protected void ApplyCommonProperties(T panel, DeterministicRandom random)
        {
            panel.Background = Background;
            panel.IsItemsHost = false;
            HomelessTestHelpers.Merge(panel.Children, Children);
        }

        #endregion
    }
}
