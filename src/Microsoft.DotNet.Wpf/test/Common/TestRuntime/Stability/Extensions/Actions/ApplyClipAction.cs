// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Media;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class ApplyClipAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public UIElement Target { get; set; }

        public Geometry Geometry { get; set; }

        public override void Perform()
        {
            Target.Clip = Geometry;
        }
    }
}
