// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      AnnotationAdorner wraps an IAnnotationComponent.  Its the bridge
//      between the component and the adorner layer.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace MS.Internal.Annotations.Component
{
    /// <summary>
    /// An adorner which wraps one IAnnotationComponent.  The wrapped component must be at least a UIElement.
    /// Adorners are UIElements.
    /// Note:-This class is sealed because it calls OnVisualChildrenChanged virtual in the 
    ///           constructor and it does not override it, but derived classes could.    
    /// </summary>
    internal sealed class AnnotationAdorner : Adorner
    {
        #region Constructors

        /// <summary>
        /// Return an initialized annotation adorner
        /// </summary>
        /// <param name="component">The annotation component to wrap in the annotation adorner</param>
        /// <param name="annotatedElement">element being annotated</param>
        public AnnotationAdorner(IAnnotationComponent component, UIElement annotatedElement) : base(annotatedElement)
        {
            //The summary on top of the file says:-
            //The wrapped component must be at least a UIElement
            if (component is UIElement)
            {
                _annotationComponent = component;
                // wrapped annotation component is added as visual child
                this.AddVisualChild((UIElement)_annotationComponent);
            }
            else
            {
                throw new ArgumentException(SR.Get(SRID.AnnotationAdorner_NotUIElement), "component");
            }
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Forwarded to the annotation component to get desired transform relative to the annotated element
        /// </summary>
        /// <param name="transform">Transform to adorned element</param>
        /// <returns>Transform to annotation component </returns>
        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            //if the component is not visual we do not need this
            if (!(_annotationComponent is UIElement))
                return null;

            // Give the superclass a chance to modify the transform
            transform = base.GetDesiredTransform(transform);

            GeneralTransform compTransform = _annotationComponent.GetDesiredTransform(transform);

            // if the annotated element is null this component must be unloaded.
            //Temporary return null until the PageViewer Load/Unload bug is fixed
            //Convert it to an exception after that.
            if (_annotationComponent.AnnotatedElement == null)
                return null;

            if (compTransform == null)
            {
                // We need to store the element we are registering on.  It may not
                // be available from the annotation component later.
                _annotatedElement = _annotationComponent.AnnotatedElement;

                // Wait for valid text view
                _annotatedElement.LayoutUpdated += OnLayoutUpdated;
                return transform;
            }

            return compTransform;
        }

        #endregion Public Methods

        #region Protected Methods

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
            if (index != 0 || _annotationComponent == null)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }

            return (UIElement)_annotationComponent;
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
        protected override int VisualChildrenCount
        {
            get { return _annotationComponent != null ? 1 : 0; }
        }

        /// <summary>
        /// Measurement override. Delegated to children.
        /// </summary>
        /// <param name="availableSize">Available size for the component</param>
        /// <returns>Return the size enclosing all children</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size childConstraint = new Size(Double.PositiveInfinity, Double.PositiveInfinity);

            Invariant.Assert(_annotationComponent != null, "AnnotationAdorner should only have one child - the annotation component.");

            ((UIElement)_annotationComponent).Measure(childConstraint);

            return new Size(0, 0);
        }

        /// <summary>
        /// Override for <seealso cref="FrameworkElement.ArrangeOverride" />  
        /// </summary>
        /// <param name="finalSize">The location reserved for this element by the parent</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Invariant.Assert(_annotationComponent != null, "AnnotationAdorner should only have one child - the annotation component.");

            ((UIElement)_annotationComponent).Arrange(new Rect(((UIElement)_annotationComponent).DesiredSize));

            return finalSize;
        }

        #endregion ProtectedMethods

        #region Internal Methods

        /// <summary>
        /// Remove all visual children of this AnnotationAdorner.
        /// Called by AdornerPresentationContext when an annotation adorner is removed from the adorner layer hosting it. 
        /// </summary>
        internal void RemoveChildren()
        {
            this.RemoveVisualChild((UIElement)_annotationComponent);
            _annotationComponent = null;
        }

        /// <summary>
        /// Call this if the visual of the annotation changes.
        /// This ill invalidate the AnnotationAdorner and the AdornerLayer which
        /// will invoke remeasuring of the annotation visual.
        /// </summary>
        internal void InvalidateTransform()
        {
            AdornerLayer adornerLayer = (AdornerLayer)VisualTreeHelper.GetParent(this);

            InvalidateMeasure();
            adornerLayer.InvalidateVisual();
        }

        #endregion Internal Methods

        #region Internal Properties

        /// <summary>
        ///  Return the annotation component that is being wrapped.  AdornerPresentationContext needs this.
        /// </summary>
        /// <value>Wrapped annotation component</value>
        internal IAnnotationComponent AnnotationComponent
        {
            get { return _annotationComponent; }
        }

        #endregion Internal Properties

        #region Private Methods

        /// <summary>
        /// LayoutUpdate event handler
        /// </summary>
        /// <param name="sender">event sender (not used)</param>
        /// <param name="args">event arguments (not used)</param>
        private void OnLayoutUpdated(object sender, EventArgs args)
        {
            // Unregister for the event
            _annotatedElement.LayoutUpdated -= OnLayoutUpdated;
            _annotatedElement = null;

            // If there are still annotations to display, update the UI
            if (_annotationComponent.AttachedAnnotations.Count > 0)
            {
                _annotationComponent.PresentationContext.Host.InvalidateMeasure();
                this.InvalidateMeasure();
            }
        }

        #endregion Private Methods

        #region Private Fields

        /// <summary>
        /// The wrapped annotation component
        /// </summary>
        private IAnnotationComponent _annotationComponent;

        /// <summary>
        /// Used to unregister for the LayoutUpdated event - necessary because in the
        /// the component may have its annotated element cleared before we can unregister.
        /// </summary>
        private UIElement _annotatedElement;

        #endregion Private Fields
    }
}




