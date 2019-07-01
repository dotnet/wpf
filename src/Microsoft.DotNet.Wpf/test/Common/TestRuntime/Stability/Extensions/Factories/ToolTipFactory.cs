// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;

// TODO: Reenable after fixing the issues with ToolTip
/*
namespace Microsoft.Test.Stability.Extensions.Factories
{
    class ToolTipFactory : ContentControlFactory<ToolTip>
    {
        public Rect Rect { get; set; }
        public UIElement UIElement { get; set; }

        public override ToolTip Create(DeterministicRandom random)
        {
            ToolTip toolTip = new ToolTip();
            ApplyContentControlProperties(toolTip);
            toolTip.HasDropShadow = random.NextBool();
            toolTip.IsOpen = random.NextBool();
            toolTip.Placement = random.NextEnum<PlacementMode>();
            toolTip.PlacementRectangle = Rect;
            toolTip.PlacementTarget = UIElement;
            return toolTip;
        }
    }
}
*/
