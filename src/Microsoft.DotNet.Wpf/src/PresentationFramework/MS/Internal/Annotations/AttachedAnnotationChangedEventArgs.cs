// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      AttachedAnnotationChangedEventArgs Is the event argument class
//      for the AttachedAnnotationChanged event fired by the AnnotationService
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Annotations.Storage;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Diagnostics;
using MS.Internal;
using MS.Utility;
using System.Reflection;        // for BindingFlags
using System.Globalization;     // for CultureInfo

namespace MS.Internal.Annotations
{
    /// <summary>
    /// AttachedAnnotationAction, Added or Deleted, or Modified
    ///</summary>
    internal enum AttachedAnnotationAction
    {
        /// <summary>
        /// an attached annotation is being added to the system during load
        /// </summary>
        Loaded,

        /// <summary>
        /// an attached annotation is being deleted from the system during unload
        /// </summary>
        Unloaded,

        /// <summary>
        /// an attached annotation is being modified through AnnotationStore.Modify
        /// </summary>
        AnchorModified,

        /// <summary>
        /// an attached annotation is being added to the system through AnnotationStore.Add
        /// </summary>
        Added,

        /// <summary>
        /// an attached annotation is being deleted from the system through AnnotationStore.Delete
        /// </summary>
        Deleted
    }



    /// <summary>
    /// The data structure that describes the event. Mainly:
    ///     1- the Action, 
    ///     2- the Original IAttachedAnnotation (null in case of Add or Delete)
    ///     3- the new IAttachedAnnotation 
    ///     4- the IsAnchorChanged, which is a bool to indicates whether the
    ///     anchor has changed or not (its value is valid only in cases of Action = Modify)
    /// </summary>
    internal class AttachedAnnotationChangedEventArgs : System.EventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// internal constructor that creates an AttachedAnnotationChangedEventArgs
        /// </summary>
        /// <param name="action">action represented by this instance</param>
        /// <param name="attachedAnnotation">annotation that was added/deleted/modified</param>
        /// <param name="previousAttachedAnchor">if action is modified, previous attached anchor</param>
        /// <param name="previousAttachmentLevel">if action is modified, previous attachment level</param>
        internal AttachedAnnotationChangedEventArgs(AttachedAnnotationAction action, IAttachedAnnotation attachedAnnotation, object previousAttachedAnchor, AttachmentLevel previousAttachmentLevel)
        {
            Invariant.Assert(attachedAnnotation != null);

            _action = action;
            _attachedAnnotation = attachedAnnotation;
            _previousAttachedAnchor = previousAttachedAnchor;
            _previousAttachmentLevel = previousAttachmentLevel;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Properties 
        //
        //------------------------------------------------------

        #region Public Properties 

        /// <summary>
        /// the update Action: Add, Delete, or Modify
        /// </summary>
        public AttachedAnnotationAction Action { get { return _action; } }

        /// <summary>
        /// the new IAttachedAnnotation
        /// </summary>
        public IAttachedAnnotation AttachedAnnotation { get { return _attachedAnnotation; } }

        /// <summary>
        /// The previous attached anchor for this attached annotation.  Will be null when Action is
        /// not Modified.
        /// </summary>
        public object PreviousAttachedAnchor { get { return _previousAttachedAnchor; } }


        /// <summary>
        /// The previous attachment level for this atttached annotation.  Only valid if Action is
        /// Modified.
        /// </summary>
        public AttachmentLevel PreviousAttachmentLevel { get { return _previousAttachmentLevel; } }

        #endregion Public Properties 

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Methods 
        //
        //------------------------------------------------------

        #region Internal Methods 

        /// <summary>
        /// factory method to create an AttachedAnnotationChangedEventArgs for the action added
        /// </summary>
        /// <param name="attachedAnnotation">the IAttachedAnnotation associated with the event</param>
        /// <returns>the constructed AttachedAnnotationChangedEventArgs</returns>
        internal static AttachedAnnotationChangedEventArgs Added(IAttachedAnnotation attachedAnnotation)
        {
            Invariant.Assert(attachedAnnotation != null);

            return new AttachedAnnotationChangedEventArgs(AttachedAnnotationAction.Added, attachedAnnotation, null, AttachmentLevel.Unresolved);
        }

        /// <summary>
        /// factory method to create an AttachedAnnotationChangedEventArgs for the action Loaded
        /// </summary>
        /// <param name="attachedAnnotation">the IAttachedAnnotation associated with the event</param>
        /// <returns>the constructed AttachedAnnotationChangedEventArgs</returns>
        internal static AttachedAnnotationChangedEventArgs Loaded(IAttachedAnnotation attachedAnnotation)
        {
            Invariant.Assert(attachedAnnotation != null);

            return new AttachedAnnotationChangedEventArgs(AttachedAnnotationAction.Loaded, attachedAnnotation, null, AttachmentLevel.Unresolved);
        }

        /// <summary>
        /// factory method to create an AttachedAnnotationChangedEventArgs for the action deleted
        /// </summary>
        /// <param name="attachedAnnotation">the IAttachedAnnotation associated with the event</param>
        /// <returns>the constructed AttachedAnnotationChangedEventArgs</returns>
        internal static AttachedAnnotationChangedEventArgs Deleted(IAttachedAnnotation attachedAnnotation)
        {
            Invariant.Assert(attachedAnnotation != null);

            return new AttachedAnnotationChangedEventArgs(AttachedAnnotationAction.Deleted, attachedAnnotation, null, AttachmentLevel.Unresolved);
        }

        /// <summary>
        /// factory method to create an AttachedAnnotationChangedEventArgs for the action Unloaded
        /// </summary>
        /// <param name="attachedAnnotation">the IAttachedAnnotation associated with the event</param>
        /// <returns>the constructed AttachedAnnotationChangedEventArgs</returns>
        internal static AttachedAnnotationChangedEventArgs Unloaded(IAttachedAnnotation attachedAnnotation)
        {
            Invariant.Assert(attachedAnnotation != null);

            return new AttachedAnnotationChangedEventArgs(AttachedAnnotationAction.Unloaded, attachedAnnotation, null, AttachmentLevel.Unresolved);
        }

        /// <summary>
        /// factory method to create an AttachedAnnotationChangedEventArgs for the action modified
        /// </summary>
        /// <param name="attachedAnnotation">the IAttachedAnnotation associated with the event</param>
        /// <param name="previousAttachedAnchor">the previous attached anchor for the attached annotation</param>
        /// <param name="previousAttachmentLevel">the previous attachment level for the attached annotation</param>
        /// <returns>the constructed AttachedAnnotationChangedEventArgs</returns>
        internal static AttachedAnnotationChangedEventArgs Modified(IAttachedAnnotation attachedAnnotation, object previousAttachedAnchor, AttachmentLevel previousAttachmentLevel)
        {
            Invariant.Assert(attachedAnnotation != null && previousAttachedAnchor != null);

            return new AttachedAnnotationChangedEventArgs(AttachedAnnotationAction.AnchorModified, attachedAnnotation, previousAttachedAnchor, previousAttachmentLevel);
        }

        #endregion Internal Methods 

        //------------------------------------------------------
        //
        //  Private Fields 
        //
        //------------------------------------------------------

        #region Private Fields 

        private AttachedAnnotationAction _action;
        private IAttachedAnnotation _attachedAnnotation;
        private object _previousAttachedAnchor;
        private AttachmentLevel _previousAttachmentLevel;

        #endregion Private Fields 
    }

    /// <summary>
    /// the signature of the AttachedAnnotationChangedEventHandler delegate
    /// </summary>
    internal delegate void AttachedAnnotationChangedEventHandler(object sender, AttachedAnnotationChangedEventArgs e);
}
