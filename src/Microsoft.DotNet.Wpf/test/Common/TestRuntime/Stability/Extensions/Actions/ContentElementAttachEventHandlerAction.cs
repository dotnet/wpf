// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class ContentElementAttachEventHandlerAction : AttachEventHandlerAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromVisualTree)]
        public ContentElement element { get; set; }

        public bool IsAttach { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            AddOrRemoveHandlers(element, IsAttach);
        }

        #endregion
    }
}
