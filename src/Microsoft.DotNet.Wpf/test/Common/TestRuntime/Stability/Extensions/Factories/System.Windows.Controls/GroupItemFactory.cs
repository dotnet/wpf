// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create GroupItem.
    /// </summary>
    internal class GroupItemFactory : ContentControlFactory<GroupItem>
    {
        /// <summary>
        /// Create a GroupItem.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override GroupItem Create(DeterministicRandom random)
        {
            GroupItem groupItem = new GroupItem();

            ApplyContentControlProperties(groupItem);

            return groupItem;
        }
    }
}
