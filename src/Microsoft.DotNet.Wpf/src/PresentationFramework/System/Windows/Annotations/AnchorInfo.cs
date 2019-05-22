// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//     IAnchorInfo is the public portion of IAttachedAnnotation.
//


using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Annotations.Storage;
using System.Windows.Media;
using MS.Utility;

namespace System.Windows.Annotations
{
    /// <summary>
    ///     IAnchorInfo represents an annotation's persisted and run-time anchor information.
    /// </summary>
    public interface IAnchorInfo
    {
        /// <summary>
        /// The Annotation that this anchor information is for
        /// </summary>
        Annotation Annotation { get;}

        /// <summary>
        /// The specific anchor in the annotation object model that is represented
        /// by this anchor information.
        /// </summary>
        AnnotationResource Anchor { get; }

        /// <summary>
        /// The part of the Avalon element tree to which this annotation is anchored.
        /// If only a part of the annotations anchor is part of the tree at this moment,
        /// then this may be a partial anchor.  For instance, if the anchor is several
        /// pages of text content, but only one page is currently visible, this would be
        /// the visible page.
        /// </summary>
        Object ResolvedAnchor { get;}
    }
}
