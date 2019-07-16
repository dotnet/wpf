// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for Table Pattern

using System;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Identifies a grid that has header information.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class TablePattern: GridPattern
#else
    public class TablePattern: GridPattern
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private TablePattern(AutomationElement el, SafePatternHandle hPattern, bool cached)
            : base(el, hPattern, cached)
        {
            // Done
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Table pattern</summary>
        public static readonly new AutomationPattern Pattern = TablePatternIdentifiers.Pattern;

        /// <summary>Property ID: RowHeaders - Collection of all row headers for this table</summary>
        public static readonly AutomationProperty RowHeadersProperty = TablePatternIdentifiers.RowHeadersProperty;

        /// <summary>Property ID: ColumnHeaders - Collection of all column headers for this table</summary>
        public static readonly AutomationProperty ColumnHeadersProperty = TablePatternIdentifiers.ColumnHeadersProperty;

        /// <summary>Property ID: RowOrColumnMajor - Indicates if the data is best presented by row or column</summary>
        public static readonly AutomationProperty RowOrColumnMajorProperty = TablePatternIdentifiers.RowOrColumnMajorProperty;

        #endregion Public Constants and Readonly Fields


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
        new public TablePatternInformation Cached
        {
            get
            {
                Misc.ValidateCached(_cached);
                return new TablePatternInformation(_el, true);
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
        new public TablePatternInformation Current
        {
            get
            {
                Misc.ValidateCurrent(_hPattern);
                return new TablePatternInformation(_el, false);
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
            return new TablePattern(el, hPattern, cached);
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        // No new fields needed here - we use those inherited from base class

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
        public struct TablePatternInformation
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal TablePatternInformation(AutomationElement el, bool useCache)
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

            /// <summary>Collection of all row headers for this table</summary>
            public AutomationElement[] GetRowHeaders()
            {
                return (AutomationElement[])_el.GetPatternPropertyValue(RowHeadersProperty, _useCache);
            }

            /// <summary>Collection of all column headers for this table</summary>
            public AutomationElement[] GetColumnHeaders()
            {
                return (AutomationElement[])_el.GetPatternPropertyValue(ColumnHeadersProperty, _useCache);
            }

            /// <summary>
            /// number of rows in the grid
            /// </summary>
            public int RowCount
            {
                get
                {
                    return (int)_el.GetPatternPropertyValue(RowCountProperty, _useCache);
                }
            }

            /// <summary>
            /// number of columns in the grid
            /// </summary>
            public int ColumnCount
            {
                get
                {
                    return (int)_el.GetPatternPropertyValue(ColumnCountProperty, _useCache);
                }
            }

            /// <summary>Indicates if the data is best presented by row or column</summary>
            public RowOrColumnMajor RowOrColumnMajor
            {
                get
                {
                    return (RowOrColumnMajor)_el.GetPatternPropertyValue(RowOrColumnMajorProperty, _useCache);
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
