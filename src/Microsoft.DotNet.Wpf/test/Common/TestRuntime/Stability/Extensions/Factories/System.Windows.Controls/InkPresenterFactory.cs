// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;
using System.Windows.Ink;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create InkPresenter.
    /// </summary>
    internal class InkPresenterFactory : DecoratorFactory<InkPresenter>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a StrokeCollection to set InkPresenter Strokes property.
        /// </summary>
        public StrokeCollection StrokeCollection { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a InkPresenter.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override InkPresenter Create(DeterministicRandom random)
        {
            InkPresenter inkPresenter = new InkPresenter();

            ApplyDecoratorProperties(inkPresenter, random);
            inkPresenter.Strokes = StrokeCollection;

            return inkPresenter;
        }

        #endregion
    }
}
