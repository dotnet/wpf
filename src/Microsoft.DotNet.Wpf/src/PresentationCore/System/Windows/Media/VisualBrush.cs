// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: This file contains the implementation of VisualBrush.
//              The VisualBrush is a TileBrush which defines its tile content
//              by use of a Visual.
//
//

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Threading;
using MS.Internal;

namespace System.Windows.Media 
{
    /// <summary>
    /// VisualBrush - This TileBrush defines its content as a Visual
    /// </summary>
    public sealed partial class VisualBrush : TileBrush, ICyclicBrush
    {
        #region Constructors

        /// <summary>
        /// Default constructor for VisualBrush.  The resulting Brush has no content.
        /// </summary>
        public VisualBrush()
        {
        }

        /// <summary>
        /// VisualBrush Constructor where the image is set to the parameter's value
        /// </summary>
        /// <param name="visual"> The Visual representing the contents of this Brush. </param>
        public VisualBrush(Visual visual)
        {
            if (this.Dispatcher != null)
            {
                MediaSystem.AssertSameContext(this, visual);
                Visual = visual;
            }
        }
        #endregion Constructors
        
        void ICyclicBrush.FireOnChanged()
        {
            // Simple loop detection to avoid stack overflow in cyclic VisualBrush
            // scenarios. This fix is only aimed at mitigating a very common 
            // VisualBrush scenario. 

            bool canEnter = Enter();

            if (canEnter)
            {
                try
                {
                    _isCacheDirty = true;
                    FireChanged();

                    // Register brush's visual tree for Render().
                    RegisterForAsyncRenderForCyclicBrush();
                }
                finally
                {
                    Exit();
                }
            }
        } 

        /// <summary> 
        /// Calling this will make sure that the render request
        /// is registered with the MediaContext.
        /// </summary>
        private void RegisterForAsyncRenderForCyclicBrush()
        {
            DUCE.IResource resource = this as DUCE.IResource;

            if (resource != null)
            {
                if ((Dispatcher != null) && !_isAsyncRenderRegistered)
                {
                    MediaContext mediaContext = MediaContext.From(Dispatcher);

                    //
                    // Only register for a deferred render if this visual brush
                    // is actually on the channel.
                    //
                    if (!resource.GetHandle(mediaContext.Channel).IsNull)
                    {
                        // Add this handler to this event means that the handler will be
                        // called on the next UIThread render for this Dispatcher.
                        ICyclicBrush cyclicBrush = this as ICyclicBrush;
                        mediaContext.ResourcesUpdated += new MediaContext.ResourcesUpdatedHandler(cyclicBrush.RenderForCyclicBrush);
                        _isAsyncRenderRegistered = true;
                    }
                }
            }
        }

       void ICyclicBrush.RenderForCyclicBrush(DUCE.Channel channel, bool skipChannelCheck)
       {
            Visual vVisual = Visual;

            // The Visual may have been registered for an asynchronous render, but may have been
            // disconnected from the VisualBrush since then.  If so, don't bother to render here, if
            // the Visual is visible it will be rendered elsewhere.
            if (vVisual != null && vVisual.CheckFlagsAnd(VisualFlags.NodeIsCyclicBrushRoot))
            {
                // ------------------------------------------------------------------------------------
                // 1) Prepare the visual for rendering.
                //
                // Updates bounding boxes.
                //

                vVisual.Precompute();


                // ------------------------------------------------------------------------------------
                // 2) Prepare the render context.
                //

                RenderContext rc = new RenderContext();

                rc.Initialize(channel, DUCE.ResourceHandle.Null);


                // ------------------------------------------------------------------------------------
                // 3) Compile the scene.

                if (channel.IsConnected)
                {
                    vVisual.Render(rc, 0);
                }
                else
                {
                    // We can issue the release here instead of putting it in queue
                    // since we are already in Render walk.
                    ((DUCE.IResource)vVisual).ReleaseOnChannel(channel);
                }
            }

            _isAsyncRenderRegistered = false;
        }


        // Implement functions used to addref and release resources in codegen that need
        // to be specialized for Visual which doesn't implement DUCE.IResource
        internal void AddRefResource(Visual visual, DUCE.Channel channel)
        {
            if (visual != null)
            {
                visual.AddRefOnChannelForCyclicBrush(this, channel);
            }
        }

        internal void ReleaseResource(Visual visual, DUCE.Channel channel)
        {
            if (visual != null)
            {
                visual.ReleaseOnChannelForCyclicBrush(this, channel);
            }
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.DependencyObject.OnPropertyChanged">DependencyObject.OnPropertyInvalidated</see>.
        /// If the property is the Visual or the AutoLayoutContent property, we re-layout the Visual if 
        /// possible.
        /// </summary>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.IsAValueChange || e.IsASubPropertyChange)
            {
                if ((e.Property == VisualProperty) || (e.Property == AutoLayoutContentProperty))
                {
                    // Should we initiate a layout on this Visual?
                    // Yes, if AutoLayoutContent is true and if the Visual is a UIElement not already in
                    // another tree (i.e. the parent is null) or its not the hwnd root.
                    if (AutoLayoutContent)
                    {
                        Debug.Assert(!_pendingLayout);                    
                        UIElement element = Visual as UIElement;
            
                        if ((element != null) 
                              && 
                              ((VisualTreeHelper.GetParent(element) == null && !(element.IsRootElement)) 
                               || (VisualTreeHelper.GetParent(element) is Visual3D)))                            
                        {
                            //
                            // We need 2 ways of initiating layout on the VisualBrush root.
                            // 1. We add a handler such that when the layout is done for the
                            // main tree and LayoutUpdated is fired, then we do layout for the
                            // VisualBrush tree.
                            // However, this can fail in the case where the main tree is composed
                            // of just Visuals and never does layout nor fires LayoutUpdated. So
                            // we also need the following approach.
                            // 2. We do a BeginInvoke to start layout on the Visual. This approach 
                            // alone, also falls short in the scenario where if we are already in 
                            // MediaContext.DoWork() then we will do layout (for main tree), then look
                            // at Loaded callbacks, then render, and then finally the Dispather will 
                            // fire us for layout. So during loaded callbacks we would not have done
                            // layout on the VisualBrush tree.
                            //
                            // Depending upon which of the two layout passes comes first, we cancel
                            // the other layout pass.
                            //
                            element.LayoutUpdated += OnLayoutUpdated;
                            _DispatcherLayoutResult = Dispatcher.BeginInvoke(
                                DispatcherPriority.Normal,
                                new DispatcherOperationCallback(LayoutCallback),
                                element);
                            _pendingLayout = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// We initiate the layout on the tree rooted at the Visual to which VisualBrush points.
        /// </summary>
        private void DoLayout(UIElement element)
        {
            Debug.Assert(element != null);

            DependencyObject parent = VisualTreeHelper.GetParent(element);
            if (!(element.IsRootElement)
                && (parent == null || parent is Visual3D))
            {
                //
                // PropagateResumeLayout sets the LayoutSuspended flag to false if it were true.
                //
                UIElement.PropagateResumeLayout(null, element);
                element.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                element.Arrange(new Rect(element.DesiredSize));
            }
        }

        /// <summary>
        /// LayoutUpdate event handler.
        /// </summary>
        /// <param name="sender">event sender (not used)</param>
        /// <param name="args">event arguments (not used)</param>
        private void OnLayoutUpdated(object sender, EventArgs args)
        {
            Debug.Assert(_pendingLayout);

            // Visual has to be a UIElement since the handler was added to it.
            UIElement element = (UIElement)Visual;
            Debug.Assert(element != null);

            // Unregister for the event
            element.LayoutUpdated -= OnLayoutUpdated;
            _pendingLayout = false;

            //
            // Since we are in this function that means that layoutUpdated fired before 
            // Dispatcher.BeginInvoke fired. So we can abort the DispatcherOperation as
            // we will do the layout here.
            //
            Debug.Assert(_DispatcherLayoutResult != null);
            Debug.Assert(_DispatcherLayoutResult.Status == DispatcherOperationStatus.Pending);
            bool abortStatus = _DispatcherLayoutResult.Abort();
            Debug.Assert(abortStatus);

            DoLayout(element);
        }


        /// <summary>
        /// DispatcherOperation callback to initiate layout.
        /// </summary>
        /// <param name="arg">The Visual root</param>
        private object LayoutCallback(object arg)
        {
            Debug.Assert(_pendingLayout);

            UIElement element = arg as UIElement;
            Debug.Assert(element != null);

            //
            // Since we are in this function that means that Dispatcher.BeginInvoke fired
            // before LayoutUpdated fired. So we can remove the LayoutUpdated handler as
            // we will do the layout here.
            //
            element.LayoutUpdated -= OnLayoutUpdated;
            _pendingLayout = false;

            DoLayout(element);

            return null;
        }

        /// <summary>
        /// Enter is used for simple cycle detection in VisualBrush. If the method returns false
        /// the brush has already been entered and cannot be entered again. Matching invocation of Exit
        /// must be skipped if Enter returns false.
        /// </summary>
        internal bool Enter()
        {
            if (_reentrancyFlag)
            {
                return false;
            }
            else
            {
                _reentrancyFlag = true;
                return true;
            }
        }

        /// <summary>
        /// Exits the VisualBrush. For more details see Enter method.
        /// </summary>
        internal void Exit()
        {
            Debug.Assert(_reentrancyFlag); // Exit must be matched with Enter. See Enter comments.
            _reentrancyFlag = false;
        }       

        /// <summary>
        /// Obtains the current bounds of the brush's content
        /// </summary>
        /// <param name="contentBounds"> Output bounds of content</param>  
        protected override void GetContentBounds(out Rect contentBounds)
        {
            // Obtain the current visual's outer space bounding box. We return the outer space
            // bounding box because we want to have the bounding box of the Visual tree including 
            // transform/offset at the root.
            if (_isCacheDirty)
            {
                _bbox = Visual.CalculateSubgraphBoundsOuterSpace();
                _isCacheDirty = false;
            }

            contentBounds = _bbox;
        }  

        private DispatcherOperation _DispatcherLayoutResult;
        private bool _pendingLayout;        
        private bool _reentrancyFlag;

        private bool _isAsyncRenderRegistered = false;
        
        // Whether we need to re-calculate our content bounds.
        private bool _isCacheDirty = true;

        // Keep our content bounds cached.
        private Rect _bbox = Rect.Empty;
    }
}

