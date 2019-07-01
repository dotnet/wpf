// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Media;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class VisualAttachEventHandlerAction : AttachEventHandlerAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromVisualTree)]
        public Visual Visual { get; set; }

        public bool IsAttach { get; set; }

        #endregion

        #region Override Members

        public override bool CanPerform()
        {
            return Visual.GetType() != typeof(Window);
        }

        public override void Perform()
        {
            AddOrRemoveHandlers(Visual, IsAttach);
        }

        #endregion
    }
}
