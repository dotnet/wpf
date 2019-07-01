// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using Microsoft.Test.Display;
using Microsoft.Test.Input;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    public class MouseInputAction : SimpleDiscoverableAction
    {
        #region Public Members

        public int MouseOperation { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public MouseButton MouseButton { get; set; }

        public double ScrollLines { get; set; }

        #endregion

        #region Override Members

        public override void Perform()
        {
            // Select Mouse operation.
            switch (MouseOperation % 6)
            {
                case 0:
                    Mouse.Click(MouseButton);
                    break;
                case 1:
                    Mouse.DoubleClick(MouseButton);
                    break;
                case 2:
                    Monitor primaryMonitor = Monitor.GetPrimary();

                    //Get a point in the WorkingArea. 
                    int left = 0, top = 0;
                    if (primaryMonitor.WorkingArea.Width > 0)
                    {
                        left = Width % primaryMonitor.WorkingArea.Width;
                    }
                    if (primaryMonitor.WorkingArea.Height > 0)
                    {
                        top = Height % primaryMonitor.WorkingArea.Height;
                    }
                    Mouse.MoveTo(new System.Drawing.Point(left, top));
                    break;
                case 3:
                    Mouse.Scroll(ScrollLines);
                    break;
                case 4:
                    Mouse.Down(MouseButton);
                    break;
                case 5:
                    Mouse.Up(MouseButton);
                    break;
            }
        }

        #endregion
    }
}
