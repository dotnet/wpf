// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using Microsoft.Test.Stability.Extensions.Constraints;
using System.Windows.Controls;
using Microsoft.Test.Stability.Extensions.Factories;

namespace Microsoft.Test.Stability.Extensions.Actions
{

    public class SetVisualScrollableAreaClipAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public ScrollAcceleratedCanvas Target { get; set; }

        public Rect Rect { get; set; }

        public override void Perform()
        {
            Target.SetVisualScrollableAreaClip(Rect);
        }
    }
}
