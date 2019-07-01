// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// FlowDocumentPageReader executes a NavigationCommand.
    /// </summary>
    public class FlowDocumentReaderExecuteNavigationCommandAction : AbstractExecuteNavigationCommandAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public FlowDocumentReader FlowDocumentReader { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            DoCommand(FlowDocumentReader, FlowDocumentReader.PageCount);
        }

        #endregion
    }
}
