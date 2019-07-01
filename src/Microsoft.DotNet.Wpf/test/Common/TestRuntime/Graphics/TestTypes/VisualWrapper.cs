// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// A wrapper for the visuals that we render
    /// </summary>
    public class VisualWrapper : Visual
    {
        /// <summary/>
        public VisualWrapper(Size windowSize)
        {
            children = new VisualCollection(this);
            backgroundColor = RenderingWindow.DefaultBackgroundColor;
            visual = null;
            this.windowSize = windowSize;

            SetBackground();
        }

        /// <summary/>
        public Visual Visual
        {
            get
            {
                return visual;
            }
            set
            {
                children.Clear();

                SetBackground();

                visual = value;
                children.Add(visual);
            }
        }

        private void SetBackground()
        {
            // We don't want to be affected by anti-aliasing,
            //  so extend the borders of the background beyond the window's width and height.

            Rect renderArea = new Rect(-10, -10, windowSize.Width + 20, windowSize.Height + 20);
            Rectangle background = new Rectangle(renderArea, backgroundColor);

            children.Add(background.Visual);
        }

        /// <summary/>
        protected override Visual GetVisualChild(int index)
        {
            if (children == null)
            {
                throw new ArgumentOutOfRangeException("There are no children to get");
            }
            if (index < 0 || children.Count <= index)
            {
                throw new ArgumentOutOfRangeException("index", index, "I don't have that many children");
            }

            return children[index];
        }

        /// <summary/>
        protected override int VisualChildrenCount
        {
            get
            {
                if (children == null)
                {
                    return 0;
                }
                return children.Count;
            }
        }

        /// <summary/>
        public Color BackgroundColor
        {
            set { backgroundColor = value; }
        }

        private VisualCollection children;
        private Color backgroundColor;
        private Visual visual;
        private Size windowSize;
    }
}


