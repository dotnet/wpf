// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
#if TESTBUILD_CLR40
    /// <summary>
    /// A factory which create SelectiveScrollingGrid.
    /// </summary>
    internal class SelectiveScrollingGridFactory : AbstractGridFactory<SelectiveScrollingGrid>
    {
        /// <summary>
        /// Create a SelectiveScrollingGrid.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override SelectiveScrollingGrid Create(DeterministicRandom random)
        {
            SelectiveScrollingGrid grid = new SelectiveScrollingGrid();

            ApplyGridProperties(grid, random);

            return grid;
        }
    }
#endif
}
