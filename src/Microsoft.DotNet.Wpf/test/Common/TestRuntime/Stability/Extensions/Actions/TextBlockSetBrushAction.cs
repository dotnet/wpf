// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Media;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class TextBlockSetBrushAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public TextBlock Target { get; set; }
        public Brush Brush { get; set; }

        public override void Perform()
        {
            Target.Background = Brush;
        }
    }
}
