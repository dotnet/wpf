// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
#if TESTBUILD_CLR40
    /// <summary>
    /// A factory which create DataGridRowHeader.
    /// </summary>
    internal class DataGridRowHeaderFactory : ButtonBaseFactory<DataGridRowHeader>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a Brush to set DataGridRowHeader SeparatorBrush property.
        /// </summary>
        public Brush SeparatorBrush { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a DataGridRowHeader.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override DataGridRowHeader Create(DeterministicRandom random)
        {
            DataGridRowHeader rowHeader = new DataGridRowHeader();

            ApplyButtonBaseProperties(rowHeader, random);
            rowHeader.SeparatorBrush = SeparatorBrush;
            rowHeader.Visibility = random.NextEnum<Visibility>();

            return rowHeader;
        }

        #endregion
    }
#endif
}
