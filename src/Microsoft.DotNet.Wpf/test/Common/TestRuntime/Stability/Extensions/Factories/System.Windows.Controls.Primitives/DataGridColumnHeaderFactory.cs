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
    /// A factory which create a DataGridColumnHeader.
    /// </summary>
    internal class DataGridColumnHeaderFactory : ButtonBaseFactory<DataGridColumnHeader>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a Brush to set DataGridColumnHeader SeparatorBrush property.
        /// </summary>
        public Brush SeparatorBrush { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a DataGridColumnHeader.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override DataGridColumnHeader Create(DeterministicRandom random)
        {
            DataGridColumnHeader columnHeader = new DataGridColumnHeader();

            ApplyButtonBaseProperties(columnHeader, random);
            columnHeader.Visibility = random.NextEnum<Visibility>();
            columnHeader.SeparatorBrush = SeparatorBrush;

            return columnHeader;
        }

        #endregion

    }
#endif
}
