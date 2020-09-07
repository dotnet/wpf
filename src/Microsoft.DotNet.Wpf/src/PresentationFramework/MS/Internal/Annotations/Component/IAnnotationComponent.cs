// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      IAnnotationComponent
//

using System;
using System.Collections;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Media;

namespace MS.Internal.Annotations.Component
{
    /// <summary>
    /// Interface that all annotation components that are to be instantiated by the framework must implement.
    /// It provides the minimal control the Annotation framework needs in order to interact with annotation components.
    /// </summary>
    internal interface IAnnotationComponent
    {
        #region Public Properties

        /// <summary>
        /// Return a copy of the list of IAttachedAnnotations held by this component
        /// </summary>
        IList AttachedAnnotations { get; }

        /// <summary>
        /// Sets and gets the context this annotation component is hosted in. 
        /// </summary>
        /// <value>Context this annotation component is hosted in</value>
        PresentationContext PresentationContext { get; set; }

        /// <summary>
        /// Gets and sets the Z-order of this component among all components
        /// with the same z-level
        /// </summary>
        int ZOrder { get; set; }

        /// <summary>
        /// If the AnnotatedElement's content changes the annotation might need to update its 
        /// visual characteristics. This flag is set to false when the AnnotatedElement content changes
        /// and back to false when the component is updated.
        /// </summary>
        bool IsDirty { get; set; }


        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// The transformation for an annotation component can be different from the transformation applied
        /// to the UIElement they annotate.  For example comments might be places in the margin, vertically aligned
        /// with the anchor point in the UI element.  Scaling of a pin on a map might not be desired when scaling the
        /// annotated map. This method takes the transform applied to the annotated element as an argument and gives the annotation
        /// component developer a chance to return the desired transform for the annotation component itself.
        /// Typically, the implementor of this method will add the anchor point within the annotated element to the transform received.
        /// This method will be called whenever layout changes.
        /// </summary>
        /// <param name="transform">Transform to the AnnotatedElement.</param>
        /// <returns>Transform to the annotation component</returns>
        GeneralTransform GetDesiredTransform(GeneralTransform transform);

        /// <summary>
        /// Returns the one element the annotation component is attached to.
        /// </summary>
        /// <value></value>
        UIElement AnnotatedElement { get; }

        /// <summary>
        /// Add an attached annotation to the component
        /// </summary>
        /// <param name="attachedAnnotation">The attached annotation to be added to the component</param>
        void AddAttachedAnnotation(IAttachedAnnotation attachedAnnotation);

        /// <summary>
        /// Remove an attached annotation from the component
        /// </summary>
        /// <param name="attachedAnnotation">The attached annotation to be removed from the component</param>
        void RemoveAttachedAnnotation(IAttachedAnnotation attachedAnnotation);

        /// <summary>
        /// Modify an attached annotation that is held by the component
        /// </summary>
        /// <param name="attachedAnnotation">The attached annotation after modification</param>
        /// <param name="previousAttachedAnchor">The attached anchor previously associated with the attached annotation.</param>
        /// <param name="previousAttachmentLevel">The previous attachment level of the attached annotation.</param>
        void ModifyAttachedAnnotation(IAttachedAnnotation attachedAnnotation, object previousAttachedAnchor, AttachmentLevel previousAttachmentLevel);

        #endregion Public Methods
    }
}
