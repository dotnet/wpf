// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//     AttachedAnnotation represents an annotation which has been loaded for a
//     specific logical tree and has a runtime anchor in that tree.  The same 
//     annotation may be loaded in a different logical tree and get a different 
//     anchor - thereby creating a different AttachedAnnotation.
//    
//     Spec: Annotation Service Spec.doc
//

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Annotations.Storage;
using System.Windows.Media;
using System.Xml;
using System.Collections.Generic;
using MS.Utility;

namespace MS.Internal.Annotations.Anchoring
{
    /// <summary>
    ///     AttachedAnnotation represents an annotation which has been loaded for a
    ///     specific logical tree and has an anchor in that tree.  The same annotation
    ///     may be loaded in a different logical tree and get a different anchor - 
    ///     thereby creating a different AttachedAnnotation.
    /// </summary>
    internal class AttachedAnnotation : IAttachedAnnotation
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Create an instance of AttachedAnnotation.
        /// </summary>
        /// <param name="manager">the LocatorManager providing processors for this anchored annotation</param>
        /// <param name="annotation">the annotation itself</param>
        /// <param name="anchor">the annotation's anchor represented by the attached anchor</param>
        /// <param name="attachedAnchor">the attached anchor itself</param>
        /// <param name="attachmentLevel">the level of the attached anchor</param>
        internal AttachedAnnotation(LocatorManager manager, Annotation annotation, AnnotationResource anchor, Object attachedAnchor, AttachmentLevel attachmentLevel)
            : this(manager, annotation, anchor, attachedAnchor, attachmentLevel, null)
        {
        }

        /// <summary>
        ///     Create an instance of AttachedAnnotation with a specified parent.  Takes an optional 
        ///     parent for the attached annotation.  This is useful when the parent is known before
        ///     hand and not available through the normal means (such as in printing).
        /// </summary>
        /// <param name="manager">the LocatorManager providing processors for this anchored annotation</param>
        /// <param name="annotation">the annotation itself</param>
        /// <param name="anchor">the annotation's anchor represented by the attached anchor</param>
        /// <param name="attachedAnchor">the attached anchor itself</param>
        /// <param name="attachmentLevel">the level of the attached anchor</param>
        /// <param name="parent">parent of the selection</param>
        internal AttachedAnnotation(LocatorManager manager, Annotation annotation, AnnotationResource anchor, Object attachedAnchor, AttachmentLevel attachmentLevel, DependencyObject parent)
        {
            Debug.Assert(manager != null, "LocatorManager can not be null");
            Debug.Assert(annotation != null, "Annotation can not be null");
            Debug.Assert(anchor != null, "Anchor can not be null");
            Debug.Assert(attachedAnchor != null, "AttachedAnchor can not be null");

            _annotation = annotation;
            _anchor = anchor;
            _locatorManager = manager;
            Update(attachedAnchor, attachmentLevel, parent);
        }


        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------        

        #region Public Methods

        /// <summary>
        /// Returns true if the attachedanchor of this IAttachedAnnotation is 
        /// equal to the attached anchor passed to the function
        /// </summary>
        /// <param name="o"></param>
        /// <returns>true if the anchors are equal</returns>
        public bool IsAnchorEqual(Object o)
        {
            //return _selectionProcessor.AnchorEquals(_attachedAnchor, o);
            return false;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The Annotation that is being attached to the element tree
        /// </summary>
        /// <value></value>
        public Annotation Annotation { get { return _annotation; } }

        /// <summary>
        /// The specific anchor that has been resolved into the element tree
        /// </summary>
        /// <value></value>
        public AnnotationResource Anchor { get { return _anchor; } }

        /// <summary>
        /// The part of the Avalon element tree that this attached annotation is anchored to
        /// </summary>
        /// <value></value>
        public Object AttachedAnchor { get { return _attachedAnchor; } }

        /// <summary>
        /// This is simply a public accessor for FullyAttachedAnchor.  We didn't want to use the
        /// FullyAttachedAnchor name in the public API but couldn't get rid of this internal
        /// property because there are a few known users of it (through reflection).
        /// </summary>
        public Object ResolvedAnchor { get { return FullyAttachedAnchor; } }

        /// <summary>
        /// For virtualized content, this property returns the full anchor for the annotation.
        /// It may not be all visible or loaded into the tree, but the full anchor is useful
        /// for some operations that operate on content that may be virtualized.  This property
        /// could be:
        ///  - equal to AttachedAnchor, if the attachment level is Full
        ///  - different from AttachedAnchor, if the attachment level was not full but the
        ///    anchor could be resolved using virtual data
        ///  - null, if the attachment level was not ful but the anchor could not be resolved
        ///    using virtual data
        /// </summary>
        public Object FullyAttachedAnchor
        {
            get
            {
                if (_attachmentLevel == AttachmentLevel.Full)
                    return _attachedAnchor;
                else
                    return _fullyAttachedAnchor;
            }
        }

        /// <summary>
        /// The level of attachment of the annotation to its attached anchor
        /// </summary>
        /// <value></value>
        public AttachmentLevel AttachmentLevel { get { return _attachmentLevel; } }

        /// <summary>
        /// The Avalon logical Parent node of the attached anchor
        /// </summary>
        /// <returns></returns>
        public DependencyObject Parent
        {
            get
            {
                return _parent;
            }
        }

        /// <summary>
        /// The Avalon coordinate of the attached anchor measured relative 
        /// from its parent coordinate
        /// </summary>
        /// <returns></returns>
        public Point AnchorPoint
        {
            get
            {
                Point pt = _selectionProcessor.GetAnchorPoint(_attachedAnchor);

                if (!Double.IsInfinity(pt.X) && !Double.IsInfinity(pt.Y))
                {
                    _cachedPoint = pt;
                }

                return _cachedPoint;
            }
        }

        /// <summary>
        /// The AnnotationStore 
        /// </summary>
        /// <returns></returns>
        public AnnotationStore Store
        {
            get { return GetStore(); }
        }

        #endregion Public Properties        

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Update the attached anchor and level for this attached annotation.  Internal method called by
        /// the annotation service when a change to an anchor is processed.  Optional parent will be used
        /// if present.  Otherwise we get the parent from the selection processor.
        /// </summary>
        /// <param name="attachedAnchor">the new attached anchor</param>
        /// <param name="attachmentLevel">the new attachment level</param>
        /// <param name="parent">optional, the parent of the attached annotation</param>
        internal void Update(object attachedAnchor, AttachmentLevel attachmentLevel, DependencyObject parent)
        {
            Debug.Assert(attachedAnchor != null, "AttachedAnchor can not be null");
            Debug.Assert(attachmentLevel > AttachmentLevel.Unresolved && attachmentLevel <= AttachmentLevel.Incomplete,
                         "Undefined attachment level");

            _attachedAnchor = attachedAnchor;
            _attachmentLevel = attachmentLevel;
            _selectionProcessor = _locatorManager.GetSelectionProcessor(attachedAnchor.GetType());
            // Use optional parent if available
            if (parent != null)
            {
                _parent = parent;
            }
            else
            {
                _parent = _selectionProcessor.GetParent(_attachedAnchor);
            }
            Debug.Assert(_selectionProcessor != null, SR.Get(SRID.NoProcessorForSelectionType, attachedAnchor.GetType()));
        }


        /// <summary>
        /// Sets the fully attached anchor for this attached annotation.  Should be used if
        /// the attachment level is not Full and the anchor could be fully resolved using virual content.
        /// </summary>
        internal void SetFullyAttachedAnchor(object fullyAttachedAnchor)
        {
            Debug.Assert(_attachmentLevel != AttachmentLevel.Full, "Should only set fully resolved anchor if attachment level is not full.");

            _fullyAttachedAnchor = fullyAttachedAnchor;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Functions

        private AnnotationStore GetStore()
        {
            // indirect via the parent element & DP
            if (this.Parent != null)
            {
                AnnotationService service = AnnotationService.GetService(this.Parent);
                if (service != null)
                    return service.Store;
            }

            return null;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // The Annotation that is being attached to the element tree
        private Annotation _annotation;

        // The specific anchor that has been resolved into the element tree
        private AnnotationResource _anchor;

        // The part of the Avalon element tree that this attached annotation is anchored to
        private Object _attachedAnchor;

        // The fully resolved anchor, if different from _attachedAnchor
        private Object _fullyAttachedAnchor;

        // The level of attachment of the annotation to its attached anchor
        private AttachmentLevel _attachmentLevel;

        // Parent object of the attached anchor.  This shouldn't change without
        // the anchor actually changing.
        private DependencyObject _parent;

        // // The selectionprocessor capable of handling this _attachedanchor
        private SelectionProcessor _selectionProcessor;

        // the locatormanager that created this attached annotation
        private LocatorManager _locatorManager;

        // cached anchor point - used if the selection processor returns Infinity
        private Point _cachedPoint;
        #endregion Private Fields
    }
}







