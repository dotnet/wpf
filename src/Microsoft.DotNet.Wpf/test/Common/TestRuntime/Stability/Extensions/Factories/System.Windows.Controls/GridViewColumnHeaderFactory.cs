// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create GridViewColumnHeader.
    /// </summary>
    internal class GridViewColumnHeaderFactory : ButtonBaseFactory<GridViewColumnHeader>
    {
        /// <summary>
        /// Create a GridViewColumnHeader.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override GridViewColumnHeader Create(DeterministicRandom random)
        {
            GridViewColumnHeader gridViewColumnHeader = new GridViewColumnHeader();

            ApplyButtonBaseProperties(gridViewColumnHeader, random);

            return gridViewColumnHeader;
        }
    }
}
