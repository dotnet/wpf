// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//     The AnnotationStore abstract class provides the basic API for
//     storing and retrieving annotations.  
//     Spec: CAF%20Storage%20Spec.doc
//


using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Windows.Annotations;
using System.Windows.Markup;

namespace System.Windows.Annotations.Storage
{
    /// <summary>
    ///     The AnnotationStore abstract class provides the basic API for
    ///     storing and retrieving annotations.  
    /// </summary>  
    public abstract class AnnotationStore : IDisposable
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        
        #region Constructors

        /// <summary>
        ///     Creates an instance of an annotation store.
        /// </summary>
        protected AnnotationStore()
        {
        }

        /// <summary>
        ///     Guarantees that Dispose() will eventually be called on this
        ///     instance.
        /// </summary>
        ~AnnotationStore()
        {
            Dispose(false);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        
        #region Public Methods     

        /// <summary>
        ///     Add a new annotation to this store.  The new annotation's Id
        ///     is set to a new value.
        /// </summary>
        /// <param name="newAnnotation">the annotation to be added to the store</param>
        /// <exception cref="ArgumentNullException">newAnnotation is null</exception>
        /// <exception cref="ArgumentException">newAnnotation already exists in this store, as determined by its Id</exception>
        /// <exception cref="ObjectDisposedException">if object has been disposed</exception>
        public abstract void AddAnnotation(Annotation newAnnotation);

        /// <summary>
        ///     Delete the specified annotation.
        /// </summary>
        /// <param name="annotationId">the Id of the annotation to be deleted</param>
        /// <returns>the annotation that was deleted</returns>
        /// <exception cref="ObjectDisposedException">if object has been disposed</exception>
        public abstract Annotation DeleteAnnotation(Guid annotationId);

        /// <summary>
        ///     Queries the AnnotationStore for annotations that have an anchor
        ///     that contains a locator that begins with the locator parts
        ///     in anchorLocator. 
        /// </summary>
        /// <param name="anchorLocator">the locator we are looking for</param>
        /// <returns>
        ///     A list of annotations that have locators in their anchors
        ///    starting with the same locator parts list as of the input locator
        ///    Can return an empty list, but never null.
        /// </returns>
        /// <exception cref="ObjectDisposedException">if object has been disposed</exception>
        public abstract IList<Annotation> GetAnnotations(ContentLocator anchorLocator);

        /// <summary>
        /// Returns a list of all annotations in the store
        /// </summary>
        /// <exception cref="ObjectDisposedException">if object has been disposed</exception>
        /// <returns>annotations list. Can return an empty list, but never null.</returns>
        public abstract IList <Annotation> GetAnnotations();

        /// <summary>
        /// Finds annotation by Id
        /// </summary>
        /// <param name="annotationId"></param>
        /// <exception cref="ObjectDisposedException">if object has been disposed</exception>
        /// <returns>The annotation. Null if the annotation does not exists</returns>
        public abstract Annotation GetAnnotation(Guid annotationId);

        /// <summary>
        ///     Causes any buffered data to be written to the underlying storage 
        ///     mechanism.  Gets called after each operation if 
        ///     AutoFlush is set to true.
        /// </summary>
        /// <remarks>
        ///     Applications that have an explicit save model may choose to call
        ///     this method directly when appropriate.  Applications that have an
        ///     implied save model, see AutoFlush.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">if object has been disposed</exception>
        /// <seealso cref="AutoFlush"/>
        public abstract void Flush();                

        /// <summary>
        ///     Disposes of any managed and unmanaged resources used by this
        ///     store.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion Public Methods

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
        ///     When set to true an implementation should call Flush()
        ///     as a side-effect after each operation.
        /// </summary>
        /// <value>
        ///     true if the implementation is set to call Flush() after 
        ///     each operation; false otherwise
        /// </value>
        public abstract bool AutoFlush { get; set; }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        #region Public Events

        /// <summary>
        /// Event fired when annotations are added or deleted from the
        /// AnnotationStore.
        /// </summary>
        public event StoreContentChangedEventHandler StoreContentChanged;

        /// <summary>
        /// Event fired when an author on any Annotation in this 
        /// AnnotationStore changes.  This event is useful if you are
        /// interested in listening to all annotations from this store
        /// without registering on each individually.
        /// </summary>
        public event AnnotationAuthorChangedEventHandler AuthorChanged;

        /// <summary>
        /// Event fired when an anchor on any Annotation in this 
        /// AnnotationStore changes.  This event is useful if you are
        /// interested in listening to all annotations from this store
        /// without registering on each individually.
        /// </summary>
        public event AnnotationResourceChangedEventHandler AnchorChanged;

        /// <summary>
        /// Event fired when a cargo on any Annotation in this 
        /// AnnotationStore changes.  This event is useful if you are
        /// interested in listening to all annotations from this store
        /// without registering on each individually.
        /// </summary>
        public event AnnotationResourceChangedEventHandler CargoChanged;

        #endregion Public Events

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        ///     Disposes of the resources (other than memory) used by the store.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged
        /// resources; false to release only unmanaged resources</param>
        protected virtual void Dispose(bool disposing)
        {
            lock (SyncRoot)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        // Do nothing here - subclasses will override this 
                        // method and dispose of things as appropriate
                    }

                    _disposed = true;
                }
            }
        }

        /// <summary>
        ///     Should be called when any annotation's author changes.
        ///     This will fire the AuthorChanged event and cause a flush
        ///     if AutoFlush is true.
        /// </summary>
        /// <param name="args">the args for the event</param>
        protected virtual void OnAuthorChanged(AnnotationAuthorChangedEventArgs args)
        {
            AnnotationAuthorChangedEventHandler authorChanged = null;

            // Ignore null authors added to an annotation
            if (args.Author == null)
                return;

            lock (SyncRoot)
            {
                authorChanged = AuthorChanged;
            }

            if (AutoFlush)
            {
                Flush();
            }

            if (authorChanged != null)
            {
                authorChanged(this, args);
            }
        }

        /// <summary>
        ///     Should be called when any annotation's anchor changes.
        ///     This will fire the AnchorChanged event and cause a flush
        ///     if AutoFlush is true.
        /// </summary>
        /// <param name="args">the args for the event</param>
        protected virtual void OnAnchorChanged(AnnotationResourceChangedEventArgs args)
        {
            AnnotationResourceChangedEventHandler anchorChanged = null;

            // Ignore null resources added to an annotation
            if (args.Resource == null)
                return;

            lock (SyncRoot)
            {
                anchorChanged = AnchorChanged;
            }

            if (AutoFlush)
            {
                Flush();
            }

            if (anchorChanged != null)
            {
                anchorChanged(this, args);
            }
        }

        /// <summary>
        ///     Should be called when any annotation's cargo changes.
        ///     This will fire the CargoChanged event and cause a flush
        ///     if AutoFlush is true.
        /// </summary>
        /// <param name="args">the args for the event</param>
        protected virtual void OnCargoChanged(AnnotationResourceChangedEventArgs args)
        {
            AnnotationResourceChangedEventHandler cargoChanged = null;

            // Ignore null resources added to an annotation
            if (args.Resource == null)
                return;
            
            lock (SyncRoot)
            {
                cargoChanged = CargoChanged;
            }

            if (AutoFlush)
            {
                Flush();
            }

            if (cargoChanged != null)
            {
                cargoChanged(this, args);
            }
        }

        /// <summary>
        ///     Should be called after every operation in order to trigger 
        ///     events and to perform an automatic flush if AutoFlush is true.       
        /// </summary>
        /// <param name="e">arguments for the event to fire</param>
        protected virtual void OnStoreContentChanged(StoreContentChangedEventArgs e)
        {
            StoreContentChangedEventHandler storeContentChanged = null;

            lock (SyncRoot)
            {
                storeContentChanged = StoreContentChanged;
            }

            if (AutoFlush)
            {
                Flush();
            }

            if (storeContentChanged != null)
            {
                storeContentChanged(this, e);
            }
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Protected Properties
        //
        //------------------------------------------------------

        #region Protected Properties
 
        /// <summary>
        /// This object should be used for synchronization
        /// by all AnnotationStore implementations
        /// </summary>
        /// <value>the lock object</value>
        protected object SyncRoot
        {
            get
            {
                return lockObject;
            }
        }

        /// <summary>
        /// Is this store disposed
        /// </summary>
        /// <value></value>
        protected bool IsDisposed
        {
            get
            {
                return _disposed;
            }
        }
        
        #endregion Protected Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Tracks the state of this store - closed or not
        private bool _disposed = false;

        /// Private object used for synchronization
        private Object lockObject = new Object();

        #endregion Private Fields
    }
}
