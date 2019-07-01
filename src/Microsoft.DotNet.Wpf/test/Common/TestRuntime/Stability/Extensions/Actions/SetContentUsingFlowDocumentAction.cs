// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Documents;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Set ContentControl Content using FlowDocument.
    /// </summary>
    public class SetContentUsingFlowDocumentAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public ContentControl Target { get; set; }

        public FlowDocument FlowDocument { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Target.Content = FlowDocument;
        }

        #endregion
    }
}
