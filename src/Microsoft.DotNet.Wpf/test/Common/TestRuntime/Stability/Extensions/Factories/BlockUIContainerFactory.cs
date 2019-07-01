// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Documents;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create BlockUIContainer.
    /// </summary>
    [TargetTypeAttribute(typeof(Block))]      
    internal class BlockUIContainerFactory : BlockFactory<BlockUIContainer>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a UIElement to set BlockUIContainer Child property.
        /// </summary>
        public UIElement UIElement { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a BlockUIContainer.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override BlockUIContainer Create(DeterministicRandom random)
        {
            BlockUIContainer blockUIContainer = new BlockUIContainer();

            ApplyBlockProperties(blockUIContainer, random);
            blockUIContainer.Child = UIElement;
            
            return blockUIContainer;
        }

        #endregion
    }
}
