// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Documents;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Span add an Inline.
    /// </summary>
    public class SpanAddInlineAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Span Target { get; set; }

        public Inline Inline { get; set; }

        #endregion

        #region Override Members

        public override bool CanPerform()
        {
            //Hyperlink cannot be placed within the scope of a Hyperlink.
            return !(Target is Hyperlink);
        }

        public override void Perform()
        {
            Target.Inlines.Add(Inline);
        }

        #endregion
    }
}
