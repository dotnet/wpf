// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      Visual tree change arguments
//

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace System.Windows.Diagnostics
{
    public enum VisualTreeChangeType { Add, Remove }

    public class VisualTreeChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Parent visual object.
        /// </summary>
        public DependencyObject Parent { get; private set; }

        /// <summary>
        /// Child visual object.
        /// </summary>
        public DependencyObject Child { get; private set; }

        /// <summary>
        /// Child's index in parent.
        /// </summary>
        public int ChildIndex { get; private set; }

        /// <summary>
        /// Visual tree change type.
        /// </summary>
        public VisualTreeChangeType ChangeType { get; private set; }

        public VisualTreeChangeEventArgs(DependencyObject parent, DependencyObject child, int childIndex, VisualTreeChangeType changeType)
        {
            Parent = parent;
            Child = child;
            ChildIndex = childIndex;
            ChangeType = changeType;
        }
    }
}
