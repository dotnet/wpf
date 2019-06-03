// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      PresentationContext
//

using System;
using System.Windows;
using System.Windows.Media;

namespace MS.Internal.Annotations.Component
{
    /// <summary>
    /// This abstract class represents the host an annotation component is embedded in.  It encapsulates methods to change
    /// the z-order for an annotation component, it enables an annotation component to additional components and remove them,
    /// and it enables the annotation compone to gain spatial information about the host and retrieve parent contexts.
    /// </summary>
    internal abstract class PresentationContext
    {
        #region Public Properties

        /// <summary>
        /// return a UI element for the panel the component is in.  
        /// This can be used to retrieve measurements and clip of the panel
        /// </summary>
        /// <value></value>
        public abstract UIElement Host { get; }

        /// <summary>
        /// Get enclosing presentation context.  If there is none, return null.
        /// </summary>
        /// <value></value>
        public abstract PresentationContext EnclosingContext { get; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Add annotation component to host.
        /// Needed by annotation components which need to add addl. components
        /// </summary>
        /// <param name="component">the component to add to the presentation context.</param>
        public abstract void AddToHost(IAnnotationComponent component);

        /// <summary>
        /// Removes annotation component from host.  
        /// Will raise exception if annotation component not contained in host
        /// </summary>
        /// <param name="component">the component to remove from the presentation context.</param>
        /// <param name="reorder">if true - recalculate z-order</param>
        public abstract void RemoveFromHost(IAnnotationComponent component, bool reorder);

        /// <summary>
        /// invalidate the transform for this adorner. called when adorner inside changed aspects of the transform
        /// this might go away if InvalidateMeasure works. Bug # TBD 
        /// </summary>
        /// <param name="component">the component to invalidate the transform on</param>
        public abstract void InvalidateTransform(IAnnotationComponent component);

        /// <summary>
        /// to change z-order of annotation component to be the top component
        /// </summary>
        /// <param name="component">the component to bring to front</param>
        public abstract void BringToFront(IAnnotationComponent component);

        /// <summary>
        /// to change z-order of annotation component to be the bottom component
        /// </summary>
        /// <param name="component">the component to send to back</param>
        public abstract void SendToBack(IAnnotationComponent component);

        #endregion Public Methods
    }
}
