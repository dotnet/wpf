// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows.Controls;
using System.Windows;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class AddContentAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public ContentControl Target { get; set; }

        public UIElement Object { get; set; }

        public override bool CanPerform()
        {
            if (Object is Window)
            {
                // A Window can't be added as Content to anything.
                // Unfortunately, the new Window already got created and shown.  Get rid of it.
                ((Window)Object).Close();
                return false;
            }
            
            return true;
        }

        public override void Perform()
        {
            Target.Content = Object;
        }
    }
}
