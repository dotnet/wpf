// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: TreeWalker class, allows client to walk custom views of
//              UIAutomation tree.

using System;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// TreeWalker - used to walk over a view of the UIAutomation tree
    /// </summary>
#if (INTERNAL_COMPILE)
    internal sealed class TreeWalker
#else
    public sealed class TreeWalker
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        /// <summary>
        /// Create a tree walker that can be used to walk a specified
        /// view of the UIAutomation tree.
        /// </summary>
        /// <param name="condition">Condition defining the view - nodes that do not satisfy this condition are skipped over</param>
        public TreeWalker(Condition condition)
        {
            Misc.ValidateArgumentNonNull(condition, "condition");
            _condition = condition;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>
        /// Predefined TreeWalker for walking the Raw view of the UIAutomation tree
        /// </summary>
        public static readonly TreeWalker RawViewWalker = new TreeWalker(Automation.RawViewCondition);

        /// <summary>
        /// Predefined TreeWalker for walking the Control view of the UIAutomation tree
        /// </summary>
        public static readonly TreeWalker ControlViewWalker = new TreeWalker(Automation.ControlViewCondition);

        /// <summary>
        /// Predefined TreeWalker for walking the Content view of the UIAutomation tree
        /// </summary>
        public static readonly TreeWalker ContentViewWalker = new TreeWalker(Automation.ContentViewCondition);

        #endregion Public Constants and Readonly Fields


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        /// Get the parent of the specified element, in the current view
        /// </summary>
        /// <param name="element">element to get the parent of</param>
        /// <returns>The parent of the specified element; can be null if
        /// specified element was the root element</returns>
        /// <remarks>The view used is determined by the condition passed to
        /// the constructor - elements that do not satisfy that condition
        /// are skipped over</remarks>
        public AutomationElement GetParent(AutomationElement element)
        {
            Misc.ValidateArgumentNonNull(element, "element");
            return element.Navigate(NavigateDirection.Parent, _condition, null);
        }

        /// <summary>
        /// Get the first child of the specified element, in the current view
        /// </summary>
        /// <param name="element">element to get the first child of</param>
        /// <returns>The frst child of the specified element - or null if
        /// the specified element has no children</returns>
        /// <remarks>The view used is determined by the condition passed to
        /// the constructor - elements that do not satisfy that condition
        /// are skipped over</remarks>
        public AutomationElement GetFirstChild(AutomationElement element)
        {
            Misc.ValidateArgumentNonNull(element, "element");
            return element.Navigate(NavigateDirection.FirstChild, _condition, null);
        }

        /// <summary>
        /// Get the last child of the specified element, in the current view
        /// </summary>
        /// <param name="element">element to get the last child of</param>
        /// <returns>The last child of the specified element - or null if
        /// the specified element has no children</returns>
        /// <remarks>The view used is determined by the condition passed to
        /// the constructor - elements that do not satisfy that condition
        /// are skipped over</remarks>
        public AutomationElement GetLastChild(AutomationElement element)
        {
            Misc.ValidateArgumentNonNull(element, "element");
            return element.Navigate(NavigateDirection.LastChild, _condition, null);
        }

        /// <summary>
        /// Get the next sibling of the specified element, in the current view
        /// </summary>
        /// <param name="element">element to get the next sibling of</param>
        /// <returns>The next sibling of the specified element - or null if the
        /// specified element has no next sibling</returns>
        /// <remarks>The view used is determined by the condition passed to
        /// the constructor - elements that do not satisfy that condition
        /// are skipped over</remarks>
        public AutomationElement GetNextSibling(AutomationElement element)
        {
            Misc.ValidateArgumentNonNull(element, "element");
            return element.Navigate(NavigateDirection.NextSibling, _condition, null);
        }

        /// <summary>
        /// Get the previous sibling of the specified element, in the current view
        /// </summary>
        /// <param name="element">element to get the previous sibling of</param>
        /// <returns>The previous sibling of the specified element - or null if the
        /// specified element has no previous sibling</returns>
        /// <remarks>The view used is determined by the condition passed to
        /// the constructor - elements that do not satisfy that condition
        /// are skipped over</remarks>
        public AutomationElement GetPreviousSibling(AutomationElement element)
        {
            Misc.ValidateArgumentNonNull(element, "element");
            return element.Navigate(NavigateDirection.PreviousSibling, _condition, null);
        }

        /// <summary>
        /// Return the element or the nearest ancestor which is present in
        /// the view of the tree used by this treewalker
        /// </summary>
        /// <param name="element">element to normalize</param>
        /// <returns>The element or the nearest ancestor which satisfies the
        /// condition used by this TreeWalker</returns>
        /// <remarks>
        /// This method starts at the specified element and walks up the
        /// tree until it finds an element that satisfies the TreeWalker's
        /// condition.
        /// 
        /// If the passed-in element itself satsifies the condition, it is
        /// returned as-is.
        /// 
        /// If the process of walking up the tree hits the root node, then
        /// the root node is returned, regardless of whether it satisfies
        /// the condition or not. 
        /// </remarks>
        public AutomationElement Normalize(AutomationElement element)
        {
            Misc.ValidateArgumentNonNull(element, "element");
            return element.Normalize(_condition, null);
        }


        /// <summary>
        /// Get the parent of the specified element, in the current view,
        /// prefetching properties
        /// </summary>
        /// <param name="element">element to get the parent of</param>
        /// <param name="request">CacheRequest specifying information to be prefetched</param>
        /// <returns>The parent of the specified element; can be null if
        /// specified element was the root element</returns>
        /// <remarks>The view used is determined by the condition passed to
        /// the constructor - elements that do not satisfy that condition
        /// are skipped over</remarks>
        public AutomationElement GetParent(AutomationElement element, CacheRequest request)
        {
            Misc.ValidateArgumentNonNull(element, "element");
            Misc.ValidateArgumentNonNull(request, "request");
            return element.Navigate(NavigateDirection.Parent, _condition, request);
        }

        /// <summary>
        /// Get the first child of the specified element, in the current view,
        /// prefetching properties
        /// </summary>
        /// <param name="element">element to get the first child of</param>
        /// <param name="request">CacheRequest specifying information to be prefetched</param>
        /// <returns>The frst child of the specified element - or null if
        /// the specified element has no children</returns>
        /// <remarks>The view used is determined by the condition passed to
        /// the constructor - elements that do not satisfy that condition
        /// are skipped over</remarks>
        public AutomationElement GetFirstChild(AutomationElement element, CacheRequest request)
        {
            Misc.ValidateArgumentNonNull(element, "element");
            Misc.ValidateArgumentNonNull(request, "request");
            return element.Navigate(NavigateDirection.FirstChild, _condition, request);
        }

        /// <summary>
        /// Get the last child of the specified element, in the current view,
        /// prefetching properties
        /// </summary>
        /// <param name="element">element to get the last child of</param>
        /// <param name="request">CacheRequest specifying information to be prefetched</param>
        /// <returns>The last child of the specified element - or null if
        /// the specified element has no children</returns>
        /// <remarks>The view used is determined by the condition passed to
        /// the constructor - elements that do not satisfy that condition
        /// are skipped over</remarks>
        public AutomationElement GetLastChild(AutomationElement element, CacheRequest request)
        {
            Misc.ValidateArgumentNonNull(element, "element");
            Misc.ValidateArgumentNonNull(request, "request");
            return element.Navigate(NavigateDirection.LastChild, _condition, request);
        }

        /// <summary>
        /// Get the next sibling of the specified element, in the current view,
        /// prefetching properties
        /// </summary>
        /// <param name="element">element to get the next sibling of</param>
        /// <param name="request">CacheRequest specifying information to be prefetched</param>
        /// <returns>The next sibling of the specified element - or null if the
        /// specified element has no next sibling</returns>
        /// <remarks>The view used is determined by the condition passed to
        /// the constructor - elements that do not satisfy that condition
        /// are skipped over</remarks>
        public AutomationElement GetNextSibling(AutomationElement element, CacheRequest request)
        {
            Misc.ValidateArgumentNonNull(element, "element");
            Misc.ValidateArgumentNonNull(request, "request");
            return element.Navigate(NavigateDirection.NextSibling, _condition, request);
        }

        /// <summary>
        /// Get the previous sibling of the specified element, in the current view,
        /// prefetching properties
        /// </summary>
        /// <param name="element">element to get the previous sibling of</param>
        /// <param name="request">CacheRequest specifying information to be prefetched</param>
        /// <returns>The previous sibling of the specified element - or null if the
        /// specified element has no previous sibling</returns>
        /// <remarks>The view used is determined by the condition passed to
        /// the constructor - elements that do not satisfy that condition
        /// are skipped over</remarks>
        public AutomationElement GetPreviousSibling(AutomationElement element, CacheRequest request)
        {
            Misc.ValidateArgumentNonNull(element, "element");
            Misc.ValidateArgumentNonNull(request, "request");
            return element.Navigate(NavigateDirection.PreviousSibling, _condition, request);
        }

        /// <summary>
        /// Return the element or the nearest ancestor which is present in
        /// the view of the tree used by this treewalker, prefetching properties
        /// for the returned node
        /// </summary>
        /// <param name="element">element to normalize</param>
        /// <param name="request">CacheRequest specifying information to be prefetched</param>
        /// <returns>The element or the nearest ancestor which satisfies the
        /// condition used by this TreeWalker</returns>
        /// <remarks>
        /// This method starts at the specified element and walks up the
        /// tree until it finds an element that satisfies the TreeWalker's
        /// condition.
        /// 
        /// If the passed-in element itself satsifies the condition, it is
        /// returned as-is.
        /// 
        /// If the process of walking up the tree hits the root node, then
        /// the root node is returned, regardless of whether it satisfies
        /// the condition or not. 
        /// </remarks>
        public AutomationElement Normalize(AutomationElement element, CacheRequest request)
        {
            Misc.ValidateArgumentNonNull(element, "element");
            Misc.ValidateArgumentNonNull(request, "request");
            return element.Normalize(_condition, request);
        }

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        /// <summary>
        /// Returns the condition used by this TreeWalker. The TreeWalker
        /// skips over nodes that do not satisfy the condition.
        /// </summary>
        public Condition Condition
        {
            get
            {
                return _condition;
            }
        }

        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private Condition _condition;

        #endregion Private Fields
    }
}
