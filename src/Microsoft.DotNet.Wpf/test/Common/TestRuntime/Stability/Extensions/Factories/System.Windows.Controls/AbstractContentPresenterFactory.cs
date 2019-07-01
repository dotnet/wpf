// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Inherited this abstract class to implement a concrete ContentPresenter factory.
    /// </summary>
    /// <typeparam name="PresenterType">ContentPresenter type</typeparam>
    [TargetTypeAttribute(typeof(ContentPresenter))]
    internal abstract class AbstractContentPresenterFactory<PresenterType> : DiscoverableFactory<PresenterType> where PresenterType : ContentPresenter
    {
        #region Public Members

        /// <summary>
        /// Gets or sets an object to set ContentPresenter Content property.
        /// </summary>
        public object Content { get; set; }

        /// <summary>
        /// Gets or sets a string to set ContentPresenter ContentSource property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public string ContentSource { get; set; }

        #endregion

        #region Protected Members

        /// <summary>
        /// Apply common ContentPresenter properties.
        /// </summary>
        /// <param name="contentPresenter"/>
        /// <param name="random"/>
        protected void ApplyContentPresenterProperties(ContentPresenter contentPresenter, DeterministicRandom random)
        {
            contentPresenter.Content = Content;
            contentPresenter.ContentSource = ContentSource;
            contentPresenter.ContentStringFormat = "Content:{0}";
        }

        #endregion
    }
}
