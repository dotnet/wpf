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
    /// Inherited this abstract class to implement a concrete ItemsControl factory.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ItemsControlFactory<T> : DiscoverableFactory<T> where T : ItemsControl
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a list of GroupStyle to set ItemsControl GroupStyle property.
        /// </summary>
        public List<GroupStyle> GroupStyleList { get; set; }

        /// <summary>
        /// Gets or sets a Style to set ItemsControl ItemContainerStyle property.
        /// </summary>
        public Style ItemContainerStyle { get; set; }

        /// <summary>
        /// Gets or sets a StyleSelector to set ItemsControl ItemContainerStyleSelector property.
        /// </summary>
        public StyleSelector ItemContainerStyleSelector { get; set; }

        /// <summary>
        /// Gets or sets a list of FrameworkElement to set ItemsControl Items property.
        /// </summary>
        public List<FrameworkElement> Children { get; set; }

        /// <summary>
        /// Gets or sets a DataTemplate to set ItemsControl ItemTemplate property.
        /// </summary>
        public DataTemplate DataTemplate { get; set; }

        #endregion

        #region Protected Members

        /// <summary>
        /// Apply common ItemsControl properties.
        /// </summary>
        /// <param name="itemsControl"></param>
        /// <param name="random"></param>
        protected void ApplyItemsControlProperties(T itemsControl, DeterministicRandom random)
        {
            HomelessTestHelpers.Merge(itemsControl.Items, Children);
            HomelessTestHelpers.Merge(itemsControl.GroupStyle, GroupStyleList);
            itemsControl.AlternationCount = random.Next();
#if TESTBUILD_CLR40
            itemsControl.IsTextSearchCaseSensitive = random.NextBool();
#endif
            itemsControl.IsTextSearchEnabled = random.NextBool();
            itemsControl.ItemContainerStyle = ItemContainerStyle;
            itemsControl.ItemContainerStyleSelector = ItemContainerStyleSelector;
            itemsControl.DisplayMemberPath = null;
            itemsControl.ItemStringFormat = "Title:{0}";

            if (random.NextDouble() < 0.1)//ItemsControl set DataTemplate 10% possibility.
            {
                itemsControl.ItemTemplate = DataTemplate;
            }
        }

        #endregion
    }
}
