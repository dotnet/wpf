// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//  Microsoft Windows Presentation Foudnation
//

using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media.Media3D;
using System.Windows.Media.Composition;
using System.Windows.Media.Animation;
using System.Security;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    public partial class BitmapCacheBrush : Brush, ICyclicBrush
    {
        #region Constructors        
        public BitmapCacheBrush()
        {
}

        /// <summary>
        /// VisualBrush Constructor where the image is set to the parameter's value
        /// </summary>
        /// <param name="visual"> The Visual representing the contents of this Brush. </param>
        public BitmapCacheBrush(Visual visual)
        {
            if (this.Dispatcher != null)
            {
                MediaSystem.AssertSameContext(this, visual);
                Target = visual;
            }
        }
        #endregion Constructors

        private ContainerVisual AutoWrapVisual
        {
            get 
            { 
                // Lazily create the dummy visual instance.
                if (_dummyVisual == null)
                {
                    _dummyVisual = new ContainerVisual();
                }
                
                return _dummyVisual;
            }
        }

        // NOTE:  This class is basically identical to VisualBrush, it should be refactored to
        //        a common place to prevent code duplication (maybe Brush.cs?)
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
            Visual vVisual = InternalTarget;

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
                if ((e.Property == TargetProperty) || (e.Property == AutoLayoutContentProperty))
                {   
                    // Should we wrap the visual in a dummy visual node for rendering?
                    if (e.Property == TargetProperty && e.IsAValueChange)
                    {
                        if (AutoWrapTarget)
                        {
                            Debug.Assert(InternalTarget == AutoWrapVisual, 
                                "InternalTarget should point to our dummy visual AutoWrapVisual when AutoWrapTarget is true.");

                            // Change the value being wrapped by AutoWrapVisual.
                            AutoWrapVisual.Children.Remove((Visual)e.OldValue);
                            AutoWrapVisual.Children.Add((Visual)e.NewValue);
                        }
                        else
                        {
                            // Target just passes through to InternalTarget.
                            InternalTarget = Target;
                        }
                    }
                    
                    // Should we initiate a layout on this Visual?
                    // Yes, if AutoLayoutContent is true and if the Visual is a UIElement not already in
                    // another tree (i.e. the parent is null) or its not the hwnd root.
                    if (AutoLayoutContent)
                    {
                        Debug.Assert(!_pendingLayout);
                        UIElement element = Target as UIElement;
            
                        if ((element != null) 
                              && 
                              ((VisualTreeHelper.GetParent(element) == null && !(element.IsRootElement)) // element is not connected to visual tree, OR
                               || (VisualTreeHelper.GetParent(element) is Visual3D) // element is a 2D child of a 3D object, OR
                               || (VisualTreeHelper.GetParent(element) == InternalTarget))) // element is only connected to visual tree via our wrapper Visual
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
                else if (e.Property == AutoWrapTargetProperty)
                {
                    // If our AutoWrap behavior changed, wrap/unwrap the target here.
                    if (AutoWrapTarget)
                    {
                        InternalTarget = AutoWrapVisual;
                        AutoWrapVisual.Children.Add(Target);
                    }
                    else
                    {
                        AutoWrapVisual.Children.Remove(Target);
                        InternalTarget = Target;
                    }
                }
            }
        }

        /// <summary>
        /// We initiate the layout on the tree rooted at the Visual to which BitmapCacheBrush points.
        /// </summary>
        private void DoLayout(UIElement element)
        {
            Debug.Assert(element != null);

            DependencyObject parent = VisualTreeHelper.GetParent(element);
            if (!(element.IsRootElement)
                && (parent == null || parent is Visual3D || parent == InternalTarget))
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
        private void OnLayoutUpdated(object sender, EventArgs args)
        {
            Debug.Assert(_pendingLayout);

            // Target has to be a UIElement since the handler was added to it.
            UIElement element = (UIElement)Target;
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
        /// Enter is used for simple cycle detection in BitmapCacheBrush. If the method returns false
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
        /// Exits the BitmapCacheBrush. For more details see Enter method.
        /// </summary>
        internal void Exit()
        {
            Debug.Assert(_reentrancyFlag); // Exit must be matched with Enter. See Enter comments.
            _reentrancyFlag = false;
        }       

        private static object CoerceOpacity(DependencyObject d, object value)
        {
            if ((double)value != (double)OpacityProperty.GetDefaultValue(typeof(BitmapCacheBrush)))
            {
                throw new InvalidOperationException(SR.Get(SRID.BitmapCacheBrush_OpacityChanged));
            }
            return 1.0;
        }

        private static object CoerceTransform(DependencyObject d, object value)
        {
            if ((Transform)value != (Transform)TransformProperty.GetDefaultValue(typeof(BitmapCacheBrush)))
            {
                throw new InvalidOperationException(SR.Get(SRID.BitmapCacheBrush_TransformChanged));
            }
            return null;
        }

        private static object CoerceRelativeTransform(DependencyObject d, object value)
        {
            if ((Transform)value != (Transform)RelativeTransformProperty.GetDefaultValue(typeof(BitmapCacheBrush)))
            {
                throw new InvalidOperationException(SR.Get(SRID.BitmapCacheBrush_RelativeTransformChanged));
            }
            return null;
        }

        private static void StaticInitialize(Type typeofThis)
        {             
            OpacityProperty.OverrideMetadata(typeofThis, new IndependentlyAnimatedPropertyMetadata(1.0, /* PropertyChangedHandle */ null, CoerceOpacity));
            TransformProperty.OverrideMetadata(typeofThis, new UIPropertyMetadata(null, /* PropertyChangedHandle */ null, CoerceTransform));
            RelativeTransformProperty.OverrideMetadata(typeofThis, new UIPropertyMetadata(null, /* PropertyChangedHandle */ null, CoerceRelativeTransform));
        }

        private ContainerVisual _dummyVisual;
        
        private DispatcherOperation _DispatcherLayoutResult;
        private bool _pendingLayout;        
        private bool _reentrancyFlag;

        private bool _isAsyncRenderRegistered = false;
    }
}
