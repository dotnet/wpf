// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: This file contains the implementation of ImageBrush.
//              The ImageBrush is a TileBrush which defines its tile content
//              by use of an ImageSource.
//

using MS.Internal;
using MS.Internal.PresentationCore;
using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    /// <summary>
    /// ImageBrush - This TileBrush defines its content as an Image
    /// </summary>
    public sealed partial class ImageBrush : TileBrush
    {
        #region Constructors

        /// <summary>
        /// Default constructor for ImageBrush.  The resulting Brush has no content.
        /// </summary>
        public ImageBrush()
        {
            // We do this so that the property, when read, is consistent - not that
            // this will every actually affect drawing.
        }

        /// <summary>
        /// ImageBrush Constructor where the image is set to the parameter's value
        /// </summary>
        /// <param name="image"> The image source. </param>
        public ImageBrush(ImageSource image)
        {
            ImageSource = image;
        }

        #endregion Constructors

        #region Protected methods
        
        /// <summary>
        /// Obtains the current bounds of the brush's content
        /// </summary>
        /// <param name="contentBounds"> Output bounds of content </param>            
        protected override void GetContentBounds(out Rect contentBounds)
        {
            // Note, only implemented for DrawingImages.

            contentBounds = Rect.Empty;
            DrawingImage di = ImageSource as DrawingImage;
            if (di != null)
            {
                Drawing drawing = di.Drawing;
                if (drawing != null)
                {
                    contentBounds = drawing.Bounds;
                }
            }
        }

        #endregion Protected methods        
    }
}
