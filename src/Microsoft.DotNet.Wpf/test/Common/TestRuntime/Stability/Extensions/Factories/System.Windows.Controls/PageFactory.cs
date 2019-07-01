// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which Create Page.
    /// </summary>
    [TargetTypeAttribute(typeof(Page))]
    internal class PageFactory : DiscoverableFactory<Page>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a Brush to set Page Background property.
        /// </summary>
        public Brush Background { get; set; }

        /// <summary>
        /// Gets or sets an object to set Page Content property.
        /// </summary>
        public object Content { get; set; }

        /// <summary>
        /// Gets or sets a FontFamily to set Page FontFamily property.
        /// </summary>
        public FontFamily FontFamily { get; set; }

        /// <summary>
        /// Gets or sets a value to set Page FontSize property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double FontSize { get; set; }

        /// <summary>
        /// Gets or sets a Brush to set Page Foreground property.
        /// </summary>
        public Brush Foreground { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// It can't create a Page unless desiredType is Page since Page need special Parent.
        /// </summary>
        /// <param name="desiredType"></param>
        /// <returns></returns>
        public override bool CanCreate(Type desiredType)
        {
            return desiredType == typeof(Page);
        }

        /// <summary>
        /// Create a Page.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override Page Create(DeterministicRandom random)
        {
            Page page = new Page();

            page.Background = Background;
            page.Content = Content;
            page.FontFamily = FontFamily;
            page.FontSize = FontSize;
            page.Foreground = Foreground;
            page.KeepAlive = random.NextBool();
            page.ShowsNavigationUI = random.NextBool();
            page.Title = "Page Title";
            page.WindowTitle = "Window Title";
            //Set WindowHeight and WindowWidth (0, 10000].
            page.WindowHeight = (1 - random.NextDouble()) * 10000;
            page.WindowWidth = (1 - random.NextDouble()) * 10000;

            return page;
        }

        #endregion
    }
}
