// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//     Map of annotation instances handed out by a particular store.
//     Used to guarantee only one instance of an annotation is ever
//     produced.  Also register for notifications on the instances
//     and passes change notifications on to the store.
//
//     Spec: CAF Storage Spec.doc
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Annotations;
using MS.Utility;


namespace MS.Internal.Annotations.Storage
{
    /// <summary>
    /// A map the store uses to maintain having one instance 
    /// of the object model
    /// </summary>
    internal class StoreAnnotationsMap
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor for StoreAnnotationsMap 
        /// It takes instances of the AnnotationAuthorChangedEventHandler, AnnotationResourceChangedEventHandler, AnnotationResourceChangedEventHandler delegates
        /// </summary>
        /// <param name="authorChanged">delegate used to register for AuthorChanged events on all annotations</param>
        /// <param name="anchorChanged">delegate used to register for AnchorChanged events on all annotations</param>
        /// <param name="cargoChanged">delegate used to register for CargoChanged events on all annotations</param>
        internal StoreAnnotationsMap(AnnotationAuthorChangedEventHandler authorChanged, AnnotationResourceChangedEventHandler anchorChanged, AnnotationResourceChangedEventHandler cargoChanged)
        {
            Debug.Assert(authorChanged != null && anchorChanged != null && cargoChanged != null,
                         "Author and Anchor and Cargo must not be null");

            _authorChanged = authorChanged;
            _anchorChanged = anchorChanged;
            _cargoChanged = cargoChanged;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Add an annotation to the map
        /// throws an exception if the annotation already exists
        /// </summary>
        /// <param name="annotation">instance of annotation to add to the map</param>
        /// <param name="dirty">the initial dirty flag of the annotation in the map</param>
        public void AddAnnotation(Annotation annotation, bool dirty)
        {
            Debug.Assert(FindAnnotation(annotation.Id) == null, "annotation  not found");
            annotation.AuthorChanged += OnAuthorChanged;
            annotation.AnchorChanged += OnAnchorChanged;
            annotation.CargoChanged += OnCargoChanged;
            _currentAnnotations.Add(annotation.Id, new CachedAnnotation(annotation, dirty));
        }

        /// <summary>
        /// remove an annotation from the map if it exists
        /// otherwise return
        /// </summary>
        /// <param name="id">id of the annotation to remove</param>
        public void RemoveAnnotation(Guid id)
        {
            CachedAnnotation existingCachedAnnot = null;
            if (_currentAnnotations.TryGetValue(id, out existingCachedAnnot))
            {
                existingCachedAnnot.Annotation.AuthorChanged -= OnAuthorChanged;
                existingCachedAnnot.Annotation.AnchorChanged -= OnAnchorChanged;
                existingCachedAnnot.Annotation.CargoChanged -= OnCargoChanged;
                _currentAnnotations.Remove(id);
            }
        }

        /// <summary>
        ///     Queries the store map for annotations that have an anchor
        ///     that contains a locator that begins with the locator parts
        ///     in anchorLocator.
        /// </summary>
        /// <param name="anchorLocator">the locator we are looking for</param>
        /// <returns>
        ///    A list of annotations that have locators in their anchors
        ///    starting with the same locator parts list as of the input locator
        ///    If no such annotations an empty list will be returned. The method
        ///    never returns null.
        /// </returns>
        public Dictionary<Guid, Annotation> FindAnnotations(ContentLocator anchorLocator)
        {
            if (anchorLocator == null)
                throw new ArgumentNullException("locator");

            Dictionary<Guid, Annotation> annotations = new Dictionary<Guid, Annotation>();

            // In the future, this probably has to be done in a way more effecient than linear search 
            // the plan of record is to start with this and improve later
            // the map is already solving a perf bottleneck for tablet
            Dictionary<Guid, CachedAnnotation>.ValueCollection.Enumerator annotationsEnumerator = _currentAnnotations.Values.GetEnumerator();

            while (annotationsEnumerator.MoveNext())
            {
                Annotation annotation = annotationsEnumerator.Current.Annotation;
                bool matchesQuery = false;

                foreach (AnnotationResource resource in annotation.Anchors)
                {
                    foreach (ContentLocatorBase locator in resource.ContentLocators)
                    {
                        ContentLocator ContentLocator = locator as ContentLocator;
                        if (ContentLocator != null)
                        {
                            if (ContentLocator.StartsWith(anchorLocator))
                            {
                                matchesQuery = true;
                            }
                        }
                        else
                        {
                            ContentLocatorGroup ContentLocatorGroup = locator as ContentLocatorGroup;
                            if (ContentLocatorGroup != null)
                            {
                                foreach (ContentLocator list in ContentLocatorGroup.Locators)
                                {
                                    if (list.StartsWith(anchorLocator))
                                    {
                                        matchesQuery = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (matchesQuery)
                        {
                            annotations.Add(annotation.Id, annotation);
                            break;
                        }
                    }

                    if (matchesQuery)
                        break;
                }
            }

            return annotations;
        }

        /// <summary>
        /// Returns a dictionary of all annotations in the map
        /// </summary>
        /// <returns>annotations list. Can return an empty list, but never null.</returns>
        public Dictionary<Guid, Annotation> FindAnnotations()
        {
            Dictionary<Guid, Annotation> annotations = new Dictionary<Guid, Annotation>();
            foreach (KeyValuePair<Guid, CachedAnnotation> annotKV in _currentAnnotations)
            {
                annotations.Add(annotKV.Key, annotKV.Value.Annotation);
            }
            return annotations;
        }

        /// <summary>
        /// Find an annottaion that has a specific id in the map
        /// returns null if not found
        /// </summary>
        /// <param name="id">id of the annotation to find</param>
        public Annotation FindAnnotation(Guid id)
        {
            CachedAnnotation cachedAnnotation = null;
            if (_currentAnnotations.TryGetValue(id, out cachedAnnotation))
            {
                return cachedAnnotation.Annotation;
            }

            return null;
        }

        /// <summary>
        /// Returns a list of all dirty annotations in the map
        /// </summary>
        /// <returns>annotations list. Can return an empty list, but never null.</returns>
        public List<Annotation> FindDirtyAnnotations()
        {
            List<Annotation> annotations = new List<Annotation>();
            foreach (KeyValuePair<Guid, CachedAnnotation> annotKV in _currentAnnotations)
            {
                if (annotKV.Value.Dirty)
                {
                    annotations.Add(annotKV.Value.Annotation);
                }
            }

            return annotations;
        }

        /// <summary>
        /// Sets all the dirty annotations in the map to clean
        /// </summary>
        public void ValidateDirtyAnnotations()
        {
            foreach (KeyValuePair<Guid, CachedAnnotation> annotKV in _currentAnnotations)
            {
                if (annotKV.Value.Dirty)
                {
                    annotKV.Value.Dirty = false;
                }
            }
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// the event handler for AnchorChanged Annotation event
        /// internally it calls the store AnnotationModified delegate
        /// </summary>
        /// <param name="sender">annotation that changed</param>
        /// <param name="args">args describing the change</param>
        private void OnAnchorChanged(object sender, AnnotationResourceChangedEventArgs args)
        {
            _currentAnnotations[args.Annotation.Id].Dirty = true;
            _anchorChanged(sender, args);
        }

        /// <summary>
        /// the event handler for CargoChanged Annotation event
        /// internally it calls the store AnnotationModified delegate
        /// </summary>
        /// <param name="sender">annotation that changed</param>
        /// <param name="args">args describing the change</param>
        private void OnCargoChanged(object sender, AnnotationResourceChangedEventArgs args)
        {
            _currentAnnotations[args.Annotation.Id].Dirty = true;
            _cargoChanged(sender, args);
        }

        /// <summary>
        /// the event handler for AuthorChanged Annotation event
        /// internally it calls the store AnnotationModified delegate
        /// </summary>
        /// <param name="sender">annotation that changed</param>
        /// <param name="args">args describing the change</param>
        private void OnAuthorChanged(object sender, AnnotationAuthorChangedEventArgs args)
        {
            _currentAnnotations[args.Annotation.Id].Dirty = true;
            _authorChanged(sender, args);
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // a dictionary that holds an annotation id to an annotation instance
        private Dictionary<Guid, CachedAnnotation> _currentAnnotations = new Dictionary<Guid, CachedAnnotation>();

        // The delegates to be notified for Annotation changes
        private AnnotationAuthorChangedEventHandler _authorChanged;
        private AnnotationResourceChangedEventHandler _anchorChanged;
        private AnnotationResourceChangedEventHandler _cargoChanged;

        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Private Classes
        //
        //------------------------------------------------------

        #region Private Classes

        /// <summary>
        /// This class contains an annotation and a dirty flag 
        /// It is needed because when it is time to flush
        /// the store - we should write only dirty annotations
        /// </summary>
        private class CachedAnnotation
        {
            /// <summary>
            /// Construct a CachedAnnotation
            /// </summary>
            /// <param name="annotation">The annotation instance to be cached</param>
            /// <param name="dirty">A flag to indicate if the annotation is dirty</param>
            public CachedAnnotation(Annotation annotation, bool dirty)
            {
                Annotation = annotation;
                Dirty = dirty;
            }

            /// <summary>
            /// The cached Annotation instance
            /// </summary>
            /// <value></value>
            public Annotation Annotation
            {
                get { return _annotation; }
                set { _annotation = value; }
            }

            /// <summary>
            /// A flag to indicate if the cached annotation is dirty or not
            /// </summary>
            /// <value></value>
            public bool Dirty
            {
                get { return _dirty; }
                set { _dirty = value; }
            }


            private Annotation _annotation;
            private bool _dirty;
        }

        #endregion Private Classes
    }
}
