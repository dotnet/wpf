// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Inherited this abstract class to implement a concrete Selector factory.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SelectorFactory<T> : ItemsControlFactory<T> where T : Selector
    {
        #region Public Members

        /// <summary>
        /// Gets or sets an object to set Selector SelectedItem property.
        /// </summary>
        public object SelectedItem { get; set; }

        /// <summary>
        /// Gets or sets an object to set Selector SelectedValue property.
        /// </summary>
        public object SelectedValue { get; set; }

        #endregion

        #region Protected Members

        /// <summary>
        /// Apply common Selector properties.
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="random"></param>
        protected void ApplySelectorProperties(T selector, DeterministicRandom random)
        {
            ApplyItemsControlProperties(selector, random);
            selector.IsSynchronizedWithCurrentItem = random.NextBool();
            selector.SelectedIndex = random.Next(selector.Items.Count);
            selector.SelectedItem = SelectedItem;
            selector.SelectedValue = SelectedValue;
        }

        #endregion
    }
}
