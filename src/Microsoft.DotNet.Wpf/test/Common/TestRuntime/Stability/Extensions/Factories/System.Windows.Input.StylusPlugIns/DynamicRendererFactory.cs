// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Ink;
using System.Windows.Input.StylusPlugIns;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create DynamicRenderer.
    /// </summary>
    internal class DynamicRendererFactory : DiscoverableFactory<DynamicRenderer>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a DrawingAttributes to set DynamicRenderer DrawingAttributes property.
        /// </summary>
        public DrawingAttributes DrawingAttributes { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a DynamicRenderer.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override DynamicRenderer Create(DeterministicRandom random)
        {
            DynamicRenderer dynamicRender = new DynamicRenderer();

            dynamicRender.Enabled = random.NextBool();
            dynamicRender.DrawingAttributes = DrawingAttributes;

            return dynamicRender;
        }

        #endregion
    }
}
