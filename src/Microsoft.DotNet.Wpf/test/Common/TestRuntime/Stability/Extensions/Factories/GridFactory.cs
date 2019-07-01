// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create Grid.
    /// </summary>
    internal class GridFactory : AbstractGridFactory<Grid>
    {
        /// <summary>
        /// Create a Grid.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override Grid Create(DeterministicRandom random)
        {
            Grid grid = new Grid();

            ApplyGridProperties(grid, random);

            return grid;
        }
    }
}
