// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      AnnotationComponentManager
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Documents;
using System.Windows.Media;

namespace MS.Internal.Annotations.Component
{
    /// <summary>
    /// Instances of this class manage listen for events from the annotation service and create/remove/modify annotation components in response.
    /// Annotation components are created via indirection through an AnnotationComponentChooser which is set in a DP on the app tree.
    /// Annotation components are hosted, wrapped inside AnnotationAdorners, in an AdornerLayer respective to the annotated element.
    /// Every time an annotation service is instantiated an instance of this class is created.
    /// </summary>
    internal class AnnotationComponentManager : DependencyObject
    {
        #region Constructors

        /// <summary>
        /// Return an annotation component manager.  If an annotation service is passed in, manager will
        /// receive notification from the service.  Otherwise it will expect to be called directly.
        /// </summary>
        /// <param name="service">optional, annotation service the annotation component manager will register on</param>
        internal AnnotationComponentManager(AnnotationService service)
            : base()
        {
            // Only register if a service was passed in. If no service was passed in, we will not receive events.
            // This means a client will be calling us directly.
            if (service != null)
            {
                service.AttachedAnnotationChanged += new AttachedAnnotationChangedEventHandler(AttachedAnnotationUpdateEventHandler);
            }
        }

        #endregion Constructors

        #region Internal Methods

        /// <summary>
        /// The given attached annotation was added by the annotation service
        /// Find annotation components for the given attached annotation, maintain 
        /// mapping from attached annotation to annotation components, and add to the
        /// adorner layer if the annotation component is not already in a presentation context.
        /// </summary>
        /// <param name="attachedAnnotation">Attached annotation to add</param>
        /// <param name="reorder">If true - update components ZOrder after adding the component</param>
        internal void AddAttachedAnnotation(IAttachedAnnotation attachedAnnotation, bool reorder)
        {
            Debug.Assert(attachedAnnotation != null, "AttachedAnnotation should not be null");

            IAnnotationComponent component = FindComponent(attachedAnnotation);
            if (component == null) return;
            AddComponent(attachedAnnotation, component, reorder);
        }

        /// <summary>
        /// The given attached annotation was removed by the annotation service.  
        /// Find all annotation components and let them know. 
        /// If an annotation component does not have any other attached annotations, remove it.
        /// Update the attachedAnnotations map.
        /// </summary>
        /// <param name="attachedAnnotation">Attached annotation to remove</param>
        /// <param name="reorder">If true - update components ZOrder after removing the component</param>
        internal void RemoveAttachedAnnotation(IAttachedAnnotation attachedAnnotation, bool reorder)
        {
            Debug.Assert(attachedAnnotation != null, "AttachedAnnotation should not be null");

            if (!_attachedAnnotations.ContainsKey(attachedAnnotation))
            {
                // note, this is not always a bug, since an annotation might have resolved, but no annotation component found
                // the tree traversal bug we have currently results in this failing even if annotation components were found, because
                // in certain cases annotationService.AttachedAnnotations returns wrong list.
                return;
            }

            IList<IAnnotationComponent> currentList = _attachedAnnotations[attachedAnnotation];

            _attachedAnnotations.Remove(attachedAnnotation); // clean up the map
            foreach (IAnnotationComponent component in currentList)
            {
                component.RemoveAttachedAnnotation(attachedAnnotation);  // let the annotation component know
                if (component.AttachedAnnotations.Count == 0)
                { // if it has no more attached annotations, remove it
                    if (component.PresentationContext != null)
                        component.PresentationContext.RemoveFromHost(component, reorder);
                }
            }
        }

        #endregion Internal Methods

        #region Private Methods

        /// <summary>
        /// Handle the notification coming from the service.  Attached annotations can be added, removed, modified.
        /// Delegate to appropriate method
        /// </summary>
        /// <param name="sender">The sender of the event, an annotation service</param>
        /// <param name="e">The event arguments containing information on added/deleted/modified attached annotation.</param>
        private void AttachedAnnotationUpdateEventHandler(object sender, AttachedAnnotationChangedEventArgs e)
        {
            switch (e.Action)
            {
                case AttachedAnnotationAction.Added:
                    this.AddAttachedAnnotation(e.AttachedAnnotation, true);
                    break;

                case AttachedAnnotationAction.Deleted:
                    this.RemoveAttachedAnnotation(e.AttachedAnnotation, true);
                    break;

                case AttachedAnnotationAction.Loaded:
                    this.AddAttachedAnnotation(e.AttachedAnnotation, false);
                    break;

                case AttachedAnnotationAction.Unloaded:
                    this.RemoveAttachedAnnotation(e.AttachedAnnotation, false);
                    break;

                case AttachedAnnotationAction.AnchorModified:
                    this.ModifyAttachedAnnotation(e.AttachedAnnotation, e.PreviousAttachedAnchor, e.PreviousAttachmentLevel);
                    break;
            }
        }

        /// <summary>
        /// given an attachedAnnotation find the chooser attached at its parent
        /// and ask it to choose a component suitable to handle the attachedAnnotation 
        /// </summary>
        /// <param name="attachedAnnotation">the attachedAnnotation we are to find a component for</param>
        /// <returns>an IAnnotationComponent that can handle the attachedAnnotation (or null)</returns>
        private IAnnotationComponent FindComponent(IAttachedAnnotation attachedAnnotation)
        {
            Debug.Assert(attachedAnnotation != null, "AttachedAnnotation should not be null");

            UIElement annotatedElement = attachedAnnotation.Parent as UIElement; // casted from DependencyObject
            Debug.Assert(annotatedElement != null, "the annotatedElement should inherit from UIElement");

            AnnotationComponentChooser chooser = AnnotationService.GetChooser(annotatedElement);

            // should we return a list instead?
            IAnnotationComponent component = chooser.ChooseAnnotationComponent(attachedAnnotation);

            return component;
        }

        /// <summary>
        /// Add an attached annotation to a component and add the component to the adorner layer 
        /// if the annotation component is not already in a presentation context.        
        /// </summary>
        /// <param name="attachedAnnotation">the attachedAnnotation we are to add to the component</param>
        /// <param name="component">the component we are to add to the adorner layer</param>
        /// <param name="reorder">if true - the z-order must be reevaluated</param>
        private void AddComponent(IAttachedAnnotation attachedAnnotation, IAnnotationComponent component, bool reorder)
        {
            UIElement annotatedElement = attachedAnnotation.Parent as UIElement; // casted from DependencyObject
            Debug.Assert(annotatedElement != null, "the annotatedElement should inherit from UIElement");

            // if annotation component is already in presentation context, nothing else to do
            if (component.PresentationContext != null) return;

            // otherwise host in the appropriate adorner layer
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(annotatedElement); // note, GetAdornerLayer requires UIElement
            if (layer == null)
            {
                if (PresentationSource.FromVisual(annotatedElement) == null)
                {
                    // The annotated element is no longer part of the application tree.
                    // This probably means we are out of sync - trying to add an annotation
                    // for an element that has already gone away.  Bug # 1580288 tracks
                    // the need to figure this out.
                    return;
                }

                throw new InvalidOperationException(SR.Get(SRID.NoPresentationContextForGivenElement, annotatedElement));
            }

            // add to the attachedAnnotations
            this.AddToAttachedAnnotations(attachedAnnotation, component);

            // let the annotation component know about the attached annotation
            // call add before adding to adorner layer so the component can be initialized
            component.AddAttachedAnnotation(attachedAnnotation); // this might cause recursion in modify if annotation component adds to annotation

            AdornerPresentationContext.HostComponent(layer, component, annotatedElement, reorder);
        }

        /// <summary>
        /// Service indicates attached annotation has changed.
        /// For now, take all annotation components maped from old attached annotation and map from new.
        /// Then iterate through annotation components to let them know.
        /// Note, this needs to change later.  If modify is radical, existing component might not want it anymore,
        /// and new one might need to be found... 
        /// </summary>
        /// <param name="attachedAnnotation">The modified attached annotation</param>
        /// <param name="previousAttachedAnchor">The previous attached anchor for the attached annotation</param>
        /// <param name="previousAttachmentLevel">The previous attachment level for the attached annotation</param>
        private void ModifyAttachedAnnotation(IAttachedAnnotation attachedAnnotation, object previousAttachedAnchor, AttachmentLevel previousAttachmentLevel)
        {
            Debug.Assert(attachedAnnotation != null, "attachedAnnotation should not be null");
            Debug.Assert(previousAttachedAnchor != null, "previousAttachedAnchor should not be null");

            // if there was no component found for this attached annotation
            // then we treat the modify case as an add 
            if (!_attachedAnnotations.ContainsKey(attachedAnnotation))
            {
                // this is not necessarily a bug, it can be that the old attached annotation does not have an
                // associated annotation component.
                this.AddAttachedAnnotation(attachedAnnotation, true);
                return;
            }

            // we have a previous component for this attached annotation
            // we find the chooser for the new attached annotation
            // and ask it to choose a component
            // 1- if it returned null then we just remove the attached annotation
            // 2- if it returned a different component, then we treat it as a remove/add
            // 3- if it returned the same component then we call ModifyAttachedAnnotation on the component
            IAnnotationComponent newComponent = FindComponent(attachedAnnotation);
            if (newComponent == null)
            {
                RemoveAttachedAnnotation(attachedAnnotation, true);
            }
            else
            {
                IList<IAnnotationComponent> currentList = _attachedAnnotations[attachedAnnotation]; //save the current list

                // if we found the new component in any of the list of components we already have for this
                // attached annotation
                if (currentList.Contains(newComponent))
                {
                    // ask the components to handle the anchor modification event
                    foreach (IAnnotationComponent component in currentList)
                    {
                        component.ModifyAttachedAnnotation(attachedAnnotation, previousAttachedAnchor, previousAttachmentLevel);  // let the annotation component know
                        if (component.AttachedAnnotations.Count == 0)
                        {
                            // if component decides it can not handle it, remove it
                            component.PresentationContext.RemoveFromHost(component, true);
                        }
                    }
                }
                else
                {
                    // remove all components
                    RemoveAttachedAnnotation(attachedAnnotation, true);

                    // add the new component
                    AddComponent(attachedAnnotation, newComponent, true);
                }
            }
        }

        /// <summary>
        /// Add to the map from attached annotations -> annotation components
        /// </summary>
        /// <param name="attachedAnnotation">The attached annotation to use as key</param>
        /// <param name="component">The component to add to list</param>
        private void AddToAttachedAnnotations(IAttachedAnnotation attachedAnnotation, IAnnotationComponent component)
        {
            Debug.Assert(attachedAnnotation != null, "attachedAnnotation should not be null");
            Debug.Assert(component != null, "component should not be null");

            IList<IAnnotationComponent> currentList;

            if (!_attachedAnnotations.TryGetValue(attachedAnnotation, out currentList))
            {
                currentList = new List<IAnnotationComponent>();
                _attachedAnnotations[attachedAnnotation] = currentList;
            }

            currentList.Add(component);
        }

        #endregion Private Methods

        #region Private Fields

        /// <summary>
        /// Map from attached annotation to list of annotation components.  
        /// This map contains all attached anntotations the component manager currently knows.
        /// </summary>
        private Dictionary<IAttachedAnnotation, IList<IAnnotationComponent>> _attachedAnnotations = new Dictionary<IAttachedAnnotation, IList<IAnnotationComponent>>();

        #endregion Private Fields
    }
}
