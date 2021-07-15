// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//     AnnotationResourceChangedEvents are fired by an Annotation when a 
//     AnnotationResource it contains has been added, removed or modified in 
//     some way.  The event includes the annotation, the AnnotationResource, 
//     and what action was taken on the resoure.
//
//     Spec: Simplifying Store Cache Model.doc
//

using System;
using System.ComponentModel;

namespace System.Windows.Annotations
{
    /// <summary>
    ///     Delegate for handlers of the AnnotationResourceChanged event on Annotation.    
    /// </summary>
    /// <param name="sender">the annotation firing the event</param>
    /// <param name="e">args describing the Resource and the action taken</param>
    public delegate void AnnotationResourceChangedEventHandler(Object sender, AnnotationResourceChangedEventArgs e);

    /// <summary>
    ///     Event args for changes to an Annotation's Resources.  This class includes
    ///     the annotation that fired the event, the Resource that was changed, and
    ///     what action was taken on the Resource - added, removed or modified.
    /// </summary>
    public sealed class AnnotationResourceChangedEventArgs : EventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates an instance of AnnotationResourceChangedEventArgs.
        /// </summary>
        /// <param name="annotation">the Annotation firing the event</param>
        /// <param name="action">the action taken on the Resource</param>
        /// <param name="resource">the Resource that was changed</param>
        /// <exception cref="ArgumentNullException">annotation or action is null</exception>
        /// <exception cref="InvalidEnumArgumentException">action is not a valid value from AnnotationAction</exception>
        public AnnotationResourceChangedEventArgs(Annotation annotation, AnnotationAction action, AnnotationResource resource)
        {
            // The resource parameter can be null here - it is possible to add a null to
            // the list of resources and we must fire an event signalling a change in the collection.

            if (annotation == null)
            {
                throw new ArgumentNullException("annotation");
            }
            if (action < AnnotationAction.Added || action > AnnotationAction.Modified)
            {
                throw new InvalidEnumArgumentException("action", (int)action, typeof(AnnotationAction));
            }

            _annotation = annotation;
            _resource = resource;
            _action = action;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     The Annotation that fired the event.
        /// </summary>
        public Annotation Annotation
        {
            get
            {
                return _annotation;
            }
        }

        /// <summary>
        ///     The Resource that was changed.
        /// </summary>
        public AnnotationResource Resource
        {
            get
            {
                return _resource;
            }
        }

        /// <summary>
        ///     The action that was taken on the Resource.
        /// </summary>
        public AnnotationAction Action
        {
            get
            {
                return _action;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// The annotation that fired the event.
        /// </summary>
        private Annotation _annotation;

        /// <summary>
        /// The Resource that was changed.
        /// </summary>
        private AnnotationResource _resource;

        /// <summary>
        /// The action taken on the Resource
        /// </summary>
        private AnnotationAction _action;

        #endregion Private Fields
    }
}