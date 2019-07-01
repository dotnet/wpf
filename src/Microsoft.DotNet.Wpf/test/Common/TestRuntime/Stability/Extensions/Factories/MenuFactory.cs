// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create Menu.
    /// </summary>
    internal class MenuFactory : ItemsControlFactory<Menu>
    {
        /// <summary>
        /// Create a Menu.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override Menu Create(DeterministicRandom random)
        {
            Menu menu = new Menu();

            ApplyItemsControlProperties(menu, random);
            menu.IsMainMenu = random.NextBool();

            return menu;
        }
    }
}
