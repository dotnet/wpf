// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: DrawingImage class
//                  An ImageSource with a Drawing for content
//
//
//

using MS.Internal;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;

namespace System.Windows.Media
{
    /// <summary>
    /// The class definition for DrawingImage
    /// </summary>
    public sealed partial class DrawingImage : ImageSource
    {
        /// <summary>
        /// Default DrawingImage ctor
        /// </summary>
        public DrawingImage()
        {
        }

        /// <summary>
        /// DrawingImage ctor that takes a Drawing
        /// </summary>
        /// <param name="drawing">The content of the DrawingImage</param>
        public DrawingImage(Drawing drawing)
        {
            Drawing = drawing;
        }

        /// <summary>
        /// Width of the DrawingImage
        /// </summary>
        public override double Width
        {
            get
            {
                ReadPreamble();

                return Size.Width;
            }
        }

        /// <summary>
        /// Height of the DrawingImage
        /// </summary>
        public override double Height
        {
            get
            {
                ReadPreamble();

                return Size.Height;
            }
        }

        /// <summary>
        /// Get the Metadata of the DrawingImage
        /// </summary>
        public override ImageMetadata Metadata
        {
            get
            {
                ReadPreamble();

                // DrawingImage does not have any metadata currently defined.
                return null;
            }
        }

        /// <summary>
        /// Size for the DrawingImage
        /// </summary>
        internal override Size Size
        {
            get
            {
                Drawing drawing = Drawing;

                if (drawing != null)
                {
                    Size size = drawing.GetBounds().Size;

                    if (!size.IsEmpty)
                    {
                        return size;
                    }
                    else
                    {
                        return new Size();
                    }
                }
                else
                {
                    return new Size();
                }
            }
        }
}
}
