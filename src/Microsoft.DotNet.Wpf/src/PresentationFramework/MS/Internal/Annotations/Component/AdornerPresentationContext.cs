// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      AdornerPresentationContext knows that annotation comonents are wrapped 
//      in an AnnotationAdorner and hosted in the AdornerLayer. Note, implementation-wise 
//      a new PresentationContext is created for every annotation component. Executing 
//      operations on a presentation context for a different annotation component 
//      (located in the same adorner layer) works, but is slower than using the 
//      presentation context stored in the annotation component.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Documents;
using System.Windows.Media;
using System.Collections;

namespace MS.Internal.Annotations.Component
{
    /// <summary>
    /// AdornerPresentationContext knows that annotation comonents are wrapped in an AnnotationAdorner and hosted in the AdornerLayer.
    /// Note,  implementation-wise a new PresentationContext is created for every annotation component. Executing operations on a presentation context
    /// for a different annotation component (located in the same adorner layer) works, but is slower than using the presentation context stored in the
    /// annotation component.
    /// </summary>
    internal class AdornerPresentationContext : PresentationContext
    {
        #region Constructors

        /// <summary>
        /// Create an initialized instance of an AdornerPresentationContext. Set the presentation context of the 
        /// component wrapped in the adorner
        /// </summary>
        /// <param name="adornerLayer">AdornerLayer this presentation context is on, must not be null</param>
        /// <param name="adorner">AnnotationAdorner that wraps the annotation component.  Will be null in case of creating enclosing context</param>
        private AdornerPresentationContext(AdornerLayer adornerLayer, AnnotationAdorner adorner)
        {
            if (adornerLayer == null) throw new ArgumentNullException("adornerLayer");

            _adornerLayer = adornerLayer;
            if (adorner != null)
            {
                if (adorner.AnnotationComponent == null)
                    throw new ArgumentNullException("annotation component");
                if (adorner.AnnotationComponent.PresentationContext != null)
                    throw new InvalidOperationException(SR.Get(SRID.ComponentAlreadyInPresentationContext, adorner.AnnotationComponent));
                _annotationAdorner = adorner;
            }
        }

        #endregion Constructors

        #region Static Methods

        /// <summary>
        /// Host a component in an adorner layer.
        /// Wrap the component in an annotation adorner, add that to the adorner layer, create and set presentation context and invalidate to pick up styles.
        /// Note, this is called from two places: (1) component manager to host choosen annotation component, and (2) presentation context when component
        /// adds additional IAnnotationComponent.
        /// </summary>
        /// <param name="adornerLayer">Adorner layer the component is hosted in</param>
        /// <param name="component">Component that is being hosted</param>
        /// <param name="annotatedElement">element being annotated</param>
        /// <param name="reorder">if true - put the component on top and calculate its z-order</param>
        internal static void HostComponent(AdornerLayer adornerLayer, IAnnotationComponent component, UIElement annotatedElement, bool reorder)
        {
            AnnotationAdorner newAdorner = new AnnotationAdorner(component, annotatedElement);
            // Create the context for the layer and adorner, make sure the adorner's component has its context.
            newAdorner.AnnotationComponent.PresentationContext = new AdornerPresentationContext(adornerLayer, newAdorner);

            int level = GetComponentLevel(component);

            if (reorder)
            {
                component.ZOrder = GetNextZOrder(adornerLayer, level);
            }

            adornerLayer.Add(newAdorner, ComponentToAdorner(component.ZOrder, level));
        }


        /// <summary>
        /// Sets the Z-order level of an annotation Component type
        /// </summary>
        /// <param name="type">the component type</param>
        /// <param name="level">level - 0 means on top of all other types, bigger number means
        /// lower level</param>
        /// <remarks> ZLevel defines the Z-order disposition of this component type according to other
        /// component types in the same adorner layer. Components with lower ZLevel will be instantiated 
        /// on top of the components with Higher ZLevel.
        /// The Z-order of all the components with the same ZLevel is defined by the value of 
        /// IAnnotationComponent.ZOrder property with zero meaning the component is on top of all other
        /// inside the same level. ZOrder property can be changed by invoking
        /// BringToTop method. This will move the component to the top of its priority group. If there are other
        /// components with higher priority they will still be on top of that component. If more than
        /// one component type have the same ZLevel that means they all can stay on top of each other.
        /// Setting IAnnotationComponent.ZOrder must be invoked only by the PrezentationContext
        /// when the Z-order changes. It can not be set by application in v1.</remarks>
        internal static void SetTypeZLevel(Type type, int level)
        {
            Invariant.Assert(level >= 0, "level is < 0");

            Invariant.Assert(type != null, "type is null");

            if (_ZLevel.ContainsKey(type))
            {
                _ZLevel[type] = level;
            }
            else
            {
                _ZLevel.Add(type, level);
            }
        }

        /// <summary>
        /// the allowed Z-order values range for this level. 
        /// Used to define minimal Z-order value for types that are supposed to live above TextSelection
        /// which has a fixed Z-order value
        /// </summary>
        /// <param name="level">the Z-order level</param>
        /// <param name="min">min Z-order value for this level</param>
        /// <param name="max">max Z-order value for this level</param>
        internal static void SetZLevelRange(int level, int min, int max)
        {
            if (_ZRanges[level] == null)
            {
                _ZRanges.Add(level, new ZRange(min, max));
            }
        }

        #endregion Static Methods


        #region Public Properties

        /// <summary>
        /// Returns the adorner layer which acts as a host for annotation components managed by the annotation component manager
        /// </summary>
        /// <value>UIElement for the adorner layer</value>
        public override UIElement Host { get { return _adornerLayer; } }

        /// <summary>
        /// Get the enclosing presentation context.
        /// </summary>
        /// <value>Enclosing PresentationContext or null if there is none</value>
        public override PresentationContext EnclosingContext
        {
            get
            {
                Visual parent = VisualTreeHelper.GetParent(_adornerLayer) as Visual;
                if (parent == null) return null;

                AdornerLayer parentLayer = AdornerLayer.GetAdornerLayer((UIElement)parent);
                if (parentLayer == null) return null;

                PresentationContext p = new AdornerPresentationContext(parentLayer, null);

                return p;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Add the component to the adornerlayer of this presentation context.
        /// Create a new presentation context which includes the wrapped annotation adorner and the adornerlayer.
        /// Assign new presentation context into the component.
        /// </summary>
        /// <param name="component">Component to add to host</param>
        public override void AddToHost(IAnnotationComponent component)
        {
            if (component == null) throw new ArgumentNullException("component");

            AdornerPresentationContext.HostComponent(_adornerLayer, component, component.AnnotatedElement, false);
        }

        /// <summary>
        /// Remove annotation component from host;  in our case: respective annotation adorner from adornerLayer.
        /// If this presentation context does not contain the component search the adorner layer.
        /// Null out the presentation context of the component and set the local annotationAdorner to null if necessary,
        /// ask the annotation adorner to remove all visual children.
        /// </summary>
        /// <param name="component">Component to remove from host</param>
        /// <param name="reorder">if true - recalculate z-order</param>
        public override void RemoveFromHost(IAnnotationComponent component, bool reorder)
        {
            if (component == null) throw new ArgumentNullException("component");

            if (IsInternalComponent(component))
            {
                _annotationAdorner.AnnotationComponent.PresentationContext = null;
                _adornerLayer.Remove(_annotationAdorner);
                _annotationAdorner.RemoveChildren();
                _annotationAdorner = null;
            }
            else
            {// need to find annotation adorner in layer, remove it and do house-keeping
                AnnotationAdorner foundAdorner = this.FindAnnotationAdorner(component);

                if (foundAdorner == null) throw new InvalidOperationException(SR.Get(SRID.ComponentNotInPresentationContext, component));

                _adornerLayer.Remove(foundAdorner);
                foundAdorner.RemoveChildren();

                // now get rid of reference from presentation context of annotation component to annotation adorner
                AdornerPresentationContext p = component.PresentationContext as AdornerPresentationContext;

                if (p != null) p.ResetInternalAnnotationAdorner();

                // finally get rid of reference from annotation component to presentation context
                component.PresentationContext = null;
            }
        }

        /// <summary>
        /// Invalidate the transform for this adorner. called when adorner inside changed aspects of the transform.
        /// This might go away if InvalidateMeasure works 
        /// (unclear if Peter means this should work on the adorner or even one down on the annotation component itself)
        /// </summary>
        /// <param name="component">Component to invalidate transform for</param>
        public override void InvalidateTransform(IAnnotationComponent component)
        {
            AnnotationAdorner adorner = GetAnnotationAdorner(component);
            adorner.InvalidateTransform();
        }

        /// <summary>
        /// Sets a component on top of its ZLevel
        /// </summary>
        /// <param name="component">Component to change z-order of</param>
        public override void BringToFront(IAnnotationComponent component)
        {
            AnnotationAdorner adorner = GetAnnotationAdorner(component);
            int level = GetComponentLevel(component);
            int nextLevel = GetNextZOrder(_adornerLayer, level);

            // Only change the ZOrder if its not already on the top
            if (nextLevel != component.ZOrder + 1)
            {
                component.ZOrder = nextLevel;
                _adornerLayer.SetAdornerZOrder(adorner, ComponentToAdorner(component.ZOrder, level));
            }
        }

        /// <summary>
        /// Sets a component on bttom of its ZLevel
        /// </summary>
        /// <param name="component">Component to change z-order of</param>
        public override void SendToBack(IAnnotationComponent component)
        {
            AnnotationAdorner adorner = GetAnnotationAdorner(component);
            int level = GetComponentLevel(component);

            // Only change the ZOrder if its not already on the bottom
            if (0 != component.ZOrder)
            {
                component.ZOrder = 0;
                UpdateComponentZOrder(component);
            }
        }

        /// <summary>
        ///     Determines if the passed in object is equal to this object.
        ///     Two AdornerPresentationContexts will be equal if they both have the same adorner layer.
        /// </summary>
        /// <param name="o">The object to compare with.</param>
        /// <returns>True if the objects are equal. False otherwise.</returns>
        public override bool Equals(object o)
        {
            AdornerPresentationContext p = o as AdornerPresentationContext;

            if (p != null)
            {
                return (p._adornerLayer == this._adornerLayer);
            }

            return false;
        }

        /// <summary>
        /// overload operator for ==, to be same as Equal implementation.
        /// </summary>
        /// <param name="left">AdornerPresentationContext to compare</param>
        /// <param name="right">AdornerPresentationContext to compare</param>
        /// <returns></returns>
        public static bool operator ==(AdornerPresentationContext left, AdornerPresentationContext right)
        {
            if ((object)left == null)
                return (object)right == null;

            return left.Equals(right);
        }

        /// <summary>
        /// overload operator for !=, to go along with definition for ==
        /// </summary>
        /// <param name="c1">AdornerPresentationContext to compare</param>
        /// <param name="c2">AdornerPresentationContext to compare</param>
        /// <returns></returns>
        public static bool operator !=(AdornerPresentationContext c1, AdornerPresentationContext c2)
        {
            return !(c1 == c2);
        }

        /// <summary>
        ///     Delegate hash to adorner layer
        /// </summary>
        public override int GetHashCode()
        {
            return (int)this._adornerLayer.GetHashCode();
        }


        /// <summary>
        /// Updates the ZOrder of the input component and on all components with the same ZLevel 
        /// that have same or bigger Z-order as the  input component on a given adorner layer
        /// </summary>
        /// <param name="component">the component</param>
        public void UpdateComponentZOrder(IAnnotationComponent component)
        {
            Invariant.Assert(component != null, "null component");

            //check Z-order range for this level
            int level = GetComponentLevel(component);
            //get the component's adorner
            AnnotationAdorner adorner = FindAnnotationAdorner(component);
            if (adorner == null)
                return;

            //set the adorner z-order
            _adornerLayer.SetAdornerZOrder(adorner, ComponentToAdorner(component.ZOrder, level));

            List<AnnotationAdorner> adorners = GetTopAnnotationAdorners(level, component);
            if (adorners == null)
                return;

            int lastZOrder = component.ZOrder + 1;
            foreach (AnnotationAdorner topAdorner in adorners)
            {
                topAdorner.AnnotationComponent.ZOrder = lastZOrder;
                _adornerLayer.SetAdornerZOrder(topAdorner, ComponentToAdorner(lastZOrder, level));
                lastZOrder++;
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Reset the annotation adorner to null.  This is needed for reset of found adorner in method RemoveFromHost
        /// </summary>
        private void ResetInternalAnnotationAdorner()
        {
            _annotationAdorner = null;
        }

        /// <summary>
        /// Return true if the given annotation component is the one this presentation context is on.
        /// </summary>
        /// <param name="component">The component that might be referred to by this presentation context</param>
        /// <returns>True if the component is internal</returns>
        private bool IsInternalComponent(IAnnotationComponent component)
        {
            return _annotationAdorner != null && component == _annotationAdorner.AnnotationComponent;
        }

        /// <summary>
        /// Return the annotation adorner for the given annotation component.
        /// will not look at local annotation adorner, will always iterate through annotation adorners of adorner layer.
        /// Return null if none can be found.
        /// </summary>
        /// <param name="component">The component that is wrapped by an annotation adorner</param>
        /// <returns>The annotation adorner that wraps the component in the adorner layer associated with this presentation context</returns>
        private AnnotationAdorner FindAnnotationAdorner(IAnnotationComponent component)
        {
            if (_adornerLayer == null) return null;

            foreach (Adorner adorner in _adornerLayer.GetAdorners(component.AnnotatedElement))
            {
                AnnotationAdorner annotationAdorner = adorner as AnnotationAdorner;

                if (annotationAdorner != null && annotationAdorner.AnnotationComponent == component) return annotationAdorner;
            }

            return null;
        }

        /// <summary>
        /// Finds all AnnotationAddorners from particular Z-level that have the same or bigger z-order as the component
        /// </summary>
        /// <param name="level">the ZLevel of interest</param>
        /// <param name="component">the component</param>
        /// <returns>the AnnotationAdorner children</returns>
        private List<AnnotationAdorner> GetTopAnnotationAdorners(int level, IAnnotationComponent component)
        {
            List<AnnotationAdorner> res = new List<AnnotationAdorner>();

            int count = VisualTreeHelper.GetChildrenCount(_adornerLayer);
            if (count == 0)
                return res;

            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(_adornerLayer, i);
                AnnotationAdorner adorner = child as AnnotationAdorner;
                if (adorner != null)
                {
                    IAnnotationComponent childComponent = adorner.AnnotationComponent;
                    if ((childComponent != component) &&
                       (GetComponentLevel(childComponent) == level) &&
                       (childComponent.ZOrder >= component.ZOrder))
                    {
                        AddAdorner(res, adorner);
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Inserts an adorner after the last adorner with ZOrder less or equal of the input one
        /// </summary>
        /// <param name="adorners">adorners list</param>
        /// <param name="adorner">the new adorner</param>
        /// <remarks>In most cases the AnnotationAdorners are already orderd so we expect that the new one
        /// will be added at the end of the list. That is why we start scaning from the end.</remarks>
        private void AddAdorner(List<AnnotationAdorner> adorners, AnnotationAdorner adorner)
        {
            Debug.Assert((adorners != null) && (adorner != null), "null adorners list or adorner");

            int index = 0;
            if (adorners.Count > 0)
            {
                for (index = adorners.Count; index > 0; index--)
                {
                    if (adorners[index - 1].AnnotationComponent.ZOrder <= adorner.AnnotationComponent.ZOrder)
                        break;
                }
            }

            adorners.Insert(index, adorner);
        }

        /// <summary>
        /// Gets the next free Z-order value for the components in this level
        /// </summary>
        /// <param name="adornerLayer">adorner layer</param>
        /// <param name="level">Z-level</param>
        /// <returns>next free Z-order value</returns>
        private static int GetNextZOrder(AdornerLayer adornerLayer, int level)
        {
            Invariant.Assert(adornerLayer != null, "null adornerLayer");

            int res = 0;

            int count = VisualTreeHelper.GetChildrenCount(adornerLayer);
            if (count == 0)
                return res;

            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(adornerLayer, i);
                AnnotationAdorner adorner = child as AnnotationAdorner;
                if (adorner != null)
                {
                    if ((GetComponentLevel(adorner.AnnotationComponent) == level) &&
                        (adorner.AnnotationComponent.ZOrder >= res))
                    {
                        res = adorner.AnnotationComponent.ZOrder + 1;
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Finds the correct AdornerLayer where a component lives
        /// </summary>
        /// <param name="component">the component</param>
        /// <returns></returns>
        private AnnotationAdorner GetAnnotationAdorner(IAnnotationComponent component)
        {
            if (component == null) throw new ArgumentNullException("component");

            //find the adornerLayer
            AnnotationAdorner adorner = _annotationAdorner;
            if (!this.IsInternalComponent(component))
            {
                adorner = this.FindAnnotationAdorner(component);

                if (adorner == null) throw new InvalidOperationException(SR.Get(SRID.ComponentNotInPresentationContext, component));
            }

            return adorner;
        }

        /// <summary>
        /// Returns the component ZLevel
        /// </summary>
        /// <param name="component">component</param>
        /// <returns>ZLevel</returns>
        private static int GetComponentLevel(IAnnotationComponent component)
        {
            int level = 0;
            Type type = component.GetType();
            if (_ZLevel.ContainsKey(type))
                level = (int)_ZLevel[type];

            return level;
        }

        /// <summary>
        /// Converts the component z-order to the Adorner z-order. The adorner z-order
        /// is shifted by the minimal value for this level. Also there is a restriction 
        /// about the maximal possible value for this level too
        /// </summary>
        /// <param name="zOrder">component z-order</param>
        /// <param name="level">component z-level</param>
        /// <returns></returns>
        private static int ComponentToAdorner(int zOrder, int level)
        {
            int res = zOrder;
            ZRange range = (ZRange)_ZRanges[level];
            if (range != null)
            {
                //adjust the Z-order (shift it with the minimal value for this range)
                //that way the component does need to know the range for its type that is 
                // set by the application. It always sets the z-order as it starts from 0
                res += range.Min;
                if (res < range.Min)
                    res = range.Min;
                if (res > range.Max)
                    res = range.Max;
            }
            return res;
        }

        #endregion Private Methods

        #region Private Fields

        /// <summary>
        /// The annotation adorner which wraps the annotation component this presentation context is optimized for.
        /// Can be null.
        /// </summary>
        private AnnotationAdorner _annotationAdorner = null;

        /// <summary>
        /// The adornerLayer which contains the annotation component.  Basically what the presentation hides.
        /// </summary>
        private AdornerLayer _adornerLayer;

        /// <summary>
        /// The hashtable holds the priority level for each Component type as defined by the application
        /// </summary>
        private static Hashtable _ZLevel = new Hashtable();

        /// <summary>
        /// The ZRanges for the ZLevels. 
        /// </summary>
        private static Hashtable _ZRanges = new Hashtable();



        #endregion Private Fields

        #region Private classes

        /// <summary>
        /// This is to control the relationships with TextSelection which lives in the same
        /// AdornerLayer. Will be removed when more flexible Z-ordering mechanism is available
        /// </summary>
        private class ZRange
        {
            public ZRange(int min, int max)
            {
                //exchange values if needed
                if (min > max)
                {
                    int temp = min;
                    min = max;
                    max = temp;
                }
                _min = min;
                _max = max;
            }

            public int Min
            {
                get
                {
                    return _min;
                }
            }
            public int Max
            {
                get
                {
                    return _max;
                }
            }

            private int _min, _max;
        }

        #endregion Internal classes
    }
}

