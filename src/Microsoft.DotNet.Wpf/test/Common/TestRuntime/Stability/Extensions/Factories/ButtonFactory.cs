// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create Button.
    /// </summary>
    internal class ButtonFactory : AbstractButtonFactory<Button>
    {
        #region Override Members

        /// <summary>
        /// Create a button.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override Button Create(DeterministicRandom random)
        {
            Button button = new Button();

            ApplyButtonProperties(button, random);

            return button;
        }

        #endregion
    }
}
