// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//     The AnnotationStore.StoreContentChanged event is generated when any
//     changes are made to an annotation in an AnnotationStore.  
//
//     File contains the StoreContentChangedEventArgs class, the 
//     AnnotationStoreEnum and the StoreContentChangedEventHandler delegate.
//     Spec: CAF%20Storage%20Spec.doc
//


using System;
using System.Collections;
using System.Xml;

namespace System.Windows.Annotations.Storage
{
    /// <summary>
    ///     Event handler delegate for AnnotationUpdated event.  Listeners for
    ///     this event must supply a delegate with this signature.
    /// </summary>
    /// <param name="sender">AnnotationStore in which the change took place</param>
    /// <param name="e">the event data</param>
    public delegate void StoreContentChangedEventHandler(object sender, StoreContentChangedEventArgs e);

    /// <summary>
    ///     Possible actions performed on an IAnnotation in an AnnotationStore.
    /// </summary>
    public enum StoreContentAction
    {
        /// <summary>
        ///     Annotation was added to the store
        /// </summary>
        Added,
        /// <summary>
        ///     Annotation was deleted from the store
        /// </summary>
        Deleted
    }

    /// <summary>
    ///     The AnnotationUpdated event is generated when any changes are made
    ///     to an annotation in an AnnotationStore.  An instance of this class 
    ///     specifies the action that was taken and the IAnnotation that was 
    ///     acted upon.
    /// </summary>
    public class StoreContentChangedEventArgs : System.EventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        
        /// <summary>
        ///     Creates an instance of AnnotationUpdatedEventArgs with the
        ///     specified action and annotation.
        /// </summary>
        /// <param name="action">the action that was performed on an annotation</param>
        /// <param name="annotation">the annotation that was updated</param>
        public StoreContentChangedEventArgs(StoreContentAction action, Annotation annotation)
        {
            if (annotation == null)
                throw new ArgumentNullException("annotation");

            _action = action;
            _annotation = annotation;
        }

        #endregion Constructors
        
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties
        
        /// <summary>
        ///     Returns the IAnnotation that was updated.
        /// </summary>
        public Annotation Annotation
        {
            get
            {
                return _annotation;
            }
        }

        /// <summary>
        ///     Returns the action that was performed on the annotation.
        /// </summary>
        public StoreContentAction Action
        {
            get
            {
                return _action;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private StoreContentAction  _action;        // action taken on the annotation
        private Annotation          _annotation;    // annotation that was updated

        #endregion Private Fields
    }
}
