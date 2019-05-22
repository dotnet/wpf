// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      AnnotationMap contains the implementation of the map
//      map between annotation id and attached annotations used by the service
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Annotations;

namespace MS.Internal.Annotations
{
    /// <summary>
    /// The AnnotationMap holds a map between the Id's of annotations and IAttachedAnnotations
    /// </summary>
    internal class AnnotationMap
    {
        /// <summary>
        /// Add an IAttachedAnnotation to the annotation map.
        /// </summary>
        /// <param name="attachedAnnotation">the IAttachedAnnotation to be added to the map</param>
        internal void AddAttachedAnnotation(IAttachedAnnotation attachedAnnotation)
        {
            List<IAttachedAnnotation> list = null;
            if (!_annotationIdToAttachedAnnotations.TryGetValue(attachedAnnotation.Annotation.Id, out list))
            {
                list = new List<IAttachedAnnotation>(1);
                _annotationIdToAttachedAnnotations.Add(attachedAnnotation.Annotation.Id, list);
            }

            list.Add(attachedAnnotation);
        }

        /// <summary>
        /// Remove an IAttachedAnnotation from the annotation map.
        /// </summary>
        /// <param name="attachedAnnotation"></param>
        internal void RemoveAttachedAnnotation(IAttachedAnnotation attachedAnnotation)
        {
            List<IAttachedAnnotation> list = null;
            if (_annotationIdToAttachedAnnotations.TryGetValue(attachedAnnotation.Annotation.Id, out list))
            {
                list.Remove(attachedAnnotation);
                if (list.Count == 0)
                {
                    _annotationIdToAttachedAnnotations.Remove(attachedAnnotation.Annotation.Id);
                }
            }
        }

        /// <summary>
        /// Returns whether or not there are any annotations currently loaded.  This can be
        /// used to avoid costly walks of the tree.
        /// </summary>
        internal bool IsEmpty
        {
            get
            {
                return _annotationIdToAttachedAnnotations.Count == 0;
            }
        }

        /// <summary>
        /// Return a list of IAttachedAnnotations for a given annotation id
        /// </summary>
        /// <param name="annotationId"></param>
        /// <returns>list of IAttachedAnnotations</returns>
        internal List<IAttachedAnnotation> GetAttachedAnnotations(Guid annotationId)
        {
            List<IAttachedAnnotation> list = null;

            if (!_annotationIdToAttachedAnnotations.TryGetValue(annotationId, out list))
            {
                // return empty list if annotation id not found
                return _emptyList;
            }

            Debug.Assert(list != null, "there should be an attached annotation list for the annotationId: " + annotationId.ToString());
            return list;
        }

        /// <summary>
        /// Return a list of all IAttachedAnnotations in the map
        /// </summary>
        /// <returns>list of IAttachedAnnotations</returns>
        internal List<IAttachedAnnotation> GetAllAttachedAnnotations()
        {
            List<IAttachedAnnotation> result = new List<IAttachedAnnotation>(_annotationIdToAttachedAnnotations.Keys.Count);

            foreach (Guid annId in _annotationIdToAttachedAnnotations.Keys)
            {
                List<IAttachedAnnotation> list = _annotationIdToAttachedAnnotations[annId];

                result.AddRange(list);
            }

            if (result.Count == 0)
            {
                return _emptyList;
            }

            return result;
        }

        // hash table to hold annotation id to AttachedAnnotations list 
        private Dictionary<Guid, List<IAttachedAnnotation>> _annotationIdToAttachedAnnotations = new Dictionary<Guid, List<IAttachedAnnotation>>();

        // a readonly empty list - cached for performance reasons
        private static readonly List<IAttachedAnnotation> _emptyList = new List<IAttachedAnnotation>(0);
    }
}
