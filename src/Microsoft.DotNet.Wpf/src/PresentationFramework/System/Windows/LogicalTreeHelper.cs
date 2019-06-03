// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: The LogicalTreeHelper class contains static methods that are
//  useful for performing common tasks with logical tree nodes.  There's nothing
//  here that the end user can't do themselves - but we do it for them and
//  gather them up here.
//
//

using System;
using System.Collections;   // In order to use IEnumerator
using System.Diagnostics;
using MS.Utility;           // In order to use SR.Get and SRID

namespace System.Windows
{
/*
    public delegate bool ParentTraversalCallback(FrameworkElement parentFE, FrameworkContentElement parentFCE, object data);
    public delegate bool ChildTraversalCallback(FrameworkElement childFE, FrameworkContentElement childFCE, object child, object data);
*/
    
/// <summary>
/// Static helper functions for dealing with the logical tree
/// </summary>
public static class LogicalTreeHelper
{
    //------------------------------------------------------
    //
    //  Constructors
    //
    //------------------------------------------------------

    //------------------------------------------------------
    //
    //  Public Properties
    //
    //------------------------------------------------------

    // None
    
    //------------------------------------------------------
    //
    //  Public Methods
    //
    //------------------------------------------------------

    #region Public Methods
    
    /// <summary>
    /// Given an element in the logical tree to start searching from,
    /// searches all its descendent nodes in the logical tree a node whose Name 
    /// matches the specified elementName.
    /// The given DependencyObject must be either a FrameworkElement or FrameworkContentElement.
    /// </summary>
    /// <remarks>
    /// We're searching in a depth-first manner.  Review this if this turns out 
    ///  to be a performance problem.  We're doing this first because it's easy
    ///  and light on memory usage as compared to breadth-first.
    /// (RogerCh):It would be cool if the DFID (depth-first iterative-deepening)
    ///  algorithm would be useful here.
    /// </remarks>
    public static DependencyObject FindLogicalNode( DependencyObject logicalTreeNode, string elementName )
    {
        if( logicalTreeNode == null )
        {
            throw new ArgumentNullException("logicalTreeNode");
        }
        if( elementName == null )
        {
            throw new ArgumentNullException("elementName");
        }
        if( elementName == String.Empty )
        {
            throw new ArgumentException(SR.Get(SRID.StringEmpty), "elementName");
        }

        DependencyObject namedElement = null;
        DependencyObject childNode = null;

        // Check given node against named element.
        IFrameworkInputElement selfNode = logicalTreeNode as IFrameworkInputElement;
        if( selfNode != null )
        {
            if( selfNode.Name == elementName )
            {
                namedElement = logicalTreeNode;
            }
        }

        if( namedElement == null )
        {
            // Nope, the given node isn't it.  See if we can check children.
            IEnumerator childEnumerator = null;

            childEnumerator = LogicalTreeHelper.GetLogicalChildren(logicalTreeNode);

            // If we can enumerate, check the children.
            if( childEnumerator != null )
            {
                childEnumerator.Reset();
                while( namedElement == null &&
                       childEnumerator.MoveNext() == true)
                {
                    childNode = childEnumerator.Current as DependencyObject;

                    if( childNode != null )
                    {
                        namedElement = FindLogicalNode( childNode, elementName );
                    }
                }
            }
        }

        // Return what we can find - may be null.
        return namedElement;
    }

    /// <summary>
    /// Get the logical parent of the given DependencyObject.
    /// The given DependencyObject must be either a FrameworkElement or FrameworkContentElement
    /// to have a logical parent.
    /// </summary>
    public static DependencyObject GetParent(DependencyObject current)
    {
        if (current == null)
        {
            throw new ArgumentNullException("current"); 
        }

        FrameworkElement fe = current as FrameworkElement;
        if (fe != null)
        {
            return fe.Parent;
        }

        FrameworkContentElement fce = current as FrameworkContentElement;
        if (fce != null)
        {
            return fce.Parent;
        }

        return null;
    }

    /// <summary>
    /// Get the logical children for the given DependencyObject.
    /// The given DependencyObject must be either a FrameworkElement or FrameworkContentElement
    /// to have logical children.
    /// </summary>
    public static IEnumerable GetChildren(DependencyObject current)
    {
        if (current == null)
        {
            throw new ArgumentNullException("current"); 
        }

        FrameworkElement fe = current as FrameworkElement;
        if (fe != null)
        {
            return new EnumeratorWrapper(fe.LogicalChildren);
        }

        FrameworkContentElement fce = current as FrameworkContentElement;
        if (fce != null)
        {
            return new EnumeratorWrapper(fce.LogicalChildren);
        }

        return EnumeratorWrapper.Empty;
    }

    /// <summary>
    /// Get the logical children for the given FrameworkElement
    /// </summary>
    public static IEnumerable GetChildren(FrameworkElement current)
    {
        if (current == null)
        {
            throw new ArgumentNullException("current"); 
        }

        return new EnumeratorWrapper(current.LogicalChildren);
    }

    /// <summary>
    /// Get the logical children for the given FrameworkContentElement
    /// </summary>
    public static IEnumerable GetChildren(FrameworkContentElement current)
    {
        if (current == null)
        {
            throw new ArgumentNullException("current"); 
        }

        return new EnumeratorWrapper(current.LogicalChildren);
    }

    /// <summary>
    /// Attempts to bring this element into view by originating a RequestBringIntoView event.
    /// </summary>
    public static void BringIntoView(DependencyObject current)
    {
        if (current == null)
        {
            throw new ArgumentNullException("current"); 
        }

        FrameworkElement fe = current as FrameworkElement;
        if (fe != null)
        {
            fe.BringIntoView();
        }

        FrameworkContentElement fce = current as FrameworkContentElement;
        if (fce != null)
        {
            fce.BringIntoView();
        }
    }

/*
    /// <summary>
    // fill this in
    /// </summary>
    public static bool WalkUpLogicalTree(DependencyObject startNode, ParentTraversalCallback callback, bool skipStart, in out object data)
    {
        FrameworkElement startFE = startNode as FrameworkElement;
        if (startFE != null)
        {
            return WalkUpLogicalTree(startFE, callback, skipStart, in out data);
        }
        else
        {
            FrameworkContentElement startFCE;
            if (startFCE != null)
            {
                return WalkUpLogicalTree(startFCE, callback, skipStart, in out data);
            }
            else
            {
                //  throw an exception
            }
        }
        return true;
    }
    
    /// <summary>
    //  fill this in
    /// </summary>
    public static bool WalkUpLogicalTree(FrameworkElement startNode, ParentTraversalCallback callback, bool skipStart, in out object data)
    {
        if (callback == null)
        {
            return false;
        }
        
        FrameworkContentElement parentFCE;
        FrameworkElement parentFE;
        if (!skipStart)
        {
            parentFE = startNode;
            parentFCE = null;
            if (callback(parentFE, parentFCE, in out data))
            {
                return true;
            }
        }
        bool hasParent = FrameworkElement.GetFrameworkParent(startNode, out parentFE, out parentFCE);
        while (hasParent)
        {
            if (callback(parentFE, parentFCE, in out data))
            {
                return true;
            }

            // No boundary or inheritance node found, continue search
            if (parentFE != null)
            {
                hasParent = GetFrameworkParent(parentFE, out parentFE, out parentFCE);                           
            }
            else
            {
                hasParent = GetFrameworkParent(parentFCE, out parentFE, out parentFCE);
            }
        }

        return false;
    }

    /// <summary>
    //  fill this in
    /// </summary>
    public static bool WalkUpLogicalTree(FrameworkContentElement startNode, ParentTraversalCallback callback, bool skipStart, in out object data)
    {
        if (callback == null)
        {
            return false;
        }
        
        FrameworkContentElement parentFCE;
        FrameworkElement parentFE;
        if (!skipStart)
        {
            parentFE = null;
            parentFCE = startNode;
            if (callback(parentFE, parentFCE, in out data))
            {
                return true;
            }
        }
        bool hasParent = FrameworkElement.GetFrameworkParent(startNode, out parentFE, out parentFCE);
        while (hasParent)
        {
            if (callback(parentFE, parentFCE, in out data))
            {
                return true;
            }

            // No boundary or inheritance node found, continue search
            if (parentFE != null)
            {
                hasParent = GetFrameworkParent(parentFE, out parentFE, out parentFCE);                           
            }
            else
            {
                hasParent = GetFrameworkParent(parentFCE, out parentFE, out parentFCE);
            }
        }

        return false;
    }

    /// <summary>
    //  fill this in
    /// </summary>
    public static bool WalkDownLogicalTree(FrameworkElement startNode, ChildTraversalCallback callback, bool skipStart, object data)
    {
        //  use descendants walker here
    }
    
    /// <summary>
    //  fill this in
    /// </summary>
    public static void WalkDownLogicalTree(FrameworkContentElement startNode, ChildTraversalCallback callback, bool skipStart, object data)
    {
        //  use descendants walker here
    }
*/    
    #endregion Public Methods

    //------------------------------------------------------
    //
    //  Public Events
    //
    //------------------------------------------------------

    // None

    //------------------------------------------------------
    //
    //  Internal Constructors
    //
    //------------------------------------------------------

    // None

    //------------------------------------------------------
    //
    //  Internal Properties
    //
    //------------------------------------------------------

    // None
    
    //------------------------------------------------------
    //
    //  Internal Methods
    //
    //------------------------------------------------------

    #region Internal Methods

    internal static void AddLogicalChild(DependencyObject parent, object child)
    {
        if (child != null && parent != null)
        {
            FrameworkElement parentFE = parent as FrameworkElement;
            if (parentFE != null)
            {
                parentFE.AddLogicalChild(child);
            }
            else
            {
                FrameworkContentElement parentFCE = parent as FrameworkContentElement;
                if (parentFCE != null)
                {
                    parentFCE.AddLogicalChild(child);
                }
            }
        }
    }

    internal static void AddLogicalChild(FrameworkElement parentFE, FrameworkContentElement parentFCE, object child)
    {
        if (child != null)
        {
            if (parentFE != null)
            {
                parentFE.AddLogicalChild(child);
            }
            else if (parentFCE != null)
            {
                parentFCE.AddLogicalChild(child);
            }
        }
    }

    internal static void RemoveLogicalChild(DependencyObject parent, object child)
    {
        if (child != null && parent != null)
        {
            FrameworkElement parentFE = parent as FrameworkElement;
            if (parentFE != null)
            {
                parentFE.RemoveLogicalChild(child);
            }
            else
            {
                FrameworkContentElement parentFCE = parent as FrameworkContentElement;
                if (parentFCE != null)
                {
                    parentFCE.RemoveLogicalChild(child);
                }
            }
        }
    }

    internal static void RemoveLogicalChild(FrameworkElement parentFE, FrameworkContentElement parentFCE, object child)
    {
        if (child != null)
        {
            Debug.Assert(parentFE != null || parentFCE != null, "Either parentFE or parentFCE should be non-null");
            if (parentFE != null)
            {
                parentFE.RemoveLogicalChild(child);
            }
            else
            {
                parentFCE.RemoveLogicalChild(child);
            }
        }
    }

    internal static IEnumerator GetLogicalChildren(DependencyObject current)
    {
        FrameworkElement fe = current as FrameworkElement;
        if (fe != null)
        {
            return fe.LogicalChildren;
        }

        FrameworkContentElement fce = current as FrameworkContentElement;
        if (fce != null)
        {
            return fce.LogicalChildren;
        }

        return MS.Internal.Controls.EmptyEnumerator.Instance;
    }

    #endregion Internal Methods

    //------------------------------------------------------
    //
    //  Internal Events
    //
    //------------------------------------------------------

    // None

    //------------------------------------------------------
    //
    //  Private Classes
    //
    //------------------------------------------------------

    private class EnumeratorWrapper : IEnumerable
    {
        public EnumeratorWrapper(IEnumerator enumerator)
        {
            if (enumerator != null)
            {
                _enumerator = enumerator;
            }
            else
            {
                _enumerator = MS.Internal.Controls.EmptyEnumerator.Instance;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _enumerator;
        }

        IEnumerator _enumerator;


        internal static EnumeratorWrapper Empty
        {
            get
            {
                if (_emptyInstance == null)
                {
                    _emptyInstance = new EnumeratorWrapper(null);
                }

                return _emptyInstance;
            }
        }

        static EnumeratorWrapper _emptyInstance;
    }
}
}
