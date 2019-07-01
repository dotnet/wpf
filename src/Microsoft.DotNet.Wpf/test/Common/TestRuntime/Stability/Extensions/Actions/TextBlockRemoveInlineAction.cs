// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Remove an Inline from TextBlock.
    /// </summary>
    public class TextBlockRemoveInlineAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public TextBlock Target { get; set; }

        public int RemoveIndex { get; set; }

        #endregion

        #region Override Members

        public override bool CanPerform()
        {
            return Target.Inlines.Count > 0;
        }

        public override void Perform()
        {
            RemoveIndex %= Target.Inlines.Count;
            Inline inline = Target.Inlines.ElementAtOrDefault<Inline>(RemoveIndex);
            Target.Inlines.Remove(inline);
        }

        #endregion
    }
}
