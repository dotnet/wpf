// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class PopupFactory : DiscoverableFactory<Popup>
    {
        public UIElement Child { get; set; }
        public UIElement PlacementTarget { get; set; }
        public Rect Rect { get; set; }

        public override sealed bool CanCreate(Type desiredType)
        {
            return desiredType == typeof(Popup);
        }

        public override Popup Create(DeterministicRandom random)
        {
            Popup popup = new Popup();
            popup.Child = Child;
            popup.IsOpen = random.NextBool();
            popup.Placement = random.NextEnum<PlacementMode>();
            popup.PlacementRectangle = Rect;
            popup.PlacementTarget = PlacementTarget;
            return popup;
        }
    }
}
