// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for Selection Pattern

using System;
using System.Collections;
using System.Windows.Automation.Provider;
using System.ComponentModel;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Class representing containers that manage selection.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class SelectionPattern: BasePattern
#else
    public class SelectionPattern: BasePattern
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private SelectionPattern(AutomationElement el, SafePatternHandle hPattern, bool cached)
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

        /// <summary>Selection pattern</summary>
        public static readonly AutomationPattern Pattern = SelectionPatternIdentifiers.Pattern;

        /// <summary>Get the currently selected elements</summary>
        public static readonly AutomationProperty SelectionProperty = SelectionPatternIdentifiers.SelectionProperty;

        /// <summary>Indicates whether the control allows more than one element to be selected</summary>
        public static readonly AutomationProperty CanSelectMultipleProperty = SelectionPatternIdentifiers.CanSelectMultipleProperty;

        /// <summary>Indicates whether the control requires at least one element to be selected</summary>
        public static readonly AutomationProperty IsSelectionRequiredProperty = SelectionPatternIdentifiers.IsSelectionRequiredProperty;

        /// <summary>
        /// Event ID: SelectionInvalidated - indicates that selection changed in a selection conainer.
        /// sourceElement refers to the selection container
        /// </summary>
        public static readonly AutomationEvent InvalidatedEvent = SelectionPatternIdentifiers.InvalidatedEvent;

        #endregion Public Constants and Readonly Fields


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        // No public methods

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
        public SelectionPatternInformation Cached
        {
            get
            {
                Misc.ValidateCached(_cached);
                return new SelectionPatternInformation(_el, true);
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
        public SelectionPatternInformation Current
        {
            get
            {
                Misc.ValidateCurrent(_hPattern);
                return new SelectionPatternInformation(_el, false);
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
            return new SelectionPattern(el, hPattern, cached);
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
        public struct SelectionPatternInformation
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal SelectionPatternInformation(AutomationElement el, bool useCache)
            {
                _el = el;
                _useCache = useCache;
            }

            #endregion Constructors


            //------------------------------------------------------
            //
            //  Public Methods
            //
            //------------------------------------------------------

            #region Public Methods

            /// <summary>
            /// Get the currently selected elements
            /// </summary>
            /// <returns>An AutomationElement array containing the currently selected elements</returns>
            public AutomationElement[] GetSelection()
            {
                return (AutomationElement[])_el.GetPatternPropertyValue(SelectionProperty, _useCache);
            }

            #endregion Public Methods

            //------------------------------------------------------
            //
            //  Public Properties
            //
            //------------------------------------------------------

            #region Public Properties

            /// <summary>
            /// Indicates whether the control allows more than one element to be selected
            /// </summary>
            /// <returns>Boolean indicating whether the control allows more than one element to be selected</returns>
            /// <remarks>If this is false, then the control is a single-select ccntrol</remarks>
            public bool CanSelectMultiple
            {
                get
                {
                    return (bool)_el.GetPatternPropertyValue(CanSelectMultipleProperty, _useCache);
                }
            }

            /// <summary>
            /// Indicates whether the control requires at least one element to be selected
            /// </summary>
            /// <returns>Boolean indicating whether the control requires at least one element to be selected</returns>
            /// <remarks>If this is false, then the control allows all elements to be unselected</remarks>
            public bool IsSelectionRequired
            {
                get
                {
                    return (bool)_el.GetPatternPropertyValue(IsSelectionRequiredProperty, _useCache);
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
