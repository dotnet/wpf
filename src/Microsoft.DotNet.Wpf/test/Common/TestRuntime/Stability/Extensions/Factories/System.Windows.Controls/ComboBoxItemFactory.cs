// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create ComboBoxItem.
    /// </summary>
    internal class ComboBoxItemFactory : ListBoxItemFactory<ComboBoxItem>
    {
        /// <summary>
        /// Create a ComboBoxItem.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override ComboBoxItem Create(DeterministicRandom random)
        {
            ComboBoxItem comboBoxItem = new ComboBoxItem();

            ApplyListBoxItemProperties(comboBoxItem, random);

            return comboBoxItem;
        }
    }
}
