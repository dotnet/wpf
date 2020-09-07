// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: 
//
//              See spec at: AdornerLayer Spec.htm
// 

using System;
using System.Windows.Media;
using System.Diagnostics;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Threading;
using System.Windows.Controls;

using MS.Utility;
using MS.Internal;
using MS.Internal.Controls;
using MS.Internal.Media;

namespace System.Windows.Documents
{
    /// <summary>
    /// Visual decoration including but not limited to adornments, rubberband selection and 
    /// non-live move feedback.
    /// 
    /// AdornerLayer expects to be parented by an AdornerDecorator.
    /// </summary>
    public class AdornerLayer : FrameworkElement
    {
        /// <summary>
        /// Adorner information
        /// </summary>
        internal class AdornerInfo
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal AdornerInfo(Adorner adorner)
            {
                Invariant.Assert(adorner != null);

                _adorner = adorner;
            }

            /// <summary>
            /// Adorner
            /// </summary>
            internal Adorner Adorner
            {
                get
                {
                    return _adorner;
                }
            }

            /// <summary>
            /// The RenderSize (bounding box) of the object we're adorning
            /// </summary>
            internal Size RenderSize
            {
                get
                {
                    return _computedSize;
                }
                set
                {
                    _computedSize = value;
                }
            }

            /// <summary>
            /// Transform on the Visual
            /// </summary>
            internal GeneralTransform Transform
            {
                get
                {
                    return _transform;
                }
                set
                {
                    _transform = value;
                }
            }

            internal int ZOrder
            {
                get
                {
                    return _zOrder;
                }
                set
                {
                    _zOrder = value;
                }
            }

            internal Geometry Clip
            {
                get
                {
                    return _clip;
                }
                set
                {
                    _clip = value;
                }
            }

            private Adorner _adorner;
            private Size _computedSize;
            private GeneralTransform _transform;
            private int _zOrder;
            private Geometry _clip;
        }

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
 
        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks>
        /// Note that because we're setting up an event handler here, we won't be GC'd until
        /// our parent is GC'd.  So the implicit assumption is that the AdornerLayer, once
        /// created, exists until its parent is deleted.
        /// </remarks>
        internal AdornerLayer() : this(Dispatcher.CurrentDispatcher)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">Dispatcher</param>
        /// <remarks>
        /// Note that because we're setting up an event handler here, we won't be GC'd until
        /// our parent is GC'd.  So the implicit assumption is that the AdornerLayer, once
        /// created, exists until its parent is deleted.
        /// </remarks>
        internal AdornerLayer(Dispatcher context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            LayoutUpdated += new EventHandler(OnLayoutUpdated);
            _children = new VisualCollection(this);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Add given Adorner to our children
        /// </summary>
        /// <param name="adorner">Adorner to add</param>
        public void Add(Adorner adorner)
        {
            Add(adorner, DefaultZOrder);
        }

        /// <summary>
        /// Remove given adorner.  This method will not complain if the given adorner is not
        /// in the AdornerLayer.
        /// </summary>
        /// <param name="adorner">adorner to remove</param>
        public void Remove(Adorner adorner)
        {
            if (adorner == null)
                throw new ArgumentNullException("adorner");

            ArrayList adornerInfos = ElementMap[adorner.AdornedElement] as ArrayList;
            if (adornerInfos == null)
            {
                // We currently allow adorners to be added on elements that can't be adorned, then
                // throw those away later without notifying anyone.  Consequently, Remove() shouldn't throw.
                return;
            }
            AdornerInfo adornerInfo = GetAdornerInfo(adornerInfos, adorner);
            if (adornerInfo == null)
            {
                // We currently allow adorners to be added on elements that can't be adorned, then
                // throw those away later without notifying anyone.  Consequently, Remove() shouldn't throw.
                return;
            }

            RemoveAdornerInfo(ElementMap, adorner, adorner.AdornedElement);
            RemoveAdornerInfo(_zOrderMap, adorner, adornerInfo.ZOrder);
            _children.Remove(adorner);
            RemoveLogicalChild(adorner);
        }

        /// <summary>
        /// Update (layout and render) all adorners.  
        /// </summary>
        public void Update()
        {
            foreach (UIElement key in ElementMap.Keys)
            {
                ArrayList adornerInfos = (ArrayList)ElementMap[key];
                int i = 0;

                if (adornerInfos != null)
                {
                    while (i < adornerInfos.Count)
                    {
                        InvalidateAdorner((AdornerInfo)adornerInfos[i++]);
                    }
                }
            }

            UpdateAdorner(null);
        }

        /// <summary>
        /// Update (layout and render) all adorners for the given element.  
        /// </summary>
        /// <param name="element">element key for redraw</param>
        public void Update(UIElement element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            ArrayList adornerInfos = ElementMap[element] as ArrayList;

            if (adornerInfos == null)
                throw new InvalidOperationException(SR.Get(SRID.AdornedElementNotFound));

            int i = 0;

            while (i < adornerInfos.Count)
            {
                InvalidateAdorner((AdornerInfo)adornerInfos[i++]);
            }

            UpdateAdorner(element);
        }

        /// <summary>
        /// Return a collection of all adorners adorning the given element
        /// </summary>
        /// <param name="element">Element for which adorners are to be retrieved</param>
        /// <returns>array of adorners on given element, or null if
        /// no adorners exist</returns>
        public Adorner[] GetAdorners(UIElement element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            ArrayList adornerInfos = ElementMap[element] as ArrayList;

            if (adornerInfos == null || adornerInfos.Count == 0)
                return null;

            Adorner[] adorners = new Adorner[adornerInfos.Count];

            for (int i = 0; i < adornerInfos.Count; i++)
                adorners[i] = ((AdornerInfo)adornerInfos[i]).Adorner;

            return adorners;
        }

        /// <summary>
        /// Determine if the given point is on an adorner.
        /// </summary>
        /// <param name="point">point to test</param>
        /// <returns>AdornerHitTestResult containing the hit visual and the adorner that visual
        /// is part of.  If the no adorner was hit, null is returned</returns>
        public AdornerHitTestResult AdornerHitTest(Point point)
        {
            PointHitTestResult result = VisualTreeUtils.AsNearestPointHitTestResult(VisualTreeHelper.HitTest(this, point, false));

            if (result != null && result.VisualHit != null)
            {
                Visual visual = result.VisualHit;

                while (visual != this)
                {
                    if (visual is Adorner)
                        return new AdornerHitTestResult(result.VisualHit, result.PointHit, visual as Adorner);

                    // we intentionally separate adorners from spanning 3D boundaries
                    // and if the parent is ever 3D there was a mistake
                    visual = (Visual)VisualTreeHelper.GetParent(visual);
                }

                return null;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Walk up the visual tree to find the nearest AdornerLayer.
        /// </summary>
        /// <param name="visual">Visual from which the treewalk begins</param>
        /// <returns>First AdornerLayer above given element, or null</returns>
        static public AdornerLayer GetAdornerLayer(Visual visual)
        {
            if (visual == null)
                throw new ArgumentNullException("visual");

            Visual parent = VisualTreeHelper.GetParent(visual) as Visual;

            while (parent != null)
            {
                if (parent is AdornerDecorator)
                    return ((AdornerDecorator)parent).AdornerLayer;
                if (parent is ScrollContentPresenter)
                    return ((ScrollContentPresenter)parent).AdornerLayer;

                parent = VisualTreeHelper.GetParent(parent) as Visual;
            }

            return null;
        }

        #endregion Public Methods        

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        #endregion Public Properties

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
        ///  Derived classes override this property to enable the Visual code to enumerate 
        ///  the Visual children. Derived classes need to return the number of children
        ///  from this method.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark: 
        ///      During this virtual method the Visual tree must not be modified.
        /// </summary>        
        protected override int VisualChildrenCount
        {
            get 
            { 
                //_children cannot be null as its initialized in the constructor
                return _children.Count; 
            }       
        }

        /// <summary>
        ///   Derived class must implement to support Visual children. The method must return
        ///    the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark: 
        ///       During this virtual call it is not valid to modify the Visual tree. 
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            //_children cannot be null as its initialized in the constructor
            // index range check done by VisualCollection        
            return _children[index];
        }

        /// <summary>
        /// Returns enumerator to logical children.
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                if ((this.VisualChildrenCount == 0))
                {
                    return EmptyEnumerator.Instance;
                }

                return _children.GetEnumerator();
            }
        }
        /// <summary>
        /// AdornerLayer always returns a size of (0,0).
        /// The AdornerLayer's size should be the same as its parent, but not take up layout space.  This means
        /// parents containing an AdornerLayer should stretch it to their own size.
        /// </summary>
        /// <param name="constraint">
        /// Sizing constraint.
        /// </param>
        protected override Size MeasureOverride(Size constraint)
        {
            // Not using an enumerator because the list can be modified during the loop when we call out.
            DictionaryEntry[] zOrderMapEntries = new DictionaryEntry[_zOrderMap.Count];
            _zOrderMap.CopyTo(zOrderMapEntries, 0);

            for (int i = 0; i < zOrderMapEntries.Length; i++)
            {
                ArrayList adornerInfos = (ArrayList)zOrderMapEntries[i].Value;
                Debug.Assert(adornerInfos != null, "No adorners found for element in AdornerLayer._zOrderMap");

                int j = 0;
                while (j < adornerInfos.Count)
                {
                    AdornerInfo adornerInfo = (AdornerInfo)adornerInfos[j++];
                    adornerInfo.Adorner.Measure(constraint);
                }
            }

            // Returning 0,0 prevents an invalidation of Measure for AdornerLayer from unnecessarily dirtying the parent.
            return new Size();
        }

        /// <summary>
        /// Override for <seealso cref="UIElement.ArrangeCore" />  
        /// </summary>
        /// <remarks>
        /// We need information from AdornerInfo.  Since we keep one AdornerInfo per
        /// Adorner, and we expect a 1:1 mapping between Adorners and our visual children,
        /// it makes more sense for us to iterate across AdornerInfos instead of
        /// our visual children here.  This means that if someone somehow adds a non-Adorner
        /// child to AdornerLayer, it will never render.
        /// </remarks>
        /// <param name="finalSize">The location reserved for this element by the parent</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // Not using an enumerator because the list can be modified during the loop when we call out.
            DictionaryEntry[] zOrderMapEntries = new DictionaryEntry[_zOrderMap.Count];
            _zOrderMap.CopyTo(zOrderMapEntries, 0);

            for (int i = 0; i < zOrderMapEntries.Length; i++)
            {
                ArrayList adornerInfos = (ArrayList)zOrderMapEntries[i].Value;

                Debug.Assert(adornerInfos != null, "No adorners found for element in AdornerLayer._zOrderMap");

                int j = 0;
                while (j < adornerInfos.Count)
                {
                    AdornerInfo adornerInfo = (AdornerInfo)adornerInfos[j++];

                    if (!adornerInfo.Adorner.IsArrangeValid)    // optimization
                    {
                        // We're dependent on Arrange to get the rendersize of the adorner, so Arrange before
                        // doing our transform magic.
                        adornerInfo.Adorner.Arrange(new Rect(new Point(), adornerInfo.Adorner.DesiredSize));
                        GeneralTransform proposedTransform = adornerInfo.Adorner.GetDesiredTransform(adornerInfo.Transform);
                        GeneralTransform adornerTransform = GetProposedTransform(adornerInfo.Adorner, proposedTransform);

                        int index = _children.IndexOf(adornerInfo.Adorner);

                        if (index >= 0)
                        {
                            // Get the matrix transform out, skip all non affine transforms
                            Transform transform = (adornerTransform != null) ? adornerTransform.AffineTransform : null;
                            
                            ((Adorner)(_children[index])).AdornerTransform = transform;
                        }
                    }
                    if (adornerInfo.Adorner.IsClipEnabled)
                    {
                        adornerInfo.Adorner.AdornerClip = adornerInfo.Clip;
                    }
                    else if (adornerInfo.Adorner.AdornerClip != null)
                    {
                        adornerInfo.Adorner.AdornerClip = null;
                    }
                }
            }

            return finalSize;
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Add given Adorner to our children
        /// </summary>
        /// <param name="adorner">Adorner to add</param>
        /// <param name="zOrder">z-order</param>
        internal void Add(Adorner adorner, int zOrder)
        {
            if (adorner == null)
                throw new ArgumentNullException("adorner");

            AdornerInfo adornerInfo = new AdornerInfo(adorner);
            adornerInfo.ZOrder = zOrder;

            AddAdornerInfo(ElementMap, adornerInfo, adorner.AdornedElement);

            AddAdornerToVisualTree(adornerInfo, zOrder);

            AddLogicalChild(adorner);

            UpdateAdorner(adorner.AdornedElement);
        }

        /// <summary>
        /// Clean all the dynamically-updated data from the Adorner
        /// </summary>
        /// <param name="adornerInfo">AdornerInfo to scrub</param>
        internal void InvalidateAdorner(AdornerInfo adornerInfo)
        {
            Debug.Assert(adornerInfo != null, "Adorner should not be null");
            adornerInfo.Adorner.InvalidateMeasure();
            adornerInfo.Adorner.InvalidateVisual();
            adornerInfo.RenderSize = new Size(Double.NaN, Double.NaN);
            adornerInfo.Transform = null;
        }

        /// <summary>
        /// OnLayoutUpdated event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        internal void OnLayoutUpdated(object sender, EventArgs args)
        {
            if (ElementMap.Count == 0)
                return;

            UpdateAdorner(null);
        }

        /// <summary>
        /// Set the zOrder on the given adorner.
        /// </summary>
        /// <param name="adorner"></param>
        /// <param name="zOrder"></param>
        internal void SetAdornerZOrder(Adorner adorner, int zOrder)
        {
            ArrayList adornerInfos = ElementMap[adorner.AdornedElement] as ArrayList;
            if (adornerInfos == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.AdornedElementNotFound));
            }
            AdornerInfo adornerInfo = GetAdornerInfo(adornerInfos, adorner);
            if (adornerInfo == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.AdornerNotFound));
            }

            RemoveAdornerInfo(_zOrderMap, adorner, adornerInfo.ZOrder);
            _children.Remove(adorner);
            adornerInfo.ZOrder = zOrder;
            AddAdornerToVisualTree(adornerInfo, zOrder);
            InvalidateAdorner(adornerInfo);
            UpdateAdorner(adorner.AdornedElement);
        }

        /// <summary>
        /// Query the zOrder on the given adorner.
        /// </summary>
        /// <param name="adorner"></param>
        /// <returns>zOrder of given adorner</returns>
        internal int GetAdornerZOrder(Adorner adorner)
        {
            ArrayList adornerInfos = ElementMap[adorner.AdornedElement] as ArrayList;
            if (adornerInfos == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.AdornedElementNotFound));
            }
            AdornerInfo adornerInfo = GetAdornerInfo(adornerInfos, adorner);
            if (adornerInfo == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.AdornerNotFound));
            }

            return adornerInfo.ZOrder;
        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal HybridDictionary ElementMap
        {
            get { return this._elementMap; }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Adds the given adorner to the visual tree in the proper zOrder order.
        /// </summary>
        /// <param name="adornerInfo"></param>
        /// <param name="zOrder"></param>
        private void AddAdornerToVisualTree(AdornerInfo adornerInfo, int zOrder)
        {
            Adorner adorner = adornerInfo.Adorner;
            Debug.Assert(adorner != null);

            AddAdornerInfo(_zOrderMap, adornerInfo, zOrder);

            // We've already added the adorner to the zOrderMap, so we can't get null back
            ArrayList adornerInfos = (ArrayList)_zOrderMap[zOrder];
            if (adornerInfos.Count > 1)
            {
                // The easy case.  Find the index of the adorner immediately in front of the
                // new one and insert the new one after it.
                int index = adornerInfos.IndexOf(adornerInfo);
                int insertionIndex = _children.IndexOf(((AdornerInfo)adornerInfos[index - 1]).Adorner) + 1;
                _children.Insert(insertionIndex, adorner);
            }
            else
            {
                // The hard case.  Find the set of adorners with the closest, but lower, zOrder.
                IList keys = _zOrderMap.GetKeyList();
                int index = keys.IndexOf(zOrder) - 1;
                if (index < 0)
                {
                    // nothing's lower than the new adorner.  Make it the first child.
                    _children.Insert(0, adorner);
                }
                else
                {
                    // find the last adorner at this zOrder and add the new one after it.
                    adornerInfos = (ArrayList)_zOrderMap[keys[index]];
                    int insertionIndex = _children.IndexOf(((AdornerInfo)adornerInfos[adornerInfos.Count - 1]).Adorner) + 1;
                    _children.Insert(insertionIndex, adorner);
                }
            }
        }

        /// <summary>
        /// Remove all adorners for the given element
        /// </summary>
        /// <param name="element">element key for removal</param>
        private void Clear(UIElement element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            ArrayList adornerInfos = ElementMap[element] as ArrayList;

            if (adornerInfos == null)
                throw new InvalidOperationException(SR.Get(SRID.AdornedElementNotFound));

            while (adornerInfos.Count > 0)
            {
                AdornerInfo info = adornerInfos[0] as AdornerInfo;

                Remove(info.Adorner);
            }

            ElementMap.Remove(element);
        }

        /// <summary>
        /// Update the Adorners for the given element
        /// </summary>
        /// <param name="element">UIElement for which we're updating AdornerSet</param>
        private void UpdateElementAdorners(UIElement element)
        {
            Size size;

            // we intentionally do not ascend in to a 3D scene
            Visual adornerLayerParent = VisualTreeHelper.GetParent(this) as Visual;
            if (adornerLayerParent == null)
            {
                return;
            }

            Debug.Assert(element != null);
            ArrayList adornerInfos = ElementMap[element] as ArrayList;
            if (adornerInfos == null)
            {
                return;
            }

            bool dirty = false;

            //
            // See if the adorners need to be rerendered due to object resizing
            //
            GeneralTransform transform = element.TransformToAncestor(adornerLayerParent);                            

            for (int i = 0; i < adornerInfos.Count; i++)
            {
                AdornerInfo adornerInfo = (AdornerInfo)adornerInfos[i];
                size = element.RenderSize;
                Geometry clip = null;
                bool clipChanged = false;
                if (adornerInfo.Adorner.IsClipEnabled)
                {
                    clip = GetClipGeometry(adornerInfo.Adorner.AdornedElement, adornerInfo.Adorner);
                    if (adornerInfo.Clip == null && clip != null || adornerInfo.Clip != null && clip == null ||
                     (adornerInfo.Clip != null && clip != null && adornerInfo.Clip.Bounds != clip.Bounds))
                    {
                        clipChanged = true;
                    }
                }

                if (adornerInfo.Adorner.NeedsUpdate(adornerInfo.RenderSize) || adornerInfo.Transform == null ||
                    transform.AffineTransform == null || adornerInfo.Transform.AffineTransform == null ||
                    transform.AffineTransform.Value != adornerInfo.Transform.AffineTransform.Value ||
                    clipChanged)
                {
                    InvalidateAdorner(adornerInfo);
                    adornerInfo.RenderSize = size;
                    adornerInfo.Transform = transform;
                    if (adornerInfo.Adorner.IsClipEnabled)
                    {
                        adornerInfo.Clip = clip;
                    }
                    dirty = true;
                }
            }

            if (dirty)
                InvalidateMeasure();
        }

        /// <summary>
        /// Update the Adorner for the given element
        /// </summary>
        /// <param name="element">UIElement for which we're updating AdornerSet</param>
        private void UpdateAdorner(UIElement element)
        {
            Visual adornerLayerParent = VisualTreeHelper.GetParent(this) as Visual;
            if (adornerLayerParent == null)
            {
                // Never update when the adorner layer is not part of a visual tree.
                return;
            }

            // We only expect one to have been removed on any one call.
            ArrayList removeList = new ArrayList(1);

            if (element != null)
            {
                // Make sure element is still beneath the adorner decorator
                if (!element.IsDescendantOf(adornerLayerParent))
                {
                    removeList.Add(element);
                }
                else
                {
                    UpdateElementAdorners(element);
                }
            }
            else
            {
                ICollection keyCollection = ElementMap.Keys;
                UIElement[] keys = new UIElement[keyCollection.Count];
                keyCollection.CopyTo(keys, 0);  // make a static copy of the keys to prevent any possible enumerator exceptions

                for (int i = 0; i < keys.Length; i++)
                {
                    UIElement elTemp = (UIElement)keys[i];

                    // Make sure element is still beneath the adorner decorator
                    if (!elTemp.IsDescendantOf(adornerLayerParent))
                    {
                        removeList.Add(elTemp);
                    }
                    else
                    {
                        UpdateElementAdorners(elTemp);
                    }
                }
            }

            for (int i = 0; i < removeList.Count; i++)
            {
                Clear((UIElement)removeList[i]);
            }
        }

        /// <summary>
        /// Walk up the tree from the adorned element to the AdornerLayer's parent, accumulating
        /// clip geometries as we go.  Called when IsClipEnabled == true to allow an adorner
        /// to be clipped (which normally, it isn't).
        /// </summary>
        private CombinedGeometry GetClipGeometry(Visual element, Adorner adorner)
        {
            Visual oldElement = null;

            // we intentionally do not ascend in to a 3D scene            
            Visual adornerLayerParent = VisualTreeHelper.GetParent(this) as Visual;
            if (adornerLayerParent == null)
            {
                return null;
            }

            CombinedGeometry combinedGeometry = null;

            // If the element has been removed from the tree and we've not yet had a chance
            // to remove the adorner, there's obviously no clipping
            if (!adornerLayerParent.IsAncestorOf(element))
            {
                return null;
            }

            while (element != adornerLayerParent && element != null)
            {
                Geometry geometry = VisualTreeHelper.GetClip(element);
                if (geometry != null)
                {
                    if (combinedGeometry == null)
                    {
                        combinedGeometry = new CombinedGeometry(geometry, null);
                    }
                    else
                    {
                        GeneralTransform transform = oldElement.TransformToAncestor(element);
                        combinedGeometry.Transform = transform.AffineTransform;
                        combinedGeometry = new CombinedGeometry(combinedGeometry, geometry);
                        combinedGeometry.GeometryCombineMode = GeometryCombineMode.Intersect;
                    }
                    oldElement = element;
                }

                // we intentionally separate adorners from spanning 3D boundaries
                // and if the parent is ever 3D there was a mistake
                element = (Visual)VisualTreeHelper.GetParent(element);
            }
            if (combinedGeometry != null)
            {
                // transform the last combined geometry up to the top
                GeneralTransform transform = oldElement.TransformToAncestor(adornerLayerParent);
                if (transform == null)
                {
                    combinedGeometry = null;
                }
                else
                {
                    TransformGroup transformGroup = new TransformGroup();
                    transformGroup.Children.Add(transform.AffineTransform);

                    // Now transform back down to the adorner
                    transform = adornerLayerParent.TransformToDescendant(adorner);
                    if (transform == null)
                    {
                        combinedGeometry = null;
                    }
                    else
                    {
                        transformGroup.Children.Add(transform.AffineTransform);
                        combinedGeometry.Transform = transformGroup;
                    }
                }
            }

            return combinedGeometry;
        }

        /// <summary>
        /// Remove the given adorner's AdornerInfo from the given AdornerInfo list.
        /// </summary>
        /// <param name="infoMap">dictionary of adornerInfo</param>
        /// <param name="adorner">adorner</param>
        /// <param name="key">key</param>
        /// <returns>true if info was found and removed</returns>
        private bool RemoveAdornerInfo(IDictionary infoMap, Adorner adorner, object key)
        {
            ArrayList adornerInfos = infoMap[key] as ArrayList;

            if (adornerInfos != null)
            {
                AdornerInfo adornerInfo = GetAdornerInfo(adornerInfos, adorner);
                if (adornerInfo != null)
                {
                    adornerInfos.Remove(adornerInfo);
                    if (adornerInfos.Count == 0)
                    {
                        infoMap.Remove(key);
                    }
                    return true;
                }
            }
            return false;
        }

        private AdornerInfo GetAdornerInfo(ArrayList adornerInfos, Adorner adorner)
        {
            if (adornerInfos != null)
            {
                int i = 0;

                while (i < adornerInfos.Count)
                {
                    if (((AdornerInfo)adornerInfos[i]).Adorner == adorner)
                    {
                        return (AdornerInfo)adornerInfos[i];
                    }
                    i++;
                }
            }
            return null;
        }

        private void AddAdornerInfo(IDictionary infoMap, AdornerInfo adornerInfo, object key)
        {
            ArrayList adornerInfos;

            if (infoMap[key] == null)
            {
                adornerInfos = new ArrayList(1);
                infoMap[key] = adornerInfos;
            }
            else
            {
                adornerInfos = (ArrayList)infoMap[key];
            }
            adornerInfos.Add(adornerInfo);
        }
        
        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 4; }
        }

        GeneralTransform GetProposedTransform(Adorner adorner, GeneralTransform sourceTransform)
        {
            // Flip horizontally if Right to Left.
            if (adorner.FlowDirection != this.FlowDirection)
            {
                GeneralTransformGroup group;
                MatrixTransform matrixTransform;
                Matrix matrix;

                group = new GeneralTransformGroup();

                matrix = new Matrix(-1.0, 0.0, 0.0, 1.0, adorner.RenderSize.Width, 0.0);
                matrixTransform = new MatrixTransform(matrix);
                group.Children.Add(matrixTransform);

                if (sourceTransform != null && sourceTransform != Transform.Identity)
                {
                    group.Children.Add(sourceTransform);
                }

                return group;
            }

            return sourceTransform;
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private HybridDictionary _elementMap = new HybridDictionary(10);
        private SortedList _zOrderMap = new SortedList(10);
        private const int DefaultZOrder = System.Int32.MaxValue;
        private VisualCollection _children;

        #endregion Private Fields
    }
}



