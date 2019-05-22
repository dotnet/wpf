// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Represents a node in a linked list used to track active containers.
    ///     Containers should instantiate and references these.
    ///     Parents hold onto the linked list.
    ///     
    ///     The list is iterated in order to call a variety of methods on containers
    ///     in response to changes on the parent.
    /// </summary>
    internal class ContainerTracking<T>
    {
        internal ContainerTracking(T container)
        {
            _container = container;
        }

        /// <summary>
        ///     The row container that this object represents.
        /// </summary>
        internal T Container
        {
            get { return _container; }
        }

        /// <summary>
        ///     The next node in the list.
        /// </summary>
        internal ContainerTracking<T> Next
        {
            get { return _next; }
        }

        /// <summary>
        ///     The previous node in the list.
        /// </summary>
        internal ContainerTracking<T> Previous
        {
            get { return _previous; }
        }

        /// <summary>
        ///     Adds this tracker to the list of active containers.
        /// </summary>
        /// <param name="root">The root of the list.</param>
        internal void StartTracking(ref ContainerTracking<T> root)
        {
            // Add the node to the root
            if (root != null)
            {
                root._previous = this;
            }

            _next = root;
            root = this;
        }

        /// <summary>
        ///     Removes this tracker from the list of active containers.
        /// </summary>
        /// <param name="root">The root of the list.</param>
        internal void StopTracking(ref ContainerTracking<T> root)
        {
            // Unhook the node from the list
            if (_previous != null)
            {
                _previous._next = _next;
            }

            if (_next != null)
            {
                _next._previous = _previous;
            }

            // Update the root reference
            if (root == this)
            {
                root = _next;
            }

            // Clear the node's references
            _previous = null;
            _next = null;
        }

        #region Debugging Helpers
        
        /// <summary>
        ///     Asserts that the container represented by this tracker is in the list represented by the given root.
        /// </summary>
        [Conditional("DEBUG")]
        internal void Debug_AssertIsInList(ContainerTracking<T> root)
        {
#if DEBUG
            Debug.Assert(IsInList(root), "This container should be in the tracking list.");
#endif
        }

        /// <summary>
        ///     Asserts that the container represented by this tracker is not in the list represented by the given root.
        /// </summary>
        [Conditional("DEBUG")]
        internal void Debug_AssertNotInList(ContainerTracking<T> root)
        {
#if DEBUG
            Debug.Assert(!IsInList(root), "This container shouldn't be in our tracking list");
#endif
        }

#if DEBUG
        /// <summary>
        ///     Checks that this tracker is present in the list starting with root.  It's a linear walk through the list, so should be used
        ///     mostly for debugging
        /// </summary>
        private bool IsInList(ContainerTracking<T> root)
        {
            ContainerTracking<T> node = root;

            while (node != null)
            {
                if (node == this)
                {
                    return true;
                }

                node = node._next;
            }

            return false;
        }
#endif

        #endregion

        private T _container;
        private ContainerTracking<T> _next;
        private ContainerTracking<T> _previous;
    }
}