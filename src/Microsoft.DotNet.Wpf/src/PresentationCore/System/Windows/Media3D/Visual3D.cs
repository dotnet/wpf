// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using MS.Internal;
using MS.Internal.Media;
using MS.Internal.Media3D;
using MS.Internal.PresentationCore;
using System;
using System.Diagnostics;
using System.Security;
using System.Windows.Diagnostics;
using System.Windows.Media.Composition;
using System.Windows.Media;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     Visual3D is the base class for all 3D visual elements.
    /// </summary>
    public abstract partial class Visual3D : DependencyObject, DUCE.IResource, IVisual3DContainer
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
        private const VisualProxyFlags c_Model3DVisualProxyFlagsDirtyMask =
              VisualProxyFlags.IsSubtreeDirtyForRender
            | VisualProxyFlags.IsContentDirty
            | VisualProxyFlags.IsTransformDirty;

        #endregion Constants

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Prevent 3rd parties from extending this abstract base class.
        internal Visual3D()
        {
            _internalIsVisible = true;
        }

        #endregion Constructors


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
        /// Returns the handle this visual has on the given channel.
        /// Note: The 3D handle is the normal handle for Visual3D
        /// </summary>
        DUCE.ResourceHandle DUCE.IResource.Get3DHandle(DUCE.Channel channel)
        {
            return _proxy.GetHandle(channel);
        }

        /// <summary>
        /// This is used to create or addref the visual resource
        /// on the given channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        DUCE.ResourceHandle DUCE.IResource.AddRefOnChannel(DUCE.Channel channel)
        {
            _proxy.CreateOrAddRefOnChannel(this, channel, DUCE.ResourceType.TYPE_VISUAL3D);

            return _proxy.GetHandle(channel);
        }

        /// <summary>
        /// Sends a command to compositor to remove the child
        /// from its parent on the channel.
        /// </summary>
        void DUCE.IResource.RemoveChildFromParent(
                DUCE.IResource parent,
                DUCE.Channel channel)
        {
            DUCE.Visual3DNode.RemoveChild(
                parent.Get3DHandle(channel),
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


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///    DependencyProperty which backs the ModelVisual3D.Transform property.
        /// </summary>
        public static readonly DependencyProperty TransformProperty =
            DependencyProperty.Register(
                    "Transform",
                    /* propertyType = */ typeof(Transform3D),
                    /* ownerType = */ typeof(Visual3D),
                    new PropertyMetadata(Transform3D.Identity, TransformPropertyChanged),
                    (ValidateValueCallback) delegate { return MediaContext.CurrentMediaContext.WriteAccessEnabled; });

        private static void TransformPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Visual3D owner = ((Visual3D) d);

            if (!e.IsASubPropertyChange)
            {
                if (e.OldValue != null)
                {
                    owner.DisconnectAttachedResource(
                        VisualProxyFlags.IsTransformDirty,
                        ((DUCE.IResource) e.OldValue));
                }

                owner.SetFlagsOnAllChannels(true, VisualProxyFlags.IsTransformDirty);
            }

            // Stop over-invalidating _bboxSubgraph
            //
            // We currently maintain a cache of both a ModelVisual3D’s content
            // and subgraph bounds.  A better solution that would be both a 2D
            // and 3D win would be to stop invalidating _bboxSubgraph when a
            // visual’s transform changes.
            owner.RenderChanged(/* sender = */ owner, EventArgs.Empty);
        }

        /// <summary>
        ///     Transform for this Visual3D.
        /// </summary>
        public Transform3D Transform
        {
            get
            {
                return (Transform3D) GetValue(TransformProperty);
            }

            set
            {
                SetValue(TransformProperty, value);
            }
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// AttachChild
        ///
        ///    Derived classes must call this method to notify the Visual3D layer that a new
        ///    child appeard in the children collection. The Visual3D layer will then call the GetVisual3DChild
        ///    method to find out where the child was added.
        ///
        ///  Remark: To move a Visual3D child in a collection it must be first disconnected and then connected
        ///    again. (Moving forward we might want to add a special optimization there so that we do not
        ///    unmarshal our composition resources).
        /// </summary>
        protected void AddVisual3DChild(Visual3D child)
        {
            // It is invalid to modify the children collection that we
            // might be iterating during a property invalidation tree walk.
            if (IsVisualChildrenIterationInProgress)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotModifyVisualChildrenDuringTreeWalk));
            }

            // invalid during a VisualTreeChanged event
            VisualDiagnostics.VerifyVisualTreeChange(this);

            Debug.Assert(child != null);
            Debug.Assert(child.InternalVisualParent == null);

            child.SetParent(this);

            // set the inheritance context so databinding, etc... work
            ProvideSelfAsInheritanceContext(child, null);

            // The child already might be dirty. Hence we need to propagate dirty information
            // from the parent and from the child.
            Visual3D.PropagateFlags(
                this,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.IsSubtreeDirtyForRender);

            Visual3D.PropagateFlags(
                child,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.IsSubtreeDirtyForRender);

            // We do not currently support layout in 3D
            // UIElement.PropagateResumeLayout(child);

            // Fire notifications
            OnVisualChildrenChanged(child, /* visualRemoved = */ null);

            child.FireOnVisualParentChanged(null);
            VisualDiagnostics.OnVisualChildChanged(this, child, true);
        }


        /// <summary>
        /// DisconnectChild
        ///
        ///    Derived classes must call this method to notify the Visual3D layer that a
        ///    child was removed from the children collection. The Visual3D layer will then call
        ///    GetVisual3DChild to find out which child has been removed.
        ///
        /// </summary>
        protected void RemoveVisual3DChild(Visual3D child)
        {
            // It is invalid to modify the children collection that we
            // might be iterating during a property invalidation tree walk.
            if (IsVisualChildrenIterationInProgress)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotModifyVisualChildrenDuringTreeWalk));
            }

            Debug.Assert(child != null);
            Debug.Assert(child.InternalVisualParent == this);

            // invalid during a VisualTreeChanged event
            VisualDiagnostics.VerifyVisualTreeChange(this);

            VisualDiagnostics.OnVisualChildChanged(this, child, false);

            child.SetParent(/* newParent = */ (Visual3D) null);  // CS0121: Call is ambigious without casting null to Visual3D.

            // remove the inheritance context
            RemoveSelfAsInheritanceContext(child, null);

            //
            // Remove the child on all the channels
            // this visual is being marshalled to.
            //

            for (int i = 0, limit = _proxy.Count; i < limit; i++)
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

            //
            // Force a full precompute and render pass for this visual.
            //

            Visual3D.PropagateFlags(
                this,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.IsSubtreeDirtyForRender);

            // We do not currently support layout in 3D
            // UIElement.PropagateSuspendLayout(child);

            // Fire notifications
            child.FireOnVisualParentChanged(this);

            OnVisualChildrenChanged(/* visualAdded = */ null , child);
        }

        /// <summary>
        ///    The InternalIsVisible property roughly corresponds to the Opacity in the Visual world.
        ///    When it is set to true, then the actual transform used for this visual on the MIL side
        ///    is set to a zero scale transform.  This makes the object invisible.
        /// </summary>
        internal bool InternalIsVisible
        {
            get
            {
                return _internalIsVisible;
            }
            set
            {
                if (_internalIsVisible != value)
                {
                    // if we're going from Not Visible -> Visibile remove the zero scale from the Channel
                    // otherwise remove the user's transform
                    if (value)
                    {
                        DisconnectAttachedResource(
                            VisualProxyFlags.IsTransformDirty,
                            ((DUCE.IResource)_zeroScale));
                    }
                    else
                    {
                        Transform3D transform = Transform;

                        if (transform != null)
                        {
                            DisconnectAttachedResource(
                                VisualProxyFlags.IsTransformDirty,
                                ((DUCE.IResource)transform));
                        }
                    }

                    SetFlagsOnAllChannels(true, VisualProxyFlags.IsTransformDirty);

                    RenderChanged(/* sender = */ this, EventArgs.Empty);

                    _internalIsVisible = value;
                }
            }
        }

        // isSubpropertyChange indicates whether a subproperty on Visual3DModel changed.
        // In this case oldValue is ignored, since the value hasn't changed.  If it isn't a
        // sub property change, then oldValue gives the previous value of the Visual3DModel property.
        private void Visual3DModelPropertyChanged(Model3D oldValue, bool isSubpropertyChange)
        {
            if (!isSubpropertyChange)
            {
                if (oldValue != null)
                {
                    DisconnectAttachedResource(VisualProxyFlags.IsContentDirty,
                                               oldValue);
                }

                SetFlagsOnAllChannels(true, VisualProxyFlags.IsContentDirty);
            }

            SetFlags(false, VisualFlags.Are3DContentBoundsValid);

            RenderChanged(/* sender = */ this, EventArgs.Empty);
        }

        private void Visual3DModelPropertyChanged(object o, EventArgs e)
        {
            // forward on to the main property changed method.  Since this method is
            // only called on subproperty chanes, oldValue is meaningless.
            Visual3DModelPropertyChanged(null, /* isSubpropertyChange = */ true);
        }

        /// <summary>
        ///     The Model3D to render
        /// </summary>
        protected Model3D Visual3DModel
        {
            get
            {
                VerifyAPIReadOnly();

                return _visual3DModel;
            }

            set
            {
                VerifyAPIReadWrite();

                if (value != _visual3DModel)
                {
                    // remove the old change listener
                    if (_visual3DModel != null && !_visual3DModel.IsFrozenInternal)
                    {
                        _visual3DModel.ChangedInternal -= Visual3DModelPropertyChanged;
                    }

                    // notify of the property change
                    Visual3DModelPropertyChanged(_visual3DModel, /* isSubpropertyChange = */ false);
                    _visual3DModel = value;

                    // set the new one
                    if (_visual3DModel != null && !_visual3DModel.IsFrozenInternal)
                    {
                        _visual3DModel.ChangedInternal += Visual3DModelPropertyChanged;
                    }
                }
            }
        }

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

            // If we are attaching to a tree then
            // send the bit up if we need to.
            if (oldParent == null)
            {
                Debug.Assert(VisualTreeHelper.GetParent(this) != null, "If oldParent is null, current parent should != null.");

                if(CheckFlagsAnd(VisualFlags.SubTreeHoldsAncestorChanged))
                {
                    Visual.SetTreeBits(
                        VisualTreeHelper.GetParent(this),
                        VisualFlags.SubTreeHoldsAncestorChanged,
                        VisualFlags.RegisteredForAncestorChanged);
                }
            }
            // If we are cutting a sub tree off then
            // clear the bit in the main tree above if we need to.
            else
            {
                if (CheckFlagsAnd(VisualFlags.SubTreeHoldsAncestorChanged))
                {
                    Visual.ClearTreeBits(
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
        ///   Add removed delegates to the VisualAncenstorChanged Event.
        /// </summary>
        /// <remarks>
        ///     This also sets/clears the tree-searching bit up the tree
        /// </remarks>
        internal event Visual.AncestorChangedEventHandler VisualAncestorChanged
        {
            add
            {
                Visual.AncestorChangedEventHandler newHandler = AncestorChangedEventField.GetValue(this);

                if (newHandler == null)
                {
                    newHandler = value;
                }
                else
                {
                    newHandler += value;
                }

                AncestorChangedEventField.SetValue(this, newHandler);

                Visual.SetTreeBits(
                    this,
                    VisualFlags.SubTreeHoldsAncestorChanged,
                    VisualFlags.RegisteredForAncestorChanged);
            }

            remove
            {
                // check that we are Disabling a node that was previously Enabled
                if(CheckFlagsAnd(VisualFlags.SubTreeHoldsAncestorChanged))
                {
                    Visual.ClearTreeBits(
                                        this,
                                        VisualFlags.SubTreeHoldsAncestorChanged,
                                        VisualFlags.RegisteredForAncestorChanged);
                }

                // if we are Disabling a Visual that was not Enabled then this
                // search should fail.  But it is safe to check.
                Visual.AncestorChangedEventHandler newHandler = AncestorChangedEventField.GetValue(this);

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
            if (e is Visual)
            {
                Visual.ProcessAncestorChangedNotificationRecursive(e, args);
            }
            else
            {
                Visual3D eAsVisual3D = e as Visual3D;

                // If the flag is not set, then we are Done.
                if(!eAsVisual3D.CheckFlagsAnd(VisualFlags.SubTreeHoldsAncestorChanged))
                {
                    return;
                }

                // If there is a handler on this node, then fire it.
                Visual.AncestorChangedEventHandler handler = AncestorChangedEventField.GetValue(eAsVisual3D);

                if(handler != null)
                {
                    handler(eAsVisual3D, args);
                }

                // Decend into the children.
                int count = eAsVisual3D.InternalVisual2DOr3DChildrenCount;
                for (int i = 0; i < count; i++)
                {
                    DependencyObject child = eAsVisual3D.InternalGet2DOr3DVisualChild(i);
                    if (child != null)
                    {
                        Visual3D.ProcessAncestorChangedNotificationRecursive(child, args);
                    }
                }
            }
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

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal bool DoesRayHitSubgraphBounds(RayHitTestParameters rayParams)
        {
            Point3D origin;
            Vector3D direction;
            rayParams.GetLocalLine(out origin, out direction);

            Rect3D bboxSubgraph = VisualDescendantBounds;
            return LineUtil.ComputeLineBoxIntersection(ref origin, ref direction, ref bboxSubgraph, rayParams.IsRay);
        }

        /// <summary>
        /// Initiate a hit test using delegates.
        /// </summary>
        internal void HitTest(
            HitTestFilterCallback filterCallback,
            HitTestResultCallback resultCallback,
            HitTestParameters3D hitTestParameters)
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

            RayHitTestParameters rayParams = hitTestParameters as RayHitTestParameters;

            if (rayParams != null)
            {
                // In case the user is reusing the same RayHitTestParameters
                rayParams.ClearResults();

                HitTestResultBehavior result = RayHitTest(filterCallback, rayParams);

                rayParams.RaiseCallback(resultCallback, filterCallback, result);
            }
            else
            {
                // This should never happen, users can not extend the abstract HitTestParameters3D class.
                Invariant.Assert(false,
                    String.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "'{0}' HitTestParameters3D are not supported on {1}.",
                        hitTestParameters.GetType().Name, this.GetType().Name));
            }
        }

        internal HitTestResultBehavior RayHitTest(
            HitTestFilterCallback filterCallback,
            RayHitTestParameters rayParams)
        {
            if (DoesRayHitSubgraphBounds(rayParams))
            {
                //
                // Determine if there is a special filter behavior defined for this
                // Visual.
                //

                HitTestFilterBehavior behavior = HitTestFilterBehavior.Continue;

                if (filterCallback != null)
                {
                    behavior = filterCallback(this);

                    if (HTFBInterpreter.SkipSubgraph(behavior)) return HitTestResultBehavior.Continue;
                    if (HTFBInterpreter.Stop(behavior)) return HitTestResultBehavior.Stop;
                }

                //
                // Hit test against the children.
                //

                if (HTFBInterpreter.IncludeChildren(behavior))
                {
                    HitTestResultBehavior result = HitTestChildren(filterCallback, rayParams);

                    if (result == HitTestResultBehavior.Stop) return HitTestResultBehavior.Stop;
                }

                //
                // Hit test against the content of this Visual.
                //

                if (HTFBInterpreter.DoHitTest(behavior))
                {
                    RayHitTestInternal(filterCallback, rayParams);
                }
            }

            return HitTestResultBehavior.Continue;
        }

        internal HitTestResultBehavior HitTestChildren(
            HitTestFilterCallback filterCallback,
            RayHitTestParameters rayParams)
        {
            return HitTestChildren(filterCallback, rayParams, this);
        }

        /// <summary>
        ///     Static helper used by ModelVisual3D and Viewport3DVisual to hit test
        ///     against their children collections.
        /// </summary>
        internal static HitTestResultBehavior HitTestChildren(
            HitTestFilterCallback filterCallback,
            RayHitTestParameters rayParams,
            IVisual3DContainer container)
        {
            if (container != null)
            {
                int childrenCount = container.GetChildrenCount();

                for (int i = childrenCount - 1; i >= 0; i--)
                {
                    Visual3D child = container.GetChild(i);

                    // Visuall3D.RayHitTest does not apply the Visual3D's Transform.  We need to
                    // transform into the content's space before hit testing.
                    Transform3D transform = child.Transform;
                    rayParams.PushVisualTransform(transform);

                    // Perform the hit-test against the child.
                    HitTestResultBehavior result = child.RayHitTest(filterCallback, rayParams);

                    rayParams.PopTransform(transform);

                    if (result == HitTestResultBehavior.Stop)
                    {
                        return HitTestResultBehavior.Stop;
                    }
                }
            }

            return HitTestResultBehavior.Continue;
        }

        internal void RayHitTestInternal(
            HitTestFilterCallback filterCallback,
            RayHitTestParameters rayParams)
        {
            Model3D model = _visual3DModel;

            if (model != null)
            {
                // If our Model3D hit test intersects anything we should return "this" Visual3D
                // as the HitTestResult.VisualHit.
                rayParams.CurrentVisual = this;

                model.RayHitTest(rayParams);
            }
        }

        /// <summary>
        ///     Generic "render changed" handler which sets IsDirtyForRender
        ///     and propagates IsSubtreeDirtyForRender/Precompute.
        /// </summary>
        internal void RenderChanged(object sender, EventArgs e)
        {
            PropagateFlags(
                this,
                VisualFlags.IsSubtreeDirtyForPrecompute,
                VisualProxyFlags.IsSubtreeDirtyForRender);
        }

        /// <summary>
        /// VisualContentBounds returns the bounding box for the contents of the current visual.
        /// </summary>
        internal Rect3D VisualContentBounds
        {
            get
            {
                // Probably too restrictive. Let's see who wants it during OnRender.
                VerifyAPIReadWrite();

                return GetContentBounds();
            }
        }

        /// <summary>
        /// Visual2DContentBounds returns the 2D bounding box for the content of this 3D object.  The 2D bounding box
        /// is the projection of the 3D content bounding box up to the nearest 2D visual that contains the Visual3D.
        /// </summary>
        [FriendAccessAllowed]
        internal Rect Visual2DContentBounds
        {
            get
            {
                VerifyAPIReadWrite();
                Rect contentBounds = Rect.Empty;

                Viewport3DVisual viewport3DVisual = (Viewport3DVisual)VisualTreeHelper.GetContainingVisual2D(this);
                if (viewport3DVisual != null) {
                    GeneralTransform3DTo2D transform = TransformToAncestor(viewport3DVisual);
                    contentBounds = transform.TransformBounds(VisualContentBounds);
                }

                return contentBounds;
            }
        }

        internal Rect3D BBoxSubgraph
        {
            get
            {
                if (CheckFlagsAnd(VisualFlags.IsSubtreeDirtyForPrecompute))
                {
                    // force an update of the bounds cache
                    Rect3D transformedBBoxSubgraphIgnored;
                    PrecomputeRecursive(out transformedBBoxSubgraphIgnored);
                }

                Debug_VerifyCachedSubgraphBounds();

                return _bboxSubgraph;
            }

        }

        /// <summary>
        /// Derived classes must override this method and return the bounding
        /// box of their content in the Visual's local space.
        /// </summary>
        internal Rect3D GetContentBounds()
        {
            Model3D model = _visual3DModel;

            if (model == null)
            {
                return Rect3D.Empty;
            }

            if (!CheckFlagsAnd(VisualFlags.Are3DContentBoundsValid))
            {
                _bboxContent = model.CalculateSubgraphBoundsOuterSpace();
                SetFlags(true, VisualFlags.Are3DContentBoundsValid);
            }

            Debug_VerifyCachedContentBounds();

            return _bboxContent;
        }

        /// <summary>
        /// Returns the subgraph bounds in the Visual3D's outer coordinate space.
        /// </summary>
        internal Rect3D CalculateSubgraphBoundsOuterSpace()
        {
            Rect3D bounds = CalculateSubgraphBoundsInnerSpace();

            return M3DUtil.ComputeTransformedAxisAlignedBoundingBox(ref bounds, Transform);
        }

        /// <summary>
        /// Returns the subgraph bounds in the Visual3D's inner coordinate space.
        /// </summary>
        internal Rect3D CalculateSubgraphBoundsInnerSpace()
        {
            return BBoxSubgraph;
        }

        /// <summary>
        /// VisualDescendantBounds returns the union of all of the content bounding
        /// boxes for all of the descendants of the current visual, but not including
        /// the contents of the current visual.
        /// </summary>
        internal Rect3D VisualDescendantBounds
        {
            get
            {
                // Probably too restrictive. Let's see who wants it during OnRender.
                VerifyAPIReadWrite();

                return CalculateSubgraphBoundsInnerSpace();
            }
        }

        // CS0536: Intreface implementation must be public or explicit so we re-expose
        // the internal methods as an explicit implementation of an internal interface.
        void IVisual3DContainer.VerifyAPIReadOnly() { this.VerifyAPIReadOnly(); }
        void IVisual3DContainer.VerifyAPIReadOnly(DependencyObject other) { this.VerifyAPIReadOnly(other); }
        void IVisual3DContainer.VerifyAPIReadWrite() { this.VerifyAPIReadWrite(); }
        void IVisual3DContainer.VerifyAPIReadWrite(DependencyObject other) { this.VerifyAPIReadWrite(other); }

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
        internal void VerifyAPIReadOnly(DependencyObject other)
        {
            VerifyAPIReadOnly();

            if (other != null)
            {
                // Make sure the visual is on the same context as we are
                MediaSystem.AssertSameContext(this, other);
            }
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
        internal void VerifyAPIReadWrite(DependencyObject other)
        {
            VerifyAPIReadWrite();

            if (other != null)
            {
                // Make sure the visual is on the same context as we are
                MediaSystem.AssertSameContext(this, other);
            }
        }

        internal void SetParent(Visual newParent)
        {
            _2DParent.SetValue(this, newParent);
            _3DParent = null;

            Debug.Assert(InternalVisualParent == newParent);
        }

        internal void SetParent(Visual3D newParent)
        {
            _2DParent.ClearValue(this);
            _3DParent = newParent;

            Debug.Assert(InternalVisualParent == newParent);
        }

        /// <summary>
        ///  Derived classes override this property to enable the Visual code to enumerate
        ///  the Visual children. Derived classes need to return the number of children
        ///  from this method.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark: During this virtual method the Visual tree must not be modified.
        /// </summary>
        protected virtual int Visual3DChildrenCount
        {
            get { return 0; }
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
        ///       3D childern is the Viewport3DVisual which is sealed.
        /// </summary>
        protected virtual Visual3D GetVisual3DChild(int index)
        {
           throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
        }

        /// <summary>
        ///     Notifies the element that you have added a child.  The Element
        ///     will update the parent pointer, fire the correct events, etc.
        /// </summary>
        void IVisual3DContainer.AddChild(Visual3D child)
        {
            AddVisual3DChild(child);
        }

        /// <summary>
        ///     Notifies the element that you have removed a child.  The Element
        ///     will update the parent pointer, fire the correct events, etc.
        /// </summary>
        void IVisual3DContainer.RemoveChild(Visual3D child)
        {
            RemoveVisual3DChild(child);
        }

        /// <summary>
        ///     Gets the number of Visual3D children that the IVisual3DContainer
        ///     contains.
        /// </summary>
        int IVisual3DContainer.GetChildrenCount()
        {
            return Visual3DChildrenCount;
        }

        /// <summary>
        ///     Gets the index children of the IVisual3DContainer
        /// </summary>
        Visual3D IVisual3DContainer.GetChild(int index)
        {
            return GetVisual3DChild(index);
        }

        #region ForceInherit property support

        internal virtual void InvalidateForceInheritPropertyOnChildren(DependencyProperty property)
        {
            UIElement3D.InvalidateForceInheritPropertyOnChildren(this, property);
        }

        #endregion ForceInherit property support

        //------------------------------------------------------
        //
        //  DEBUG
        //
        //------------------------------------------------------

        #region DEBUG

        [Conditional("DEBUG")]
        internal void Debug_VerifyBoundsEqual(Rect3D bounds1, Rect3D bounds2, string errorString)
        {
            // The funny boolean logic below avoids asserts when the cached
            // bounds contain NaNs.  (NaN != NaN)
            bool boundsAreEqual =
                !(bounds1.X < bounds2.X || bounds1.X > bounds2.X) &&
                !(bounds1.Y < bounds2.Y || bounds1.Y > bounds2.Y) &&
                !(bounds1.Z < bounds2.Z || bounds1.Z > bounds2.Z) &&
                !(bounds1.SizeX < bounds2.SizeX || bounds1.SizeX > bounds2.SizeX) &&
                !(bounds1.SizeY < bounds2.SizeY || bounds1.SizeY > bounds2.SizeY) &&
                !(bounds1.SizeZ < bounds2.SizeZ || bounds1.SizeZ > bounds2.SizeZ);

            Debug.Assert(boundsAreEqual, errorString);
        }

        [Conditional("DEBUG")]
        internal void Debug_VerifyCachedSubgraphBounds()
        {
            Rect3D currentBounds = Rect3D.Empty;
#if DEBUG
            currentBounds = Debug_CalculateSubgraphBounds();
#endif
            // currentBounds includes Transform so we need to
            // temporarily transform _bboxSubgraph
            Rect3D cachedBounds = M3DUtil.ComputeTransformedAxisAlignedBoundingBox(ref _bboxSubgraph, Transform);

            Debug_VerifyBoundsEqual(cachedBounds, currentBounds, "Cached bbox subgraph is incorrect!");
        }

        [Conditional("DEBUG")]
        internal void Debug_VerifyCachedContentBounds()
        {
            Model3D model = _visual3DModel;

            Debug.Assert(model != null);

            Debug_VerifyBoundsEqual(model.CalculateSubgraphBoundsOuterSpace(),
                                    _bboxContent, "Cached content bounds is incorrect!");
        }

// [Conditional] does not work on methods that return values
#if DEBUG
        internal Rect3D Debug_CalculateSubgraphBounds()
        {
            Rect3D currentSubgraphBounds = GetContentBounds();

            for (int i = 0, count = Visual3DChildrenCount; i < count; i++)
            {
                currentSubgraphBounds.Union(
                    GetVisual3DChild(i).Debug_CalculateSubgraphBounds()
                    );
            }

            return M3DUtil.ComputeTransformedAxisAlignedBoundingBox(ref currentSubgraphBounds, Transform);
        }
#endif

        #endregion DEBUG

        /// <summary>
        /// Precompute pass.
        /// </summary>
        internal void PrecomputeRecursive(out Rect3D bboxSubgraph)
        {
            if (CheckFlagsAnd(VisualFlags.IsSubtreeDirtyForPrecompute))
            {
                //
                // Update the subgraph bounding box which includes the content bounds
                // and the bounds of our children.
                //

                _bboxSubgraph = GetContentBounds();

                for (int i = 0, count = Visual3DChildrenCount; i < count; i++)
                {
                    Visual3D child = GetVisual3DChild(i);

                    Rect3D bboxSubgraphChild;
                    child.PrecomputeRecursive(out bboxSubgraphChild);
                    _bboxSubgraph.Union(bboxSubgraphChild);
                }

                SetFlags(false, VisualFlags.IsSubtreeDirtyForPrecompute);
            }

            bboxSubgraph = M3DUtil.ComputeTransformedAxisAlignedBoundingBox(ref _bboxSubgraph, Transform);
       }

        internal void RenderRecursive(RenderContext ctx)
        {
            DUCE.Channel channel = ctx.Channel;
            DUCE.ResourceHandle handle = DUCE.ResourceHandle.Null;
            VisualProxyFlags flags = c_Model3DVisualProxyFlagsDirtyMask;

            //
            // Ensure that the resource for this Visual is sent to our current channel.
            //
            bool isOnChannel = IsOnChannel(channel);
            if (isOnChannel)
            {
                //
                // Good, we're already on channel. Get the handle and the flags.
                //

                handle = _proxy.GetHandle(channel);
                flags = _proxy.GetFlags(channel);
            }
            else
            {
                //
                // Create the visual resource on the current channel.
                //
                // Note: we need to update all set properties (the dirty
                //       bit mask is set by default).
                //

                handle = ((DUCE.IResource)this).AddRefOnChannel(channel);
            }

            //
            // Hookup content to the Visual
            //

            if ((flags & VisualProxyFlags.IsContentDirty) != 0)
            {
                RenderContent(ctx, isOnChannel);
            }

            //
            // Update the transform.
            //

            if ((flags & VisualProxyFlags.IsTransformDirty) != 0)
            {
                Transform3D transform = Transform;
                if (transform != null && InternalIsVisible)
                {
                    //
                    // Set the new transform resource for this visual.  If transform is
                    // null we don't need to do this.  Also note that the old transform
                    // was disconnected in the Transform property setter.
                    //

                    DUCE.Visual3DNode.SetTransform(
                        handle,
                        ((DUCE.IResource)transform).AddRefOnChannel(channel),
                        channel);
                }
                else if (!InternalIsVisible)
                {
                    DUCE.Visual3DNode.SetTransform(
                        handle,
                        ((DUCE.IResource)_zeroScale).AddRefOnChannel(channel),
                        channel);
                }
                else if (!isOnChannel) /* Transform == null */
                {
                    DUCE.Visual3DNode.SetTransform(
                        handle,
                        DUCE.ResourceHandle.Null,
                        channel);
                }
            }

            // Visit children of this node -----------------------------------------------------------------------

            Debug.Assert((flags & VisualProxyFlags.IsContentNodeConnected) == 0,
                "Only HostVisuals are expected to have a content node.");

            for (int i = 0; i < Visual3DChildrenCount; i++)
            {
                Visual3D child = GetVisual3DChild(i);

                if (child != null)
                {
                    if (child.CheckFlagsAnd(channel, VisualProxyFlags.IsSubtreeDirtyForRender) || // or the visual is dirty
                        !(child.IsOnChannel(channel))) // or the child has not been marshalled yet.
                    {
                        child.RenderRecursive(ctx);
                    }

                    if (child.IsOnChannel(ctx.Channel))
                    {
                        if (!child.CheckFlagsAnd(channel, VisualProxyFlags.IsConnectedToParent))
                        {
                            DUCE.Visual3DNode.InsertChildAt(
                                handle,
                                ((DUCE.IResource)child).GetHandle(channel),
                                /* iPosition = */ (uint)i,
                                ctx.Channel);

                            child.SetFlags(channel, true, VisualProxyFlags.IsConnectedToParent);
                        }
                    }
                }
            }

            //
            // Finally, reset the dirty flags for this visual (at this point,
            // we have handled them all).
            //

            SetFlags(channel, false, c_Model3DVisualProxyFlagsDirtyMask);
        }

        /// <summary>
        /// RenderContent is implemented to hook up the Visual3Ds content.
        /// The implementer of this function can assert that the _hCompNode
        /// is valid on a channel when the function is executed.
        /// </summary>
        internal void RenderContent(RenderContext ctx, bool isOnChannel)
        {
            DUCE.Channel channel = ctx.Channel;

            Debug.Assert(!CheckFlagsAnd(channel, VisualProxyFlags.IsContentConnected));
            Debug.Assert(IsOnChannel(channel));

            //
            // Create the content on the channel.
            //

            if (_visual3DModel != null)
            {
                //
                // Attach the content to the visual resource
                //

                DUCE.Visual3DNode.SetContent(
                    ((DUCE.IResource)this).GetHandle(channel),
                    ((DUCE.IResource)_visual3DModel).AddRefOnChannel(channel),
                    channel);

                SetFlags(channel, true, VisualProxyFlags.IsContentConnected);
            }
            else if (isOnChannel) /* Model == null */
            {
                DUCE.Visual3DNode.SetContent(
                    ((DUCE.IResource)this).GetHandle(channel),
                    DUCE.ResourceHandle.Null,
                    channel);
            }
        }

        /// <summary>
        /// Returns true if the specified ancestor is really the ancestor of the
        /// given descendant.
        /// </summary>
        public bool IsAncestorOf(DependencyObject descendant)
        {
            Visual visual;
            Visual3D visual3D;

            VisualTreeUtils.AsNonNullVisual(descendant, out visual, out visual3D);

            // x86 branch prediction skips the branch on first encounter.  We favor 3D.
            if(visual != null)
            {
                return visual.IsDescendantOf(this);
            }

            return visual3D.IsDescendantOf(this);
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

            // Walk up the parent chain of the descendant untill we run out
            // of 3D parents or we find the ancestor.
            Visual3D current = this;

            while (current != null && current != ancestor)
            {
                // If our 3D parent is null then continue walk in 2D
                if (current._3DParent == null)
                {
                    DependencyObject parent2D = current.InternalVisualParent;

                    if (parent2D != null)
                    {
                        // The type has to be Visual because of the above if condition
                        return ((Visual)parent2D).IsDescendantOf(ancestor);
                    }
                }

                current = current._3DParent;
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
            Visual3D current = this;

            do
            {
                current.SetFlags(value, flag);

                if (current._3DParent == null)
                {
                    VisualTreeUtils.SetFlagsToRoot(InternalVisualParent, value, flag);
                    return;
                }

                current = current._3DParent;
            }
            while (current != null);
        }

        /// <summary>
        ///     Finds the first ancestor of the given element which has the given
        ///     flags set.
        /// </summary>
        internal DependencyObject FindFirstAncestorWithFlagsAnd(VisualFlags flag)
        {
            Visual3D current = this;

            do
            {
                if (current.CheckFlagsAnd(flag))
                {
                    // The other Visual crossed through this Visual's parent chain. Hence this is our
                    // common ancestor.
                    return current;
                }

                if (current._3DParent == null)
                {
                    return VisualTreeUtils.FindFirstAncestorWithFlagsAnd(InternalVisualParent, flag);
                }

                current = current._3DParent;
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

        /// <summary>
        /// Override this function in derived classes to release unmanaged resources during Dispose
        /// and during removal of a subtree.
        /// </summary>
        internal void FreeDUCEResources(DUCE.Channel channel)
        {
            Transform3D transform = Transform;
            if (!CheckFlagsAnd(channel, VisualProxyFlags.IsTransformDirty))
            {
                if (InternalIsVisible)
                {
                    if (transform != null)
                    {
                        ((DUCE.IResource)transform).ReleaseOnChannel(channel);
                    }
                }
                else
                {
                    ((DUCE.IResource)_zeroScale).ReleaseOnChannel(channel);
                }
            }

            Model3D model = _visual3DModel;
            if ((model != null) && (!CheckFlagsAnd(channel, VisualProxyFlags.IsContentDirty)))
            {
                ((DUCE.IResource)model).ReleaseOnChannel(channel);
            }

            Debug.Assert(IsOnChannel(channel));
            Debug.Assert(!CheckFlagsAnd(channel, VisualProxyFlags.IsContentNodeConnected));

            _proxy.ReleaseOnChannel(channel);
        }

        void DUCE.IResource.ReleaseOnChannel(DUCE.Channel channel)
        {
            ReleaseOnChannelCore(channel);
        }

        internal void ReleaseOnChannelCore(DUCE.Channel channel)
        {
            if (!IsOnChannel(channel))
            {
                return;
            }

            // at this point the tree is not connected any more.
            SetFlags(channel, false, VisualProxyFlags.IsConnectedToParent);

            FreeDUCEResources(channel);

            for (int i = 0; i < Visual3DChildrenCount; i++)
            {
                Visual3D child = GetVisual3DChild(i);
                ((DUCE.IResource)child).ReleaseOnChannel(channel);
            }
        }

        /// <summary>
        /// Disconnects a resource attached to this visual.
        /// </summary>
        internal void DisconnectAttachedResource(
            VisualProxyFlags correspondingFlag,
            DUCE.IResource attachedResource)
        {
            //
            // Iterate over the channels this visual is being marshaled to
            //

            for (int i = 0; i < _proxy.Count; i++)
            {
                VisualProxyFlags flags = _proxy.GetFlags(i);

                if ((flags & correspondingFlag) == 0)
                {
                    DUCE.Channel channel = _proxy.GetChannel(i);

                    //
                    // Set the flag so that during render we send
                    // update to the compositor.
                    //
                    SetFlags(channel, true, correspondingFlag);

                    if (correspondingFlag == VisualProxyFlags.IsContentDirty)
                    {
                        _proxy.SetFlags(i, false, VisualProxyFlags.IsContentConnected);
                    }

                    attachedResource.ReleaseOnChannel(channel);
                }
            }
        }

        // Normally the inheritence context is the same as our parent.  When it differs,
        // we store the value in the _inheritanceContext UncommonField.  See comments
        // on the _inheritanceContext and UseParentAsContext members for more info.
        internal override DependencyObject InheritanceContext
        {
            get
            {
                DependencyObject value = _inheritanceContext.GetValue(this);

                if (value == UseParentAsContext)
                {
                    return InternalVisualParent;
                }

                return value;
            }
        }

        internal override void AddInheritanceContext(DependencyObject context, DependencyProperty property)
        {
            base.AddInheritanceContext(context, property);

            // Call local helper
            AddOrRemoveInheritanceContext(context);
        }

        internal override void RemoveInheritanceContext(DependencyObject context, DependencyProperty property)
        {
            base.RemoveInheritanceContext(context, property);

            // Call local helper
            AddOrRemoveInheritanceContext(null);
        }

        // Local helping for adding or removing an inheritance context
        private void AddOrRemoveInheritanceContext(DependencyObject newInheritanceContext)
        {
            // If we are using our parent as our inheritance context the InheritanceContext
            // property will already be returning the new context, but we still need to treat
            // this as a change so we notify our dependants.
            bool contextChanged =
                InheritanceContext != newInheritanceContext ||
                (_inheritanceContext.GetValue(this) == UseParentAsContext &&
                    newInheritanceContext == InternalVisualParent);

            if (contextChanged)
            {
                // Pick up the new context
                SetInheritanceContext(newInheritanceContext);
                OnInheritanceContextChanged(EventArgs.Empty);
            }
        }

        internal override bool HasMultipleInheritanceContexts
        {
            get { return base.HasMultipleInheritanceContexts; }
        }

        internal override void OnInheritanceContextChangedCore(EventArgs args) // ancestor changed
        {
            base.OnInheritanceContextChangedCore(args);

            for (int i = 0; i < Visual3DChildrenCount; i++)
            {
                DependencyObject child = GetVisual3DChild(i);

                Debug.Assert(child.InheritanceContext == this,
                    "How did a child get inserted without propagating our inheritance context?");

                child.OnInheritanceContextChanged(args);
            }
        }

        #endregion Internal Methods

        // --------------------------------------------------------------------
        //
        //   Visual-to-Visual Transforms
        //
        // --------------------------------------------------------------------

        #region Visual-to-Visual Transforms

        /// <summary>
        /// Returns a transform that can be used to transform coordinates from this
        /// node to the specified ancestor, or null if the transformation cannot be created.
        /// 2D is allowed to be between the 3D nodes.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// If ancestor is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the ancestor Visual3D is not a ancestor of Visual3D.
        /// </exception>
        /// <exception cref="InvalidOperationException">If the Visual3Ds are not connected.</exception>
        public GeneralTransform3D TransformToAncestor(Visual3D ancestor)
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
        /// is non-invertible.  This is the case when 2D is between the nodes.
        ///
        /// </summary>
        /// <exception cref="ArgumentException">
        /// If the reference Visual3D is not a ancestor of the descendant Visual3D.
        /// </exception>
        /// <exception cref="ArgumentNullException">If the descendant argument is null.</exception>
        /// <exception cref="InvalidOperationException">If the Visual3Ds are not connected.</exception>
        public GeneralTransform3D TransformToDescendant(Visual3D descendant)
        {
            if (descendant == null)
            {
                throw new ArgumentNullException("descendant");
            }

            VerifyAPIReadOnly(descendant);

            return descendant.InternalTransformToAncestor(this, true);
        }

        /// <summary>
        /// Returns the transform or the inverse transform between this visual and the specified ancestor.
        /// If inverse is requested but does not exist (if the transform is not invertible), null is returned.
        /// </summary>
        /// <param name="ancestor">Ancestor visual.</param>
        /// <param name="inverse">Returns inverse if this argument is true.</param>
        private GeneralTransform3D InternalTransformToAncestor(Visual3D ancestor, bool inverse)
        {
            Debug.Assert(ancestor != null);

            // used to track if all the collected transforms on the way to the ancestor were valid
            bool success = true;

            DependencyObject g = this;
            Visual3D lastVisual3D = null;

            Matrix3D m = Matrix3D.Identity;
            GeneralTransform3DGroup group = null;

            // This while loop will walk up the visual tree until we encounter the ancestor.
            // As it does so, it will accumulate the descendent->ancestor transform.
            // In most cases, this is simply a matrix, though if we encounter a 2D node we need to
            // transform from 3D out in to 2D and then back in to 3D and continue the parent walk.
            // We will accumulate the current transform in a matrix until we encounter a 2D parent,
            // at which point we will add the matrix's current value and the transform from 3D to 2D to 3D
            // to the GeneralTransform3DGroup and continue to accumulate further transforms in the matrix again.
            // At the end of this loop, we will have 0 or more transforms in the GeneralTransform3DGroup
            // and the matrix which, if not identity, should be appended to the GeneralTransform3DGroup.
            // If, as is commonly the case, this loop terminates without encountering a 2D parent
            // we will simply use the Matrix3D.

            while ((VisualTreeHelper.GetParent(g) != null) && (g != ancestor))
            {
                Visual3D gAsVisual3D = g as Visual3D;
                if (gAsVisual3D != null)
                {
                    Transform3D transform = gAsVisual3D.Transform;
                    if (transform != null)
                    {
                        transform.Append(ref m);
                    }

                    lastVisual3D = gAsVisual3D;
                    g = VisualTreeHelper.GetParent(gAsVisual3D);
                }
                else
                {
                    if (group == null)
                    {
                        group = new GeneralTransform3DGroup();
                    }

                    group.Children.Add(new MatrixTransform3D(m));
                    m = Matrix3D.Identity;

                    // construct the 3D to 2D to 3D transform
                    Visual gAsVisual = g as Visual;
                    GeneralTransform3DTo2D transform3DTo2D = lastVisual3D.TransformToAncestor(gAsVisual);

                    // now find the 3D parent of the 2D object
                    Visual3D containing3DParent = VisualTreeHelper.GetContainingVisual3D(gAsVisual);

                    // if containing3DParent is null, then the ancestor parameter is not really an ancestor
                    // break out of the loop to allow it to fail
                    if (containing3DParent == null)
                    {
                        break;
                    }

                    GeneralTransform2DTo3D transform2DTo3D = gAsVisual.TransformToAncestor(containing3DParent);

                    // if either transform ends up being null then we don't have a transform
                    if (transform3DTo2D == null || transform2DTo3D == null)
                    {
                        // we don't want to break here because although we own't be able to create a valid transformation
                        // we also want to throw an exception if the ancestor passed in is not a valid ancestor.  We then
                        // continue the tree walk to make sure.
                        success = false;
                    }
                    else
                    {
                        group.Children.Add(new GeneralTransform3DTo2DTo3D(transform3DTo2D, transform2DTo3D));
                    }

                    // the last visual3D found is where we continue the search
                    g = containing3DParent;
                }
            }

            if (g != ancestor)
            {
                throw new System.InvalidOperationException(SR.Get(inverse ? SRID.Visual_NotADescendant : SRID.Visual_NotAnAncestor));
            }

            // construct the generaltransform3d to return and invert it if necessary
            GeneralTransform3D finalTransform = null;

            // if we successfully found a transform then we can create it here, otherwise finalTransform stays null
            if (success)
            {
                if (group != null)
                {
                    finalTransform = group;
                }
                else
                {
                    finalTransform = new MatrixTransform3D(m);
                }

                if (inverse)
                {
                    finalTransform = finalTransform.Inverse;
                }
            }

            if (finalTransform != null)
            {
                finalTransform.Freeze();
            }

            return finalTransform;
        }

        /// <summary>
        /// Returns a transform that can be used to transform coordinate from this
        /// node to the specified ancestor, or null if the transform does not exist.
        /// This transform will take a 3D point, and then project it in to 2D space.
        /// The resulting point is the transformed 3D point in the coordinate space
        /// of the given ancestor.
        ///
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// If ancestor is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the ancestor Visual is not an ancestor of this.
        /// </exception>
        /// <exception cref="InvalidOperationException">If the Visual3D and Visual are not connected.</exception>
        public GeneralTransform3DTo2D TransformToAncestor(Visual ancestor)
        {
            if (ancestor == null)
            {
                throw new ArgumentNullException("ancestor");
            }

            VerifyAPIReadOnly(ancestor);

            return InternalTransformToAncestor(ancestor);
        }

        /// <summary>
        /// Provides the transform between this visual3D and the specified ancestor, or null
        /// if the transform does not exist.
        ///
        /// </summary>
        /// <param name="ancestor">Ancestor visual.</param>
        /// <returns>The transform from 3D to 2D</returns>
        internal GeneralTransform3DTo2D InternalTransformToAncestor(Visual ancestor)
        {
            Debug.Assert(ancestor != null);

            // get the transform out of 3D and in to 2D
            Viewport3DVisual containingViewport;
            Matrix3D projectionTransform;

            if (!M3DUtil.TryTransformToViewport3DVisual(this, out containingViewport, out projectionTransform))
            {
                return null;
            }

            // get the transform from the Viewport3DVisual to the ancestor
            GeneralTransform transformIn2D = containingViewport.TransformToAncestor(ancestor);

            // package the two up in the transformTo2D
            GeneralTransform3DTo2D result = new GeneralTransform3DTo2D(projectionTransform, transformIn2D);
            result.Freeze();

            return result;
        }

        #endregion Visual-to-Visual Transforms

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal DependencyObject InternalVisualParent
        {
            get
            {
                if (_3DParent != null)
                {
                    Debug.Assert(_2DParent.GetValue(this) == null,
                        "Only one parent pointer should be set at a time.");

                    return _3DParent;
                }

                DependencyObject parent2D = _2DParent.GetValue(this);

                Debug.Assert(parent2D == null || parent2D is Viewport3DVisual,
                    "The only acceptable 2D parent for a Visual3D is a Viewport3DVisual.");

                return parent2D;
            }
        }

        internal int ParentIndex
        {
            get { return _parentIndex; }
            set { _parentIndex = value; }
        }

        // Are we in the process of iterating the visual children.
        // This flag is set during a descendents walk, for property invalidation.
        internal bool IsVisualChildrenIterationInProgress
        {
            [FriendAccessAllowed]
            get { return CheckFlagsAnd(VisualFlags.IsVisualChildrenIterationInProgress); }

            [FriendAccessAllowed]
            set { SetFlags(value, VisualFlags.IsVisualChildrenIterationInProgress); }
        }

        #endregion Internal Properties

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
        internal void SetFlags(bool value, VisualFlags Flags)
        {
            _flags = value ? (_flags | Flags) : (_flags & (~Flags));
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
        /// Returns the child at index "index" (in most cases this will be
        /// a Visual, but it some cases, Viewport3DVisual for instance,
        /// this is a Visual3D).
        ///
        /// Used only by VisualTreeHelper.
        /// </summary>
        internal virtual DependencyObject InternalGet2DOr3DVisualChild(int index)
        {
            // Call the right virtual method.
            return GetVisual3DChild(index);
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
                return Visual3DChildrenCount;
            }
        }

        /// <summary>
        /// Checks if any of the specified flags is set on a given channel.
        /// </summary>
        /// <remarks>
        /// If there aren't any bits set on the specified flags
        /// the method returns true.
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
        ///     Check all the children for a bit.
        /// </summary>
        internal static bool DoAnyChildrenHaveABitSet(Visual3D pe,
                                                      VisualFlags flag)
        {
            int count = pe.InternalVisual2DOr3DChildrenCount;
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = pe.InternalGet2DOr3DVisualChild(i);

                Visual v = null;
                Visual3D v3D = null;
                VisualTreeUtils.AsNonNullVisual(child, out v, out v3D);

                if (v != null && v.CheckFlagsAnd(flag))
                {
                    return true;
                }
                else if (v3D != null && v3D.CheckFlagsAnd(flag))
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
            Visual3D e,
            VisualFlags flags,
            VisualProxyFlags proxyFlags)
        {
            while ((e != null) &&
                   (!e.CheckFlagsAnd(flags) || !e.CheckFlagsOnAllChannels(proxyFlags)))
            {
                // These asserts are mostly for documentation when diffing the 2D/3D
                // implementations.
                Debug.Assert(!e.CheckFlagsOr(VisualFlags.ShouldPostRender),
                    "Visual3Ds should never be the root of a tree.");
                 Debug.Assert(!e.CheckFlagsOr(VisualFlags.NodeIsCyclicBrushRoot),
                    "Visual3Ds should never be the root of an ICyclicBrush.");

                e.SetFlags(true, flags);
                e.SetFlagsOnAllChannels(true, proxyFlags);

                // If our 3D parent is null call back into VisualTreeUtils to potentially
                // continue the walk in 2D.
                if (e._3DParent == null)
                {
                    Viewport3DVisual viewport = e.InternalVisualParent as Viewport3DVisual;

                    Debug.Assert((viewport == null) == (e.InternalVisualParent == null),
                        "Viewport3DVisual is the only supported 2D parent of a 3D visual.");

                    if(viewport != null)
                    {
                        // We must notify the 2D visual that its contents have changed.
                        // This will cause the 2D visual to set it's content dirty flag
                        // and continue the propagation of IsDirtyForRender/Precompute.
                        viewport.Visual3DTreeChanged();

                        // continue propagating flags up the 2D world
                        Visual.PropagateFlags(viewport, flags, proxyFlags);
                    }

                    // Stop propagating.  We are at the root of the 3D subtree.
                    return;
                }

                e = e._3DParent;
            }
        }

        #endregion Visual flags manipulation

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void SetInheritanceContext(DependencyObject newInheritanceContext)
        {
            if (newInheritanceContext == InternalVisualParent)
            {
                _inheritanceContext.ClearValue(this);

                Debug.Assert(_inheritanceContext.GetValue(this) == UseParentAsContext,
                    "Clearing the _inheritanceContext uncommon field should put us in the UseParentAsContext state.");
            }
            else
            {
                _inheritanceContext.SetValue(this, newInheritanceContext);
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        internal VisualProxy _proxy;

        #endregion Internal Fields

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // If the parent of the Visual3D is another Visual3D we store the parent in the _3DParent field.
        // If the parent is a 2D node (as is the case when this is the root of a Viewport3DVisual) we
        // store the parent in an UncommonField.  Both fields must be considered when determining
        // the parent of this node.
        private static readonly UncommonField<Visual> _2DParent =
            new UncommonField<Visual>(/* defaultValue = */ null);

        // Sentinel value we use to differentiate between a null inheritance context stored in the
        // _inheritanceContext uncommon field and "empty", meaning use the parent as context.
        private static readonly DependencyObject UseParentAsContext = new DependencyObject();

        // Normally the inheritance context is the same as the parent, except when we are parent to
        // a Viewport3D visual, in which case we use this uncommon field to store are alternate context.
        private static readonly UncommonField<DependencyObject> _inheritanceContext =
            new UncommonField<DependencyObject>(/* defaultValue = */ UseParentAsContext);

        private static readonly UncommonField<Visual.AncestorChangedEventHandler> AncestorChangedEventField
            = new UncommonField<Visual.AncestorChangedEventHandler>();

        private Visual3D _3DParent;
        private int _parentIndex = -1;

        private VisualFlags _flags;

        // Untransformed *cached* content bounds. Do not access it directly -- instead
        // use GetContentBounds()
        private Rect3D _bboxContent;

        // Untransformed *cached* subgraph bounds. Do not access it directly -- instead
        // use the BBoxSubgraph property.
        private Rect3D _bboxSubgraph = Rect3D.Empty;

        private bool _internalIsVisible;

        private static readonly ScaleTransform3D _zeroScale = new ScaleTransform3D(0, 0, 0);

        private Model3D _visual3DModel;

        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Protected Fields
        //
        //------------------------------------------------------

        #region Protected Fields

        #endregion Protected Fields
    }
}


