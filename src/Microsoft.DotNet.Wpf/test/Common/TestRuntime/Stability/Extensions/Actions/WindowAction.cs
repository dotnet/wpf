// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    [TargetTypeAttribute(typeof(WindowAction))]
    public class WindowAction : SimpleDiscoverableAction
    {
        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Window Target { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Size WindowSize { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Point WindowTopLeftPoint { get; set; }

        public WindowState WindowState { get; set; }
        
        //resize and reposition the Window.
        public override void Perform()
        {
            Target.Width = WindowSize.Width;
            Target.Height = WindowSize.Height;
            Target.Left = WindowTopLeftPoint.X;
            Target.Top = WindowTopLeftPoint.Y;
            Target.WindowState = WindowState;
        }
    }
}
