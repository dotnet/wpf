// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for MultipleView Pattern

using System;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    ///<summary>wrapper class for MultipleView pattern </summary>
#if (INTERNAL_COMPILE)
    internal class MultipleViewPattern: BasePattern
#else
    public class MultipleViewPattern: BasePattern
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private MultipleViewPattern(AutomationElement el, SafePatternHandle hPattern, bool cached)
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

        /// <summary>MultipleView pattern</summary>
        public static readonly AutomationPattern Pattern = MultipleViewPatternIdentifiers.Pattern;

        /// <summary>Property ID: CurrentView - The view ID corresponding to the control's current state. This ID is control-specific.</summary>
        public static readonly AutomationProperty CurrentViewProperty = MultipleViewPatternIdentifiers.CurrentViewProperty;

        /// <summary>Property ID: SupportedViews - Returns an array of ints representing the full set of views available in this control.</summary>
        public static readonly AutomationProperty SupportedViewsProperty = MultipleViewPatternIdentifiers.SupportedViewsProperty;

        #endregion Public Constants and Readonly Fields


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        /// The string view name string must be suitable for use by TTS, Braille, etc.
        /// </summary>
        /// <param name="viewId">
        /// The view ID corresponding to the control's current state. This ID is control-specific and can should
        /// be the same across instances.
        /// </param>
        /// <returns>Return a localized, human readable string in the application's current UI language.</returns>
        public string GetViewName( int viewId )
        {
            return UiaCoreApi.MultipleViewPattern_GetViewName(_hPattern, viewId);
        }
        
        /// <summary>
        /// Change the current view using an ID returned from GetSupportedViews()        
        /// </summary>
        public void SetCurrentView( int viewId )
        {
            UiaCoreApi.MultipleViewPattern_SetCurrentView(_hPattern, viewId);
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
        public MultipleViewPatternInformation Cached
        {
            get
            {
                Misc.ValidateCached(_cached);
                return new MultipleViewPatternInformation(_el, true);
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
        public MultipleViewPatternInformation Current
        {
            get
            {
                Misc.ValidateCurrent(_hPattern);
                return new MultipleViewPatternInformation(_el, false);
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
            return new MultipleViewPattern(el, hPattern, cached);
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
        public struct MultipleViewPatternInformation
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            internal MultipleViewPatternInformation(AutomationElement el, bool useCache)
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

            /// <summary>The view ID corresponding to the control's current state. This ID is control-specific</summary>
            public int CurrentView
            {
                get
                {
                    return (int)_el.GetPatternPropertyValue(CurrentViewProperty, _useCache);
                }
            }

            /// <summary>Returns an array of ints representing the full set of views available in this control.</summary>
            public int [] GetSupportedViews()
            {
                return (int [])_el.GetPatternPropertyValue(SupportedViewsProperty, _useCache);
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
