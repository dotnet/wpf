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
    /// A factory which create InlineUIContainer.
    /// </summary>
    [TargetTypeAttribute(typeof(TextElement))]  
    internal class InlineUIContainerFactory : InlineFactory<InlineUIContainer>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets an UIElement to set InlineUIContainer Child property.
        /// </summary>
        public UIElement UIElement { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create an InlineUIContainer.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override InlineUIContainer Create(DeterministicRandom random)
        {
            InlineUIContainer inlineUIContainer = new InlineUIContainer();
            ApplyInlineProperties(inlineUIContainer, random);
            inlineUIContainer.Child = UIElement;

            return inlineUIContainer;
        }

        #endregion
    }
}
