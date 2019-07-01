// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
//DataGrid is not present in pre-4.0 .NET
#if TESTBUILD_CLR40
    /// <summary>
    /// A factory which create DataGrid ControlTemplate.
    /// </summary>
    internal class DataGridControlTemplateFactory : DiscoverableFactory<ControlTemplate>
    {
        /// <summary>
        /// Create a DataGrid ControlTemplate.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override ControlTemplate Create(DeterministicRandom random)
        {
            ControlTemplate controlTemplate = new ControlTemplate(typeof(DataGrid));

            FrameworkElementFactory panel = new FrameworkElementFactory(typeof(StackPanel));
            FrameworkElementFactory columnHeaders = new FrameworkElementFactory(typeof(DataGridColumnHeadersPresenter));
            FrameworkElementFactory rows = new FrameworkElementFactory(typeof(DataGridRowsPresenter));
            rows.SetValue(Panel.IsItemsHostProperty, true);
            FrameworkElementFactory details = new FrameworkElementFactory(typeof(DataGridDetailsPresenter));
            FrameworkElementFactory cells = new FrameworkElementFactory(typeof(DataGridCellsPresenter));
            panel.AppendChild(rows);
            panel.AppendChild(columnHeaders);
            panel.AppendChild(details);
            panel.AppendChild(cells);

            controlTemplate.VisualTree = panel;

            return controlTemplate;
        }
    }
#endif
}
