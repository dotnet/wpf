// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for Grid Pattern

using System;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    ///<summary></summary>
#if (INTERNAL_COMPILE)
    internal class GridPattern: BasePattern
#else
    public class GridPattern: BasePattern
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        internal GridPattern(AutomationElement el, SafePatternHandle hPattern, bool cached)
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

        /// <summary>Grid pattern</summary>
        public static readonly AutomationPattern Pattern = GridPatternIdentifiers.Pattern;

        /// <summary>RowCount</summary>
        public static readonly AutomationProperty RowCountProperty = GridPatternIdentifiers.RowCountProperty;

        /// <summary>ColumnCount</summary>
        public static readonly AutomationProperty ColumnCountProperty = GridPatternIdentifiers.ColumnCountProperty;

        #endregion Public Constants and Readonly Fields


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods
        /// <summary>
        /// Obtain the AutomationElement at an zero based absolute position in the grid.  
        /// Where 0,0 is top left
        /// </summary>
        /// <param name="row">Row of item to get</param>
        /// <param name="column">Column of item to get</param>
        public AutomationElement GetItem(int row, int column) 
        {
            SafeNodeHandle hNode = UiaCoreApi.GridPattern_GetItem(_hPattern, row, column);
            return AutomationElement.Wrap(hNode);
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
        public GridPatternInformation Cached
        {
            get
            {
                Misc.ValidateCached(_cached);
                return new GridPatternInformation(_el, true);
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
        public GridPatternInformation Current
        {
            get
            {
                Misc.ValidateCurrent(_hPattern);
                return new GridPatternInformation(_el, false);
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
            return new GridPattern( el, hPattern, cached );
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        internal SafePatternHandle _hPattern;
        internal bool _cached; // internal, since protected makes this publically visible

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
        public struct GridPatternInformation
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal GridPatternInformation(AutomationElement el, bool useCache)
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
