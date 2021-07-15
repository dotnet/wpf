// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//     AttachedAnnotation defines the IAttachedAnnotation interface
//

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Annotations.Storage;
using System.Windows.Media;
using MS.Utility;

namespace MS.Internal.Annotations
{
    /// <summary>
    /// Represents how much of the locator was resolved
    /// to produced the attached anchor.
    /// </summary>
    [Flags]
    internal enum AttachmentLevel
    {
        /// <summary>
        /// The locator was completely resolved.
        /// </summary>
        Full            = 0x00000007,
        /// <summary>
        /// The start portion of the locator resolved.
        /// </summary>        
        StartPortion    = 0x00000004,
        /// <summary>
        /// The middle portion of the locator resolved.
        /// </summary>
        MiddlePortion   = 0x00000002,
        /// <summary>
        /// The end portion of the locator was resolved.
        /// </summary>
        EndPortion      = 0x00000001,
        /// <summary>
        /// The locator was resolved, but not to the level of content.
        /// </summary>
        Incomplete      = 0x00000100,
        /// <summary>
        /// The locator was not resolved at all.
        /// </summary>
        Unresolved      = 0x00000000
    }

    /// <summary>
    ///     IAttachedAnnotation represents where a particular anchor of a particular annotation
    ///     is attached in the element tree of a running application.
    /// </summary>
    internal interface IAttachedAnnotation : IAnchorInfo
    {
        /// <summary>
        /// The part of the Avalon element tree that this attached annotation is attached to
        /// </summary>
        Object AttachedAnchor { get; }

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
        Object FullyAttachedAnchor { get; }

        /// <summary>
        /// The level of attachment of the annotation to its attached anchor
        /// </summary>
        AttachmentLevel AttachmentLevel { get; }

        /// <summary>
        /// The Avalon logical Parent node of the attached anchor
        /// </summary>
        DependencyObject Parent { get; }

        /// <summary>
        /// The Avalon coordinate of the attached anchor measured relative 
        /// from its parent coordinate
        /// </summary>
        Point AnchorPoint { get; }

        /// <summary>
        /// compare the attached anchor of the current attached annotations
        /// and another attached anchor
        /// </summary>
        /// <param name="o">an attached anchor to be compared against</param>
        /// <returns>true if the passed attached anchors and the attached annotation's 
        /// attached anchor are equal</returns>
        bool IsAnchorEqual(object o);

        /// <summary>
        /// the Store of this attached annotation 
        /// </summary>
        AnnotationStore Store { get; }
    }
}
