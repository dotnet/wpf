// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: IHierarchicalVirtualizationAndScrollInfo interface
//


using System;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     Interface through which a VirtualizingPanel communicates with the 
    ///     element being virtualized (such as TreeViewItem or GroupItem). 
    ///     This is used for hierarchial virtualization where an element may contain a Header
    ///     and and an ItemsHost to layout its child elements.
    /// </summary>
    public interface IHierarchicalVirtualizationAndScrollInfo 
    {
        /// <summary>
        /// Returns the constraints for the control.
        /// Constraints include viewport rect, cache before and 
        /// after the viewport, cache unit.
        /// </summary>
        HierarchicalVirtualizationConstraints Constraints { get; set; }

        /// <summary>
        /// Returns desired size (pixel and logical) of the header element.
        /// </summary>
        HierarchicalVirtualizationHeaderDesiredSizes HeaderDesiredSizes { get; }

        /// <summary>
        /// Returns desired size of the items.
        /// </summary>
        HierarchicalVirtualizationItemDesiredSizes ItemDesiredSizes { get; set; }

        /// <summary>
        /// Return the panel that is the ItemsHost for this control.
        /// </summary>
        Panel ItemsHost { get; }

        /// <summary>
        /// This describes if the neighboring panels must disable virtualization.
        /// </summary>
        bool MustDisableVirtualization { get; set; }

        /// <summary>
        /// This property describes if this is the background layout pass 
        /// typically meant to generate containers for the cache regions.
        /// </summary>
        bool InBackgroundLayout { get; set; }
    }
}
