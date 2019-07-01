// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create UniformGrid.
    /// </summary>
    internal class UniformGridFactory : PanelFactory<UniformGrid>
    {
        /// <summary>
        /// Create a UniformGrid.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override UniformGrid Create(DeterministicRandom random)
        {
            UniformGrid grid = new UniformGrid();

            ApplyCommonProperties(grid, random);
            grid.Columns = random.Next();
            grid.FirstColumn = random.Next(grid.Columns);
            grid.Rows = random.Next();

            return grid;
        }
    }
}
