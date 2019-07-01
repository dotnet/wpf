// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Test.Stability.Extensions.Actions
{
    /// <summary>
    /// Layout an Element in Canvas.
    /// </summary>
    public class LayoutElementInCanvasAction : SimpleDiscoverableAction
    {
        #region Public Members

        [InputAttribute(ContentInputSource.GetFromLogicalTree)]
        public Canvas Canvas { get; set; }

        public int ChildIndex { get; set; }

        public double HorizontalValue { get; set; }

        public double VerticalValue { get; set; }

        public int AttachPropertyWay { get; set; }

        #endregion

        #region Override Members

        public override bool CanPerform()
        {
            return Canvas.Children.Count > 0;
        }

        public override void Perform()
        {
            ChildIndex %= Canvas.Children.Count;
            UIElement child = Canvas.Children[ChildIndex];

            switch (AttachPropertyWay % 4)
            {
                case 0:
                    Canvas.SetLeft(child, (HorizontalValue - 0.5) * Canvas.Width * 2);
                    Canvas.SetBottom(child, (VerticalValue - 0.5) * Canvas.Height * 2);
                    break;
                case 1:
                    Canvas.SetLeft(child, (HorizontalValue - 0.5) * Canvas.Width * 2);
                    Canvas.SetTop(child, (VerticalValue - 0.5) * Canvas.Height * 2);
                    break;
                case 2:
                    Canvas.SetRight(child, (HorizontalValue - 0.5) * Canvas.Width * 2);
                    Canvas.SetBottom(child, (VerticalValue - 0.5) * Canvas.Height * 2);
                    break;
                case 3:
                    Canvas.SetRight(child, (HorizontalValue - 0.5) * Canvas.Width * 2);
                    Canvas.SetTop(child, (VerticalValue - 0.5) * Canvas.Height * 2);
                    break;
            }
        }

        #endregion
    }
}
