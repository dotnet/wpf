// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create MenuItem.
    /// </summary>
    internal class MenuItemFactory : HeaderedItemsControlFactory<MenuItem>
    {
        /// <summary>
        /// Create a MenuItem.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override MenuItem Create(DeterministicRandom random)
        {
            MenuItem menuItem = new MenuItem();
            ApplyHeaderedItemsControlProperties(menuItem, random);
            return menuItem;
        }
    }
}
