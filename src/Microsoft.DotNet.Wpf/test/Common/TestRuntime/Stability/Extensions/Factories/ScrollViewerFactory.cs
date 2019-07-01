// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class ScrollViewerFactory : ContentControlFactory<ScrollViewer>
    {
        public override ScrollViewer Create(DeterministicRandom random)
        {
            ScrollViewer scrollViewer = new ScrollViewer();
            ApplyContentControlProperties(scrollViewer);
            scrollViewer.HorizontalScrollBarVisibility = random.NextEnum<ScrollBarVisibility>();
            scrollViewer.VerticalScrollBarVisibility = random.NextEnum<ScrollBarVisibility>();
            return scrollViewer;
        }
    }
}
