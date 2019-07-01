// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Inherited this abstract class to implement a concrete ListBoxItem factory.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class ListBoxItemFactory<T> : ContentControlFactory<T> where T : ListBoxItem
    {
        #region Protected Members

        /// <summary>
        /// Apply common ListBoxItem properties.
        /// </summary>
        /// <param name="listBoxItem"></param>
        /// <param name="random"></param>
        protected void ApplyListBoxItemProperties(T listBoxItem, DeterministicRandom random)
        {
            ApplyContentControlProperties(listBoxItem);
            listBoxItem.IsSelected = random.NextBool();
            listBoxItem.Selected += new RoutedEventHandler(OnSelected);
            listBoxItem.Unselected += new RoutedEventHandler(OnUnselected);
        }

        #endregion

        #region Private Events

        private void OnSelected(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("List box was selected.");
        }

        private void OnUnselected(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("List box was unselected.");
        }

        #endregion
    }
}
