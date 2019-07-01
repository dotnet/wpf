// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create ScrollContentPresenter
    /// </summary>
    internal class ScrollContentPresenterFactory : AbstractContentPresenterFactory<ScrollContentPresenter>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a ScrollViewer to set ScrollContentPresenter ScrollOwner property.
        /// </summary>
        public ScrollViewer ScrollOwner { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a ScrollContentPresenter.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override ScrollContentPresenter Create(Core.DeterministicRandom random)
        {
            ScrollContentPresenter scrollContentPresenter = new ScrollContentPresenter();

            ApplyContentPresenterProperties(scrollContentPresenter, random);
            scrollContentPresenter.CanContentScroll = random.NextBool();
            scrollContentPresenter.CanHorizontallyScroll = random.NextBool();
            scrollContentPresenter.CanVerticallyScroll = random.NextBool();
            scrollContentPresenter.ScrollOwner = ScrollOwner;

            return scrollContentPresenter;
        }

        #endregion
    }
}
