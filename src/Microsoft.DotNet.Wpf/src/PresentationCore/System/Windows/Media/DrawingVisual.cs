// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Windows.Threading;

using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Diagnostics;
using System.Collections.Generic;
using MS.Internal;
using MS.Win32;
using System.Resources;
using System.Runtime.InteropServices;

namespace System.Windows.Media
{
    /// <summary>
    /// A DrawingVisual is a Visual that can be used to render Vector graphics on the screen.
    /// The content is persistet by the System.
    /// </summary>
    public class DrawingVisual : ContainerVisual
    {
        // bbox in inner coordinate space. Note that this bbox does not
        // contain the childrens extent.
        IDrawingContent _content;

        /// <summary>
        /// HitTestCore implements precise hit testing against render contents
        /// </summary>
        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            if (hitTestParameters == null)
            {
                throw new ArgumentNullException("hitTestParameters");
            }

            if (_content != null)
            {                
                if (_content.HitTestPoint(hitTestParameters.HitPoint))
                {
                    return new PointHitTestResult(this, hitTestParameters.HitPoint);
                }
            }

            return null;
        }

        /// <summary>
        /// HitTestCore implements precise hit testing against render contents
        /// </summary>
        protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
        {                   
            if (hitTestParameters == null)
            {
                throw new ArgumentNullException("hitTestParameters");
            }

            if ((_content != null) && GetHitTestBounds().IntersectsWith(hitTestParameters.Bounds))
            {                 
                IntersectionDetail intersectionDetail;

                intersectionDetail = _content.HitTestGeometry(hitTestParameters.InternalHitGeometry);
                Debug.Assert(intersectionDetail != IntersectionDetail.NotCalculated);

                if (intersectionDetail != IntersectionDetail.Empty)
                {
                    return new GeometryHitTestResult(this, intersectionDetail);
                }
}

            return null;
        }


        /// <summary>
        /// Opens the DrawingVisual for rendering. The returned DrawingContext can be used to
        /// render into the DrawingVisual.
        /// </summary>
        public DrawingContext RenderOpen()
        {
            VerifyAPIReadWrite();

            return new VisualDrawingContext(this);
        }

        /// <summary>
        /// Called from the DrawingContext when the DrawingContext is closed.
        /// </summary>
        internal override void RenderClose(IDrawingContent newContent)
        {
            IDrawingContent oldContent;

            //
            // First cleanup the old content and the state associate with this node
            // related to it's content.
            //

            oldContent = _content;
            _content = null;

            if (oldContent != null)
            {
                //
                // Remove the notification handlers.
                //

                oldContent.PropagateChangedHandler(ContentsChangedHandler, false /* remove */);


                //
                // Disconnect the old content from this visual.
                //

                DisconnectAttachedResource(
                    VisualProxyFlags.IsContentConnected,
                    ((DUCE.IResource)oldContent));
            }


            //
            // Prepare the new content.
            // 

            if (newContent != null)
            {
                // Propagate notification handlers.
                newContent.PropagateChangedHandler(ContentsChangedHandler, true /* adding */);                
            }

            _content = newContent;


            //
            // Mark the visual dirty on all channels and propagate 
            // the flags up the parent chain.
            //

            SetFlagsOnAllChannels(true, VisualProxyFlags.IsContentDirty);

            PropagateFlags(
                this,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.IsSubtreeDirtyForRender);
}

        /// <summary>
        /// Overriding this function to release DUCE resources during Dispose and during removal of a subtree.
        /// </summary>
        internal override void FreeContent(DUCE.Channel channel)
        {
            Debug.Assert(_proxy.IsOnChannel(channel));
            
            if (_content != null)
            {
                if (CheckFlagsAnd(channel, VisualProxyFlags.IsContentConnected))
                {
                    DUCE.CompositionNode.SetContent(
                        _proxy.GetHandle(channel), 
                        DUCE.ResourceHandle.Null, 
                        channel);

                    SetFlags(
                        channel, 
                        false, 
                        VisualProxyFlags.IsContentConnected);

                    ((DUCE.IResource)_content).ReleaseOnChannel(channel);
                }
            }

            // Call the base method too
            base.FreeContent(channel);
        }

        /// <summary>
        /// Returns the bounding box of the content.
        /// </summary>
        internal override Rect GetContentBounds()
        {            
            if (_content != null)
            {
                Rect resultRect = Rect.Empty;
                MediaContext mediaContext = MediaContext.From(Dispatcher);                
                BoundsDrawingContextWalker ctx = mediaContext.AcquireBoundsDrawingContextWalker();

                resultRect = _content.GetContentBounds(ctx);
                mediaContext.ReleaseBoundsDrawingContextWalker(ctx);
                
                return resultRect;
            }
            else
            {
                return Rect.Empty;
            }
        }

        /// <summary>
        /// WalkContent - method which walks the content (if present) and calls out to the
        /// supplied DrawingContextWalker.
        /// </summary>
        /// <param name="walker">
        ///   DrawingContextWalker - the target of the calls which occur during
        ///   the content walk.
        /// </param>
        internal void WalkContent(DrawingContextWalker walker)
        {
            VerifyAPIReadOnly();

            if (_content != null)
            {
                _content.WalkContent(walker);
            }
        }

        /// <summary>
        /// RenderContent is implemented by derived classes to hook up their
        /// content. The implementer of this function can assert that the _hCompNode
        /// is valid on a channel when the function is executed.
        /// </summary>
        internal override void RenderContent(RenderContext ctx, bool isOnChannel)
        {
            DUCE.Channel channel = ctx.Channel;

            Debug.Assert(!CheckFlagsAnd(channel, VisualProxyFlags.IsContentConnected));
            Debug.Assert(_proxy.IsOnChannel(channel));


            //
            // Create the content on the channel.
            //

            if (_content != null)
            {
                DUCE.CompositionNode.SetContent(
                    _proxy.GetHandle(channel),
                    ((DUCE.IResource)_content).AddRefOnChannel(channel),
                    channel);

                SetFlags(
                    channel, 
                    true, 
                    VisualProxyFlags.IsContentConnected);
            }
            else if (isOnChannel) /*_content == null*/
            {
                DUCE.CompositionNode.SetContent(
                    _proxy.GetHandle(channel),
                    DUCE.ResourceHandle.Null,
                    channel);
            }
        }
        
        /// <summary>
        /// GetDrawing - Returns the drawing content of this Visual.  
        /// </summary>
        /// <remarks>
        /// Changes to this DrawingGroup will not be propagated to the Visual's content.
        /// This method is called by both the Drawing property, and VisualTreeHelper.GetDrawing()
        /// </remarks>        
        internal override DrawingGroup GetDrawing()
        {
            // Need to determine if Visual.Drawing should return mutable content    

            VerifyAPIReadOnly();

            DrawingGroup drawingGroupContent = null;                  

            // Convert our content to a DrawingGroup, if content exists
            if (_content != null)
            {
                drawingGroupContent = DrawingServices.DrawingGroupFromRenderData((RenderData) _content);
            }

            return drawingGroupContent;
        }                  

        /// <summary>
        /// Drawing - Returns the drawing content of this Visual.  
        /// </summary>
        /// <remarks>
        /// Changes to this DrawingGroup will not propagated to the Visual's content.
        /// </remarks>
        public DrawingGroup Drawing
        {
            get
            {
                return GetDrawing();
            }                
        }  
    }
}

