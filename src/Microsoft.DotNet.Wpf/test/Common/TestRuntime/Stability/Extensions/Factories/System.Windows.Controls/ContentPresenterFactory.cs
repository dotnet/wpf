// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create ContentPresenter.
    /// </summary>
    internal class ContentPresenterFactory : AbstractContentPresenterFactory<ContentPresenter>
    {
        /// <summary>
        /// Create a ContentPresenter.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override ContentPresenter Create(DeterministicRandom random)
        {
            ContentPresenter contentPresenter = new ContentPresenter();

            ApplyContentPresenterProperties(contentPresenter, random);

            return contentPresenter;
        }
    }
}
