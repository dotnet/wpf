// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//     AnnotationAuthorChangedEvents are fired by an Annotation when an author
//     it contains has been added, removed or modified in some way.  The
//     event includes the annotation, the author, and what action was
//     taken on the author
//
//     Spec: Simplifying Store Cache Model.doc
//

using System;
using System.ComponentModel;

namespace System.Windows.Annotations
{
    /// <summary>
    ///     Delegate for handlers of the AuthorChanged event on Annotation.    
    /// </summary>
    /// <param name="sender">the annotation firing the event</param>
    /// <param name="e">args describing the Author and the action taken</param>
    public delegate void AnnotationAuthorChangedEventHandler(Object sender, AnnotationAuthorChangedEventArgs e);

    /// <summary>
    ///     Event args for changes to an Annotation's Authors.  This class includes
    ///     the annotation that fired the event, the Author that was changed, and
    ///     what action was taken on the Author - added, removed or modified.
    /// </summary>
    public sealed class AnnotationAuthorChangedEventArgs : EventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates an instance of AuthorChangedEventArgs.
        /// </summary>
        /// <param name="annotation">the Annotation firing the event</param>
        /// <param name="action">the action taken on the Author</param>
        /// <param name="author">the Author that was changed</param>
        /// <exception cref="ArgumentNullException">annotation is null</exception>
        /// <exception cref="InvalidEnumArgumentException">action is not a valid value from AnnotationAction</exception>
        public AnnotationAuthorChangedEventArgs(Annotation annotation, AnnotationAction action, Object author)
        {
            // The author parameter can be null here - it is possible to add a null to
            // the list of authors and we must fire an event signalling a change in the collection.

            if (annotation == null)
            {
                throw new ArgumentNullException("annotation");
            }
            if (action < AnnotationAction.Added || action > AnnotationAction.Modified)
            {
                throw new InvalidEnumArgumentException("action", (int)action, typeof(AnnotationAction));
            }

            _annotation = annotation;
            _author = author;
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
        ///     The Author that was changed.
        /// </summary>
        public Object Author
        {
            get
            {
                return _author;
            }
        }

        /// <summary>
        ///     The action that was taken on the Author.
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
        /// The Author that was changed.
        /// </summary>
        private Object _author;

        /// <summary>
        /// The action taken on the Author
        /// </summary>
        private AnnotationAction _action;

        #endregion Private Fields
    }
}