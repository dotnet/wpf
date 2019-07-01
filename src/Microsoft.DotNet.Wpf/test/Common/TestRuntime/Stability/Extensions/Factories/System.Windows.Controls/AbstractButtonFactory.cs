// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Inherited this abstract class to implement a concrete Button factory.
    /// </summary>
    /// <typeparam name="ButtonType"/>
    public abstract class AbstractButtonFactory<ButtonType> : ButtonBaseFactory<ButtonType> where ButtonType : Button
    {
        /// <summary>
        /// Apply common button properties.
        /// </summary>
        /// <param name="button"/>
        /// <param name="random"/>
        protected void ApplyButtonProperties(ButtonType button, DeterministicRandom random)
        {
            ApplyButtonBaseProperties(button, random);
            button.IsCancel = random.NextBool();
            button.IsDefault = random.NextBool();
        }
    }
}
