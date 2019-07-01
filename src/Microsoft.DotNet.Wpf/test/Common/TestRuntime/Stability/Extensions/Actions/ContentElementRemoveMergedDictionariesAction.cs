// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class ContentElementRemoveMergedDictionariesAction : RemoveMergedDictionariesAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public FrameworkContentElement Element { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            RemoveMergedDictionaries(Element.Resources.MergedDictionaries);
        }

        public override bool CanPerform()
        {
            return Element.Resources.MergedDictionaries.Count > 0;
        }

        #endregion
    }
}
