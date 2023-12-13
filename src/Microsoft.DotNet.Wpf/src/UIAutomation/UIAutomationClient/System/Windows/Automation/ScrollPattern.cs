// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for Scroll Pattern

using System;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Represents UI elements that are expressing a value
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class ScrollPattern: BasePattern
#else
    public class ScrollPattern: BasePattern
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private ScrollPattern(AutomationElement el, SafePatternHandle hPattern, bool cached)
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

        /// <summary>Value used by SetSCrollPercent to indicate that no scrolling should take place in the specified direction</summary>
        public const double NoScroll = -1.0;
        
        /// <summary>Scroll pattern</summary>
        public static readonly AutomationPattern Pattern = ScrollPatternIdentifiers.Pattern;

        /// <summary>Property ID: HorizontalScrollPercent - Current horizontal scroll position</summary>
        public static readonly AutomationProperty HorizontalScrollPercentProperty = ScrollPatternIdentifiers.HorizontalScrollPercentProperty;
        
        /// <summary>Property ID: HorizontalViewSize - Minimum possible horizontal scroll position</summary>
        public static readonly AutomationProperty HorizontalViewSizeProperty = ScrollPatternIdentifiers.HorizontalViewSizeProperty;
        
        /// <summary>Property ID: VerticalScrollPercent - Current vertical scroll position</summary>
        public static readonly AutomationProperty VerticalScrollPercentProperty = ScrollPatternIdentifiers.VerticalScrollPercentProperty;
        
        /// <summary>Property ID: VerticalViewSize </summary>
        public static readonly AutomationProperty VerticalViewSizeProperty = ScrollPatternIdentifiers.VerticalViewSizeProperty;
        
        /// <summary>Property ID: HorizontallyScrollable</summary>
        public static readonly AutomationProperty HorizontallyScrollableProperty = ScrollPatternIdentifiers.HorizontallyScrollableProperty;
        
        /// <summary>Property ID: VerticallyScrollable</summary>
        public static readonly AutomationProperty VerticallyScrollableProperty = ScrollPatternIdentifiers.VerticallyScrollableProperty;

        #endregion Public Constants and Readonly Fields


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary> Request to set the current horizontal and Vertical scroll position 
        /// by percent (0-100).  Passing in the value of "-1" will indicate that 
        /// scrolling in that direction should be ignored.
        /// The ability to call this method and simultaneously scroll horizontally and 
        /// vertically provides simple panning support.</summary>
        /// <param name="horizontalPercent">Amount to scroll by horizontally</param>
        /// <param name="verticalPercent">Amount to scroll by vertically </param>
        public void SetScrollPercent( double horizontalPercent, double verticalPercent )
        {
            UiaCoreApi.ScrollPattern_SetScrollPercent(_hPattern, horizontalPercent, verticalPercent);
        }

        /// <summary> Request to scroll horizontally and vertically by the specified amount.
        /// The ability to call this method and simultaneously scroll horizontally 
        /// and vertically provides simple panning support.  If only horizontal or vertical percent
        /// needs to be changed the constant SetScrollPercentUnchanged can be used for 
        /// either parameter and that axis wil be unchanged.</summary>
        ///
        /// <param name="horizontalAmount">amount to scroll by horizontally</param>
        /// <param name="verticalAmount">amount to scroll by vertically </param>
        public void Scroll( ScrollAmount horizontalAmount, ScrollAmount verticalAmount )
        {
            UiaCoreApi.ScrollPattern_Scroll(_hPattern, horizontalAmount, verticalAmount);
        }

        /// <summary>
        /// Request to scroll horizontally by the specified amount
        /// </summary>
        /// <param name="amount">Amount to scroll by</param>
        public void ScrollHorizontal( ScrollAmount amount )
        {
            UiaCoreApi.ScrollPattern_Scroll(_hPattern, amount, ScrollAmount.NoAmount);
        }

        /// <summary>
        /// Request to scroll vertically by the specified amount
        /// </summary>
        /// <param name="amount">Amount to scroll by</param>
        public void ScrollVertical( ScrollAmount amount )
        {
            UiaCoreApi.ScrollPattern_Scroll(_hPattern, ScrollAmount.NoAmount, amount);
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
        public ScrollPatternInformation Cached
        {
            get
            {
                Misc.ValidateCached(_cached);
                return new ScrollPatternInformation(_el, true);
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
        public ScrollPatternInformation Current
        {
            get
            {
                Misc.ValidateCurrent(_hPattern);
                return new ScrollPatternInformation(_el, false);
            }
        }

        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        static internal object Wrap(AutomationElement el, SafePatternHandle hPattern, bool cached)
        {
            return new ScrollPattern(el, hPattern, cached);
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        SafePatternHandle _hPattern;
        bool _cached;

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
        public struct ScrollPatternInformation
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal ScrollPatternInformation(AutomationElement el, bool useCache)
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
            /// Get the current horizontal scroll position
            /// </summary>
            public double HorizontalScrollPercent
            {
                get
                {
                    return (double)_el.GetPatternPropertyValue(HorizontalScrollPercentProperty, _useCache);
                }
            }

            /// <summary>
            /// Get the current vertical scroll position
            /// </summary>
            public double VerticalScrollPercent
            {
                get
                {
                    return (double)_el.GetPatternPropertyValue(VerticalScrollPercentProperty, _useCache);
                }
            }

            /// <summary>
            /// Equal to the horizontal percentage of the entire control that is currently viewable.
            /// </summary>
            public double HorizontalViewSize
            {
                get
                {
                    return (double)_el.GetPatternPropertyValue(HorizontalViewSizeProperty, _useCache);
                }
            }

            /// <summary>
            /// Equal to the horizontal percentage of the entire control that is currently viewable.
            /// </summary>
            public double VerticalViewSize
            {
                get
                {
                    return (double)_el.GetPatternPropertyValue(VerticalViewSizeProperty, _useCache);
                }
            }

            /// <summary>
            /// True if control can scroll horizontally
            /// </summary>
            public bool HorizontallyScrollable
            {
                get
                {
                    return (bool)_el.GetPatternPropertyValue(HorizontallyScrollableProperty, _useCache);
                }
            }

            /// <summary>
            /// True if control can scroll vertically
            /// </summary>
            public bool VerticallyScrollable
            {
                get
                {
                    return (bool)_el.GetPatternPropertyValue(VerticallyScrollableProperty, _useCache);
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
