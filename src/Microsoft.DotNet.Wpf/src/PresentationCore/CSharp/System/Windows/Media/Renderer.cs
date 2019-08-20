// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      Though called "Renderer", this is used only for the 'rendering
//      to a BitmapImage' case, and not when rendering to an HwndTarget.
//

using System;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Threading;

using System.Diagnostics;

using MS.Internal;
using System.Windows.Media;
using System.Windows.Media.Composition;

using MS.Win32;
using System.Security;

namespace System.Windows.Media
{
    static internal class Renderer
    {
        static public void Render(
            IntPtr pRenderTarget,
            DUCE.Channel channel,
            Visual visual,
            int width,
            int height,
            double dpiX,
            double dpiY)
        {
            Render(pRenderTarget, channel, visual, width, height, dpiX, dpiY, Matrix.Identity, Rect.Empty);
        }

        /// <summary>
        /// If fRenderForBitmapEffect is true, the method calls special methods on visual
        /// to render it specifically for an effect to be applied to it. It excludes
        /// properties such as transform, clip, offset and guidelines.
        /// </summary>
        static internal void Render(
            IntPtr pRenderTarget,
            DUCE.Channel channel,
            Visual visual,
            int width,
            int height,
            double dpiX,
            double dpiY,
            Matrix worldTransform,
            Rect windowClip
            )
        {
            DUCE.Resource target =
                new DUCE.Resource();
            DUCE.Resource root =
                new DUCE.Resource();

            DUCE.ResourceHandle targetHandle = DUCE.ResourceHandle.Null;
            DUCE.ResourceHandle rootHandle = DUCE.ResourceHandle.Null;

            Matrix deviceTransform = new Matrix(
                dpiX * (1.0 / 96.0),    0,
                0,                      dpiY * (1.0 / 96.0),
                0,                      0);

            deviceTransform = worldTransform * deviceTransform;
            MatrixTransform mtDeviceTransform = new MatrixTransform(deviceTransform);

            DUCE.ResourceHandle deviceTransformHandle =
                ((DUCE.IResource)mtDeviceTransform).AddRefOnChannel(channel);


            try
            {
                // ------------------------------------------------------------
                //   Create the composition target and root visual resources.

                target.CreateOrAddRefOnChannel(target, channel, DUCE.ResourceType.TYPE_GENERICRENDERTARGET);
                targetHandle = target.Handle;

                DUCE.CompositionTarget.PrintInitialize(
                    targetHandle,
                    pRenderTarget,
                    width,
                    height,
                    channel);

                root.CreateOrAddRefOnChannel(root, channel, DUCE.ResourceType.TYPE_VISUAL);
                rootHandle = root.Handle;

                DUCE.CompositionNode.SetTransform(
                    rootHandle,
                    deviceTransformHandle,
                    channel);

                DUCE.CompositionTarget.SetRoot(
                    targetHandle,
                    rootHandle,
                    channel);

                channel.CloseBatch();
                channel.Commit();


                // ------------------------------------------------------------
                //   Render the freshly created target.

                RenderContext renderContext = new RenderContext();

                renderContext.Initialize(channel, rootHandle);

                visual.Precompute();

                visual.Render(renderContext, 0);

                // ------------------------------------------------------------
                //   Flush the channel and present the composition target.

                channel.CloseBatch();
                channel.Commit();
                channel.Present();

                MediaContext mediaContext = MediaContext.CurrentMediaContext;
                mediaContext.NotifySyncChannelMessage(channel);
}
            finally
            {
                // ------------------------------------------------------------
                //   Clean up and release the root visual.

                if (!rootHandle.IsNull)
                {
                    DUCE.CompositionNode.RemoveAllChildren(
                        rootHandle,
                        channel);

                    ((DUCE.IResource)visual).ReleaseOnChannel(channel);

                    root.ReleaseOnChannel(channel);
                }


                // ------------------------------------------------------------
                //   Release the world transform.

                if (!deviceTransformHandle.IsNull)
                {
                    ((DUCE.IResource)mtDeviceTransform).ReleaseOnChannel(channel);
                }


                // ------------------------------------------------------------
                //   Clean up and release the composition target.

                if (!targetHandle.IsNull)
                {
                    DUCE.CompositionTarget.SetRoot(
                        targetHandle,
                        DUCE.ResourceHandle.Null,
                        channel);

                    target.ReleaseOnChannel(channel);
                }

                // ------------------------------------------------------------
                //   Flush the channel and present the composition target.

                channel.CloseBatch();
                channel.Commit();
                channel.Present();

                MediaContext mediaContext = MediaContext.CurrentMediaContext;
                mediaContext.NotifySyncChannelMessage(channel);
            }
        }
    }
}

