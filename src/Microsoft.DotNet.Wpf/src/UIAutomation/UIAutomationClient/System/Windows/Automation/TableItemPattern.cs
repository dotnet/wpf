// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for TableItem Pattern


using System;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Used to expose grid items with header information.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class TableItemPattern: GridItemPattern
#else
    public class TableItemPattern: GridItemPattern
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private TableItemPattern(AutomationElement el, SafePatternHandle hPattern, bool cached)
            : base(el, hPattern, cached)
        {
            _hPattern = hPattern;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>TableItem pattern</summary>
        public static readonly new AutomationPattern Pattern = TableItemPatternIdentifiers.Pattern;

        /// <summary>Property ID: RowHeaderItems - Collection of all row headers for this cell</summary>
        public static readonly AutomationProperty RowHeaderItemsProperty = TableItemPatternIdentifiers.RowHeaderItemsProperty;

        /// <summary>Property ID: ColumnHeaderItems - Collection of all column headers for this cell</summary>
        public static readonly AutomationProperty ColumnHeaderItemsProperty = TableItemPatternIdentifiers.ColumnHeaderItemsProperty;

        #endregion Public Constants and Readonly Fields


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods


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
        new public TableItemPatternInformation Cached
        {
            get
            {
                Misc.ValidateCached(_cached);
                return new TableItemPatternInformation(_el, true);
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
        new public TableItemPatternInformation Current
        {
            get
            {
                Misc.ValidateCurrent(_hPattern);
                return new TableItemPatternInformation(_el, false);
            }
        }

        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static new object Wrap(AutomationElement el, SafePatternHandle hPattern, bool cached)
        {
            return new TableItemPattern(el, hPattern, cached);
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private SafePatternHandle _hPattern;

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
        public struct TableItemPatternInformation
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal TableItemPatternInformation(AutomationElement el, bool useCache)
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
            /// the row number of the element.  This is zero based.
            /// </summary>
            public int Row
            {
                get
                {
                    return (int)_el.GetPatternPropertyValue(RowProperty, _useCache);
                }
            }

            /// <summary>
            /// the column number of the element.  This is zero based.
            /// </summary>
            public int Column
            {
                get
                {
                    return (int)_el.GetPatternPropertyValue(ColumnProperty, _useCache);
                }
            }

            /// <summary>
            /// count of how many rows the element spans
            /// -- non merged cells should always return 1
            /// </summary>
            public int RowSpan
            {
                get
                {
                    return (int)_el.GetPatternPropertyValue(RowSpanProperty, _useCache);
                }
            }

            /// <summary>
            /// count of how many columns the element spans
            /// -- non merged cells should always return 1
            ///</summary>
            public int ColumnSpan
            {
                get
                {
                    return (int)_el.GetPatternPropertyValue(ColumnSpanProperty, _useCache);
                }
            }

            /// <summary>
            /// The logical element that supports the GripPattern for this Item
            ///</summary>
            public AutomationElement ContainingGrid
            {
                get
                {
                    return (AutomationElement)_el.GetPatternPropertyValue(ContainingGridProperty, _useCache);
                }
            }

            /// <summary>Collection of all row headers for this cell</summary>
            public AutomationElement[] GetRowHeaderItems()
            {
                return (AutomationElement[])_el.GetPatternPropertyValue(RowHeaderItemsProperty, _useCache);
            }

            /// <summary>Collection of all column headers for this cell</summary>
            public AutomationElement[] GetColumnHeaderItems()
            {
                return (AutomationElement[])_el.GetPatternPropertyValue(ColumnHeaderItemsProperty, _useCache);
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
