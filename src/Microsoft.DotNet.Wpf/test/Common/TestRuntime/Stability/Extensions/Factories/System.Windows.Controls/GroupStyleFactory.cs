// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create GroupStyle.
    /// </summary>
    internal class GroupStyleFactory : DiscoverableFactory<GroupStyle>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a Style to set GroupStyle ContainerStyle property.
        /// </summary>
        public Style ContainerStyle { get; set; }

        /// <summary>
        /// Gets or sets a StyleSelector to set GroupStyle ContainerStyleSelector property.
        /// </summary>
        public StyleSelector ContainerStyleSelector { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a GroupStyle.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override GroupStyle Create(DeterministicRandom random)
        {
            GroupStyle groupStyle = new GroupStyle();

            groupStyle.AlternationCount = random.Next();
            groupStyle.ContainerStyle = ContainerStyle;
            groupStyle.ContainerStyleSelector = ContainerStyleSelector;
            groupStyle.HidesIfEmpty = random.NextBool();
            groupStyle.HeaderStringFormat = "Title:{0}";

            return groupStyle;
        }

        #endregion
    }
}
