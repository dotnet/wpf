// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Windows;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;
using MS.Internal;
using System.Security;
using System.Windows.Media;
using System.Windows.Media.Composition;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using MS.Win32.PresentationCore;

namespace System.Windows.Media.Imaging
{
    #region public class BitmapVisualManager
    /// <summary>
    /// BitmapVisualManager holds state and context for a drawing a visual to an bitmap.
    /// </summary>
    internal class BitmapVisualManager : DispatcherObject
    {
        #region Constructors
        private BitmapVisualManager()
        {
        }

        /// <summary>
        /// Create an BitmapVisualManager for drawing a visual to the bitmap.
        /// </summary>
        /// <param name="bitmapTarget">Where the resulting bitmap is rendered</param>
        public BitmapVisualManager(RenderTargetBitmap bitmapTarget)
        {
            if (bitmapTarget == null)
            {
                throw new ArgumentNullException("bitmapTarget");
            }

            if (bitmapTarget.IsFrozen)
            {
                throw new ArgumentException(SR.Get(SRID.Image_CantBeFrozen, null));
            }

            _bitmapTarget = bitmapTarget;
        }

        #endregion

        #region Public methods
        /// <summary>
        /// Render visual to printer.
        /// </summary>
        /// <param name="visual">Root of the visual to render</param>
        public void Render(Visual visual)
        {
            Render(visual, Matrix.Identity, Rect.Empty);
        }

        /// <summary>
        /// Render visual to printer.
        /// </summary>
        /// <param name="visual">Root of the visual to render</param>
        /// <param name="worldTransform">World transform to apply to the root visual</param>
        /// <param name="windowClip">The window clip of the outermost window or Empty</param>
        /// <param name="fRenderForBitmapEffect">True if we are rendering the visual
        /// to apply an effect to it</param>
        /// 
        internal void Render(Visual visual, Matrix worldTransform, Rect windowClip)
        {
            if (visual == null)
            {
                throw new ArgumentNullException("visual");
            }

            // If the bitmapTarget we're writing to is frozen then we can't proceed.  Note that
            // it's possible for the BitmapVisualManager to be constructed with a mutable BitmapImage
            // and for the app to later freeze it.  Such an application is misbehaving if
            // they subsequently try to render to the BitmapImage.
            if (_bitmapTarget.IsFrozen)
            {
                throw new ArgumentException(SR.Get(SRID.Image_CantBeFrozen));
            }

            int sizeX = _bitmapTarget.PixelWidth;
            int sizeY = _bitmapTarget.PixelHeight;
            double dpiX = _bitmapTarget.DpiX;
            double dpiY = _bitmapTarget.DpiY;

            Debug.Assert ((sizeX > 0) && (sizeY > 0));
            Debug.Assert ((dpiX > 0) && (dpiY > 0));

            // validate the data
            if ((sizeX <= 0) || (sizeY <= 0))
            {
                return; // nothing to draw
            }

            if ((dpiX <= 0) || (dpiY <= 0))
            {
                dpiX = 96;
                dpiY = 96;
            }

            SafeMILHandle renderTargetBitmap = _bitmapTarget.MILRenderTarget;
            Debug.Assert (renderTargetBitmap != null, "Render Target is null");

            IntPtr pIRenderTargetBitmap = IntPtr.Zero;

            try
            {
                //
                // Allocate a fresh synchronous channel.
                //

                MediaContext mctx = MediaContext.CurrentMediaContext;
                DUCE.Channel channel = mctx.AllocateSyncChannel();


                //
                // Acquire the target bitmap.
                //

                Guid iidRTB = MILGuidData.IID_IMILRenderTargetBitmap;

                HRESULT.Check(UnsafeNativeMethods.MILUnknown.QueryInterface(
                    renderTargetBitmap,
                    ref iidRTB,
                    out pIRenderTargetBitmap));


                //
                // Render the visual on the synchronous channel.
                //

                Renderer.Render(
                    pIRenderTargetBitmap,
                    channel,
                    visual,
                    sizeX,
                    sizeY,
                    dpiX,
                    dpiY,
                    worldTransform,                    
                    windowClip);

                //
                // Release the synchronous channel. This way we can
                // re-use that channel later.
                //

                mctx.ReleaseSyncChannel(channel);
            }
            finally
            {
                UnsafeNativeMethods.MILUnknown.ReleaseInterface(ref pIRenderTargetBitmap);
            }

            _bitmapTarget.RenderTargetContentsChanged();
        }
        #endregion

        #region Member Variables
        private RenderTargetBitmap _bitmapTarget = null;
        #endregion
    }
    #endregion
}




