// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create StyleSelector.
    /// </summary>
    internal class StyleSelectorFactory : DiscoverableFactory<StyleSelector>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a Style to initialize a CunstomStyleSelector. 
        /// </summary>
        public Style Style { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a StyleSelector.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override StyleSelector Create(DeterministicRandom random)
        {
            StyleSelector styleSelector = new CustomStyleSelector(Style);
            return styleSelector;
        }

        #endregion
    }

    /// <summary>
    /// A Custom Style Selector class.
    /// XamlObjectReader need this class be public.
    /// </summary>
    public class CustomStyleSelector : StyleSelector
    {
        /// <summary>
        /// a style used to return.
        /// </summary>
        private Style customStyle;

        /// <summary>
        /// Initializes a new instance of the CustomStyleSelector class.
        /// XamlObjectReader need a default constructor.
        /// </summary>
        public CustomStyleSelector() { }

        /// <summary>
        /// Initializes a new instance of the CustomStyleSelector class.
        /// </summary>
        /// <param name="style"/>
        public CustomStyleSelector(Style style)
        {
            customStyle = style;
        }

        /// <summary>
        /// Select the customStyle to return.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public override Style SelectStyle(object item, DependencyObject container)
        {
            return customStyle;
        }
    }
}
