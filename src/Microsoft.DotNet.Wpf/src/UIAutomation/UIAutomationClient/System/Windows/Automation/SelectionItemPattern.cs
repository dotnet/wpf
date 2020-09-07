// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for SelectionItem Pattern

using System;
using System.Collections;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Class representing containers that manage selection.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class SelectionItemPattern: BasePattern
#else
    public class SelectionItemPattern: BasePattern
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private SelectionItemPattern(AutomationElement el, SafePatternHandle hPattern, bool cached)
            : base(el, hPattern)
        {
            _hPattern = hPattern;
            _cached = cached;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>SelectionItem pattern</summary>
        public static readonly AutomationPattern Pattern = SelectionItemPatternIdentifiers.Pattern;

        /// <summary>Indicates the element is currently selected.</summary>
        public static readonly AutomationProperty IsSelectedProperty = SelectionItemPatternIdentifiers.IsSelectedProperty;
        /// <summary>Indicates the element is currently selected.</summary>
        public static readonly AutomationProperty SelectionContainerProperty = SelectionItemPatternIdentifiers.SelectionContainerProperty;

        /// <summary>
        /// Event ID: ElementAddedToSelection - indicates an element was added to the selection.
        /// sourceElement  refers to the element that was added to the selection.
        /// </summary>
        public static readonly AutomationEvent ElementAddedToSelectionEvent = SelectionItemPatternIdentifiers.ElementAddedToSelectionEvent;
        /// <summary>
        /// Event ID: ElementRemovedFromSelection - indicates an element was removed from the selection.
        /// sourceElement refers to the element that was removed from the selection.
        /// </summary>
        public static readonly AutomationEvent ElementRemovedFromSelectionEvent = SelectionItemPatternIdentifiers.ElementRemovedFromSelectionEvent;
        /// <summary>
        /// Event ID: ElementSelected - indicates an element was selected in a selection container, deselecting
        /// any previously selected elements in that container.
        /// sourceElement refers to the selected element
        /// </summary>
        public static readonly AutomationEvent ElementSelectedEvent = SelectionItemPatternIdentifiers.ElementSelectedEvent;

        #endregion Public Constants and Readonly Fields


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        /// Sets the current element as the selection
        /// This clears the selection from other elements in the container 
        /// </summary>
        public void Select()
        {
            UiaCoreApi.SelectionItemPattern_Select(_hPattern);
        }
        /// <summary>
        /// Adds current element to selection
        /// </summary>
        public void AddToSelection()
        {
            UiaCoreApi.SelectionItemPattern_AddToSelection(_hPattern);
        }
        
        /// <summary>
        /// Removes current element from selection
        /// </summary>
        public void RemoveFromSelection()
        {
            UiaCoreApi.SelectionItemPattern_RemoveFromSelection(_hPattern);
        }

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties
        /// <summary>
        /// This member allows access to previously requested
        /// cached properties for this element. The returned object
        /// has accessors for each property defined for this pattern.
        /// </summary>
        /// <remarks>
        /// Cached property values must have been previously requested
        /// using a CacheRequest. If you try to access a cached
        /// property that was not previously requested, an InvalidOperation
        /// Exception will be thrown.
        /// 
        /// To get the value of a property at the current point in time,
        /// access the property via the Current accessor instead of
        /// Cached.
        /// </remarks>
        public SelectionItemPatternInformation Cached
        {
            get
            {
                Misc.ValidateCached(_cached);
                return new SelectionItemPatternInformation(_el, true);
            }
        }

        /// <summary>
        /// This member allows access to current property values
        /// for this element. The returned object has accessors for
        /// each property defined for this pattern.
        /// </summary>
        /// <remarks>
        /// This pattern must be from an AutomationElement with a
        /// Full reference in order to get current values. If the
        /// AutomationElement was obtained using AutomationElementMode.None,
        /// then it contains only cached data, and attempting to get
        /// the current value of any property will throw an InvalidOperationException.
        /// 
        /// To get the cached value of a property that was previously
        /// specified using a CacheRequest, access the property via the
        /// Cached accessor instead of Current.
        /// </remarks>
        public SelectionItemPatternInformation Current
        {
            get
            {
                Misc.ValidateCurrent(_hPattern);
                return new SelectionItemPatternInformation(_el, false);
            }
        }


        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static object Wrap(AutomationElement el, SafePatternHandle hPattern, bool cached)
        {
            return new SelectionItemPattern(el, hPattern, cached);
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private SafePatternHandle _hPattern;
        private bool _cached;

        #endregion Private Fields


        //------------------------------------------------------
        //
        //  Nested Classes
        //
        //------------------------------------------------------

        #region Nested Classes

        /// <summary>
        /// This class provides access to either Cached or Current
        /// properties on a pattern via the pattern's .Cached or
        /// .Current accessors.
        /// </summary>
        public struct SelectionItemPatternInformation
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal SelectionItemPatternInformation(AutomationElement el, bool useCache)
            {
                _el = el;
                _useCache = useCache;
            }

            #endregion Constructors


            //------------------------------------------------------
            //
            //  Public Properties
            //
            //------------------------------------------------------
 
            #region Public Properties

            /// <summary>
            /// Check whether an element is selected
            /// </summary>
            /// <returns>returns true if the element is selected</returns>
            public bool IsSelected
            {
                get
                {
                    return (bool)_el.GetPatternPropertyValue(IsSelectedProperty, _useCache);
                }
            }

            /// <summary>
            /// The logical element that supports the SelectionPattern for this Item
            /// </summary>
            /// <returns>returns an AutomationElement</returns>
            public AutomationElement SelectionContainer
            {
                get
                {
                    return (AutomationElement)_el.GetPatternPropertyValue(SelectionContainerProperty, _useCache);
                }
            }

            #endregion Public Properties

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            private AutomationElement _el; // AutomationElement that contains the cache or live reference
            private bool _useCache; // true to use cache, false to use live reference to get current values

            #endregion Private Fields
        }
        #endregion Nested Classes
    }
}
