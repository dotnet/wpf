// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      Defines a node in the composition scene graph.
//

using System;
using System.Security;
using System.Windows.Threading;
using MS.Win32;
using System.Windows.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Effects;
using System.Diagnostics;
using System.Collections;
using System.Windows.Interop;
using System.Collections.Generic;
using MS.Internal;
using MS.Internal.Media;
using MS.Internal.Media3D;
using System.Resources;
using MS.Utility;
using System.Runtime.InteropServices;
using MS.Internal.PresentationCore;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

//------------------------------------------------------------------------------
// This section lists various things that we could improve on the Visual class.
//
// - (Pail) Don't allocate a managed Pail object.
// - (Finalizer) Currently we delete the cob explicitly when a node is removed from
//   the scene graph. However, we don't do this when we remove the root node.
//   currently this is done by the finalizer. If we clean explicitly up when we
//   remove the root node we won't need a finalizer.
//------------------------------------------------------------------------------


//------------------------------------------------------------------------------
// PUBLIC API EXPOSURE RULES
//
// If you expose a public/protected API you need to check a couple things:
//
// A) Call the correct version of VerifyAPI.  This checks the following
//      1) That the calling thread has entered the context of this object
//      2) That the current object is not disposed.
//      3) If another object is passed in, that it has the same
//         context affinity as this object.  This should be used for
//         arguments to the API
//      4) That the current permissions are acceptable.
//
// B) That other arguments are not disposed if needed.
//
//
//------------------------------------------------------------------------------

namespace System.Windows.Media
{
    // this class is used to wrap the Map struct into an object so
    // that we can use it with the UncommonField infrastructure.
    internal class MapClass
    {
        internal MapClass()
        {
            _map_ofBrushes = new DUCE.Map<bool>();
        }

        internal bool IsEmpty
        {
            get
            {
                return _map_ofBrushes.IsEmpty();
            }
        }

        public DUCE.Map<bool> _map_ofBrushes;
    }

    /// <summary>
    /// The Visual class is the base class for all Visual types. It provides
    /// services and properties that all Visuals have in common. Services include
    /// hit-testing, coordinate transformation, bounding box calculations. Properties
    /// are for example a transform property and an opacity property.
    ///
    /// Derived Visuals render their content first and then render the children, or in other
    /// words, the content of a Visual is always behind the content of its children.
    /// </summary>
    public abstract partial class Visual : DependencyObject, DUCE.IResource
    {
        // --------------------------------------------------------------------
        //
        //   Constants
        //
        // --------------------------------------------------------------------

        #region Constants

        /// <summary>
        /// This is the dirty mask for a visual, set every time we marshall
        /// a visual to a channel and reset by the end of the render pass.
        /// </summary>
        private const VisualProxyFlags c_ProxyFlagsDirtyMask =
              VisualProxyFlags.IsSubtreeDirtyForRender
            | VisualProxyFlags.IsContentDirty
            | VisualProxyFlags.IsTransformDirty
            | VisualProxyFlags.IsGuidelineCollectionDirty
            | VisualProxyFlags.IsClipDirty
            | VisualProxyFlags.IsOpacityDirty
            | VisualProxyFlags.IsOpacityMaskDirty
            | VisualProxyFlags.IsOffsetDirty
            | VisualProxyFlags.IsEdgeModeDirty
            | VisualProxyFlags.IsEffectDirty
            | VisualProxyFlags.IsBitmapScalingModeDirty
            | VisualProxyFlags.IsScrollableAreaClipDirty
            | VisualProxyFlags.IsClearTypeHintDirty
            | VisualProxyFlags.IsCacheModeDirty
            | VisualProxyFlags.IsTextRenderingModeDirty
            | VisualProxyFlags.IsTextHintingModeDirty;


        /// <summary>
        /// This is the dirty mask for a visual, set every time we marshall
        /// a visual to a channel and reset by the end of the render pass.
        ///
        /// This mask is only for Viewport3D visual, since the contents
        /// of the Viewport3D are rendered during RenderContent, we
        /// need to set those flags as dirty if the visual is not on
        /// channel yet
        /// </summary>
        private const VisualProxyFlags c_Viewport3DProxyFlagsDirtyMask =
              VisualProxyFlags.Viewport3DVisual_IsCameraDirty
            | VisualProxyFlags.Viewport3DVisual_IsViewportDirty;

        #endregion Constants



        // --------------------------------------------------------------------
        //
        //   Internal Constructor
        //
        // --------------------------------------------------------------------

        #region Internal Constructor

        /// <summary>
        /// This internal ctor is a hook to allow Visual subclasses
        /// to create their unique type of a visual resource.
        /// </summary>
        internal Visual(DUCE.ResourceType resourceType)
        {
#if DEBUG
            _parentIndex = -1;
#endif

            switch (resourceType)
            {
            case DUCE.ResourceType.TYPE_VISUAL:
                // Default setting
                break;

            case DUCE.ResourceType.TYPE_VIEWPORT3DVISUAL:
                SetFlags(true, VisualFlags.IsViewport3DVisual);
                break;

            default:
                Debug.Assert(false, "TYPE_VISUAL or TYPE_VIEWPORT3DVISUAL expected.");
                break;
            }
        }

        #endregion Protected Constructor


        // --------------------------------------------------------------------
        //
        //   Protected Constructor
        //
        // --------------------------------------------------------------------

        #region Protected Constructor

        /// <summary>
        /// Ctor Visual
        /// </summary>
        protected Visual() : this(DUCE.ResourceType.TYPE_VISUAL)
        {
        }

        #endregion Protected Constructor


        // --------------------------------------------------------------------
        //
        //   IResource implementation
        //
        // --------------------------------------------------------------------

        #region IResource implementation

        /// <summary>
        /// This is used to check if the composition node
        /// for the visual is on channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        internal bool IsOnChannel(DUCE.Channel channel)
        {
            return _proxy.IsOnChannel(channel);
        }

        /// <summary>
        /// Returns the handle this visual has on the given channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        DUCE.ResourceHandle DUCE.IResource.GetHandle(DUCE.Channel channel)
        {
            return _proxy.GetHandle(channel);
        }

        /// <summary>
        /// Only Viewport3DVisual and Visual3D implements this.
        /// Vieport3DVisual has two handles. One stored in _proxy
        /// and the other one stored in _proxy3D. This function returns
        /// the handle stored in _proxy3D.
        /// </summary>
        DUCE.ResourceHandle DUCE.IResource.Get3DHandle(DUCE.Channel channel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This is used to create or addref the visual resource
        /// on the given channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        DUCE.ResourceHandle DUCE.IResource.AddRefOnChannel(DUCE.Channel channel)
        {
            return AddRefOnChannelCore(channel);
        }

        internal virtual DUCE.ResourceHandle AddRefOnChannelCore(DUCE.Channel channel)
        {
            DUCE.ResourceType resourceType = DUCE.ResourceType.TYPE_VISUAL;

            if (CheckFlagsAnd(VisualFlags.IsViewport3DVisual))
            {
                resourceType = DUCE.ResourceType.TYPE_VIEWPORT3DVISUAL;
            }

            _proxy.CreateOrAddRefOnChannel(this, channel, resourceType);

            return _proxy.GetHandle(channel);
        }

        /// <summary>
        /// this is used to release the comp node of the visual
        /// on the given channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        internal virtual void ReleaseOnChannelCore(DUCE.Channel channel)
        {
            _proxy.ReleaseOnChannel(channel);
        }


        /// <summary>
        /// Sends a command to compositor to remove the child
        /// from its parent on the channel.
        /// </summary>
        void DUCE.IResource.RemoveChildFromParent(
                DUCE.IResource parent,
                DUCE.Channel channel)
        {
            DUCE.CompositionNode.RemoveChild(
                parent.GetHandle(channel),
                _proxy.GetHandle(channel),
                channel);
        }

        int DUCE.IResource.GetChannelCount()
        {
            return _proxy.Count;
        }

        DUCE.Channel DUCE.IResource.GetChannel(int index)
        {
            return _proxy.GetChannel(index);
        }

        #endregion IResource implementation


        // --------------------------------------------------------------------
        //
        //   IElement implementation
        //
        // --------------------------------------------------------------------

        // --------------------------------------------------------------------
        //
        //   Internal Properties
        //
        // --------------------------------------------------------------------

        #region Internal Properties

        // Are we in the process of iterating the visual children.
        // This flag is set during a descendents walk, for property invalidation.
        internal bool IsVisualChildrenIterationInProgress
        {
            [FriendAccessAllowed]
            get { return CheckFlagsAnd(VisualFlags.IsVisualChildrenIterationInProgress); }

            [FriendAccessAllowed]
            set { SetFlags(value, VisualFlags.IsVisualChildrenIterationInProgress); }
        }


        /// <summary>
        /// The CompositionTarget marks the root element. The root element is responsible
        /// for posting renders.
        /// </summary>
        /// <remarks>
        /// The property getter is also used to ensure that the Visual is
        /// not used in multiple CompositionTargets.
        /// </remarks>
        internal bool IsRootElement
        {
            get
            {
                return CheckFlagsAnd(VisualFlags.ShouldPostRender);
            }

            set
            {
                SetFlags(value, VisualFlags.ShouldPostRender);
            }
        }
        #endregion Internal Properties



        // --------------------------------------------------------------------
        //
        //   Visual Content
        //
        // --------------------------------------------------------------------

        #region Visual Content

        /// <summary>
        /// Derived classes must override this method and return the bounding
        /// box of their content.
        /// </summary>
        internal virtual Rect GetContentBounds()
        {
            return Rect.Empty;
        }


        /// <summary>
        /// RenderContent is implemented by derived classes to hook up their
        /// content. The implementer of this function can assert that the
        /// visual is marshaled on the current channel when the function
        /// is executed.
        /// </summary>
        internal virtual void RenderContent(RenderContext ctx, bool isOnChannel)
        {
            /* do nothing */
        }

        /// <summary>
        /// This method is overrided on Visuals that can instantiate IDrawingContext
        /// Currently, DrawingVisual and UIElement
        /// </summary>
        internal virtual void RenderClose(IDrawingContent newContent)
        {
        }

        /// <summary>
        /// VisualContentBounds returns the bounding box for the contents of the current visual.
        /// </summary>
        internal Rect VisualContentBounds
        {
            get
            {
                // Probably too restrictive. Let's see who wants it during OnRender.
                VerifyAPIReadWrite();

                return GetContentBounds();
            }
        }


        /// <summary>
        /// VisualDescendantBounds returns the union of all of the content bounding
        /// boxes for all of the descendants of the current visual,and also including
        /// the contents of the current visual. So we end up with the
        /// bounds of the whole sub-graph in inner space.
        /// </summary>
        internal Rect VisualDescendantBounds
        {
            get
            {
                // Probably too restrictive. Let's see who wants it during OnRender.
                VerifyAPIReadWrite();

                Rect bboxSubgraph = CalculateSubgraphBoundsInnerSpace();

                // If bounding box has NaN, then we set the bounding box to infinity.
                if (DoubleUtil.RectHasNaN(bboxSubgraph))
                {
                    bboxSubgraph.X = Double.NegativeInfinity;
                    bboxSubgraph.Y = Double.NegativeInfinity;
                    bboxSubgraph.Width = Double.PositiveInfinity;
                    bboxSubgraph.Height = Double.PositiveInfinity;
                }
                return bboxSubgraph;
            }
        }


        /// <summary>
        /// Computes the union of all content bounding boxes of this Visual's sub-graph
        /// in inner space. Note that the result includes the root Visual's content.
        ///
        /// Definition: Outer/Inner Space:
        ///
        ///      A Visual has a set of properties which include clip, transform, offset
        ///      and bitmap effect. Those properties affect in which space (coordinate
        ///      clip space) a Visual's vector graphics and sub-graph is interpreted.
        ///      Inner space is the space before applying any of the properties. Outer
        ///      space is the space where all the properties have been taken into account.
        ///      For example if the Visual draws a rectangle {0, 0, 100, 100} and the
        ///      Offset property is set to {20, 20} and the clip is set to {40, 40, 10, 10}
        ///      then the bounding box of the Visual in inner space is {0, 0, 100, 100} and
        ///      in outer space {60, 60, 10, 10} (start out with the bbox of {0, 0, 100, 100}
        ///      then apply the clip {40, 40, 10, 10} which leaves us with a bbox of
        ///      {40, 40, 10, 10} and finally apply the offset and we end up with a bbox
        ///      of {60, 60, 10, 10}
        /// </summary>
        internal Rect CalculateSubgraphBoundsInnerSpace()
        {
            return CalculateSubgraphBoundsInnerSpace(false);
        }

        /// <summary>
        /// Computes the union of all rendering bounding boxes of this Visual's sub-graph
        /// in inner space. Note that the result includes the root Visual's content.
        /// </summary>
        internal Rect CalculateSubgraphRenderBoundsInnerSpace()
        {
            return CalculateSubgraphBoundsInnerSpace(true);
        }

        /// <summary>
        /// Same as the parameterless CalculateSubgraphBoundsInnerSpace except it takes a
        /// boolean indicating whether or not to calculate the rendering bounds.
        /// If the renderBounds parameter is set to true then the render bounds are returned.
        /// The render bounds differ in that they treat zero area bounds as emtpy rectangles.
        ///
        /// 
        /// This is needed since MIL and the managed size differ about how big content bounds are
        /// WPF considers geometric bounds (i.e. it will union in points) while MIL considers anything
        /// with zero area to be empty.  This poses problems when looking for the exact size of a
        /// CyclicBrush.
        /// </summary>
        internal virtual Rect CalculateSubgraphBoundsInnerSpace(bool renderBounds)
        {
            Rect bboxSubgraph = Rect.Empty;

            // Recursively calculate sub-graph bounds of children of this node. We get
            // the bounds of each child Visual in outer space which is this Visual's
            // inner space and union them together.

            int count = VisualChildrenCount;

            for (int i = 0; i < count; i++)
            {
                Visual child = GetVisualChild(i);
                if (child != null)
                {
                    Rect bboxSubgraphChild = child.CalculateSubgraphBoundsOuterSpace(renderBounds);

                    bboxSubgraph.Union(bboxSubgraphChild);
                }
            }

            // Get the content bounds of the Visual.  In the case that we're interested in render
            // bounds (i.e. what MIL will consider the size of the object), we set 0 area rects
            // to be empty so that they don't union to create larger sized rects.
            Rect contentBounds = GetContentBounds();
            if (renderBounds && IsEmptyRenderBounds(ref contentBounds /* ref for perf - not modified */))
            {
                contentBounds = Rect.Empty;
            }

            // Union the content bounds to the sub-graph bounds so that we end up with the
            // bounds of the whole sub-graph in inner space and return it.
            bboxSubgraph.Union(contentBounds);

            return bboxSubgraph;
        }



        /// <summary>
        /// Computes the union of all content bounding boxes of this Visual's sub-graph
        /// in outer space. Note that the result includes the root Visual's content.
        /// For a definition of outer/inner space see CalculateSubgraphBoundsInnerSpace.
        /// </summary>
        internal Rect CalculateSubgraphBoundsOuterSpace()
        {
            return CalculateSubgraphBoundsOuterSpace(false /* renderBounds */);
        }

        /// <summary>
        /// Computes the union of all rendering bounding boxes of this Visual's sub-graph
        /// in outer space. Note that the result includes the root Visual's content.
        /// For a definition of outer/inner space see CalculateSubgraphBoundsInnerSpace.
        /// </summary>
        internal Rect CalculateSubgraphRenderBoundsOuterSpace()
        {
            return CalculateSubgraphBoundsOuterSpace(true /* renderBounds */);
        }

        /// <summary>
        /// Same as the parameterless CalculateSubgraphBoundsOuterSpace except it takes a
        /// boolean indicating whether or not to calculate the rendering bounds.
        /// If the renderBounds parameter is set to true then the render bounds are returned.
        /// The render bounds differ in that they treat zero area bounds as emtpy rectangles.
        ///
        /// 
        /// This is needed since MIL and the managed size differ about how big content bounds are
        /// WPF considers geometric bounds (i.e. it will union in points) while MIL considers anything
        /// with zero area to be empty.  This poses problems when looking for the exact size of a
        /// CyclicBrush.
        /// </summary>
        private Rect CalculateSubgraphBoundsOuterSpace(bool renderBounds)
        {
            Rect bboxSubgraph = Rect.Empty;

            // Get the inner space bounding box of this node and then transform it into outer
            // space.

            bboxSubgraph = CalculateSubgraphBoundsInnerSpace(renderBounds);

            // Apply Effect

            if (CheckFlagsAnd(VisualFlags.NodeHasEffect))
            {
                Rect effectBounds;

                Effect effect = EffectField.GetValue(this);
                if (effect != null)
                {
                    // The Effect always deals in unit bounds, so transform the
                    // unit rect and then map back into the world space bounds
                    // defined by bboxSubgraph.

                    Rect unitBounds = new Rect(0,0,1,1);
                    Rect unitTransformedBounds = effect.EffectMapping.TransformBounds(unitBounds);
                    effectBounds = Effect.UnitToWorld(unitTransformedBounds, bboxSubgraph);

                    bboxSubgraph.Union(effectBounds);
                }
                else
                {
                    Debug.Assert(BitmapEffectStateField.GetValue(this) != null);
                    // BitmapEffects are deprecated so they no longer affect bounds.
                }
            }

            // Apply Clip.

            Geometry clip = ClipField.GetValue(this);
            if (clip != null)
            {
                bboxSubgraph.Intersect(clip.Bounds);
            }

            // Apply Transform.
            Transform transform = TransformField.GetValue(this);

            if ((transform != null) && (!transform.IsIdentity))
            {
                Matrix m = transform.Value;
                MatrixUtil.TransformRect(ref bboxSubgraph, ref m);
            }

            // Apply Offset.
            if (!bboxSubgraph.IsEmpty)
            {
                bboxSubgraph.X += _offset.X;
                bboxSubgraph.Y += _offset.Y;
            }

            // Apply scrollable-area clip.
            Rect? scrollClip = ScrollableAreaClipField.GetValue(this);
            if (scrollClip.HasValue)
            {
                bboxSubgraph.Intersect(scrollClip.Value);
            }

            // If bounding box has NaN, then we set the bounding box to infinity.
            if (DoubleUtil.RectHasNaN(bboxSubgraph))
            {
                bboxSubgraph.X = Double.NegativeInfinity;
                bboxSubgraph.Y = Double.NegativeInfinity;
                bboxSubgraph.Width = Double.PositiveInfinity;
                bboxSubgraph.Height = Double.PositiveInfinity;
            }

            return bboxSubgraph;
        }

        /// <summary>
        /// This method returns true if the given WPF bounds will be considered
        /// empty in terms of rendering.  This is the case when the bounds describe
        /// a zero-area space.  bounds are passed by ref for speed but are not modified
        ///
        /// 
        /// See above CalculateSubgraphBounds* methods for more detail.  This helper method
        /// goes with them.
        /// </summary>
        private bool IsEmptyRenderBounds(ref Rect bounds)
        {
            return (bounds.Width <= 0 || bounds.Height <= 0);
        }

        #endregion Visual Content

        // --------------------------------------------------------------------
        //
        //   Resource Marshalling and Unmarshalling
        //
        // --------------------------------------------------------------------

        #region Resource Marshalling and Unmarshalling

        /// <summary>
        /// Override this function in derived classes to release unmanaged resources during Dispose
        /// and during removal of a subtree.
        /// </summary>
        internal virtual void FreeContent(DUCE.Channel channel)
        {
            Debug.Assert(IsOnChannel(channel));
            Debug.Assert(!CheckFlagsAnd(channel, VisualProxyFlags.IsContentNodeConnected));
        }

        /// <summary>
        /// Returns true if this is a root of a ICyclicBrush on the specified channel
        /// </summary>
        private bool IsCyclicBrushRootOnChannel(DUCE.Channel channel)
        {
            bool isCyclicBrushRootOnChannel = false;

            Dictionary<DUCE.Channel, int> channelsToCyclicBrushMap =
              ChannelsToCyclicBrushMapField.GetValue(this);

            if (channelsToCyclicBrushMap != null)
            {
                int references;

                if (channelsToCyclicBrushMap.TryGetValue(channel, out references))
                {
                    isCyclicBrushRootOnChannel = (references > 0);
                }
            }

            return isCyclicBrushRootOnChannel;
        }

        /// <summary>
        ///   Frees up resources in this visual's subtree.
        /// </summary>
        /// <param name="channel">
        ///   The channel to release the resources on.
        /// </param>
        void DUCE.IResource.ReleaseOnChannel(DUCE.Channel channel)
        {
            if (!IsOnChannel(channel)
                || CheckFlagsAnd(channel, VisualProxyFlags.IsDeleteResourceInProgress))
            {
                return;
            }

            // Set the flag to true to prevent re-entrancy.
            SetFlags(channel, true, VisualProxyFlags.IsDeleteResourceInProgress);

            try
            {
                // at this point the tree is not connected any more.
                SetFlags(channel, false, VisualProxyFlags.IsConnectedToParent);

                //
                // Before unmarshaling this visual and its subtree, check if there are any visual
                // brushes holding references to it. In such case, we want to keep this visual
                // in the marshaled state and wait for all the visual brushes to release their
                // references through ReleaseOnChannelForCyclicBrush.
                //

                //
                // RenderTargetBitmap and BitmapEffects use synchronous channels. If a
                // node on the synchronous channel is the root of a VisualBrush from another
                // channel, then the node will never be deleted. Instead we really need to
                // check if the node is the root of a VisualBrush _on the same channel_.
                // This check is more expensive so we'll leave the faster check first to avoid
                // the more expensive check which isn't necessary most of the time.
                //
                // If the node is the root of a VisualBrush and the VisualBrush is one of
                // the node's children then a cycle is created. All of the nodes on the
                // cycle will leak. On sync channels, this is particularly bad because
                // the user doesn't know about the sync channel and has no control over it.
                // We have a queue of sync channels that are reused and leaking can lead
                // to conflicts on channel reuse resulting in a crash.
                //
                // *** DANGER *** Fortunately, as of today, tree structure on a sync channel is
                // never manipulated. The tree gets built, the tree gets drawn, and the tree gets
                // released. Because of this, we can just always delete. In the future if that
                // changes, the isSynchronous check here will cause a problem. *** DANGER ***
                //


                if (   !CheckFlagsOr(VisualFlags.NodeIsCyclicBrushRoot)
                            // If we aren't a root of a CyclicBrush, then we aren't referenced
                            // at all and we can go away
                    || !channel.IsConnected
                            // If the channel isn't connected, there's no reason to keep things alive
                    || channel.IsSynchronous
                            // If the channel is synchronous, the node isn't going to stick around
                            // so just delete it. *** THIS IS DANGEROUS ***. See above for
                            // more comments.
                    || !IsCyclicBrushRootOnChannel(channel)
                            // If we got to here, we are the root of a VisualBrush. We can go away
                            // only if the VB is on a different channel. This check is more expensive
                            // and not very common so we put it last.
                       )
                {
                    FreeContent(channel);

                    // Free dependent DUCE resources.
                    //
                    // We don't need to free the dependent resources if they're
                    // marked as dirty because when the flag is set, we also
                    // disconnect the resource from the visual resource.

                    Transform transform = TransformField.GetValue(this);
                    if ((transform != null)
                        && (!CheckFlagsAnd(channel, VisualProxyFlags.IsTransformDirty)))
                    {
                        //
                        // Note that in this particular case, the transform is not
                        // really dirty. Namely because the visual is not marshalled.
                        //

                        ((DUCE.IResource)transform).ReleaseOnChannel(channel);
                    }

                    Effect effect = EffectField.GetValue(this);
                    if ((effect != null)
                        && (!CheckFlagsAnd(channel, VisualProxyFlags.IsEffectDirty)))
                    {
                        ((DUCE.IResource)effect).ReleaseOnChannel(channel);
                    }

                    Geometry clip = ClipField.GetValue(this);
                    if ((clip != null)
                        && (!CheckFlagsAnd(channel, VisualProxyFlags.IsClipDirty)))
                    {
                        ((DUCE.IResource)clip).ReleaseOnChannel(channel);
                    }

                    Brush opacityMask = OpacityMaskField.GetValue(this);
                    if ((opacityMask != null)
                        && (!CheckFlagsAnd(channel, VisualProxyFlags.IsOpacityMaskDirty)))
                    {
                        ((DUCE.IResource)opacityMask).ReleaseOnChannel(channel);
                    }

                    CacheMode cacheMode = CacheModeField.GetValue(this);
                    if ((cacheMode != null)
                        && (! CheckFlagsAnd(channel, VisualProxyFlags.IsCacheModeDirty)))
                    {
                        ((DUCE.IResource)cacheMode).ReleaseOnChannel(channel);
                    }

                    //
                    // Release the visual.
                    //

                    this.ReleaseOnChannelCore(channel);

                    //
                    // Finally, the children.
                    //
                    int count = VisualChildrenCount;

                    for (int i = 0; i < count; i++)
                    {
                        Visual visual = GetVisualChild(i);
                        if (visual != null)
                        {
                            ((DUCE.IResource)visual).ReleaseOnChannel(channel);
                        }
                    }
                }
            }
            finally
            {
                //
                // We need to reset this flag if we are still on channel since we
                // have only decreased the ref-count and not deleted the resource.
                //
                if (IsOnChannel(channel))
                {
                    SetFlags(channel, false, VisualProxyFlags.IsDeleteResourceInProgress);
                }
            }
        }

        internal virtual void AddRefOnChannelForCyclicBrush(
            ICyclicBrush cyclicBrush,
            DUCE.Channel channel)
        {
            //
            // Since the ICyclicBrush to visual relationship is being created on this channel,
            // we need to update the number of cyclic brushes using this visual on this channel.
            //
            Dictionary<DUCE.Channel, int> channelsToCyclicBrushMap =
                ChannelsToCyclicBrushMapField.GetValue(this);
            if (channelsToCyclicBrushMap == null)
            {
                channelsToCyclicBrushMap = new Dictionary<DUCE.Channel, int>();
                ChannelsToCyclicBrushMapField.SetValue(this, channelsToCyclicBrushMap);
            }

            if (!channelsToCyclicBrushMap.ContainsKey(channel))
            {
                // If on this channel we were not previously using this Visual as the root
                // node of a VisualBrush, set the flag indicating that it is the root now.
                // Also set the number of uses on this channel to 1.
                SetFlags(true, VisualFlags.NodeIsCyclicBrushRoot);

                channelsToCyclicBrushMap[channel] = 1;
            }
            else
            {
                Debug.Assert(channelsToCyclicBrushMap[channel] > 0);

                channelsToCyclicBrushMap[channel] += 1;
            }


            //
            // Since the ICyclicBrush to visual relationship is being created on this channel,
            // we need to update the number of times this cyclic brush is used across all
            // channels.
            //
            Dictionary<ICyclicBrush, int> cyclicBrushToChannelsMap =
                CyclicBrushToChannelsMapField.GetValue(this);

            if (cyclicBrushToChannelsMap == null)
            {
                cyclicBrushToChannelsMap = new Dictionary<ICyclicBrush, int>();
                CyclicBrushToChannelsMapField.SetValue(this, cyclicBrushToChannelsMap);
            }

            if (!cyclicBrushToChannelsMap.ContainsKey(cyclicBrush))
            {
                cyclicBrushToChannelsMap[cyclicBrush] = 1;
            }
            else
            {
                Debug.Assert(cyclicBrushToChannelsMap[cyclicBrush] > 0);

                cyclicBrushToChannelsMap[cyclicBrush] += 1;
            }

            //
            // Render the brush's visual.
            //

            cyclicBrush.RenderForCyclicBrush(channel, false);
        }


        /// <summary>
        /// Override this function in derived classes to release unmanaged resources
        /// during Dispose and during removal of a subtree.
        /// </summary>
        internal virtual void ReleaseOnChannelForCyclicBrush(
            ICyclicBrush cyclicBrush,
            DUCE.Channel channel)
        {
            // Update the number of times this visual brush uses this visual across all channels.
            Dictionary<ICyclicBrush, int> cyclicBrushToChannelsMap =
                CyclicBrushToChannelsMapField.GetValue(this);

            Debug.Assert(cyclicBrushToChannelsMap != null);
            Debug.Assert(cyclicBrushToChannelsMap.ContainsKey(cyclicBrush));
            Debug.Assert(cyclicBrushToChannelsMap[cyclicBrush] > 0);


            if (cyclicBrushToChannelsMap[cyclicBrush] == 1)
            {
                //
                // If the ICyclicBrush no longer uses this Visual across all channels, then
                // we can remove it from the map.
                //
                cyclicBrushToChannelsMap.Remove(cyclicBrush);
            }
            else
            {
                // Decrease the number os times this ICyclicBrush uses this Visual across all channels
                cyclicBrushToChannelsMap[cyclicBrush] =
                    cyclicBrushToChannelsMap[cyclicBrush] - 1;
            }

            // Decrease the number of ICyclicBrush using the visual as root on this channel
            Dictionary<DUCE.Channel, int> channelsToCyclicBrushMap =
                ChannelsToCyclicBrushMapField.GetValue(this);
            Debug.Assert(channelsToCyclicBrushMap != null);
            Debug.Assert(channelsToCyclicBrushMap.ContainsKey(channel));
            Debug.Assert(channelsToCyclicBrushMap[channel] > 0);

            channelsToCyclicBrushMap[channel] =
                    channelsToCyclicBrushMap[channel] - 1;

            //
            // If on this channel, there are no more ICyclicBrushes using this visual as
            // a root then we need to remove the flag saying that the visual is a visual
            // brush root and make sure that the dependant resources are released in
            // case we are no longer connected to the visual tree.
            //

            if (channelsToCyclicBrushMap[channel] == 0)
            {
                channelsToCyclicBrushMap.Remove(channel);

                SetFlags(false, VisualFlags.NodeIsCyclicBrushRoot);

                PropagateFlags(
                    this,
                    VisualFlags.None,
                    VisualProxyFlags.IsSubtreeDirtyForRender);

                //
                // If we do not have a parent or we have already disconnected from
                // the parent and we are also not the root then we need to clear out
                // the tree.
                //
                if ( (_parent == null
                      || !CheckFlagsAnd(channel, VisualProxyFlags.IsConnectedToParent))
                    && !IsRootElement)
                {
                    ((DUCE.IResource)this).ReleaseOnChannel(channel);
                }
            }
        }

        #endregion Resource Marshalling and Unmarshalling


        // --------------------------------------------------------------------
        //
        //   Access Verification
        //
        // --------------------------------------------------------------------

        #region Access Verification

        /// <summary>
        /// Applies various API checks
        /// </summary>
        internal void VerifyAPIReadOnly()
        {
            // Verify that we are executing on the right context
            VerifyAccess();
        }

        /// <summary>
        /// Applies various API checks
        /// </summary>
        internal void VerifyAPIReadOnly(DependencyObject value)
        {
            VerifyAPIReadOnly();

            // Make sure the value is on the same context as the visual.
            // AssertSameContext handles null and Dispatcher-free values.
            MediaSystem.AssertSameContext(this, value);
        }

        /// <summary>
        /// Applies various API checks for read/write
        /// </summary>
        internal void VerifyAPIReadWrite()
        {
            VerifyAPIReadOnly();

            // Verify the correct access permissions
            MediaContext.From(this.Dispatcher).VerifyWriteAccess();
        }

        /// <summary>
        /// Applies various API checks
        /// </summary>
        internal void VerifyAPIReadWrite(DependencyObject value)
        {
            VerifyAPIReadWrite();

            // Make sure the value is on the same context as the visual.
            // AssertSameContext handles null and Dispatcher-free values.
            MediaSystem.AssertSameContext(this, value);
        }

        #endregion Access Verification

        // --------------------------------------------------------------------
        //
        //   Pre-compute / render passes
        //
        // --------------------------------------------------------------------

        #region Pre-compute / render passes

        internal void Precompute()
        {
            if (CheckFlagsAnd(VisualFlags.IsSubtreeDirtyForPrecompute))
            {
                // Disable processing of the queue during blocking operations to prevent unrelated reentrancy.
                using(Dispatcher.DisableProcessing())
                {
                    MediaContext mediaContext = MediaContext.From(Dispatcher);

                    try
                    {
                        mediaContext.PushReadOnlyAccess();

                        Rect bboxSubgraph;

                        PrecomputeRecursive(out bboxSubgraph);
                    }
                    finally
                    {
                        mediaContext.PopReadOnlyAccess();
                    }
                }
            }
        }

        /// <summary>
        /// Derived class can do precomputations on their content by overriding this method.
        /// Derived classes must call the base class.
        /// </summary>
        internal virtual void PrecomputeContent()
        {
            _bboxSubgraph = GetHitTestBounds();

            // If bounding box has NaN, then we set the bounding box to infinity.
            if (DoubleUtil.RectHasNaN(_bboxSubgraph))
            {
                _bboxSubgraph.X = Double.NegativeInfinity;
                _bboxSubgraph.Y = Double.NegativeInfinity;
                _bboxSubgraph.Width = Double.PositiveInfinity;
                _bboxSubgraph.Height = Double.PositiveInfinity;
            }
        }

        internal void PrecomputeRecursive(out Rect bboxSubgraph)
        {
            // Simple loop detection to avoid stack overflow in cyclic Visual
            // scenarios. This fix is only aimed at mitigating a very common
            // VisualBrush scenario.
            bool canEnter = Enter();

            if (canEnter)
            {
                try
                {
                    if (CheckFlagsAnd(VisualFlags.IsSubtreeDirtyForPrecompute))
                    {
                        PrecomputeContent();

                        int childCount = VisualChildrenCount;

                        for (int i = 0; i < childCount; i++)
                        {
                            Visual child = GetVisualChild(i);
                            if (child != null)
                            {
                                Rect bboxSubgraphChild;

                                child.PrecomputeRecursive(out bboxSubgraphChild);

                                _bboxSubgraph.Union(bboxSubgraphChild);
                            }
                        }

                        SetFlags(false, VisualFlags.IsSubtreeDirtyForPrecompute);
                    }

                    // Bounding boxes are cached in inner space (below offset, transform, and clip).
                    // Before returning them we need
                    // to transform them into outer space.

                    bboxSubgraph = _bboxSubgraph;

                    Geometry clip = ClipField.GetValue(this);
                    if (clip != null)
                    {
                        bboxSubgraph.Intersect(clip.Bounds);
                    }

                    Transform transform = TransformField.GetValue(this);

                    if ((transform != null) && (!transform.IsIdentity))
                    {
                        Matrix m = transform.Value;
                        MatrixUtil.TransformRect(ref bboxSubgraph, ref m);
                    }

                    if (!bboxSubgraph.IsEmpty)
                    {
                        bboxSubgraph.X += _offset.X;
                        bboxSubgraph.Y += _offset.Y;
                    }

                    Rect? scrollClip = ScrollableAreaClipField.GetValue(this);
                    if (scrollClip.HasValue)
                    {
                        bboxSubgraph.Intersect(scrollClip.Value);
                    }

                    // If child's bounding box has NaN, then we set the bounding box to infinity.
                    if (DoubleUtil.RectHasNaN(bboxSubgraph))
                    {
                        bboxSubgraph.X = Double.NegativeInfinity;
                        bboxSubgraph.Y = Double.NegativeInfinity;
                        bboxSubgraph.Width = Double.PositiveInfinity;
                        bboxSubgraph.Height = Double.PositiveInfinity;
                    }
                }
                finally
                {
                    Exit();
                }
            }
            else
            {
                bboxSubgraph = new Rect();
            }
        }

        internal void Render(RenderContext ctx, UInt32 childIndex)
        {
            DUCE.Channel channel = ctx.Channel;

            //
            // Currently everything is sent to the compositor. IsSubtreeDirtyForRender
            // indicates that something in the sub-graph of this Visual needs to have an update
            // sent to the compositor. Hence traverse if this bit is set. Also traverse when the
            // sub-graph has not yet been sent to the compositor.
            //

            if (CheckFlagsAnd(channel, VisualProxyFlags.IsSubtreeDirtyForRender)
                || !IsOnChannel(channel))
            {
                RenderRecursive(ctx);
            }


            //
            // Connect the root visual to the composition root if necessary.
            //

            if (IsOnChannel(channel)
                && !CheckFlagsAnd(channel, VisualProxyFlags.IsConnectedToParent)
                && !ctx.Root.IsNull)
            {
                DUCE.CompositionNode.InsertChildAt(
                    ctx.Root,
                    _proxy.GetHandle(channel),
                    childIndex,
                    channel);

                SetFlags(
                    channel,
                    true,
                    VisualProxyFlags.IsConnectedToParent);
            }
        }

        internal virtual void RenderRecursive(
            RenderContext ctx)
        {
            // Simple loop detection to avoid stack overflow in cyclic Visual
            // scenarios. This fix is only aimed at mitigating a very common
            // VisualBrush scenario.
            bool canEnter = Enter();

            if (canEnter)
            {
                try
                {
                    DUCE.Channel channel = ctx.Channel;
                    DUCE.ResourceHandle handle = DUCE.ResourceHandle.Null;
                    VisualProxyFlags flags = VisualProxyFlags.None;

                    //
                    // See if this visual is already on that channel
                    //

                    bool isOnChannel = IsOnChannel(channel);

                    //
                    // Ensure that the visual resource for this Visual
                    // is being sent to our current channel.
                    //

                    if (isOnChannel)
                    {
                        //
                        // Good, we're already on channel. Get the handle and flags.
                        //

                        handle = _proxy.GetHandle(channel);
                        flags = _proxy.GetFlags(channel);
                    }
                    else
                    {
                        //
                        // Create the visual resource on the current channel.
                        //
                        // Need to update all set properties.
                        //

                        handle = ((DUCE.IResource)this).AddRefOnChannel(channel);

                        // we need to set the Viewport3D flags, if the visual is not
                        // on channel so that the viewport sends all its resources
                        // to the compositor. we need the explicit set, because
                        // the update happens during RenderContent and we have no
                        // other way to pass the flags
                        //
                        // We do that for all visuals. the flags will be ignored
                        // if the visual is not a Viewport3D visual
                        SetFlags(channel, true, c_Viewport3DProxyFlagsDirtyMask);

                        flags = c_ProxyFlagsDirtyMask;
                    }

                    UpdateCacheMode(channel, handle, flags, isOnChannel);
                    UpdateTransform(channel, handle, flags, isOnChannel);
                    UpdateClip(channel, handle, flags, isOnChannel);
                    UpdateOffset(channel, handle, flags, isOnChannel);
                    UpdateEffect(channel, handle, flags, isOnChannel);
                    UpdateGuidelines(channel, handle, flags, isOnChannel);
                    UpdateContent(ctx, flags, isOnChannel);
                    UpdateOpacity(channel, handle, flags, isOnChannel);
                    UpdateOpacityMask(channel, handle, flags, isOnChannel);
                    UpdateRenderOptions(channel, handle, flags, isOnChannel);
                    UpdateChildren(ctx, handle);
                    UpdateScrollableAreaClip(channel, handle, flags, isOnChannel);

                    //
                    // Finally, reset the dirty flags for this visual (at this point,
                    // we have handled them all).
                    SetFlags(channel, false, VisualProxyFlags.IsSubtreeDirtyForRender);
                }
                finally
                {
                    Exit();
                }
            }
        }

        /// <summary>
        /// Enter is used for simple cycle detection in Visual. If the method returns false
        /// the Visual has already been entered and cannot be entered again. Matching invocation of Exit
        /// must be skipped if Enter returns false.
        /// </summary>
        internal bool Enter()
        {
            if (CheckFlagsAnd(VisualFlags.ReentrancyFlag))
            {
                return false;
            }
            else
            {
                SetFlags(true, VisualFlags.ReentrancyFlag);
                return true;
            }
        }

        /// <summary>
        /// Exits the Visual. For more details see Enter method.
        /// </summary>
        internal void Exit()
        {
            Debug.Assert(CheckFlagsAnd(VisualFlags.ReentrancyFlag)); // Exit must be matched with Enter. See Enter comments.
            SetFlags(false, VisualFlags.ReentrancyFlag);
        }

        /// <summary>
        /// Update opacity
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="handle"></param>
        /// <param name="flags"></param>
        /// <param name="isOnChannel"></param>
        private void UpdateOpacity(DUCE.Channel channel,
                                   DUCE.ResourceHandle handle,
                                   VisualProxyFlags flags,
                                   bool isOnChannel)
        {
            // Opacity ----------------------------------------------------------------------------
            if ((flags & VisualProxyFlags.IsOpacityDirty) != 0)
            {
                double opacity = OpacityField.GetValue(this);

                if (isOnChannel || !(opacity >= 1.0))
                {
                    //
                    // Opacity is 1.0 by default -- do not send it for new visuals.
                    //

                    DUCE.CompositionNode.SetAlpha(
                        handle,
                        opacity,
                        channel);
                }
                SetFlags(channel, false, VisualProxyFlags.IsOpacityDirty);
            }
        }

        /// <summary>
        /// Update OpacityMask
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="handle"></param>
        /// <param name="flags"></param>
        /// <param name="isOnChannel">The Visual exists on channel.</param>
        private void UpdateOpacityMask(DUCE.Channel channel,
                                       DUCE.ResourceHandle handle,
                                       VisualProxyFlags flags,
                                       bool isOnChannel)
        {
            // Opacity Mask ----------------------------------------------------------------------------

            if ((flags & VisualProxyFlags.IsOpacityMaskDirty) != 0)
            {
                Brush opacityMask = OpacityMaskField.GetValue(this);

                if (opacityMask != null)
                {
                    //
                    // Set the new opacity mask resource on the visual.
                    // If opacityMask is null we don't need to do this.
                    // Also note that the old opacity mask was disconnected
                    // in the OpacityMask property setter.
                    //

                    DUCE.CompositionNode.SetAlphaMask(
                        handle,
                        ((DUCE.IResource)opacityMask).AddRefOnChannel(channel),
                        channel);
                }
                else if (isOnChannel) /* opacityMask == null */
                {
                    DUCE.CompositionNode.SetAlphaMask(
                        handle,
                        DUCE.ResourceHandle.Null,
                        channel);
                }
                SetFlags(channel, false, VisualProxyFlags.IsOpacityMaskDirty);
            }
}

        /// <summary>
        /// Update transform
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="handle"></param>
        /// <param name="flags"></param>
        /// <param name="isOnChannel">The Visual exists on channel.</param>
        private void UpdateTransform(DUCE.Channel channel,
                                     DUCE.ResourceHandle handle,
                                     VisualProxyFlags flags,
                                     bool isOnChannel)
        {
            // Transform -------------------------------------------------------------------------------

            if ((flags & VisualProxyFlags.IsTransformDirty) != 0)
            {
                Transform transform = TransformField.GetValue(this);

                if (transform != null)
                {
                    //
                    // Set the new transform resource on the visual.
                    // If transform is null we don't need to do this.
                    // Also note that the old transform was disconnected
                    // in the Transform property setter.
                    //

                    DUCE.CompositionNode.SetTransform(
                        handle,
                        ((DUCE.IResource)transform).AddRefOnChannel(channel),
                        channel);
                }
                else if (isOnChannel) /* transform == null */
                {
                    DUCE.CompositionNode.SetTransform(
                        handle,
                        DUCE.ResourceHandle.Null,
                        channel);
                }
                SetFlags(channel, false, VisualProxyFlags.IsTransformDirty);
            }
        }

        /// Update effect.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="handle"></param>
        /// <param name="flags"></param>
        /// <param name="isOnChannel">The Visual exists on channel.</param>
        private void UpdateEffect(DUCE.Channel channel,
                                     DUCE.ResourceHandle handle,
                                     VisualProxyFlags flags,
                                     bool isOnChannel)
        {
            // Effect  -------------------------------------------------------------------------------

            if ((flags & VisualProxyFlags.IsEffectDirty) != 0)
            {
                Effect effect = EffectField.GetValue(this);

                if (effect != null)
                {
                    //
                    // Set the new effect resource on the visual.
                    // If effect is null we don't need to do this.
                    // Also note that the old effect was disconnected
                    // in the Effect property setter.
                    //

                    DUCE.CompositionNode.SetEffect(
                        handle,
                        ((DUCE.IResource)effect).AddRefOnChannel(channel),
                        channel);
                }
                else if (isOnChannel) /* effect == null */
                {
                    DUCE.CompositionNode.SetEffect(
                        handle,
                        DUCE.ResourceHandle.Null,
                        channel);
                }
                SetFlags(channel, false, VisualProxyFlags.IsEffectDirty);
            }
        }

        /// <summary>
        /// Update cache mode.
        /// </summary>
        private void UpdateCacheMode(DUCE.Channel channel,
                                     DUCE.ResourceHandle handle,
                                     VisualProxyFlags flags,
                                     bool isOnChannel)
        {
            // Cache Mode  -------------------------------------------------------------------------------

            if ((flags & VisualProxyFlags.IsCacheModeDirty) != 0)
            {
                CacheMode cacheMode = CacheModeField.GetValue(this);

                if (cacheMode != null)
                {
                    //
                    // Set the new cache mode resource on the visual.
                    // If cacheMode is null we don't need to do this.
                    // Also note that the old cache mode was disconnected
                    // in the CacheMode property setter.
                    //

                    DUCE.CompositionNode.SetCacheMode(
                        handle,
                        ((DUCE.IResource)cacheMode).AddRefOnChannel(channel),
                        channel);
                }
                else if (isOnChannel) /* cacheMode == null */
                {
                    DUCE.CompositionNode.SetCacheMode(
                        handle,
                        DUCE.ResourceHandle.Null,
                        channel);
                }
                SetFlags(channel, false, VisualProxyFlags.IsCacheModeDirty);
            }
        }

        /// <summary>
        /// Update clip
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="handle"></param>
        /// <param name="flags"></param>
        /// <param name="isOnChannel">The Visual exists on channel.</param>
        private void UpdateClip(DUCE.Channel channel,
                                DUCE.ResourceHandle handle,
                                VisualProxyFlags flags,
                                bool isOnChannel)
        {
            // Clip ------------------------------------------------------------------------------------

            if ((flags & VisualProxyFlags.IsClipDirty) != 0)
            {
                Geometry clip = ClipField.GetValue(this);

                if (clip != null)
                {
                    //
                    // Set the new clip resource on the composition node.
                    // If clip is null we don't need to do this.  Also note
                    // that the old clip was disconnected in the Clip
                    // property setter.
                    //

                    DUCE.CompositionNode.SetClip(
                        handle,
                        ((DUCE.IResource)clip).AddRefOnChannel(channel),
                        channel);
                }
                else if (isOnChannel) /* clip == null */
                {
                    DUCE.CompositionNode.SetClip(
                        handle,
                        DUCE.ResourceHandle.Null,
                        channel);
                }

                SetFlags(channel, false, VisualProxyFlags.IsClipDirty);
            }
        }

        /// <summary>
        /// Update scrollable area clip
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="handle"></param>
        /// <param name="flags"></param>
        /// <param name="isOnChannel">The Visual exists on channel.</param>
        private void UpdateScrollableAreaClip(DUCE.Channel channel,
                                              DUCE.ResourceHandle handle,
                                              VisualProxyFlags flags,
                                              bool isOnChannel)
        {
            if ((flags & VisualProxyFlags.IsScrollableAreaClipDirty) != 0)
            {
                Rect? scrollableArea = ScrollableAreaClipField.GetValue(this);

                if (isOnChannel || (scrollableArea != null))
                {
                    DUCE.CompositionNode.SetScrollableAreaClip(
                        handle,
                        scrollableArea,
                        channel);
}
                SetFlags(channel, false, VisualProxyFlags.IsScrollableAreaClipDirty);
            }
        }


        /// <summary>
        /// Update offset
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="handle"></param>
        /// <param name="flags"></param>
        /// <param name="isOnChannel"></param>
        private void UpdateOffset(DUCE.Channel channel,
                                  DUCE.ResourceHandle handle,
                                  VisualProxyFlags flags,
                                  bool isOnChannel)
        {
            // Offset --------------------------------------------------------------------------------------------

            if ((flags & VisualProxyFlags.IsOffsetDirty) != 0)
            {
                if (isOnChannel || _offset != new Vector())
                {
                    //
                    // Offset is (0, 0) by default so do not update it for new visuals.
                    //

                    DUCE.CompositionNode.SetOffset(
                        handle,
                        _offset.X,
                        _offset.Y,
                        channel);
                }
                SetFlags(channel, false, VisualProxyFlags.IsOffsetDirty);
            }
        }

        /// <summary>
        /// Update guidelines
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="handle"></param>
        /// <param name="flags"></param>
        /// <param name="isOnChannel"></param>
        private void UpdateGuidelines(DUCE.Channel channel,
                                      DUCE.ResourceHandle handle,
                                      VisualProxyFlags flags,
                                      bool isOnChannel)
        {
            // Guidelines --------------------------------------------------------------------

            if ((flags & VisualProxyFlags.IsGuidelineCollectionDirty) != 0)
            {
                DoubleCollection guidelinesX = GuidelinesXField.GetValue(this);
                DoubleCollection guidelinesY = GuidelinesYField.GetValue(this);

                if (isOnChannel || (guidelinesX != null || guidelinesY != null))
                {
                    //
                    // Guidelines are null by default, so do not update them for new visuals.
                    //

                    DUCE.CompositionNode.SetGuidelineCollection(
                        handle,
                        guidelinesX,
                        guidelinesY,
                        channel);
                }
                SetFlags(channel, false, VisualProxyFlags.IsGuidelineCollectionDirty);
            }
}

        /// <summary>
        /// Update EdgeMode
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="handle"></param>
        /// <param name="flags"></param>
        /// <param name="isOnChannel"></param>
        private void UpdateRenderOptions(DUCE.Channel channel,
                                    DUCE.ResourceHandle handle,
                                    VisualProxyFlags flags,
                                    bool isOnChannel)
        {
            if (((flags & VisualProxyFlags.IsEdgeModeDirty) != 0)          ||
                ((flags & VisualProxyFlags.IsBitmapScalingModeDirty) != 0) ||
                ((flags & VisualProxyFlags.IsClearTypeHintDirty) != 0)     ||
                ((flags & VisualProxyFlags.IsTextRenderingModeDirty) != 0) ||
                ((flags & VisualProxyFlags.IsTextHintingModeDirty) != 0))
            {
                MilRenderOptions renderOptions = new MilRenderOptions();

                // EdgeMode ----------------------------------------------------------------------------
                // "isOnChannel" (if true) indicates that this Visual was on channel
                // previous to this update.  If this is the case, all changes to the EdgeMode
                // must be reflected in the composition node.  If "isOnChannel" is false it means
                // that this Visual has just been added to a channel.  In this case, we can
                // skip an EdgeMode update if the EdgeMode is Unspecified, as this is the default
                // behavior.
                EdgeMode edgeMode = EdgeModeField.GetValue(this);
                if (isOnChannel || (edgeMode != EdgeMode.Unspecified))
                {
                    renderOptions.Flags |= MilRenderOptionFlags.EdgeMode;
                    renderOptions.EdgeMode = edgeMode;
                }

                // ImageScalingMode ----------------------------------------------------------------------------
                BitmapScalingMode bitmapScalingMode = BitmapScalingModeField.GetValue(this);
                if (isOnChannel || (bitmapScalingMode != BitmapScalingMode.Unspecified))
                {
                    renderOptions.Flags |= MilRenderOptionFlags.BitmapScalingMode;
                    renderOptions.BitmapScalingMode = bitmapScalingMode;
                }

                ClearTypeHint clearTypeHint = ClearTypeHintField.GetValue(this);
                if (isOnChannel || (clearTypeHint != ClearTypeHint.Auto))
                {
                    renderOptions.Flags |= MilRenderOptionFlags.ClearTypeHint;
                    renderOptions.ClearTypeHint = clearTypeHint;
                }

                TextRenderingMode textRenderingMode = TextRenderingModeField.GetValue(this);
                if (isOnChannel || (textRenderingMode != TextRenderingMode.Auto))
                {
                    renderOptions.Flags |= MilRenderOptionFlags.TextRenderingMode;
                    renderOptions.TextRenderingMode = textRenderingMode;
                }

                TextHintingMode textHintingMode = TextHintingModeField.GetValue(this);
                if (isOnChannel || (textHintingMode != TextHintingMode.Auto))
                {
                    renderOptions.Flags |= MilRenderOptionFlags.TextHintingMode;
                    renderOptions.TextHintingMode = textHintingMode;
                }

                if (renderOptions.Flags != 0)
                {
                    DUCE.CompositionNode.SetRenderOptions(
                        handle,
                        renderOptions,
                        channel);
                }
                SetFlags(
                    channel,
                    false,
                    VisualProxyFlags.IsEdgeModeDirty |
                    VisualProxyFlags.IsBitmapScalingModeDirty |
                    VisualProxyFlags.IsClearTypeHintDirty |
                    VisualProxyFlags.IsTextRenderingModeDirty |
                    VisualProxyFlags.IsTextHintingModeDirty
                    );
            }
        }

        /// <summary>
        /// Update content
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="flags"></param>
        /// <param name="isOnChannel">The Visual exists on channel.</param>
        private void UpdateContent(RenderContext ctx,
                                   VisualProxyFlags flags,
                                   bool isOnChannel)
        {
            //
            // Hookup content to the Visual
            //

            if ((flags & VisualProxyFlags.IsContentDirty) != 0)
            {
                RenderContent(ctx, isOnChannel);
                SetFlags(ctx.Channel, false, VisualProxyFlags.IsContentDirty);
            }
        }

        /// <summary>
        /// Update children
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="handle"></param>
        private void UpdateChildren(RenderContext ctx,
                                    DUCE.ResourceHandle handle)
        {
            DUCE.Channel channel = ctx.Channel;

            //
            // Visit children of this visual.
            //
            //
            // If content node is connected child node indicies need to be offset by one.
            //

            UInt32 connectedChildIndex =
                CheckFlagsAnd(channel, VisualProxyFlags.IsContentNodeConnected) ? (UInt32)1 : 0;

            bool isChildrenZOrderDirty = CheckFlagsAnd(channel, VisualProxyFlags.IsChildrenZOrderDirty);
            int childCount = VisualChildrenCount;

            //
            // If the visual children have been re-ordered, enqueue a packet to RemoveAllChildren,
            // then reinsert all the children.  The parent visual will release the children when
            // the RemoveAllChildren packet, but the managed visuals will still have references
            // to them so that they won't be destructed and recreated.
            //
            if (isChildrenZOrderDirty)
            {
                DUCE.CompositionNode.RemoveAllChildren(
                    handle,
                    channel);
            }

            for (int i = 0; i < childCount; i++)
            {
                Visual child = GetVisualChild(i);
                if (child != null)
                {
                    //
                    // Recurse if the child visual is dirty
                    // or it has not been marshalled yet.
                    //
                    if (child.CheckFlagsAnd(channel, VisualProxyFlags.IsSubtreeDirtyForRender)
                        || !(child.IsOnChannel(channel)))
                    {
                        child.RenderRecursive(ctx);
                    }

                    //
                    // Make sure that all the marshaled children are
                    // connected to the parent visual or that the ZOrder
                    // of the children has changed.
                    //
                    if (child.IsOnChannel(channel))
                    {
                        bool isConnectedToParent = child.CheckFlagsAnd(channel, VisualProxyFlags.IsConnectedToParent);

                        if (!isConnectedToParent || isChildrenZOrderDirty)
                        {
                            DUCE.CompositionNode.InsertChildAt(
                                handle,
                                ((DUCE.IResource)child).GetHandle(channel),
                                connectedChildIndex,
                                channel);

                            child.SetFlags(
                                channel,
                                true,
                                VisualProxyFlags.IsConnectedToParent);
                        }

                        connectedChildIndex++;
                    }
                }
            }

            SetFlags(channel, false, VisualProxyFlags.IsChildrenZOrderDirty);
        }

        #endregion Pre-compute / render passes



        // --------------------------------------------------------------------
        //
        //   Hit Testing
        //
        // --------------------------------------------------------------------

        #region Hit Testing

        internal class TopMostHitResult
        {
            internal HitTestResult _hitResult = null;

            internal HitTestResultBehavior HitTestResult(HitTestResult result)
            {
                _hitResult = result;

                return HitTestResultBehavior.Stop;
            }

            internal HitTestFilterBehavior NoNested2DFilter(DependencyObject potentialHitTestTarget)
            {
                if (potentialHitTestTarget is Viewport2DVisual3D)
                {
                    return HitTestFilterBehavior.ContinueSkipChildren;
                }

                return HitTestFilterBehavior.Continue;
            }
        }


        /// <summary>
        /// Used by derived classes to invalidate their hit-test bounds.
        /// </summary>
        internal void InvalidateHitTestBounds()
        {
            VerifyAPIReadWrite();

            PropagateFlags(
                this,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.None);
        }


        /// <summary>
        /// Derived classes return the hit-test bounding box from the
        /// GetHitTestBounds virtual. Visual uses the bounds to optimize
        /// hit-testing.
        /// </summary>
        internal virtual Rect GetHitTestBounds()
        {
            return GetContentBounds();
        }


        /// <summary>
        /// Return top most visual of a hit test.
        /// </summary>
        internal HitTestResult HitTest(Point point)
        {
            return HitTest(point, true);
        }

        /// <summary>
        /// Return top most visual of a hit test.  If include2DOn3D is true we will
        /// hit test in to 2D on 3D children, otherwise we will ignore that part of
        /// the tree.
        /// </summary>
        internal HitTestResult HitTest(Point point, bool include2DOn3D)
        {
            // 

            TopMostHitResult result = new TopMostHitResult();

            VisualTreeHelper.HitTest(
                this,
                include2DOn3D? null : new HitTestFilterCallback(result.NoNested2DFilter),
                new HitTestResultCallback(result.HitTestResult),
                new PointHitTestParameters(point));

            return result._hitResult;
        }

        /// <summary>
        /// Initiate a hit test using delegates.
        /// </summary>
        internal void HitTest(
            HitTestFilterCallback filterCallback,
            HitTestResultCallback resultCallback,
            HitTestParameters hitTestParameters)
        {
            if (resultCallback == null)
            {
                throw new ArgumentNullException("resultCallback");
            }

            if (hitTestParameters == null)
            {
                throw new ArgumentNullException("hitTestParameters");
            }

            VerifyAPIReadWrite();

            Precompute();

            PointHitTestParameters pointParams = hitTestParameters as PointHitTestParameters;

            if (pointParams != null)
            {
                // Because we call dynamic code during the hit testing walk we need to back up
                // the original hit point in case the user's delegate throws an exception so that
                // we can restore it.
                Point backupHitPoint = pointParams.HitPoint;

                try
                {
                    HitTestPoint(filterCallback, resultCallback, pointParams);
                }
                catch
                {
                    // If an exception occured, restore the user's hit point and rethrow.
                    pointParams.SetHitPoint(backupHitPoint);

                    throw;
                }
                finally
                {
                    Debug.Assert(Point.Equals(pointParams.HitPoint, backupHitPoint),
                        "Failed to restore user's hit point back to the original coordinate system.");
                }
            }
            else
            {
                GeometryHitTestParameters geometryParams = hitTestParameters as GeometryHitTestParameters;

                if (geometryParams != null)
                {
                    // Because we call dynamic code during the hit testing walk we need to ensure
                    // that if the user's delegate throws an exception we restore the original
                    // transform on the hit test geometry.
#if DEBUG
                    // Internally we replace the hit geometry with a copy which is guaranteed to have
                    // a MatrixTransform so we do not need to worry about null dereferences here.
                    Matrix originalMatrix = geometryParams.InternalHitGeometry.Transform.Value;
#endif // DEBUG
                    try
                    {
                        HitTestGeometry(filterCallback, resultCallback, geometryParams);
                    }
                    catch
                    {
                        geometryParams.EmergencyRestoreOriginalTransform();

                        throw;
                    }
#if DEBUG
                    finally
                    {
                        Debug.Assert(Matrix.Equals(geometryParams.InternalHitGeometry.Transform.Value, originalMatrix),
                            "Failed to restore user's hit geometry back to the original coordinate system.");
                    }
#endif // DEBUG
                }
                else
                {
                    // This should never happen, users can not extend the abstract HitTestParameters class.
                    Invariant.Assert(false,
                        String.Format(System.Globalization.CultureInfo.InvariantCulture,
                            "'{0}' HitTestParameters are not supported on {1}.",
                            hitTestParameters.GetType().Name, this.GetType().Name));
                }
            }
        }

        internal HitTestResultBehavior HitTestPoint(
            HitTestFilterCallback filterCallback,
            HitTestResultCallback resultCallback,
            PointHitTestParameters pointParams)
        {
            // we do not need parameter checks because they are done in HitTest()

            Geometry clip = VisualClip;

            // Before we continue hit-testing we check against the hit-test bounds for the sub-graph.
            // If the point is not with-in the hit-test bounds, the sub-graph can be skipped.
            if (_bboxSubgraph.Contains(pointParams.HitPoint) &&
                ((null == clip) || clip.FillContains(pointParams.HitPoint))) // Check that the hit-point is with-in the clip.
            {
                //
                // Determine if there is a special filter behavior defined for this
                // Visual.
                //

                HitTestFilterBehavior filter = HitTestFilterBehavior.Continue;
                if (filterCallback != null)
                {
                    filter = filterCallback(this);

                    if (filter == HitTestFilterBehavior.ContinueSkipSelfAndChildren)
                    {
                        return HitTestResultBehavior.Continue;
                    }

                    if (filter == HitTestFilterBehavior.Stop)
                    {
                        return HitTestResultBehavior.Stop;
                    }
                }

                // if there is a bitmap effect transform the point
                // Backup the hit point so that we can restore it later on.
                Point originalHitPoint = pointParams.HitPoint;
                Point hitPoint = originalHitPoint;

                if (CheckFlagsAnd(VisualFlags.NodeHasEffect))
                {
                    Effect imageEffect = EffectField.GetValue(this);
                    if (imageEffect != null)
                    {
                        GeneralTransform effectHitTestInverse = imageEffect.EffectMapping.Inverse;

                        // only do work if the transform isn't the identity transform
                        if (effectHitTestInverse != Transform.Identity)
                        {
                            bool ok = false;

                            // Convert to unit space
                            Point? unitHitPoint = Effect.WorldToUnit(originalHitPoint, _bboxSubgraph);
                            if (unitHitPoint != null)
                            {
                                Point transformedPt = new Point();

                                // Do the transform
                                if (effectHitTestInverse.TryTransform(unitHitPoint.Value, out transformedPt))
                                {
                                    // Convert back to world space
                                    Point? worldSpace = Effect.UnitToWorld(transformedPt, _bboxSubgraph);
                                    if (worldSpace != null)
                                    {
                                        hitPoint = worldSpace.Value;
                                        ok = true;
                                    }
                                }
                            }

                            if (!ok)
                            {
                                return HitTestResultBehavior.Continue;
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert(BitmapEffectStateField.GetValue(this) != null);
                        // BitmapEffects are deprecated so they no longer affect hit testing.
                    }
                }



                //
                // Hit test against the children.
                //
                if (filter != HitTestFilterBehavior.ContinueSkipChildren)
                {
                    int childCount = VisualChildrenCount;
                    for (int i=childCount-1; i>=0; i--)
                    {
                        Visual child = GetVisualChild(i);
                        if (child != null)
                        {
                            // Hit the scollClip bounds first, which are in the child's outer-space.
                            Rect? scrollClip = ScrollableAreaClipField.GetValue(child);
                            if (scrollClip.HasValue && !scrollClip.Value.Contains(hitPoint))
                            {
                                // Skip child if the point is not within the ScrollableClip.
                                continue;
                            }

                            //
                            // Transform the hit-test point below offset and transform.
                            //

                            Point newHitPoint = hitPoint;

                            // Apply the offset.
                            newHitPoint = newHitPoint - child._offset;

                            // If we have a transform, apply the transform.
                            Transform childTransform = TransformField.GetValue(child);
                            if (childTransform != null)
                            {
                                Matrix inv = childTransform.Value;

                                // If we can't invert the transform, the child is not hitable. This makes sense since
                                // the node's rendered content is degenerate, i.e. does not really take up any space.
                                // Skip the child by continuing in the loop.
                                if (!inv.HasInverse)
                                {
                                    continue;
                                }

                                inv.Invert();

                                newHitPoint = newHitPoint * inv;
                            }

                            // Set the new hittesting point into the hittest params.
                            pointParams.SetHitPoint(newHitPoint);

                            // Perform the hit-test against the child.
                            HitTestResultBehavior result =
                                child.HitTestPoint(filterCallback, resultCallback, pointParams);

                            // Restore the hit-test point.
                            pointParams.SetHitPoint(originalHitPoint);


                            if (result == HitTestResultBehavior.Stop)
                            {
                                return HitTestResultBehavior.Stop;
                            }
                        }
                    }
                }

                //
                // Hit test against the content of this Visual.
                //

                if (filter != HitTestFilterBehavior.ContinueSkipSelf)
                {
                    // set the transformed hit point
                    pointParams.SetHitPoint(hitPoint);

                    HitTestResultBehavior result = HitTestPointInternal(filterCallback, resultCallback, pointParams);

                    // restore the hit point back to its original
                    pointParams.SetHitPoint(originalHitPoint);

                    if (result == HitTestResultBehavior.Stop)
                    {
                        return HitTestResultBehavior.Stop;
                    }
                }
            }

            return HitTestResultBehavior.Continue;
        }

        // provides a transform that goes between the Visual's coordinate space
        // and that after applying the transforms that bring it to outer space.
        internal GeneralTransform TransformToOuterSpace()
        {
            Matrix m = Matrix.Identity;
            GeneralTransformGroup group = null;
            GeneralTransform result = null;

            if (CheckFlagsAnd(VisualFlags.NodeHasEffect))
            {
                Effect effect = EffectField.GetValue(this);
                if (effect != null)
                {
                    GeneralTransform gt = effect.CoerceToUnitSpaceGeneralTransform(
                        effect.EffectMapping,
                        VisualDescendantBounds);

                    Transform affineTransform = gt.AffineTransform;
                    if (affineTransform != null)
                    {
                        Matrix cm = affineTransform.Value;
                        MatrixUtil.MultiplyMatrix(ref m, ref cm);
                    }
                    else
                    {
                        group = new GeneralTransformGroup();
                        group.Children.Add(gt);
                    }
                }
                else
                {
                    BitmapEffectState bitmapEffectState = BitmapEffectStateField.GetValue(this);
                    // If we have an effect on this node and it isn't an Effect, it must be a BitmapEffect.
                    // Since BitmapEffects are deprecated and ignored, they do not change a Visual's transform.
                    Debug.Assert(bitmapEffectState != null);
                }
            }

            Transform transform = TransformField.GetValue(this);
            if (transform != null)
            {
                Matrix cm = transform.Value;
                MatrixUtil.MultiplyMatrix(ref m, ref cm);
            }
            m.Translate(_offset.X, _offset.Y); // Consider having a bit that indicates that we have a non-null offset.

            if (group == null)
            {
                result = new MatrixTransform(m);
            }
            else
            {
                group.Children.Add(new MatrixTransform(m));
                result = group;
            }

            result.Freeze();
            return result;
        }

        internal HitTestResultBehavior HitTestGeometry(
            HitTestFilterCallback filterCallback,
            HitTestResultCallback resultCallback,
            GeometryHitTestParameters geometryParams)
        {
            // we do not need parameter checks because they are done in HitTest()
            Geometry clip = VisualClip;
            if (clip != null)
            {
                // HitTest with a Geometry and a clip should hit test with
                // the intersection of the geometry and the clip, not the entire geometry
                IntersectionDetail intersectionDetail = clip.FillContainsWithDetail(geometryParams.InternalHitGeometry);

                Debug.Assert(intersectionDetail != IntersectionDetail.NotCalculated);
                if (intersectionDetail == IntersectionDetail.Empty)
                {
                    // bail out if there is a clip and this region is not inside
                    return HitTestResultBehavior.Continue;
                }
            }

            //
            // Check if the geometry intersects with our hittest bounds.
            // If not, the Visual is not hit-testable at all.

            if (_bboxSubgraph.IntersectsWith(geometryParams.Bounds))
            {
                //
                // Determine if there is a special filter behavior defined for this
                // Visual.
                //

                HitTestFilterBehavior filter = HitTestFilterBehavior.Continue;

                if (filterCallback != null)
                {
                    filter = filterCallback(this);

                    if (filter == HitTestFilterBehavior.ContinueSkipSelfAndChildren)
                    {
                        return HitTestResultBehavior.Continue;
                    }

                    if (filter == HitTestFilterBehavior.Stop)
                    {
                        return HitTestResultBehavior.Stop;
                    }
                }

                //
                // Hit-test against the children.
                //

                int childCount = VisualChildrenCount;

                if (filter != HitTestFilterBehavior.ContinueSkipChildren)
                {
                    for (int i=childCount-1; i>=0; i--)
                    {
                        Visual child = GetVisualChild(i);
                        if (child != null)
                        {
                            // Hit the scollClip bounds first, which are in the child's outer-space.
                            Rect? scrollClip = ScrollableAreaClipField.GetValue(child);
                            if (scrollClip.HasValue)
                            {
                                // Hit-testing with a Geometry and a clip should hit test with
                                // the intersection of the geometry and the clip, not the entire geometry
                                RectangleGeometry rectClip = new RectangleGeometry(scrollClip.Value);
                                IntersectionDetail intersectionDetail = rectClip.FillContainsWithDetail(geometryParams.InternalHitGeometry);

                                Debug.Assert(intersectionDetail != IntersectionDetail.NotCalculated);
                                if (intersectionDetail == IntersectionDetail.Empty)
                                {
                                    // Skip child if there is a scrollable clip and this region is not inside it.
                                    continue;
                                }
                            }

                            // Transform the geometry below offset and transform.
                            Matrix inv = Matrix.Identity;
                            inv.Translate(-child._offset.X, -child._offset.Y);

                            Transform childTransform = TransformField.GetValue(child);
                            if (childTransform != null)
                            {
                                Matrix m = childTransform.Value;

                                // If we can't invert the transform, the child is not hitable. This makes sense since
                                // the node's rendered content is degnerated, i.e. does not really take up any space.
                                // Skipping the child by continuing the loop.
                                if (!m.HasInverse)
                                {
                                   continue;
                                }

                                // Inverse the transform.
                                m.Invert();

                                // Multiply the inverse and the offset together.
                                // inv = inv * m;
                                MatrixUtil.MultiplyMatrix(ref inv, ref m);
                            }

                            // Push the transform on the geometry params.
                            geometryParams.PushMatrix(ref inv);

                            // Hit-Test against the children.

                            HitTestResultBehavior result =
                                child.HitTestGeometry(filterCallback, resultCallback, geometryParams);

                            // Pop the transform from the geometry params.

                            geometryParams.PopMatrix();

                            // Process the result.
                            if (result == HitTestResultBehavior.Stop)
                            {
                                return HitTestResultBehavior.Stop;
                            }
                        }
                    }
                }

                //
                // Hit-test against the content of the Visual.
                //

                if (filter != HitTestFilterBehavior.ContinueSkipSelf)
                {
                    GeometryHitTestResult hitResult = HitTestCore(geometryParams);

                    if (hitResult != null)
                    {
                        Debug.Assert(resultCallback != null);

                        return resultCallback(hitResult);
                    }
                }
            }

            return HitTestResultBehavior.Continue;
        }


        /// <summary>
        /// This method provides an internal extension point for Viewport3DVisual
        /// to grab the HitTestFilterCallback and ResultDelegate before it gets lost in the
        /// forward to HitTestCore.
        /// </summary>
        internal virtual HitTestResultBehavior HitTestPointInternal(
            HitTestFilterCallback filterCallback,
            HitTestResultCallback resultCallback,
            PointHitTestParameters hitTestParameters)
        {
            HitTestResult hitResult = HitTestCore(hitTestParameters);

            if (hitResult != null)
            {
                return resultCallback(hitResult);
            }

            return HitTestResultBehavior.Continue;
        }

        /// <summary>
        /// HitTestCore implements whether we have hit the bounds of this visual.
        /// </summary>
        protected virtual HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            if (hitTestParameters == null)
            {
                throw new ArgumentNullException("hitTestParameters");
            }

            // If we don't have a clip, or if the clip contains the point, keep going.
            if (GetHitTestBounds().Contains(hitTestParameters.HitPoint))
            {
                return new PointHitTestResult(this, hitTestParameters.HitPoint);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// HitTestCore implements whether we have hit the bounds of this visual.
        /// </summary>
        protected virtual GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
        {
            if (hitTestParameters == null)
            {
                throw new ArgumentNullException("hitTestParameters");
            }

            IntersectionDetail intersectionDetail;

            RectangleGeometry contentGeometry = new RectangleGeometry(GetHitTestBounds());

            intersectionDetail = contentGeometry.FillContainsWithDetail(hitTestParameters.InternalHitGeometry);
            Debug.Assert(intersectionDetail != IntersectionDetail.NotCalculated);

            if (intersectionDetail != IntersectionDetail.Empty)
            {
                return new GeometryHitTestResult(this, intersectionDetail);
            }


            return null;
        }

        #endregion Hit Testing



        // --------------------------------------------------------------------
        //
        //   Visual Operations API
        //
        // --------------------------------------------------------------------

        #region VisualChildren

        /// <summary>
        ///  Derived classes override this property to enable the Visual code to enumerate
        ///  the Visual children. Derived classes need to return the number of children
        ///  from this method.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark: During this virtual method the Visual tree must not be modified.
        /// </summary>
        protected virtual int VisualChildrenCount
        {
            get { return 0; }
        }

        /// <summary>
        /// Returns the number of 2D children. This returns 0 for visuals
        /// whose children are Visual3Ds.
        /// </summary>
        internal int InternalVisualChildrenCount
        {
            get
            {
                // Call the right virtual method.
                return VisualChildrenCount;
            }
        }

        /// <summary>
        /// Returns the number of children of this object (in most cases this will be
        /// the number of Visuals, but it some cases, Viewport3DVisual for instance,
        /// this is the number of Visual3Ds).
        ///
        /// Used only by VisualTreeHelper.
        /// </summary>
        internal virtual int InternalVisual2DOr3DChildrenCount
        {
            get
            {
                // Call the right virtual method.
                return VisualChildrenCount;
            }
        }

        ///<Summary>
        ///Flag to check if this visual has any children
        ///</Summary>
        internal bool HasVisualChildren
        {
            get
            {
                return ((_flags & VisualFlags.HasChildren) != 0);
            }
        }

        /// <summary>
        ///   Derived class must implement to support Visual children. The method must return
        ///    the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark:
        ///       Need to lock down Visual tree during the callbacks.
        ///       During this virtual call it is not valid to modify the Visual tree.
        ///
        ///       It is okay to type this protected API to the 2D Visual.  The only 2D Visual with
        ///       3D childern is the Viewport3DVisual which is sealed
        /// </summary>
        protected virtual Visual GetVisualChild(int index)
        {
           throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
        }

        /// <summary>
        /// Returns the 2D child at index "index". This will fail for Visuals
        /// whose children are Visual3Ds.
        /// </summary>
        internal Visual InternalGetVisualChild(int index)
        {
            // Call the right virtual method.
            return GetVisualChild(index);
        }

        /// <summary>
        /// Returns the child at index "index" (in most cases this will be
        /// a Visual, but it some cases, Viewport3DVisual for instance,
        /// this is a Visual3D).
        ///
        /// Used only by VisualTreeHelper.
        /// </summary>
        internal virtual DependencyObject InternalGet2DOr3DVisualChild(int index)
        {
            // Call the right virtual method.
            return GetVisualChild(index);
        }

        /// <summary>
        /// Helper method to provide access to AddVisualChild for the VisualCollection.
        /// </summary>
        internal void InternalAddVisualChild(Visual child)
        {
            this.AddVisualChild(child);
        }

        /// <summary>
        /// Helper method to provide access to RemoveVisualChild for the VisualCollection.
        /// </summary>
        internal void InternalRemoveVisualChild(Visual child)
        {
            this.RemoveVisualChild(child);
        }

        /// <summary>
        /// AttachChild
        ///
        ///    Derived classes must call this method to notify the Visual layer that a new
        ///    child appeard in the children collection. The Visual layer will then call the GetVisualChild
        ///    method to find out where the child was added.
        ///
        ///  Remark: To move a Visual child in a collection it must be first disconnected and then connected
        ///    again. (Moving forward we might want to add a special optimization there so that we do not
        ///    unmarshal our composition resources).
        ///
        ///    It is okay to type this protected API to the 2D Visual.  The only 2D Visual with
        ///    3D childern is the Viewport3DVisual which is sealed.
        /// </summary>
        protected void AddVisualChild(Visual child)
        {
            if (child == null)
            {
                return;
            }

            if (child._parent != null)
            {
                throw new ArgumentException(SR.Get(SRID.Visual_HasParent));
            }

            // invalid during a VisualTreeChanged event
            VisualDiagnostics.VerifyVisualTreeChange(this);

            SetFlags(true, VisualFlags.HasChildren);

            // Set the parent pointer.

            child._parent = this;

            //
            // The child might be dirty. Hence we need to propagate dirty information
            // from the parent and from the child.
            //

            Visual.PropagateFlags(
                this,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.IsSubtreeDirtyForRender);

            Visual.PropagateFlags(
                child,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.IsSubtreeDirtyForRender);

            //
            // Resume layout.
            //
            UIElement.PropagateResumeLayout(this, child);

            if (HwndTarget.IsProcessPerMonitorDpiAware == true && HwndTarget.IsPerMonitorDpiScalingEnabled)
            {
                bool flag1 = CheckFlagsAnd(VisualFlags.DpiScaleFlag1);
                bool flag2 = CheckFlagsAnd(VisualFlags.DpiScaleFlag2);
                int index = 0; // dummy value;
                if (flag1 && flag2)
                {
                    index = DpiIndex.GetValue(this);
                }

                child.RecursiveSetDpiScaleVisualFlags(new DpiRecursiveChangeArgs(new DpiFlags(flag1, flag2, index),
                    child.GetDpi(), this.GetDpi()));
            }

            // Fire notifications
            this.OnVisualChildrenChanged(child, null /* no removed child */);
            child.FireOnVisualParentChanged(null);
            VisualDiagnostics.OnVisualChildChanged(this, child, true);
        }

        /// <summary>
        /// DisconnectChild
        ///
        ///    Derived classes must call this method to notify the Visual layer that a
        ///    child was removed from the children collection. The Visual layer will then call
        ///    GetChildren to find out which child has been removed.
        ///
        /// </summary>
        protected void RemoveVisualChild(Visual child)
        {
            if (child == null || child._parent == null)
            {
                return;
            }

            if (child._parent != this)
            {
                throw new ArgumentException(SR.Get(SRID.Visual_NotChild));
            }

            // invalid during a VisualTreeChanged event
            VisualDiagnostics.VerifyVisualTreeChange(this);

            VisualDiagnostics.OnVisualChildChanged(this, child, false);

            if (InternalVisual2DOr3DChildrenCount == 0)
            {
                SetFlags(false, VisualFlags.HasChildren);
            }

            //
            // Remove the child on all channels its current parent is marshalled to.
            //

            for (int i = 0; i < _proxy.Count; i++)
            {
                DUCE.Channel channel = _proxy.GetChannel(i);

                if (child.CheckFlagsAnd(channel, VisualProxyFlags.IsConnectedToParent))
                {
                    child.SetFlags(channel, false, VisualProxyFlags.IsConnectedToParent);
                    DUCE.IResource childResource = (DUCE.IResource)child;
                    childResource.RemoveChildFromParent(this, channel);
                    childResource.ReleaseOnChannel(channel);
                }
            }

            // Set the parent pointer to null.

            child._parent = null;

            Visual.PropagateFlags(
                this,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.IsSubtreeDirtyForRender);

            UIElement.PropagateSuspendLayout(child);

            // Fire notifications
            child.FireOnVisualParentChanged(this);
            OnVisualChildrenChanged(null /* no child added */, child);
        }

        /// <summary>
        /// InvalidateZOrder
        /// Note: must do invalidation without removing / adding
        /// to avoid loosing focused element by input system
        /// </summary>
        [FriendAccessAllowed]
        internal void InvalidateZOrder()
        {
            //  if we don't have any children, there is nothing to do
            if (VisualChildrenCount == 0)
                return;

            Visual.PropagateFlags(
                       this,
                        VisualFlags.IsSubtreeDirtyForPrecompute,
                        VisualProxyFlags.IsSubtreeDirtyForRender);

            this.SetFlagsOnAllChannels(true, VisualProxyFlags.IsChildrenZOrderDirty);

            //  This is a workaround
            //  Input system needs to be notified about the changes on screen to be able to re-hittest
            System.Windows.Input.InputManager.SafeCurrentNotifyHitTestInvalidated();
        }

        //This is used by LayoutManager as a perf optimization for layout updates.
        //During layout updates, LM needs to find which areas of the visual tree
        //are higher in the tree - they have to be processed first to avoid multiple
        //updates of lower descendants. The tree level counter is maintained by
        //UIElement.PropagateResume/SuspendLayout methods and uses 8 bits in VisualFlags to
        //keep the count.
        internal uint TreeLevel
        {
            get
            {
                return ((uint)_flags & 0xFFE00000) >> 21;
            }
            set
            {
                if(value > TreeLevelLimit)
                {
                    throw new InvalidOperationException(SR.Get(SRID.LayoutManager_DeepRecursion, TreeLevelLimit));
                }

                _flags = (VisualFlags)(((uint)_flags & 0x001FFFFF) | (value << 21));
            }
        }


        #endregion VisualChildren


        #region VisualParent

        /// <summary>
        /// Returns the parent of this Visual.  Parent may be either a Visual or Visual3D.
        /// </summary>
        protected DependencyObject VisualParent
        {
            get
            {
                VerifyAPIReadOnly();

                return InternalVisualParent;
            }
        }

        /// <summary>
        /// Identical to VisualParent, except that skips verify access for perf.
        /// </summary>
        internal DependencyObject InternalVisualParent
        {
            get
            {
                return _parent;
            }
        }

        #endregion VisualParent

        // These 2 method will be REMOVED once Hamid is back and can
        // explain why Window needs to Bypass layout for setting Flow Direction.
        // These methods are only called from InternalSetLayoutTransform which is called only from Window
        [FriendAccessAllowed]
        internal void InternalSetOffsetWorkaround(Vector offset)
        {
            VisualOffset = offset;
        }
        [FriendAccessAllowed]
        internal void InternalSetTransformWorkaround(Transform transform)
        {
            VisualTransform = transform;
        }

        // --------------------------------------------------------------------
        //
        //   Visual Properties
        //
        // --------------------------------------------------------------------

        #region Visual Properties

        /// <summary>
        /// Gets or sets the transform of this Visual.
        /// </summary>
        protected internal Transform VisualTransform
        {
            get
            {
                VerifyAPIReadOnly();

                return TransformField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite(value);

                Transform transform = TransformField.GetValue(this);
                if (transform == value)
                {
                    return;
                }

                Transform newTransform = value;

                // Add changed notifications for the new transform if necessary.
                if (newTransform != null && !newTransform.IsFrozen)
                {
                    newTransform.Changed += TransformChangedHandler;
                }

                if (transform != null)
                {
                    //
                    // Remove changed notifications for the old transform if necessary.
                    //

                    if (!transform.IsFrozen)
                    {
                        transform.Changed -= TransformChangedHandler;
                    }

                    //
                    // Disconnect the transform from this visual.
                    //

                    DisconnectAttachedResource(
                        VisualProxyFlags.IsTransformDirty,
                        ((DUCE.IResource)transform));
                }

                //
                // Set the new clip and mark it dirty
                //

                TransformField.SetValue(this, newTransform);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsTransformDirty);

                TransformChanged(/* sender */ null, /* args */ null);
            }
        }

        /// <summary>
        /// Gets or sets the Effect of this Visual.
        /// </summary>
        protected internal Effect VisualEffect
        {
            get
            {
                VerifyAPIReadOnly();

                return VisualEffectInternal;
            }
            protected set
            {
                VerifyAPIReadWrite(value);

                // Legacy BitmapEffects and new Effects cannot be mixed because the new image effect
                // pipeline may be used to emulate a legacy BitmapEffect.
                BitmapEffectState bed = UserProvidedBitmapEffectData.GetValue(this);
                if (bed != null)
                {
                    if (value != null) // UIElement has a tendency to set a lot of properties to null even if it
                                       // never set a property to a different value in the first place.
                    {
                        // If a BitmapEffect is set, the user cannot set an Effect, since
                        // mixing of legacy BitmapEffects is not allowed with Effects.
                        throw new Exception(SR.Get(SRID.Effect_CombinedLegacyAndNew));
                    }
                    else
                    {
                        return;
                    }
                }

                VisualEffectInternal = value;
}
        }

        /// <summary>
        /// Internal accessor to image effect property that gets or sets the Effect of this Visual.
        /// The internal accessor is used by the VisualBitmapEffect emulation layer to avoid some of the
        /// compatibility checks in the protected VisualEffect property.
        /// </summary>
        internal Effect VisualEffectInternal
        {
            get
            {
                // Legacy BitmapEffects and new Effects cannot be mixed because the new image effect
                // pipeline may be used to emulate a legacy BitmapEffect. Therefore, if a BitmapEffect is
                // assigned to this node, the Effect is conceptually not set and null must be returned
                // from this getter. If no BitmapEffect is set on this node, the Effect has been provided
                // by the user and therefore the Effect is returned.

                if (NodeHasLegacyBitmapEffect)
                {
                    return null;
                }
                else
                {
                    return EffectField.GetValue(this);
                }
            }

            set
            {
                Effect imageEffect = EffectField.GetValue(this);
                if (imageEffect == value)
                {
                    return;
                }

                Effect newEffect = value;

                // Add changed notifications for the new Effect if necessary.
                if (newEffect != null && !newEffect.IsFrozen)
                {
                    newEffect.Changed += EffectChangedHandler;
                }

                if (imageEffect != null)
                {
                    //
                    // Remove changed notifications for the old Effect if necessary.
                    //

                    if (!imageEffect.IsFrozen)
                    {
                        imageEffect.Changed -= EffectChangedHandler;
                    }

                    //
                    // Disconnect the Effect from this visual.
                    //

                    DisconnectAttachedResource(
                        VisualProxyFlags.IsEffectDirty,
                        ((DUCE.IResource)imageEffect));
                }

                //
                // Set the new effect and mark it dirty
                //

                SetFlags(newEffect != null, VisualFlags.NodeHasEffect);

                EffectField.SetValue(this, newEffect);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsEffectDirty);

                EffectChanged(/* sender */ null, /* args */ null);
            }
        }

        /// <summary>
        /// BitmapEffect Property -
        /// Gets or sets the optional BitmapEffect.  If set, the BitmapEffect will
        /// be applied Visual's rendered content, after which the OpacityMask and/or Opacity
        /// will be applied (if present).
        /// </summary>
        [Obsolete(MS.Internal.Media.VisualTreeUtils.BitmapEffectObsoleteMessage)]
        protected internal BitmapEffect VisualBitmapEffect
        {
            get
            {
                VerifyAPIReadOnly();

                BitmapEffectState bed = UserProvidedBitmapEffectData.GetValue(this);

                if (bed != null)
                {
                    return bed.BitmapEffect;
                }
                else
                {
                    return null;
                }
            }

            protected set
            {
                VerifyAPIReadWrite(value);

                //
                // Figure out if a image effect has been provided by the user. If so, calling this API is illegal
                // since new Effects and legacy BitmapEffects cannot be mixed.

                Effect imageEffect = EffectField.GetValue(this);
                BitmapEffectState bed = UserProvidedBitmapEffectData.GetValue(this);
                if (   (bed == null)
                    && (imageEffect != null))

                {
                    if (value != null) // Allowing incoming value of null because UIElements tend
                                       // to aggressively set this property to null even if it has never been set.
                    {
                        // If no BitmapEffect is set and an Effect is set, the Effect has been
                        // provided by the user and not by emulation. Since mixing of legacy
                        // BitmapEffects is not allowed with Effects, setting a BitmapEffect is illegal.
                        throw new Exception(SR.Get(SRID.Effect_CombinedLegacyAndNew));
                    }
                    else
                    {
                        return;
                    }
                }


                //
                // To enable emulation of the legacy effects on top of the new effects pipeline, store the
                // bitmap effect information in our staging uncommon field: UserProvidedBitmapEffectData.

                BitmapEffect oldBitmapEffect = (bed == null) ? null : bed.BitmapEffect;
                if (oldBitmapEffect == value) // If new and old value are the same, this set call can be treated as a no-op.
                {
                    return;
                }

                BitmapEffect newBitmapEffect = value;

                if (newBitmapEffect == null)
                {
                    Debug.Assert(bed != null, "Must be non-null because otherwise the code would have earlied out where new value is compared against old value.");
                    // The following line of code will effectively set the BitmapEffectInput property to null. This is strange behavior for WPF properties, but follows the
                    // original BitmapEffects implementation.
                    UserProvidedBitmapEffectData.SetValue(this, null);
                }
                else
                {
                    if (bed == null)
                    {
                        bed = new BitmapEffectState();
                        UserProvidedBitmapEffectData.SetValue(this, bed);
                    }

                    bed.BitmapEffect = newBitmapEffect;
                }

                if (newBitmapEffect != null && !newBitmapEffect.IsFrozen)
                {
                    newBitmapEffect.Changed += new EventHandler(BitmapEffectEmulationChanged);
                }
                if (oldBitmapEffect != null && !oldBitmapEffect.IsFrozen)
                {
                    oldBitmapEffect.Changed -= new EventHandler(BitmapEffectEmulationChanged);
                }

                // Notify about the bitmap effect changes to configure the new emulation.
                BitmapEffectEmulationChanged(/* sender */ null, /* args */ null);
            }
        }


        /// <summary>
        /// BitmapEffectInput Property -
        /// Gets or sets the optional BitmapEffectInput.  If set, the BitmapEffectInput will
        /// be applied Visual's rendered content, after which the OpacityMask and/or Opacity
        /// will be applied (if present).
        /// </summary>
        [Obsolete(MS.Internal.Media.VisualTreeUtils.BitmapEffectObsoleteMessage)]
        protected internal BitmapEffectInput VisualBitmapEffectInput
        {
            get
            {
                VerifyAPIReadOnly();

                BitmapEffectState bed = UserProvidedBitmapEffectData.GetValue(this);

                if (bed != null)
                {
                    return bed.BitmapEffectInput;
                }
                else
                {
                    return null;
                }
            }

            protected set
            {
                VerifyAPIReadWrite(value);

                //
                // Figure out if a image effect has been provided by the user. If so, calling this API is illegal
                // sinc new Effects and legacy BitmapEffects cannot be mixed.

                Effect imageEffect = EffectField.GetValue(this);
                BitmapEffectState bed = UserProvidedBitmapEffectData.GetValue(this);
                if ((bed == null) && (imageEffect != null))
                {
                    if (value != null) // Allowing null because parser and UIElement tend to set this property to null
                                       // even if it has never been set to non-null.
                    {
                        // If no BitmapEffect is set and an Effect is set, the Effect has been
                        // provided by the user. Since mixing of legacy BitmapEffects is not allowed with
                        // Effects, setting a BitmapEffect is illegal.
                        throw new Exception(SR.Get(SRID.Effect_CombinedLegacyAndNew));
                    }
                    else
                    {
                        return;
                    }
                }


                //
                // To enable emulation of the legacy effects on top of the new effects pipeline, store the
                // bitmap effect input information in our staging uncommon field: UserProvidedBitmapEffectData.

                BitmapEffectInput oldBitmapEffectInput = (bed == null) ? null : bed.BitmapEffectInput;
                BitmapEffectInput newBitmapEffectInput = value;

                if (oldBitmapEffectInput == newBitmapEffectInput) // If new and old value are the same, this set call can be treated as a no-op.
                {
                    return;
                }

                // Make sure there is a BitmapEffectData instance allocated.
                if (bed == null)
                {
                    bed = new BitmapEffectState();
                    UserProvidedBitmapEffectData.SetValue(this, bed);
                }

                bed.BitmapEffectInput = newBitmapEffectInput;


                if (newBitmapEffectInput != null && !newBitmapEffectInput.IsFrozen)
                {
                    newBitmapEffectInput.Changed += new EventHandler(BitmapEffectEmulationChanged);
                }
                if (oldBitmapEffectInput != null && !oldBitmapEffectInput.IsFrozen)
                {
                    oldBitmapEffectInput.Changed -= new EventHandler(BitmapEffectEmulationChanged);
                }

                // Notify about the bitmap effect changes to configure the new emulation.
                BitmapEffectEmulationChanged(/* sender */ null, /* args */ null);
            }
        }


        // <summary>
        // This handler reconfigures the bitmap effects pipeline whenever anything changes. It is
        // responsible for figuring out if a legacy effect can be emulated on the new pipeline or
        // not.
        // </summary>
        internal void BitmapEffectEmulationChanged(object sender, EventArgs e)
        {
            BitmapEffectState bed = UserProvidedBitmapEffectData.GetValue(this);
            BitmapEffect currentBitmapEffect = (bed == null) ? null : bed.BitmapEffect;
            BitmapEffectInput currentBitmapEffectInput = (bed == null) ? null : bed.BitmapEffectInput;

            // Note that when this method is called, a legacy BitmapEffect has been set or reset on
            // the Visual by the user. The next step is to try to emulate the effect in case the current
            // effect is non null or reset the emulation layer if the user has set the effect to null.

            if (currentBitmapEffect == null)
            {
                // This means the effect has been disconnected from this Visual. Setting the internal
                // bitmap effect property and the image effect property to null to disconnect all the
                // effects. The Effect property needs to be set to null because the effect might
                // be emulated.
                VisualBitmapEffectInternal = null;
                VisualBitmapEffectInputInternal = null;
                VisualEffectInternal = null;
            }
            else if (currentBitmapEffectInput != null)
            {
                // If a BitmapEffectInput is specified, make sure the legacy effect is not being
                // emulated using the Effect pipeline since the new pipeline does not support
                // BitmapEffecInputs.
                VisualEffectInternal = null;
                VisualBitmapEffectInternal = currentBitmapEffect;
                VisualBitmapEffectInputInternal = currentBitmapEffectInput;
            }
            else if (RenderCapability.IsShaderEffectSoftwareRenderingSupported &&
                    currentBitmapEffect.CanBeEmulatedUsingEffectPipeline() &&
                    (!CheckFlagsAnd(VisualFlags.BitmapEffectEmulationDisabled)))
            {
                // If we can emulate the effect switch to emulating it.
                VisualBitmapEffectInternal = null;
                VisualBitmapEffectInputInternal = null;
                Effect emulatingEffect = currentBitmapEffect.GetEmulatingEffect();
                Debug.Assert(currentBitmapEffect.IsFrozen == emulatingEffect.IsFrozen);

                VisualEffectInternal = emulatingEffect;
            }
            else
            {
                // Cannot emulate the effect, using legacy pipeline.
                VisualEffectInternal = null;
                VisualBitmapEffectInputInternal = null;
                VisualBitmapEffectInternal = currentBitmapEffect;
            }
}

        /// <summary>
        /// Used by the test team to disable bitmap effect emulation for testing purposes.
        /// </summary>
        internal bool BitmapEffectEmulationDisabled
        {
            get
            {
                return CheckFlagsAnd(VisualFlags.BitmapEffectEmulationDisabled);
            }
            set
            {
                if (value != CheckFlagsAnd(VisualFlags.BitmapEffectEmulationDisabled))
                {
                    SetFlags(value, VisualFlags.BitmapEffectEmulationDisabled);

                    // Notify about the bitmap effect changes to configure the new emulation.
                    BitmapEffectEmulationChanged(/* sender */ null, /* args */ null);
                }
            }
        }

        /// <summary>
        /// Internal accessor to BitmapEffect property that gets or sets the BitmapEffect of this Visual.
        /// The internal accessor is used by the VisualBitmapEffect emulation layer to avoid some of the
        /// compatibility checks in the protected VisualBitmapEffect property.
        /// </summary>
        internal BitmapEffect VisualBitmapEffectInternal
        {
            get
            {
                VerifyAPIReadOnly();

                if (NodeHasLegacyBitmapEffect)
                {
                    return BitmapEffectStateField.GetValue(this).BitmapEffect;
                }
                else
                {
                    return null;
                }
            }

            set
            {
                BitmapEffectState bitmapEffectState = BitmapEffectStateField.GetValue(this);

                BitmapEffect bitmapEffect = (bitmapEffectState == null) ? null : bitmapEffectState.BitmapEffect;
                if (bitmapEffect == value)
                {
                    return;
                }

                BitmapEffect newBitmapEffect = value;

                if (newBitmapEffect == null)
                {
                    Debug.Assert(bitmapEffectState != null);

                    BitmapEffectStateField.SetValue(this, null);
                }
                else
                {
                    if (bitmapEffectState == null)
                    {
                        bitmapEffectState = new BitmapEffectState();
                        BitmapEffectStateField.SetValue(this, bitmapEffectState);
                    }

                    bitmapEffectState.BitmapEffect = newBitmapEffect;

                    Debug.Assert(EffectField.GetValue(this) == null, "Not expecting both BitmapEffect and Effect to be set on the same node");
                }
            }
        }

        /// <summary>
        /// Internal accessor to BitmapEffectInput property that gets or sets the BitmapEffectInput of this Visual.
        /// The internal accessor is used by the VisualBitmapEffect emulation layer to avoid some of the
        /// compatibility checks in the protected VisualBitmapEffectInput property.
        /// </summary>
        internal BitmapEffectInput VisualBitmapEffectInputInternal
        {
            get
            {
                VerifyAPIReadOnly();
                BitmapEffectState bitmapEffectState = BitmapEffectStateField.GetValue(this);
                if (bitmapEffectState != null)
                    return bitmapEffectState.BitmapEffectInput;

                return null;
            }

            set
            {
                VerifyAPIReadWrite();
                BitmapEffectState bitmapEffectState = BitmapEffectStateField.GetValue(this);

                BitmapEffectInput bitmapEffectInput = (bitmapEffectState == null) ? null : bitmapEffectState.BitmapEffectInput;
                if (bitmapEffectInput == value)
                {
                    return;
                }

                BitmapEffectInput newBitmapEffectInput = value;

                if (bitmapEffectState == null)
                {
                    bitmapEffectState = new BitmapEffectState();
                    BitmapEffectStateField.SetValue(this, bitmapEffectState);
                }

                bitmapEffectState.BitmapEffectInput = newBitmapEffectInput;
            }
        }


        /// <summary>
        /// Gets or sets the caching behavior for the Visual.
        /// </summary>
        protected internal CacheMode VisualCacheMode
        {
            get
            {
                VerifyAPIReadOnly();

                return CacheModeField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite(value);

                CacheMode cacheMode = CacheModeField.GetValue(this);
                if (cacheMode == value)
                {
                    return;
                }

                CacheMode newCacheMode = value;

                // Add changed notifications for the new cache mode if necessary.
                if (newCacheMode != null && !newCacheMode.IsFrozen)
                {
                    newCacheMode.Changed += CacheModeChangedHandler;
                }

                if (cacheMode != null)
                {
                    //
                    // Remove changed notifications for the old cache mode if necessary.
                    //

                    if (!cacheMode.IsFrozen)
                    {
                        cacheMode.Changed -= CacheModeChangedHandler;
                    }

                    //
                    // Disconnect the cache mode from this visual.
                    //
                    DisconnectAttachedResource(
                        VisualProxyFlags.IsCacheModeDirty,
                        ((DUCE.IResource)cacheMode));
                }

                //
                // Set the new cache mode and mark it dirty
                //

                CacheModeField.SetValue(this, newCacheMode);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsCacheModeDirty);

                CacheModeChanged(/* sender */ null, /* args */ null);
            }
        }

        /// <summary>
        /// Gets or sets the scrollable area clip for the Visual.
        /// </summary>
        protected internal Rect? VisualScrollableAreaClip
        {
            get
            {
                VerifyAPIReadOnly();

                return ScrollableAreaClipField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite();

                Rect? currentValue = ScrollableAreaClipField.GetValue(this);

                if (currentValue != value)
                {
                    ScrollableAreaClipField.SetValue(this, value);

                    SetFlagsOnAllChannels(true, VisualProxyFlags.IsScrollableAreaClipDirty);

                    ScrollableAreaClipChanged(/* sender */ null, /* args */ null);
                }
            }
        }

        /// <summary>
        /// Gets or sets the clip of this Visual.
        /// </summary>
        protected internal Geometry VisualClip
        {
            get
            {
                VerifyAPIReadOnly();

                return ClipField.GetValue(this);
            }
            protected set
            {
                ChangeVisualClip(value, false /* dontSetWhenClose */);
            }
        }

        /// <summary>
        ///     Processes changing the clip from the old clip to the new clip.
        ///     Called from Visual.set_VisualClip and from places that want
        ///     to optimize setting a new clip (like UIElement.ensureClip).
        /// </summary>
        internal void ChangeVisualClip(Geometry newClip, bool dontSetWhenClose)
        {
            VerifyAPIReadWrite(newClip);

            Geometry oldClip = ClipField.GetValue(this);
            if ((oldClip == newClip) ||
                (dontSetWhenClose && (oldClip != null) && (newClip != null) && oldClip.AreClose(newClip)))
            {
                return;
            }

            // Add changed notifications for the new clip if necessary.
            if (newClip != null && !newClip.IsFrozen)
            {
                newClip.Changed += ClipChangedHandler;
            }

            if (oldClip != null)
            {
                //
                // Remove changed notifications for the old clip if necessary.
                //

                if (!oldClip.IsFrozen)
                {
                    oldClip.Changed -= ClipChangedHandler;
                }

                //
                // Disconnect the clip from this visual.
                //

                DisconnectAttachedResource(
                    VisualProxyFlags.IsClipDirty,
                    ((DUCE.IResource)oldClip));
            }

            //
            // Set the new clip and mark it dirty
            //

            ClipField.SetValue(this, newClip);

            SetFlagsOnAllChannels(true, VisualProxyFlags.IsClipDirty);

            ClipChanged(/* sender */ null, /* args */ null);
        }

        /// <summary>
        /// Gets and sets the offset.
        /// </summary>
        protected internal Vector VisualOffset
        {
            get
            {
                // VerifyAPIReadOnly(); // Intentionally removed for performance reasons.
                return _offset;
            }
            protected set
            {
                VerifyAPIReadWrite();

                if (value != _offset) // Fuzzy comparison might be better here.
                {
                    VisualFlags flags;

                    _offset = value;

                    SetFlagsOnAllChannels(true, VisualProxyFlags.IsOffsetDirty);

                    flags = VisualFlags.IsSubtreeDirtyForPrecompute;

                    PropagateFlags(
                        this,
                        flags,
                        VisualProxyFlags.IsSubtreeDirtyForRender);
                }
            }
        }

        /// <summary>
        /// Gets or sets the opacity of the Visual.
        /// </summary>
        protected internal double VisualOpacity
        {
            get
            {
                VerifyAPIReadOnly();

                return OpacityField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite();

                if (OpacityField.GetValue(this) == value)
                {
                    return;
                }

                OpacityField.SetValue(this, value);

                // Microsoft: We need to do more here for animated opacity.

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsOpacityDirty);

                PropagateFlags(
                    this,
                    VisualFlags.None,
                    VisualProxyFlags.IsSubtreeDirtyForRender);
            }
        }

        /// <summary>
        /// Gets or sets the EdgeMode of the Visual.
        /// </summary>
        protected internal EdgeMode VisualEdgeMode
        {
            get
            {
                VerifyAPIReadOnly();

                return EdgeModeField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite();

                if (EdgeModeField.GetValue(this) == value)
                {
                    return;
                }

                EdgeModeField.SetValue(this, value);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsEdgeModeDirty);

                PropagateFlags(
                    this,
                    VisualFlags.None,
                    VisualProxyFlags.IsSubtreeDirtyForRender);
            }
        }

        /// <summary>
        /// Gets or sets the ImageScalingMode of the Visual.
        /// </summary>
        protected internal BitmapScalingMode VisualBitmapScalingMode
        {
            get
            {
                VerifyAPIReadOnly();

                return BitmapScalingModeField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite();

                if (BitmapScalingModeField.GetValue(this) == value)
                {
                    return;
                }

                BitmapScalingModeField.SetValue(this, value);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsBitmapScalingModeDirty);

                PropagateFlags(
                    this,
                    VisualFlags.None,
                    VisualProxyFlags.IsSubtreeDirtyForRender);
            }
        }

        /// <summary>
        /// Gets or sets the ClearTypeHint of the Visual.
        /// </summary>
        protected internal ClearTypeHint VisualClearTypeHint
        {
            get
            {
                VerifyAPIReadOnly();

                return ClearTypeHintField.GetValue(this);
            }
            set
            {
                VerifyAPIReadWrite();

                if (ClearTypeHintField.GetValue(this) == value)
                {
                    return;
                }

                ClearTypeHintField.SetValue(this, value);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsClearTypeHintDirty);

                PropagateFlags(
                    this,
                    VisualFlags.None,
                    VisualProxyFlags.IsSubtreeDirtyForRender);
            }
        }

        /// <summary>
        /// Gets or sets the TextRenderingMode of the Visual.
        /// </summary>
        protected internal TextRenderingMode VisualTextRenderingMode
        {
            get
            {
                VerifyAPIReadOnly();

                return TextRenderingModeField.GetValue(this);
            }
            set
            {
                VerifyAPIReadWrite();

                if (TextRenderingModeField.GetValue(this) == value)
                {
                    return;
                }

                TextRenderingModeField.SetValue(this, value);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsTextRenderingModeDirty);

                PropagateFlags(
                    this,
                    VisualFlags.None,
                    VisualProxyFlags.IsSubtreeDirtyForRender);
            }
        }

        /// <summary>
        /// Gets or sets the TextRenderingMode of the Visual.
        /// </summary>
        protected internal TextHintingMode VisualTextHintingMode
        {
            get
            {
                VerifyAPIReadOnly();

                return TextHintingModeField.GetValue(this);
            }
            set
            {
                VerifyAPIReadWrite();

                if (TextHintingModeField.GetValue(this) == value)
                {
                    return;
                }

                TextHintingModeField.SetValue(this, value);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsTextHintingModeDirty);

                PropagateFlags(
                    this,
                    VisualFlags.None,
                    VisualProxyFlags.IsSubtreeDirtyForRender);
            }
        }

        /// <summary>
        /// OpacityMask Property -
        /// Gets or sets the optional OpacityMask.  If set, the Brush's opacity will
        /// be combined multiplicitively with the Visual's rendered content.
        /// </summary>
        protected internal Brush VisualOpacityMask
        {
            get
            {
                VerifyAPIReadOnly();

                return OpacityMaskField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite(value);

                Brush opacityMask = OpacityMaskField.GetValue(this);
                if (opacityMask == value)
                {
                    return;
                }

                Brush newOpacityMask = value;

                // Add changed notifications for the new opacity mask if necessary.
                if (newOpacityMask != null && !newOpacityMask.IsFrozen)
                {
                    newOpacityMask.Changed += OpacityMaskChangedHandler;
                }

                if (opacityMask != null)
                {
                    //
                    // Remove changed notifications for the old opacity mask if necessary.
                    //

                    if (!opacityMask.IsFrozen)
                    {
                        opacityMask.Changed -= OpacityMaskChangedHandler;
                    }

                    //
                    // Disconnect the opacity mask from this visual.
                    //
                    DisconnectAttachedResource(
                        VisualProxyFlags.IsOpacityMaskDirty,
                        ((DUCE.IResource)opacityMask));
                }

                //
                // Set the new opacity mask and mark it dirty
                //

                OpacityMaskField.SetValue(this, newOpacityMask);

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsOpacityMaskDirty);

                OpacityMaskChanged(/* sender */ null, /* args */ null);
            }
        }


        /// <summary>
        /// Gets or sets X- (vertical) guidelines on this Visual.
        /// </summary>
        protected internal DoubleCollection VisualXSnappingGuidelines
        {
            get
            {
                VerifyAPIReadOnly();

                return GuidelinesXField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite(value);

                DoubleCollection guidelines = GuidelinesXField.GetValue(this);
                if (guidelines == value)
                {
                    return;
                }

                DoubleCollection newGuidelines = value;

                // Add changed notifications for the new guidelines if necessary.
                if (newGuidelines != null && !newGuidelines.IsFrozen)
                {
                    newGuidelines.Changed += GuidelinesChangedHandler;
                }

                // Remove changed notifications for the old guidelines if necessary.
                if (guidelines != null && !guidelines.IsFrozen)
                {
                    guidelines.Changed -= GuidelinesChangedHandler;
                }

                GuidelinesXField.SetValue(this, newGuidelines);

                GuidelinesChanged(/* sender */ null, /* args */ null);
            }
        }


        /// <summary>
        /// Gets or sets Y- (horizontal) guidelines of this Visual.
        /// </summary>
        protected internal DoubleCollection VisualYSnappingGuidelines
        {
            get
            {
                VerifyAPIReadOnly();

                return GuidelinesYField.GetValue(this);
            }
            protected set
            {
                VerifyAPIReadWrite(value);

                DoubleCollection guidelines = GuidelinesYField.GetValue(this);
                if (guidelines == value)
                {
                    return;
                }

                DoubleCollection newGuidelines = value;

                // Add changed notifications for the new guidelines if necessary.
                if (newGuidelines != null && !newGuidelines.IsFrozen)
                {
                    newGuidelines.Changed += GuidelinesChangedHandler;
                }

                // Remove changed notifications for the old guidelines if necessary.
                if (guidelines != null && !guidelines.IsFrozen)
                {
                    guidelines.Changed -= GuidelinesChangedHandler;
                }

                GuidelinesYField.SetValue(this, newGuidelines);

                GuidelinesChanged(/* sender */ null, /* args */ null);
            }
        }

        #endregion Visual Properties

        /// <summary>
        /// Disconnects a resource attached to this visual.
        /// </summary>
        internal void DisconnectAttachedResource(
            VisualProxyFlags correspondingFlag,
            DUCE.IResource attachedResource)
        {
            //
            // We need a special case for the content (corresponding
            // to the IsContentConnected flag).
            //

            bool needToReleaseContent =
                correspondingFlag == VisualProxyFlags.IsContentConnected;


            //
            // Iterate over the channels this visual is being marshaled to
            //

            for (int i = 0; i < _proxy.Count; i++)
            {
                DUCE.Channel channel = _proxy.GetChannel(i);
                VisualProxyFlags flags = _proxy.GetFlags(i);

                //
                // See if the corresponding flag is set...
                //

                bool correspondingFlagSet =
                    (flags & correspondingFlag) != 0;


                //
                // We want to perform an action if IsContentConnected
                // flag is set or a Is*Dirty flag is not set:
                //

                if (correspondingFlagSet == needToReleaseContent)
                {
                    //
                    // Set the flag so that during render we send
                    // update to the compositor.
                    //
                    SetFlags(channel, true, correspondingFlag);

                    attachedResource.ReleaseOnChannel(channel);


                    if (needToReleaseContent)
                    {
                        //
                        // Mark the content of this visual as disconnected.
                        //

                        _proxy.SetFlags(i, false, VisualProxyFlags.IsContentConnected);
                    }
                }
            }
        }




        /// <summary>
        /// GetDrawing - Returns the Drawing content of this Visual
        /// </summary>
        internal virtual DrawingGroup GetDrawing()
        {
            VerifyAPIReadOnly();

            // Default implementation returns null for Visual's that
            // don't have drawings
            return null;
        }



        // --------------------------------------------------------------------
        //
        //   Visual Ancestry Relations
        //
        // --------------------------------------------------------------------

        #region Visual Ancestry Relations

        /// <summary>
        /// This is called when the parent link of the Visual is changed.
        /// This method executes important base functionality before calling the
        /// overridable virtual.
        /// </summary>
        /// <param name="oldParent">Old parent or null if the Visual did not have a parent before.</param>
        internal virtual void FireOnVisualParentChanged(DependencyObject oldParent)
        {
            // Call the ParentChanged virtual before firing the Ancestor Changed Event
            OnVisualParentChanged(oldParent);

            // Clean up bits when the tree is Cut or Pasted.

            // If we are attaching to a tree then
            // send the bit up if we need to.
            if(oldParent == null)
            {
                Debug.Assert(_parent != null, "If oldParent is null, current parent should != null.");

                if(CheckFlagsAnd(VisualFlags.SubTreeHoldsAncestorChanged))
                {
                    SetTreeBits(
                        _parent,
                        VisualFlags.SubTreeHoldsAncestorChanged,
                        VisualFlags.RegisteredForAncestorChanged);
                }
            }
            // If we are cutting a sub tree off then
            // clear the bit in the main tree above if we need to.
            else
            {
                if(CheckFlagsAnd(VisualFlags.SubTreeHoldsAncestorChanged))
                {
                    ClearTreeBits(
                        oldParent,
                        VisualFlags.SubTreeHoldsAncestorChanged,
                        VisualFlags.RegisteredForAncestorChanged);
                }
            }

            // Fire the Ancestor changed Event on the nodes.
            AncestorChangedEventArgs args = new AncestorChangedEventArgs(this, oldParent);
            ProcessAncestorChangedNotificationRecursive(this, args);
        }


        /// <summary>
        /// OnVisualParentChanged is called when the parent of the Visual is changed.
        /// </summary>
        /// <param name="oldParent">Old parent or null if the Visual did not have a parent before.</param>
        protected internal virtual void OnVisualParentChanged(DependencyObject oldParent)
        {
        }

        /// <summary>
        /// OnVisualChildrenChanged is called when the VisualCollection of the Visual is edited.
        /// </summary>
        protected internal virtual void OnVisualChildrenChanged(
            DependencyObject visualAdded,
            DependencyObject visualRemoved)
        {
        }

        /// <summary>
        /// OnDpiChanged is called when the DPI at which this visual is rendered, changes.
        /// </summary>
        protected virtual void OnDpiChanged(
            DpiScale oldDpi,
            DpiScale newDpi)
        {
        }


        /// <summary>
        ///   Add removed delegates to the VisualAncenstorChanged Event.
        /// </summary>
        /// <remarks>
        ///     This also sets/clears the tree-searching bit up the tree
        /// </remarks>
        internal event AncestorChangedEventHandler VisualAncestorChanged
        {
            add
            {
                AncestorChangedEventHandler newHandler = AncestorChangedEventField.GetValue(this);

                if (newHandler == null)
                {
                    newHandler = value;
                }
                else
                {
                    newHandler += value;
                }

                AncestorChangedEventField.SetValue(this, newHandler);

                SetTreeBits(
                    this,
                    VisualFlags.SubTreeHoldsAncestorChanged,
                    VisualFlags.RegisteredForAncestorChanged);
            }

            remove
            {
                // check that we are Disabling a node that was previously Enabled
                if(CheckFlagsAnd(VisualFlags.SubTreeHoldsAncestorChanged))
                {
                    ClearTreeBits(
                        this,
                        VisualFlags.SubTreeHoldsAncestorChanged,
                        VisualFlags.RegisteredForAncestorChanged);
                }

                // if we are Disabling a Visual that was not Enabled then this
                // search should fail.  But it is safe to check.
                AncestorChangedEventHandler newHandler = AncestorChangedEventField.GetValue(this);

                if (newHandler != null)
                {
                    newHandler -= value;

                    if(newHandler == null)
                    {
                        AncestorChangedEventField.ClearValue(this);
                    }
                    else
                    {
                        AncestorChangedEventField.SetValue(this, newHandler);
                    }
                }
            }
        }


        /// <summary>
        ///     Walks down in the tree for nodes that have AncestorChanged Handlers
        ///     registered and calls them.
        ///     It uses Flag bits that help it prune the walk.  This should go
        ///     straight to the relevent nodes.
        /// </summary>
        internal static void ProcessAncestorChangedNotificationRecursive(DependencyObject e, AncestorChangedEventArgs args)
        {
            if (e is Visual3D)
            {
                Visual3D.ProcessAncestorChangedNotificationRecursive(e, args);
            }
            else
            {
                Visual eAsVisual = e as Visual;

                // If the flag is not set, then we are Done.
                if(!eAsVisual.CheckFlagsAnd(VisualFlags.SubTreeHoldsAncestorChanged))
                {
                    return;
                }

                // If there is a handler on this node, then fire it.
                AncestorChangedEventHandler handler = AncestorChangedEventField.GetValue(eAsVisual);

                if(handler != null)
                {
                    handler(eAsVisual, args);
                }

                // Decend into the children.
                int count = eAsVisual.InternalVisual2DOr3DChildrenCount;

                for (int i = 0; i < count; i++)
                {
                    DependencyObject childVisual = eAsVisual.InternalGet2DOr3DVisualChild(i);
                    if (childVisual != null)
                    {
                        ProcessAncestorChangedNotificationRecursive(childVisual, args);
                    }
                }
            }
        }


        /// <summary>
        /// Returns true if the specified ancestor (this) is really the ancestor of the
        /// given descendant (argument).
        /// </summary>
        public bool IsAncestorOf(DependencyObject descendant)
        {
            Visual visual;
            Visual3D visual3D;

            VisualTreeUtils.AsNonNullVisual(descendant, out visual, out visual3D);

            // x86 branch prediction skips the branch on first encounter.  We favor 2D.
            if(visual3D != null)
            {
                return visual3D.IsDescendantOf(this);
            }

            return visual.IsDescendantOf(this);
        }

        /// <summary>
        /// Returns true if the refernece Visual (this) is a descendant of the argument Visual.
        /// </summary>
        public bool IsDescendantOf(DependencyObject ancestor)
        {
            if (ancestor == null)
            {
                throw new ArgumentNullException("ancestor");
            }

            VisualTreeUtils.EnsureVisual(ancestor);

            // Walk up the parent chain of the descendant until we run out
            // of 2D parents or we find the ancestor.
            DependencyObject current = this;

            while ((current != null) && (current != ancestor))
            {
                Visual currentAsVisual = current as Visual;

                if (currentAsVisual != null)
                {
                    current = currentAsVisual._parent;
                }
                else
                {
                    Visual3D currentAsVisual3D = current as Visual3D;

                    if (currentAsVisual3D != null)
                    {
                        current = currentAsVisual3D.InternalVisualParent;
                    }
                    else
                    {
                        current = null;
                    }
                }
            }

            return current == ancestor;
        }


        /// <summary>
        ///     Walks up the Visual tree setting or clearing the given flags.  Unlike
        ///     PropagateFlags this does not terminate when it reaches node with
        ///     the flags already set.  It always walks all the way to the root.
        /// </summary>
        internal void SetFlagsToRoot(bool value, VisualFlags flag)
        {
            Visual current = this;

            do
            {
                current.SetFlags(value, flag);


                Visual currentParent = current._parent as Visual;

                // if the cast to currentParent failed and yet current._parent is not null then
                // we have a 3D element.  Call SetFlagsToRoot on it instead.
                if (current._parent != null && currentParent == null)
                {
                    ((Visual3D)current._parent).SetFlagsToRoot(value, flag);
                    return;
                }

                current = currentParent;
            }
            while (current != null);
        }


        /// <summary>
        ///     Finds the first ancestor of the given element which has the given
        ///     flags set.
        /// </summary>
        internal DependencyObject FindFirstAncestorWithFlagsAnd(VisualFlags flag)
        {
            Visual current = this;

            do
            {
                if (current.CheckFlagsAnd(flag))
                {
                    // The other Visual crossed through this Visual's parent chain. Hence this is our
                    // common ancestor.
                    return current;
                }

                DependencyObject parent = current._parent;

                // first attempt to see if parent is a Visual, in which case we continue the loop.
                // Otherwise see if it's a Visual3D, and call the similar method on it.
                current = parent as Visual;
                if (current == null)
                {
                    Visual3D parentAsVisual3D = parent as Visual3D;
                    if (parentAsVisual3D != null)
                    {
                        return parentAsVisual3D.FindFirstAncestorWithFlagsAnd(flag);
                    }
                }
            }
            while (current != null);

            return null;
        }


        /// <summary>
        /// Finds the common ancestor of two Visuals.
        /// </summary>
        /// <returns>Returns the common ancestor if the Visuals have one or otherwise null.</returns>
        /// <exception cref="ArgumentNullException">If the argument is null.</exception>
        public DependencyObject FindCommonVisualAncestor(DependencyObject otherVisual)
        {
            VerifyAPIReadOnly(otherVisual);

            if (otherVisual == null)
            {
                throw new System.ArgumentNullException("otherVisual");
            }

            // Since we can't rely on code running in the CLR, we need to first make sure
            // that the FindCommonAncestor flag is not set. It is enought to ensure this
            // on one path to the root Visual.

            // Later, when we get from the CLR the "RunForSure" section support, we can replace
            // this algorithm with one that is linear in the distance of the two visuals to
            // their common ancestor.

            SetFlagsToRoot(false, VisualFlags.FindCommonAncestor);

            // Walk up the other visual's parent chain and set the FindCommonAncestor flag.
            VisualTreeUtils.SetFlagsToRoot(otherVisual, true, VisualFlags.FindCommonAncestor);

            // Now see if the other Visual's parent chain crosses our parent chain.
            return FindFirstAncestorWithFlagsAnd(VisualFlags.FindCommonAncestor);
        }

        #endregion Visual Ancestry Relations

        #region ForceInherit property support

        internal virtual void InvalidateForceInheritPropertyOnChildren(DependencyProperty property)
        {
            UIElement.InvalidateForceInheritPropertyOnChildren(this, property);
        }

        #endregion ForceInherit property support

        // --------------------------------------------------------------------
        //
        //   Visual-to-Visual Transforms
        //
        // --------------------------------------------------------------------

        #region Visual-to-Visual Transforms

        /// <summary>
        /// Returns a transform that can be used to transform coordinate from this
        /// node to the specified ancestor.  It allows 3D to be between the 2D nodes.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// If ancestor is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the ancestor Visual is not a ancestor of Visual.
        /// </exception>
        /// <exception cref="InvalidOperationException">If the Visuals are not connected.</exception>
        public GeneralTransform TransformToAncestor(
            Visual ancestor)
        {
            if (ancestor == null)
            {
                throw new ArgumentNullException("ancestor");
            }

            VerifyAPIReadOnly(ancestor);

            return InternalTransformToAncestor(ancestor, false);
        }

        /// <summary>
        /// Returns a transform that can be used to transform coordinate from this
        /// node to the specified ancestor.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// If ancestor is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the ancestor Visual3D is not a ancestor of Visual.
        /// </exception>
        /// <exception cref="InvalidOperationException">If the Visuals are not connected.</exception>
        public GeneralTransform2DTo3D TransformToAncestor(Visual3D ancestor)
        {
            if (ancestor == null)
            {
                throw new ArgumentNullException("ancestor");
            }

            VerifyAPIReadOnly(ancestor);

            return InternalTransformToAncestor(ancestor, false);
        }

        /// <summary>
        /// Returns a transform that can be used to transform coordinates from this
        /// node to the specified descendant, or null if the transform from descendant to "this"
        /// is non-invertible.  It allows 3D to be between the 2D nodes.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// If the reference Visual is not a ancestor of the descendant Visual.
        /// </exception>
        /// <exception cref="ArgumentNullException">If the descendant argument is null.</exception>
        /// <exception cref="InvalidOperationException">If the Visuals are not connected.</exception>
        public GeneralTransform TransformToDescendant(Visual descendant)
        {
            if (descendant == null)
            {
                throw new ArgumentNullException("descendant");
            }

            VerifyAPIReadOnly(descendant);

            return descendant.InternalTransformToAncestor(this, true);
        }


        /// <summary>
        /// The returned matrix can be used to transform coordinates from this Visual to
        /// the specified Visual.
        /// Returns null if no such transform exists due to a non-invertible Transform.
        /// </summary>
        /// <exception cref="ArgumentNullException">If visual is null.</exception>
        /// <exception cref="InvalidOperationException">If the Visuals are not connected.</exception>
        public GeneralTransform TransformToVisual(Visual visual)
        {
            DependencyObject ancestor = FindCommonVisualAncestor(visual);
            Visual ancestorAsVisual = ancestor as Visual;

            if (ancestorAsVisual == null)
            {
                throw new System.InvalidOperationException(SR.Get(SRID.Visual_NoCommonAncestor));
            }

            GeneralTransform g0;
            Matrix m0;

            bool isSimple0 = this.TrySimpleTransformToAncestor(ancestorAsVisual,
                                                               false,
                                                               out g0,
                                                               out m0);

            GeneralTransform g1;
            Matrix m1;

            bool isSimple1 = visual.TrySimpleTransformToAncestor(ancestorAsVisual,
                                                                 true,
                                                                 out g1,
                                                                 out m1);

            // combine the transforms
            // if both transforms are simple Matrix transforms, just multiply them and
            // return the result.
            if (isSimple0 && isSimple1)
            {
                MatrixUtil.MultiplyMatrix(ref m0, ref m1);
                MatrixTransform m = new MatrixTransform(m0);
                m.Freeze();
                return m;
            }

            // Handle the case where 0 is simple and 1 is complex.
            if (isSimple0)
            {
                g0 = new MatrixTransform(m0);
                g0.Freeze();
            }
            else if (isSimple1)
            {
                g1 = new MatrixTransform(m1);
                g1.Freeze();
            }

            // If inverse was requested, TrySimpleTransformToAncestor can return null
            // add the transform only if it is not null
            if (g1 != null)
            {
                GeneralTransformGroup group = new GeneralTransformGroup();
                group.Children.Add(g0);
                group.Children.Add(g1);
                group.Freeze();
                return group;
            }

            return g0;
        }

        /// <summary>
        /// Returns the transform or the inverse transform between this visual and the specified ancestor.
        /// If inverse is requested but does not exist (if the transform is not invertible), null is returned.
        /// </summary>
        /// <param name="ancestor">Ancestor visual.</param>
        /// <param name="inverse">Returns inverse if this argument is true.</param>
        private GeneralTransform InternalTransformToAncestor(Visual ancestor, bool inverse)
        {
            GeneralTransform generalTransform;
            Matrix simpleTransform;

            bool isSimple = TrySimpleTransformToAncestor(ancestor,
                                                         inverse,
                                                         out generalTransform,
                                                         out simpleTransform);

            if (isSimple)
            {
                MatrixTransform matrixTransform = new MatrixTransform(simpleTransform);
                matrixTransform.Freeze();
                return matrixTransform;
            }
            else
            {
                return generalTransform;
            }
        }

        /// <summary>
        /// Provides the transform or the inverse transform between this visual and the specified ancestor.
        /// Returns true if the transform is "simple" - in which case the GeneralTransform is null
        /// and the caller should use the Matrix.
        /// Otherwise, returns false - use the GeneralTransform and ignore the Matrix.
        /// If inverse is requested but not available (if the transform is not invertible), false is
        /// returned and the GeneralTransform is null.
        /// </summary>
        /// <param name="ancestor">Ancestor visual.</param>
        /// <param name="inverse">Returns inverse if this argument is true.</param>
        /// <param name="generalTransform">The GeneralTransform if this method returns false.</param>
        /// <param name="simpleTransform">The Matrix if this method returns true.</param>
        internal bool TrySimpleTransformToAncestor(Visual ancestor,
                                                   bool inverse,
                                                   out GeneralTransform generalTransform,
                                                   out Matrix simpleTransform)
        {
            Debug.Assert(ancestor != null);

            // flag to indicate if we have a case where we do multile 2D->3D->2D transitions
            bool embedded2Don3D = false;

            DependencyObject g = this;
            Matrix m = Matrix.Identity;

            // Keep this null until it's needed
            GeneralTransformGroup group = null;

            // This while loop will walk up the visual tree until we encounter the ancestor.
            // As it does so, it will accumulate the descendent->ancestor transform.
            // In most cases, this is simply a matrix, though if we encounter a bitmap effect we
            // will need to use a general transform group to store the transform.
            // We will accumulate the current transform in a matrix until we encounter a bitmap effect,
            // at which point we will add the matrix's current value and the bitmap effect's transforms
            // to the GeneralTransformGroup and continue to accumulate further transforms in the matrix again.
            // At the end of this loop, we will have 0 or more transforms in the GeneralTransformGroup
            // and the matrix which, if not identity, should be appended to the GeneralTransformGroup.
            // If, as is commonly the case, this loop terminates without encountering a bitmap effect
            // we will simply use the Matrix.

            while ((VisualTreeHelper.GetParent(g) != null) && (g != ancestor))
            {
                Visual gAsVisual = g as Visual;
                if (gAsVisual != null)
                {
                    if (gAsVisual.CheckFlagsAnd(VisualFlags.NodeHasEffect))
                    {
                        // Only check for Effect, not legacy BitmapEffect.  Previous
                        // version had an incorrect BitmapEffect implementation
                        // here, and there's no need to improve on our
                        // BitmapEffect implementation if it didn't work
                        // before.

                        Effect imageEffect = EffectField.GetValue(gAsVisual);
                        if (imageEffect != null)
                        {
                            GeneralTransform gt = imageEffect.CoerceToUnitSpaceGeneralTransform(
                                imageEffect.EffectMapping,
                                gAsVisual.VisualDescendantBounds);

                            Transform affineTransform = gt.AffineTransform;
                            if (affineTransform != null)
                            {
                                Matrix cm = affineTransform.Value;
                                MatrixUtil.MultiplyMatrix(ref m, ref cm);
                            }
                            else
                            {
                                if (group == null)
                                {
                                    group = new GeneralTransformGroup();
                                }

                                group.Children.Add(new MatrixTransform(m));
                                m = Matrix.Identity;

                                group.Children.Add(gt);
                            }
                        }
                    }

                    Transform transform = TransformField.GetValue(gAsVisual);
                    if (transform != null)
                    {
                        Matrix cm = transform.Value;
                        MatrixUtil.MultiplyMatrix(ref m, ref cm);
                    }
                    m.Translate(gAsVisual._offset.X, gAsVisual._offset.Y); // Consider having a bit that indicates that we have a non-null offset.
                    g = gAsVisual._parent;
                }
                else
                {
                    // we just hit a Visual3D - use a GeneralTransform to go from 2D -> 3D -> 2D
                    // and then return to the tree using the 2D parent - the general transform will deal with the
                    // actual transformation.  This Visual3D also must be a Viewport2DVisual3D since this is the only
                    // Visual3D that can have a 2D child.
                    Viewport2DVisual3D gAsVisual3D = g as Viewport2DVisual3D;

                    if (group == null)
                    {
                        group = new GeneralTransformGroup();
                    }

                    group.Children.Add(new MatrixTransform(m));
                    m = Matrix.Identity;

                    Visual visualForGenTransform = null;
                    if (embedded2Don3D)
                    {
                        visualForGenTransform = gAsVisual3D.Visual;
                    }
                    else
                    {
                        visualForGenTransform = this;
                        embedded2Don3D = true;
                    }

                    group.Children.Add(new GeneralTransform2DTo3DTo2D(gAsVisual3D, visualForGenTransform));

                    g = VisualTreeHelper.GetContainingVisual2D(gAsVisual3D);
                }
            }

            if (g != ancestor)
            {
                throw new System.InvalidOperationException(SR.Get(inverse ? SRID.Visual_NotADescendant : SRID.Visual_NotAnAncestor));
            }

            // At this point, we will have 0 or more transforms in the GeneralTransformGroup
            // and the matrix which, if not identity, should be appended to the GeneralTransformGroup.
            // If, as is commonly the case, this loop terminates without encountering a bitmap effect
            // we will simply use the Matrix.

            // Assert that a non-null group implies at least one child
            Debug.Assert((group == null) || (group.Children.Count > 0));

            // Do we have a group?
            if (group != null)
            {
                if (!m.IsIdentity)
                {
                    group.Children.Add(new MatrixTransform(m));
                }

                if (inverse)
                {
                    group = (GeneralTransformGroup)group.Inverse;
                }

                // group can be null if it does not have an inverse
                if (group != null)
                {
                    group.Freeze();
                }

                // Initialize out params
                generalTransform = group;
                simpleTransform = new Matrix();
                return false; // simple transform failed
            }
            // If not, the entire transform is stored in the matrix
            else
            {
                // Initialize out params
                generalTransform = null;

                if (inverse)
                {
                    if (!m.HasInverse)
                    {
                        simpleTransform = new Matrix();
                        return false; // inversion failed, so simple transform failed.
                    }

                    m.Invert();
                }

                simpleTransform = m;
                return true; // simple transform succeeded
            }
        }

        /// <summary>
        /// Returns the transform or the inverse transform between this visual and the specified ancestor.
        /// If inverse is requested but does not exist (if the transform is not invertible), null is returned.
        /// </summary>
        /// <param name="ancestor">Ancestor visual.</param>
        /// <param name="inverse">Returns inverse if this argument is true.</param>
        private GeneralTransform2DTo3D InternalTransformToAncestor(Visual3D ancestor, bool inverse)
        {
            GeneralTransform2DTo3D transformTo3D = null;

            if (TrySimpleTransformToAncestor(ancestor,
                                             out transformTo3D))
            {
                transformTo3D.Freeze();
                return transformTo3D;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Provides the transform to go from 2D to 3D.
        /// </summary>
        /// <param name="ancestor">Ancestor visual.</param>
        /// <param name="transformTo3D">The transform to use to go to 3D</param>
        internal bool TrySimpleTransformToAncestor(Visual3D ancestor,
                                                   out GeneralTransform2DTo3D transformTo3D)
        {
            Debug.Assert(ancestor != null);

            // get the 3D object that contains this visual
            // this must be a Viewport2DVisual3D since this is the only 3D class that can contain 2D content as a child
            Viewport2DVisual3D containingVisual3D = VisualTreeHelper.GetContainingVisual3D(this) as Viewport2DVisual3D;

            // if containingVisual3D is null then ancestor is not the ancestor
            if (containingVisual3D == null)
            {
                throw new System.InvalidOperationException(SR.Get(SRID.Visual_NotAnAncestor));
            }

            GeneralTransform transform2D = this.TransformToAncestor(containingVisual3D.Visual);
            GeneralTransform3D transform3D = containingVisual3D.TransformToAncestor(ancestor);
            transformTo3D = new GeneralTransform2DTo3D(transform2D, containingVisual3D, transform3D);

            return true;
        }

        /// <summary>
        /// Returns the DPI information at which this Visual is rendered.
        /// </summary>
        internal DpiScale GetDpi()
        {
            DpiScale dpi;
            lock (UIElement.DpiLock)
            {
                if (UIElement.DpiScaleXValues.Count == 0)
                {
                    // This is for scenarios where an HWND hasn't been created yet.
                    return UIElement.EnsureDpiScale();
                }

                // initialized to system DPI as a fallback value
                dpi = new DpiScale(UIElement.DpiScaleXValues[0], UIElement.DpiScaleYValues[0]);

                int index = 0;
                index = CheckFlagsAnd(VisualFlags.DpiScaleFlag1) ? index | 1 : index;
                index = CheckFlagsAnd(VisualFlags.DpiScaleFlag2) ? index | 2 : index;

                if (index < 3 && UIElement.DpiScaleXValues[index] != 0 && UIElement.DpiScaleYValues[index] != 0)
                {
                    dpi = new DpiScale(UIElement.DpiScaleXValues[index], UIElement.DpiScaleYValues[index]);
                }

                else if (index >= 3)
                {
                    int actualIndex = DpiIndex.GetValue(this);
                    dpi = new DpiScale(UIElement.DpiScaleXValues[actualIndex], UIElement.DpiScaleYValues[actualIndex]);
                }
            }
            return dpi;
        }

        /// <summary>
        /// This method converts a point in the current Visual's coordinate
        /// system into a point in screen coordinates.
        /// </summary>
        public Point PointToScreen(Point point)
        {
            VerifyAPIReadOnly();

            PresentationSource inputSource = PresentationSource.FromVisual(this);

            if (inputSource == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.Visual_NoPresentationSource));
            }

            // Translate the point from the visual to the root.
            GeneralTransform gUp = this.TransformToAncestor(inputSource.RootVisual);
            if (gUp == null || !gUp.TryTransform(point, out point))
            {
                throw new InvalidOperationException(SR.Get(SRID.Visual_CannotTransformPoint));
            }

            // Translate the point from the root to the screen
            point = PointUtil.RootToClient(point, inputSource);
            point = PointUtil.ClientToScreen(point, inputSource);

            return point;
        }

        /// <summary>
        /// This method converts a point in screen coordinates into a point
        /// in the current Visual's coordinate system.
        /// </summary>
        public Point PointFromScreen(Point point)
        {
            VerifyAPIReadOnly();

            PresentationSource inputSource = PresentationSource.FromVisual(this);

            if (inputSource == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.Visual_NoPresentationSource));
            }

            // Translate the point from the screen to the root
            point = PointUtil.ScreenToClient(point, inputSource);
            point = PointUtil.ClientToRoot(point, inputSource);

            // Translate the point from the root to the visual.
            GeneralTransform gDown = inputSource.RootVisual.TransformToDescendant(this);
            if (gDown == null || !gDown.TryTransform(point, out point))
            {
                throw new InvalidOperationException(SR.Get(SRID.Visual_CannotTransformPoint));
            }

            return point;
        }

        #endregion Visual-to-Visual Transforms



        // --------------------------------------------------------------------
        //
        //   Internal Event Handlers
        //
        // --------------------------------------------------------------------

        #region Internal Event Handlers

        internal EventHandler ClipChangedHandler
        {
            get
            {
                return new EventHandler(ClipChanged);
            }
        }

        internal void ClipChanged(object sender, EventArgs e)
        {
            PropagateChangedFlags();
        }

        internal EventHandler ScrollableAreaClipChangedHandler
        {
            get
            {
                return new EventHandler(ScrollableAreaClipChanged);
            }
        }

        internal void ScrollableAreaClipChanged(object sender, EventArgs e)
        {
            PropagateChangedFlags();
        }

        internal EventHandler TransformChangedHandler
        {
            get
            {
                return new EventHandler(TransformChanged);
            }
        }

        internal void TransformChanged(object sender, EventArgs e)
        {
            PropagateChangedFlags();
        }


        internal EventHandler EffectChangedHandler
        {
            get
            {
                return new EventHandler(EffectChanged);
            }
        }

        internal void EffectChanged(object sender, EventArgs e)
        {
            PropagateChangedFlags();
        }

        internal EventHandler CacheModeChangedHandler
        {
            get
            {
                return new EventHandler(EffectChanged);
            }
        }

        internal void CacheModeChanged(object sender, EventArgs e)
        {
            PropagateChangedFlags();
        }

        internal EventHandler GuidelinesChangedHandler
        {
            get
            {
                return new EventHandler(GuidelinesChanged);
            }
        }

        internal void GuidelinesChanged(object sender, EventArgs e)
        {
            SetFlagsOnAllChannels(
                true,
                VisualProxyFlags.IsGuidelineCollectionDirty);

            PropagateChangedFlags();
        }

        internal EventHandler OpacityMaskChangedHandler
        {
            get
            {
                return new EventHandler(OpacityMaskChanged);
            }
        }

        internal void OpacityMaskChanged(object sender, EventArgs e)
        {
            PropagateChangedFlags();
        }

        internal EventHandler ContentsChangedHandler
        {
            get
            {
                return new EventHandler(ContentsChanged);
            }
        }

        internal virtual void ContentsChanged(object sender, EventArgs e)
        {
            PropagateChangedFlags();
        }

        #endregion Internal Event Handlers



        // --------------------------------------------------------------------
        //
        //   Visual flags manipulation
        //
        // --------------------------------------------------------------------

        #region Visual flags manipulation

        /// <summary>
        /// SetFlagsOnAllChannels is used to set or unset one
        /// or multiple flags on all channels this visual is
        /// marshaled to.
        /// </summary>
        internal void SetFlagsOnAllChannels(
            bool value,
            VisualProxyFlags flagsToChange)
        {
            _proxy.SetFlagsOnAllChannels(
                value,
                flagsToChange);
        }


        /// <summary>
        /// SetFlags is used to set or unset one or multiple flags on a given channel.
        /// </summary>
        internal void SetFlags(
            DUCE.Channel channel,
            bool value,
            VisualProxyFlags flagsToChange)
        {
            _proxy.SetFlags(
                channel,
                value,
                flagsToChange);
        }


        /// <summary>
        /// SetFlags is used to set or unset one or multiple node flags on the node.
        /// </summary>
        internal void SetFlags(bool value, VisualFlags flags)
        {
            _flags = value ? (_flags | flags) : (_flags & (~flags));
        }

        /// <summary>
        /// Sets the DPI scale Visual flags on the current visual.
        /// </summary>
        internal void SetDpiScaleVisualFlags(DpiRecursiveChangeArgs args)
        {
            _flags = args.DpiScaleFlag1 ? (_flags | VisualFlags.DpiScaleFlag1) : (_flags & ~VisualFlags.DpiScaleFlag1);
            _flags = args.DpiScaleFlag2 ? (_flags | VisualFlags.DpiScaleFlag2) : (_flags & ~VisualFlags.DpiScaleFlag2);
            if (args.DpiScaleFlag1 && args.DpiScaleFlag2)
            {
                DpiIndex.SetValue(this, args.Index);
            }

            if (!args.OldDpiScale.Equals(args.NewDpiScale))
            {
                OnDpiChanged(args.OldDpiScale, args.NewDpiScale);
            }
        }

        /// <summary>
        /// Recursively sets the DPI scale visual flags.
        /// </summary>
        internal void RecursiveSetDpiScaleVisualFlags(DpiRecursiveChangeArgs args)
        {
            SetDpiScaleVisualFlags(args);
            int count = InternalVisualChildrenCount;
            for (int i = 0; i < count; i++)
            {
                Visual cv = InternalGetVisualChild(i);
                if (cv != null)
                {
                    cv.RecursiveSetDpiScaleVisualFlags(args);
                }
            }
        }


        /// <summary>
        /// CheckFlagsOnAllChannels returns true if all flags in
        /// the bitmask flags are set on all channels this visual is
        /// marshaled to.
        /// </summary>
        /// <remarks>
        /// If there aren't any bits set on the specified flags
        /// the method returns true.
        /// </remarks>
        internal bool CheckFlagsOnAllChannels(VisualProxyFlags flagsToCheck)
        {
            return _proxy.CheckFlagsOnAllChannels(flagsToCheck);
        }


        /// <summary>
        /// CheckFlagsAnd returns true if all flags in the bitmask flags
        /// are set on a given channel.
        /// </summary>
        /// <remarks>
        /// If there aren't any bits set on the specified flags
        /// the method returns true.
        /// </remarks>
        internal bool CheckFlagsAnd(
            DUCE.Channel channel,
            VisualProxyFlags flagsToCheck)
        {
            return (_proxy.GetFlags(channel) & flagsToCheck) == flagsToCheck;
        }


        /// <summary>
        /// CheckFlagsAnd returns true if all flags in the bitmask flags are set on the node.
        /// </summary>
        /// <remarks>If there aren't any bits set on the specified flags the method
        /// returns true</remarks>
        internal bool CheckFlagsAnd(VisualFlags flags)
        {
            return (_flags & flags) == flags;
        }

        /// <summary>
        /// Checks if any of the specified flags is set on a given channel.
        /// </summary>
        /// <remarks>
        /// If there aren't any bits set on the specified flags
        /// the method returns false.
        /// </remarks>
        internal bool CheckFlagsOr(
            DUCE.Channel channel,
            VisualProxyFlags flagsToCheck)
        {
            return (_proxy.GetFlags(channel) & flagsToCheck) != VisualProxyFlags.None;
        }


        /// <summary>
        /// Checks if any of the specified flags is set on the node.
        /// </summary>
        /// <remarks>If there aren't any bits set on the specified flags the method
        /// returns true</remarks>
        internal bool CheckFlagsOr(VisualFlags flags)
        {
            return (flags == 0) || ((_flags & flags) > 0);
        }

        /// <summary>
        ///     Set a bit in a Visual node and in all its direct ancestors.
        /// </summary>
        /// <param name="e">The Visual Element</param>
        /// <param name="treeFlag">The Flag that marks a sub tree to search</param>
        /// <param name="nodeFlag">The Flag that marks the node to search for.</param>
        internal static void SetTreeBits(
            DependencyObject e,
            VisualFlags treeFlag,
            VisualFlags nodeFlag)
        {
            Visual eAsVisual;
            Visual3D eAsVisual3D;

            if (e != null)
            {
                eAsVisual = e as Visual;
                if (eAsVisual != null)
                {
                    eAsVisual.SetFlags(true, nodeFlag);
                }
                else
                {
                    ((Visual3D)e).SetFlags(true, nodeFlag);
                }
            }

            while (null!=e)
            {
                eAsVisual = e as Visual;
                if (eAsVisual != null)
                {
                    // if the bit is already set, then we're done.
                    if(eAsVisual.CheckFlagsAnd(treeFlag))
                        return;

                    eAsVisual.SetFlags(true, treeFlag);
                }
                else
                {
                    eAsVisual3D = e as Visual3D;

                    // if the bit is already set, then we're done.
                    if(eAsVisual3D.CheckFlagsAnd(treeFlag))
                        return;

                    eAsVisual3D.SetFlags(true, treeFlag);
                }

                e = VisualTreeHelper.GetParent(e);
            }
        }


        /// <summary>
        ///     Clean a bit in a Visual node and in all its direct ancestors;
        ///     unless the ancestor also has
        /// </summary>
        /// <param name="e">The Visual Element</param>
        /// <param name="treeFlag">The Flag that marks a sub tree to search</param>
        /// <param name="nodeFlag">The Flag that marks the node to search for.</param>
        internal static void ClearTreeBits(
            DependencyObject e,
            VisualFlags treeFlag,
            VisualFlags nodeFlag)
        {
            Visual eAsVisual;
            Visual3D eAsVisual3D;

            // This bit might not be set, but checking costs as much as setting
            // So it is faster to just clear it everytime.
            if (e != null)
            {
                eAsVisual = e as Visual;
                if (eAsVisual != null)
                {
                    eAsVisual.SetFlags(false, nodeFlag);
                }
                else
                {
                    ((Visual3D)e).SetFlags(false, nodeFlag);
                }
            }

            while (e != null)
            {
                eAsVisual = e as Visual;
                if (eAsVisual != null)
                {
                    if(eAsVisual.CheckFlagsAnd(nodeFlag))
                    {
                        return;  // Done;   if a parent also has the Node bit set.
                    }

                    if(DoAnyChildrenHaveABitSet(eAsVisual, treeFlag))
                    {
                        return;  // Done;   if a other subtrees are set.
                    }

                    eAsVisual.SetFlags(false, treeFlag);
                }
                else
                {
                    eAsVisual3D = e as Visual3D;

                    if(eAsVisual3D.CheckFlagsAnd(nodeFlag))
                    {
                        return;  // Done;   if a parent also has the Node bit set.
                    }

                    if(Visual3D.DoAnyChildrenHaveABitSet(eAsVisual3D, treeFlag))
                    {
                        return;  // Done;   if a other subtrees are set.
                    }

                    eAsVisual3D.SetFlags(false, treeFlag);
                }

                e = VisualTreeHelper.GetParent(e);
            }
        }


        /// <summary>
        ///     Check all the children for a bit.
        /// </summary>
        private static bool DoAnyChildrenHaveABitSet(
            Visual pe,
            VisualFlags flag)
        {
            int count = pe.VisualChildrenCount;
            for (int i = 0; i < count; i++)
            {
                Visual v = pe.GetVisualChild(i);
                if (v != null && v.CheckFlagsAnd(flag))
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Propagates the flags up to the root.
        /// </summary>
        /// <remarks>
        /// The walk stops on a node with all of the required flags set.
        /// </remarks>
        internal static void PropagateFlags(
            Visual e,
            VisualFlags flags,
            VisualProxyFlags proxyFlags)
        {
            while ((e != null) &&
                   (!e.CheckFlagsAnd(flags) || !e.CheckFlagsOnAllChannels(proxyFlags)))
            {
                if (e.CheckFlagsOr(VisualFlags.ShouldPostRender))
                {
                    MediaContext mctx = MediaContext.From(e.Dispatcher);

                    if (mctx.Channel != null)
                    {
                        mctx.PostRender();
                    }
                }
                else if (e.CheckFlagsAnd(VisualFlags.NodeIsCyclicBrushRoot))
                {
                    //
                    // For visuals that are root nodes in visual brushes we
                    // need to fire OnChanged on the owning brushes.
                    //

                    Dictionary<ICyclicBrush, int> cyclicBrushToChannelsMap =
                        CyclicBrushToChannelsMapField.GetValue(e);

                    Debug.Assert(cyclicBrushToChannelsMap != null, "Visual brush roots need to have the visual brush to channels map!");


                    //
                    // Iterate over the visual brushes and fire the OnChanged event.
                    //

                    foreach (ICyclicBrush cyclicBrush in cyclicBrushToChannelsMap.Keys)
                    {
                        cyclicBrush.FireOnChanged();
                    }
                }

                e.SetFlags(true, flags);
                e.SetFlagsOnAllChannels(true, proxyFlags);

                if (e._parent == null)
                {
                    // Stop propagating.  We are at the root of the 2D subtree.
                    return;
                }

                Visual parentAsVisual = e._parent as Visual;
                if (parentAsVisual == null)
                {
                    // if the parent is not null (saw this with earlier null check) and is not a Visual
                    // it must be a Visual3D - continue the propagation
                    Visual3D.PropagateFlags((Visual3D)e._parent, flags, proxyFlags);
                    return;
                }

                e = parentAsVisual;
            }
        }

        /// <summary>
        /// Propagates the dirty flags up to the root.
        /// </summary>
        /// <remarks>
        /// The walk stops on a node with all of the required flags set.
        /// </remarks>
        internal void PropagateChangedFlags()
        {
            PropagateFlags(
                this,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.IsSubtreeDirtyForRender);
        }


        private bool NodeHasLegacyBitmapEffect
        {
            get
            {
                // NodeHasEffect flag is overloaded for both legacy
                // BitmapEffects and the newer Effects
                return
                    CheckFlagsAnd(VisualFlags.NodeHasEffect) &&
                    BitmapEffectStateField.GetValue(this) != null;
            }
        }


        #endregion Visual flags manipulation

        // --------------------------------------------------------------------
        //
        //   Internal Fields
        //
        // --------------------------------------------------------------------

        #region Internal Fields

        internal static readonly UncommonField<BitmapEffectState> BitmapEffectStateField = new UncommonField<BitmapEffectState>();

        internal delegate void AncestorChangedEventHandler(object sender, AncestorChangedEventArgs e);

        // index in parent child array. no meaning if parent is null.
        // note that we maintain in debug that the _parentIndex is -1 if the parent is null.
        // Exception: children added to TextBoxView and InkPresenter.
        internal int _parentIndex;

        // We may have to change the API so that we can save
        // here. For now that is good enough.
        internal DependencyObject _parent;

        internal VisualProxy _proxy;

        #endregion Internal Fields



        // --------------------------------------------------------------------
        //
        //   Private Fields
        //
        // --------------------------------------------------------------------

        #region Private Fields

        // bbox in inner coordinate space of this node including its children.
        private Rect _bboxSubgraph = Rect.Empty;

        //
        // Store the cyclic brushes that hold on to this visual. Also store the corresponding
        // number of channel, on which that cyclic brush holds on to this visual.
        //
        private static readonly UncommonField<Dictionary<ICyclicBrush, int>> CyclicBrushToChannelsMapField
            = new UncommonField<Dictionary<ICyclicBrush, int>>();

        //
        // Store the channels on which cyclic brushes hold on to this visual. Also store the
        // corresponding number of cyclic brushes on that channel, holding on to this visual.
        //
        private static readonly UncommonField<Dictionary<DUCE.Channel, int>> ChannelsToCyclicBrushMapField
            = new UncommonField<Dictionary<DUCE.Channel, int>>();

        internal static readonly UncommonField<int> DpiIndex = new UncommonField<int>();
        private static readonly UncommonField<Geometry> ClipField = new UncommonField<Geometry>();
        private static readonly UncommonField<double> OpacityField = new UncommonField<double>(1.0);
        private static readonly UncommonField<Brush> OpacityMaskField = new UncommonField<Brush>();
        private static readonly UncommonField<EdgeMode> EdgeModeField = new UncommonField<EdgeMode>();
        private static readonly UncommonField<BitmapScalingMode> BitmapScalingModeField = new UncommonField<BitmapScalingMode>();
        private static readonly UncommonField<ClearTypeHint> ClearTypeHintField = new UncommonField<ClearTypeHint>();

        private static readonly UncommonField<Transform> TransformField = new UncommonField<Transform>();
        private static readonly UncommonField<Effect> EffectField = new UncommonField<Effect>();
        private static readonly UncommonField<CacheMode> CacheModeField = new UncommonField<CacheMode>();

        private static readonly UncommonField<DoubleCollection> GuidelinesXField = new UncommonField<DoubleCollection>();
        private static readonly UncommonField<DoubleCollection> GuidelinesYField = new UncommonField<DoubleCollection>();

        private static readonly UncommonField<AncestorChangedEventHandler> AncestorChangedEventField
            = new UncommonField<AncestorChangedEventHandler>();

        private static readonly UncommonField<BitmapEffectState> UserProvidedBitmapEffectData = new UncommonField<BitmapEffectState>();

        private static readonly UncommonField<Rect?> ScrollableAreaClipField = new UncommonField<Rect?>(null);

        private static readonly UncommonField<TextRenderingMode> TextRenderingModeField = new UncommonField<TextRenderingMode>();
        private static readonly UncommonField<TextHintingMode> TextHintingModeField = new UncommonField<TextHintingMode>();

        private Vector _offset;
        private VisualFlags _flags;

        private const uint TreeLevelLimit = 0x7FF;

        #endregion Private Fields
    }
}






