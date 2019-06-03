// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//  Spec: LayoutInformation class.doc

using System;
using System.Windows.Media;
using System.Windows.Threading;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// This interface exposes additional layout information not exposed otherwise on FrameworkElement.
    /// This information is mostly used by the designer programs to produce additional visual clues for the user
    /// during interactive editing of the elements and layout properties.
    /// </summary>
    public static class LayoutInformation
    {
        private static void CheckArgument(FrameworkElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
        }

        /// <summary>
        /// Returns the rectangle that represents Layout Slot - the layout partition reserved for the 
        /// child by the layout parent. This info is in the coordinte system of the layout parent.
        /// </summary>
        public static Rect GetLayoutSlot(FrameworkElement element)
        {
            CheckArgument(element);
            return element.PreviousArrangeRect;
        }

        /// <summary>
        /// Returns a geometry which was computed by layout for the child. This is generally a visible region of the child.
        /// Layout can compute automatic clip region when the child is larger then layout constraints or has ClipToBounds 
        /// property set. Note that because of LayoutTransform, this could be a non-rectangular geometry. While general geometry is somewhat 
        /// complex to operate with, it is possible to check if the Geometry returned is RectangularGeometry or, if not - use Geometry.Bounds
        /// property to get bounding box of the visible portion of the element.
        /// </summary>
        public static Geometry GetLayoutClip(FrameworkElement element)
        {
            CheckArgument(element);
            return element.GetLayoutClipInternal();
        }

        /// <summary>
        /// Returns a UIElement which was being processed by Layout Engine at the moment 
        /// an unhandled exception casued Layout Engine to abandon the operation and unwind.
        /// Returns non-null result only for a period of time before next layout update is 
        /// initiated. Can be examined from the application exception handler.
        /// </summary>
        /// <param name="dispatcher">The Dispatcher object that specifies the scope of operation. There is one Layout Engine per Dispatcher.</param>
        public static UIElement GetLayoutExceptionElement(Dispatcher dispatcher)
        {
            if(dispatcher == null)
                throw new ArgumentNullException("dispatcher");

            UIElement e = null;
            ContextLayoutManager lm = ContextLayoutManager.From(dispatcher);

            if(lm != null)
                e = lm.GetLastExceptionElement();

            return e;
        }
    }
}

