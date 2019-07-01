// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// FlowDocumentPageViewer executes a NavigationCommand.
    /// </summary>
    public class FlowDocumentPageViewerExecuteNavigationCommandAction : AbstractExecuteNavigationCommandAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public FlowDocumentPageViewer FlowDocumentPageViewer { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            DoCommand(FlowDocumentPageViewer, FlowDocumentPageViewer.PageCount);
        }

        #endregion
    }
}
