// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Documents;
using System.Diagnostics;
using System.Windows.Markup;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Paragraph add an Inline.
    /// </summary>
    public class ParagraphAddInlineAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Paragraph Target { get; set; }

        public Inline Inline { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            Target.Inlines.Add(Inline);
        }

        #endregion
    }
}
