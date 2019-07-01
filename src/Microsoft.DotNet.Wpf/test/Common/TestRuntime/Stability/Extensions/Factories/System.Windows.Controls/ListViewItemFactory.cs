// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create ListViewItem.
    /// </summary>
    internal class ListViewItemFactory : ListBoxItemFactory<ListViewItem>
    {
        /// <summary>
        /// Create a ListViewItem.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override ListViewItem Create(DeterministicRandom random)
        {
            ListViewItem listViewItem = new ListViewItem();

            ApplyListBoxItemProperties(listViewItem, random);

            return listViewItem;
        }
    }
}
